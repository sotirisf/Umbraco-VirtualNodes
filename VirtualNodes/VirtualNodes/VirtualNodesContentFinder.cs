using DotSee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Routing;

/// <summary>
/// ContentFinder for OmitUrlSegmentsUrlProvider
/// </summary>
public class VirtualNodesContentFinder : IContentFinder
{
    public bool TryFindContent(PublishedContentRequest contentRequest)
    {
        //Get a cached dictionary of urls and node ids
        var cachedVirtualNodeUrls = (Dictionary<string, int>)HttpContext.Current.Cache["cachedVirtualNodes"];

        //Get the request path
        string path = contentRequest.Uri.AbsolutePath;

        //If found in the cached dictionary, get the node id from there
        if (cachedVirtualNodeUrls != null && cachedVirtualNodeUrls.ContainsKey(path)) {
            int nodeId = cachedVirtualNodeUrls[path];
            contentRequest.PublishedContent = new UmbracoHelper(UmbracoContext.Current).Content(nodeId);
            return true;
        }

        //If not found on the cached dictionary, traverse nodes and find the node that corresponds to the URL
        var rootNodes = contentRequest.RoutingContext.UmbracoContext.ContentCache.GetAtRoot();
        IPublishedContent item = null;
            item = rootNodes.DescendantsOrSelf<IPublishedContent>().Where(x => x.Url == (path + "/") || x.Url == path).FirstOrDefault();

        //If item is found, return it after adding it to the cache so we don't have to go through the same process again.
        if (cachedVirtualNodeUrls == null) { cachedVirtualNodeUrls = new Dictionary<string, int>(); }
        if (item != null)
        {
            //This check is redundant, but better to be on the safe side.
            if (!cachedVirtualNodeUrls.ContainsKey(path))
            {
                cachedVirtualNodeUrls.Add(path, item.Id);
            }
            
            //Add to cache
            HttpContext.Current.Cache.Add("cachedVirtualNodes",
                cachedVirtualNodeUrls,
                                        null,
                                        DateTime.Now.AddDays(1),
                                        System.Web.Caching.Cache.NoSlidingExpiration,
                                        System.Web.Caching.CacheItemPriority.High,
                                        null);
            
            //Return 
            contentRequest.PublishedContent = item;
            return true;
        }

        return false;
    }
}

