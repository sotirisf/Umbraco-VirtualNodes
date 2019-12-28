using DotSee.VirtualNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Routing;

/// <summary>
/// ContentFinder for VirtualNodesUrlProvider
/// </summary>
public class VirtualNodesContentFinder : IContentFinder
{
    public bool TryFindContent(PublishedContentRequest contentRequest)
    {

        //Get a cached dictionary of urls and node ids
        var cachedVirtualNodeUrls = ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem<Dictionary<string, int>>("cachedVirtualNodes");

        //Get the request path

        string path;
        if (contentRequest.HasDomain)
            path = contentRequest.UmbracoDomain.DomainName + DomainHelper.PathRelativeToDomain(contentRequest.DomainUri, contentRequest.Uri.GetAbsolutePathDecoded());
        else
            path = contentRequest.Uri.GetAbsolutePathDecoded();        

        //If found in the cached dictionary, get the node id from there
        if (cachedVirtualNodeUrls != null && cachedVirtualNodeUrls.ContainsKey(path)) {
            int nodeId = cachedVirtualNodeUrls[path];
            contentRequest.PublishedContent = new UmbracoHelper(UmbracoContext.Current).TypedContent(nodeId);
            return true;
        }

        //If not found on the cached dictionary, traverse nodes and find the node that corresponds to the URL                        

        var virtualNodes = GetAllVirtualNodes(contentRequest);
        IPublishedContent item = null;
        foreach (var virtualNode in virtualNodes)
        {
            item = FindDescendants(virtualNode, path, contentRequest.HasDomain);
            if (item != null)
            {
                break;
            }
        }                   

        //If item is found, return it after adding it to the cache so we don't have to go through the same process again.
        if (cachedVirtualNodeUrls == null) { cachedVirtualNodeUrls = new Dictionary<string, int>(); }

        //If we have found a node that corresponds to the URL given
        if (item != null)
        {
            //This check is redundant, but better to be on the safe side.
            if (!cachedVirtualNodeUrls.ContainsKey(path))
            {
                //Add the new path and id to the dictionary so that we don't have to go through the tree again next time.
                cachedVirtualNodeUrls.Add(path, item.Id);
            }

            //Update cache
            ApplicationContext.Current.ApplicationCache.RuntimeCache.InsertCacheItem<Dictionary<string, int>>(
                    "cachedVirtualNodes",
                    () => cachedVirtualNodeUrls, 
                    null, 
                    false, 
                    System.Web.Caching.CacheItemPriority.High);
            
            //That's all folks
            contentRequest.PublishedContent = item;
            return true;
        }

        //Abandon all hope ye who enter here. This means that we didn't find a node so we return false to let
        //the next ContentFinder (if any) take over.
        return false;
    }
    
    /// <summary>
    /// find this and it's children to match url
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public IPublishedContent FindDescendants(IPublishedContent parent, string url, bool hasDomain)
    {
        if (parent.IsNotPageNode())
            return null;

        if (IsMatch(url, parent, hasDomain))    
        {
            return parent;
        }
        foreach (var child in parent.Children)
        {
            if (child.IsNotPageNode())
                continue;
            /*we suppose that don't have virtualFolders inside (is children) of an other virtualFolder*/
            //if (!Helpers.IsVirtualNode(child))
            //{
            if (IsMatch(url, child, hasDomain))
            {
                return child;
            }
            else
            {
                foreach (var childLv2 in child.Children)
                {
                    var result = FindDescendants(childLv2, url, hasDomain);
                    if (result != null)
                        return result;
                }
            }
            //}
            /*don't have to look deeper because this included in the loop through all virtual nodes above*/
            //else
            //{                      
            //foreach (var childLv2 in child.Children)
            //{
            //    var result = FindDescendants(childLv2, url);
            //    if (result != null)
            //        return result;
            //}
            //}
        }
        return null;
    }
    
    /// <summary>
    /// get all VirtualFolder nodes, only check at level 2, for Interlink only, to fit the peformance
    /// </summary>
    /// <param name="contentRequest"></param>
    /// <returns></returns>
    public List<IPublishedContent> GetAllVirtualNodes(PublishedContentRequest contentRequest)
    {
        List<int> allVirtualNodes = ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem<List<int>>("AllVirtualNodes");
        if(allVirtualNodes == null)
        {
            allVirtualNodes = new List<int>();
            foreach (var root in contentRequest.RoutingContext.UmbracoContext.ContentCache.GetAtRoot())
            {
                if (Helpers.IsVirtualNode(root))
                {
                    allVirtualNodes.Add(root.Id);
                }
                foreach(var item in root.Children) {
                    if (Helpers.IsVirtualNode(item))
                    {
                        allVirtualNodes.Add(item.Id);
                    }
                }
            }            
            //Update cache
            ApplicationContext.Current.ApplicationCache.RuntimeCache.InsertCacheItem<List<int>>(
                    "AllVirtualNodes",
                    () => allVirtualNodes,
                    null,
                    false,
                    System.Web.Caching.CacheItemPriority.High);
        }
        return new UmbracoHelper(UmbracoContext.Current).TypedContent(allVirtualNodes).ToList();
    }

    public bool IsMatch(string requestPath, IPublishedContent node, bool hasDomain)
    {
        if (hasDomain)
        {
            var nodeUrl = node.UrlWithDomain();
            var httpIndex = nodeUrl.IndexOf("://");
            if (httpIndex != -1)
            {
                nodeUrl = nodeUrl.Substring(httpIndex + 3);
                return nodeUrl == (requestPath + "/") || nodeUrl == requestPath;
            }
        }
        return node.Url == (requestPath + "/") || node.Url == requestPath;
    }
}

