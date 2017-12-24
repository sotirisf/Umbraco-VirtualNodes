using System;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace DotSee.VirtualNodes
{
    /// <summary>
    /// Handles events related to virtual nodes
    /// </summary>
    public class VirtualNodesEventHandler : ApplicationEventHandler
    {
        /// <summary>
        /// Registers events as well as the UrlProvider and ContentFinder for virtual nodes.
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Register provider
            UrlProviderResolver.Current.InsertTypeBefore<DefaultUrlProvider, VirtualNodesUrlProvider>();
            ContentFinderResolver.Current.InsertTypeBefore<ContentFinderByNotFoundHandlers, VirtualNodesContentFinder>();

            base.ApplicationStarting(umbracoApplication, applicationContext);
            ContentService.Saving+= ContentServiceSaving;
            ContentService.Published += ContentServicePublished;
        }

        private void ContentServicePublished(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            //Clear the content finder cache.
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem("cachedVirtualNodes");
        }

        private void ContentServiceSaving(IContentService sender, SaveEventArgs<IContent> args)
        {

            ///Go through nodes being published          
            foreach (IContent node in args.SavedEntities)
            {
                
                //Name of node hasn't changed, so don't do anything.
                if (node.HasIdentity && !node.IsPropertyDirty("Name")) { continue; }

                IPublishedContent parent;

                try
                {
                    //If there is no parent, exit
                    if (node.ParentId == 0 || (!node.IsNewEntity() && node.Level == 1) || (node.HasIdentity && node.Level == 0)) { continue; }

                    //Switch to IPublishedContent to go faster
                    parent = new UmbracoHelper(UmbracoContext.Current).TypedContent(node.Parent().Id);

                    //If parent is home (redundant) and parent is not a virtual node, exit current iteration
                    if (parent == null || parent.Level < 2 || !parent.IsVirtualNode()) { continue; }

                }
                catch (Exception ex) { continue; }

                //Start the counter. This will count the nodes with the same name (taking numbering under consideration) 
                //that will be found under all the node's parent siblings that are virtual nodes.

                int nodesFound = 0;
                int maxNumber = 0;

                foreach (IPublishedContent farSibling in parent.Siblings())
                {

                    //Don't take other nodes under considerations - only virtual nodes
                    //I know the name "farSibling" is not that pretty, couldn't think of anything else though.
                    if (!farSibling.IsVirtualNode()) { continue; }

                    //For each sibling of the node's parent, get all children and check names
                    foreach (IPublishedContent potentialDuplicate in farSibling.Children())
                    {

                        string p = potentialDuplicate.Name.ToLower();
                        string y = node.Name.ToLower();

                        //Don't take the node itself under consideration - only other nodes.
                        if (potentialDuplicate.Id == node.Id) { continue; }

                        //If we find a node that already has the same name, increase counter by 1.
                        if (p.Equals(y))
                        {
                            nodesFound++;
                        }
                        //If we find a node with the same name and numbering immediately after, increase counter by 1.
                        //Maxnumber will be the max number we found in node numbering, even if there are deleted node numbers in between.
                        //For example, if we have "aaa (1)" and "aaa(5)" only, maxNumber will be 5.
                        else if (Helpers.MatchDuplicateName(p, y))
                        {
                            nodesFound++;
                            maxNumber = Helpers.GetMaxNodeNameNumbering(p, y, maxNumber);
                        }
                    }
                }

                //Change the node's name to the appropriate number if duplicates were found.
                //The number of nodes found will be the actual node number since we'll already have a node with 
                //no numbering. Meaning that if there is "aaa", "aaa (1)" and "aaa (2)" then 
                //our new node (initially named "aaa") will be renamed to "aaa (3)" - that is 3 nodes found.
                if (nodesFound > 0)
                {
                    node.Name += " (" + (maxNumber + 1).ToString() + ")";
                }
            }
        }

    }
}