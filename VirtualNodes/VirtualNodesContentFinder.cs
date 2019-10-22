using DotSee.VirtualNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Cache;
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
        string path = contentRequest.Uri.AbsolutePath;
        path = path.Length != 1 ? path.TrimEnd('/') : path;//except homepage: url = /        

        //If found in the cached dictionary, get the node id from there
        if (cachedVirtualNodeUrls != null && cachedVirtualNodeUrls.ContainsKey(path)) {
            int nodeId = cachedVirtualNodeUrls[path];
            contentRequest.PublishedContent = new UmbracoHelper(UmbracoContext.Current).TypedContent(nodeId);
            return true;
        }

        //If not found on the cached dictionary, traverse nodes and find the node that corresponds to the URL        

        var rootNodes = contentRequest.RoutingContext.UmbracoContext.ContentCache.GetAtRoot();        
        IPublishedContent item = null;
        if (path == "/")
        {
            item = rootNodes.FirstOrDefault(e => e.Url == path);
        }
        else
        {
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 1)
            {
                item = rootNodes.FirstOrDefault(e => e.Url == (path + "/") || e.Url == path);
                if (item == null)
                {
                    var homePage = rootNodes.First(e => e.Url == "/");
                    foreach(var child in homePage.Children)
                    {
                        item = FindDescendants(child, path);     
                        if(item != null)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var child in rootNodes)
                {
                    item = FindDescendants(child, path);
                    if (item != null)
                    {
                        break;
                    }
                }
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
    public IPublishedContent FindDescendants(IPublishedContent parent, string url)
    {
        if (parent.IsNotPageNode())
            return null;

        if (parent.Url == (url + "/") || parent.Url == url)
        {
            return parent;
        }        
        foreach (var child in parent.Children)
        {
            if (child.IsNotPageNode())
                continue;
            if (!Helpers.IsVirtualNode(child))
            {
                if (child.Url == (url + "/") || child.Url == url)
                {
                    return child;                    
                }
                else
                {
                    foreach (var childLv2 in child.Children)
                    {
                        var result = FindDescendants(childLv2, url);
                        if (result != null)
                            return result;
                    }
                }
            }
            else
            {                
                foreach (var childLv2 in child.Children)
                {
                    var result = FindDescendants(childLv2, url);
                    if (result != null)
                        return result;
                }
            }
        }
        return null;
    }
}

