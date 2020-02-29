using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Routing;

[RuntimeLevel(MinLevel = RuntimeLevel.Run)]
public class ContentFinderStartUp : IUserComposer
{
    public void Compose(Composition composition)
    {
        composition.ContentFinders().Insert<VirtualNodesContentFinder>();
    }
}

/// <summary>
/// ContentFinder for VirtualNodesUrlProvider
/// </summary>
public class VirtualNodesContentFinder : IContentFinder
{
    public bool TryFindContent(PublishedRequest contentRequest)
    {
        //Get a cached dictionary of urls and node ids
        var cachedVirtualNodeUrls = Current.AppCaches.RuntimeCache.GetCacheItem<Dictionary<string, int>>("cachedVirtualNodes");

        //Get the request path
        string path = contentRequest.Uri.AbsoluteUri;
        if (path.IndexOf('?') != -1)
        {
            path = path.Substring(0, path.IndexOf('?'));
        }

        //If found in the cached dictionary, get the node id from there
        if (cachedVirtualNodeUrls != null && cachedVirtualNodeUrls.ContainsKey(path))
        {
            int nodeId = cachedVirtualNodeUrls[path];
            contentRequest.PublishedContent = contentRequest.UmbracoContext.Content.GetById(nodeId);
            return true;
        }

        //Get the culture for ML content
        string culture = (contentRequest.Culture!=null) ? contentRequest.Culture.Name : null;

        //If not found on the cached dictionary, traverse nodes and find the node that corresponds to the URL
        var rootNodes = contentRequest.UmbracoContext.Content.GetAtRoot(culture:contentRequest.Culture.Name);
        IPublishedContent item = null;
        item = rootNodes
                .DescendantsOrSelf<IPublishedContent>(culture:culture)
                .Where(x => x.Url(culture:culture, mode:UrlMode.Absolute) == (path + "/") || x.Url(culture:culture, mode:UrlMode.Absolute) == path)
                .FirstOrDefault();

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
            Current.AppCaches.RuntimeCache.InsertCacheItem<Dictionary<string, int>>(
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
}