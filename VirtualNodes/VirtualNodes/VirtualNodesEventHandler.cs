using Umbraco.Core;
using Umbraco.Web.Routing;
using Umbraco.Core.Services;
using Umbraco.Core.Publishing;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using System.Web;
using Umbraco.Web;

namespace DotSee.VirtualNodes
{
    public class VirtualNodesEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Register provider
            UrlProviderResolver.Current.InsertTypeBefore<DefaultUrlProvider, VirtualNodesUrlProvider>();
            ContentFinderResolver.Current.InsertTypeBefore<ContentFinderByNotFoundHandlers, VirtualNodesContentFinder>();

            base.ApplicationStarting(umbracoApplication, applicationContext);
            ContentService.Publishing += ContentServicePublishing;
            ContentService.Published += ContentServicePublished;
        }

        private void ContentServicePublished(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            //Clear the content finder cache.
            HttpContext.Current.Cache.Remove("cachedVirtualNodes");
        }

        private void ContentServicePublishing(IPublishingStrategy sender, PublishEventArgs<IContent> args)

        {
            //UmbracoHelper h = new UmbracoHelper(UmbracoContext.Current);
            foreach (IContent node in args.PublishedEntities)
            {
                //If there is no parent, exit
                if (!node.IsNewEntity() && node.Level==1) { continue; }


                //Switch to IPublishedContent to go faster
                IPublishedContent parent = new Umbraco.Web.UmbracoHelper(UmbracoContext.Current).TypedContent(node.Parent().Id);

                //If parent is home (redundant) and parent is not a virtual node, exit
                if (parent.Level < 2 || !parent.ContentType.Alias.ToLower().StartsWith("virtualnode"))
                {
                    continue;
                }

                //Start the counter. This will cound the nodes with the same name (taking numbering under consideration) 
                //that will be found under all the node's parent siblings that are virtual nodes.

                int nodesFound = 0;
                int maxNumber = 0;

                foreach (IPublishedContent farSibling in parent.Siblings()) {
                
                    //Don't take other nodes under considerations - only virtual nodes
                    if (!farSibling.ContentType.Alias.ToLower().StartsWith("virtualnode")) { continue; }

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
                if (nodesFound > 0) {
                    node.Name += " (" + (maxNumber+1).ToString() + ")";

                }


            }
        }
    }


}