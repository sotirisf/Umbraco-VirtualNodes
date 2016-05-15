using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Routing;

public class VirtualNodesUrlProvider : DefaultUrlProvider
{
    //V8 will use the constructor below, doesn't seem to work for the time being. A warning is being thrown during compilation
    //that the default parameterless constructor is obsolete, but it looks like we still have to use that.
    ////https://our.umbraco.org/forum/developers/api-questions/72856-override-defaulturlprovider-not-working-when-new-constructor-is-used
    //public OmitSegmentsUrlProvider(IRequestHandlerSection requestSettings) : base(UmbracoConfig.For.UmbracoSettings().RequestHandler)
    //{
    //}

    public override IEnumerable<string> GetOtherUrls(UmbracoContext umbracoContext, int id, Uri current)
    {
        return (base.GetOtherUrls(umbracoContext, id, current));
    }

    public override string GetUrl(UmbracoContext umbracoContext, int id, Uri current, UrlProviderMode mode)
    {
        var content = umbracoContext.ContentCache.GetById(id);

        //Just in case
        if (content == null) { return null; }
        
        //If this is a virtual node itself, no need to handle it - should return normal URL
        //if (content.DocumentTypeAlias.ToLower().StartsWith("virtualnode")) { return null; }

        bool hasVirtualNodeInPath = false;
        foreach (IPublishedContent item in content.Ancestors().Union(content.Children()))
        {
            if (item.DocumentTypeAlias.ToLower().StartsWith("virtualnode")) {
                hasVirtualNodeInPath = true;
                break;
            }
        }

        //if (!content.DocumentTypeAlias.ToLower().StartsWith("virtualnode"))
        //{
        //    return null;
        //}

        return (hasVirtualNodeInPath?ConstructUrl(umbracoContext, id, current, mode, content):null);


        //Find the first rule that matches the current document's doctype
        //foreach (OmitSegmentsRule rule in OmitSegmentsRuleManager.Instance.Rules)
        //{
        //    if (rule.DocTypeAlias.Equals(content.DocumentTypeAlias))
        //    {
        //        return (ConstructUrl(umbracoContext, id, current, mode, content, rule));
        //    }
        //}

        //Return null if no conditions are met so that the DefaultUrlProvider takes over and produces the default url.
        //return null;
    }

    private string ConstructUrl(UmbracoContext umbracoContext, int id, Uri current, UrlProviderMode mode, IPublishedContent content)
    {

        string path = content.Path;

        //Keep path items in par with path segments in url
        //If we are hiding the top node from path, then we'll have to skip one path item (the root). 
        //If we are not, then we'll have to skip two path items (root and home)
        string hideTopNode = ConfigurationManager.AppSettings.Get("umbracoHideTopLevelNodeFromPath");
        if (string.IsNullOrEmpty(hideTopNode)) { hideTopNode = "false"; }
        int pathItemsToSkip = (hideTopNode == "true") ? 2 : 1;

        //Get the path ids but skip what's needed in order to have the same number of elements in url and path ids.
        string[] pathIds = path.Split(',').Skip(pathItemsToSkip).Reverse().ToArray();
        
        //Get the default url 
        //DO NOT USE THIS - RECURSES: string url = content.Url;
        //https://our.umbraco.org/forum/developers/extending-umbraco/73533-custom-url-provider-stackoverflowerror
        //https://our.umbraco.org/forum/developers/extending-umbraco/66741-iurlprovider-cannot-evaluate-expression-because-the-current-thread-is-in-a-stack-overflow-state
        string url = base.GetUrl(umbracoContext, id, current, mode);

        //Strip leading and trailing slashes 
        if ((url.EndsWith("/")))
        {
            url = url.Substring(0, url.Length - 1);
        }
        if ((url.StartsWith("/")))
        {
            url = url.Substring(1, url.Length - 1);
        }

        //Now split the url. We should have as many elements as those in pathIds.
        string[] urlParts = url.Split('/').Reverse().ToArray();

        //Iterate the url parts. Check the corresponding path id and if the document that corresponds there
        //is of a type that must be excluded from the path, just make that url part an empty string.
        int cnt = 0;
        foreach (string p in urlParts)
        {
            string docTypeAlias = umbracoContext.ContentCache.GetById(int.Parse(pathIds[cnt])).DocumentTypeAlias;

            //Omit any virtual node unless it's leaf level (we still need this otherwise it will be pointing to parent's URL)
            if (docTypeAlias.ToLower().StartsWith("virtualnode") && cnt>0)
            {
                urlParts[cnt] = "";
            }
            cnt++;
        }

        //Reconstruct the url, leaving out all parts that we emptied above. This 
        //will be our final url, without the parts that correspond to excluded nodes.
        string finalUrl = string.Join("/", urlParts.Reverse().Where(x => x != "").ToArray());

        //Just in case - check if there are trailing and leading slashes and add them if not.
        if (!(finalUrl.EndsWith("/")))
        {
            finalUrl += "/";
        }
        if (!(finalUrl.StartsWith("/")))
        {
            finalUrl = "/" + finalUrl;
        }

        //Voila.
        return (finalUrl);
    }
}
