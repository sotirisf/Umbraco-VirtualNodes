using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace DotSee.VirtualNodes
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UrlProviderStartUp : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.UrlProviders().InsertBefore<DefaultUrlProvider, VirtualNodesUrlProvider>();
        }
    }

    public class VirtualNodesUrlProvider : DefaultUrlProvider, IUrlProvider
    {
        //private readonly UmbracoHelper _u;
        private readonly IRequestHandlerSection _requestSettings;
        private readonly ILogger _logger;
        private readonly IGlobalSettings _globalSettings;
        private readonly ISiteDomainHelper _siteDomainHelper;

        //V8 will use the constructor below, doesn't seem to work for the time being. A warning is being thrown during compilation
        //that the default parameterless constructor is obsolete, but it looks like we still have to use that.
        ////https://our.umbraco.org/forum/developers/api-questions/72856-override-defaulturlprovider-not-working-when-new-constructor-is-used
        public VirtualNodesUrlProvider(IRequestHandlerSection requestSettings, ILogger logger, IGlobalSettings globalSettings, ISiteDomainHelper siteDomainHelper) : base(requestSettings, logger, globalSettings, siteDomainHelper)
        {
            //_u = u;
            _requestSettings = requestSettings;
            _logger = logger;
            _globalSettings = globalSettings;
            _siteDomainHelper = siteDomainHelper;
        }

        public override IEnumerable<UrlInfo> GetOtherUrls(UmbracoContext umbracoContext, int id, Uri current)
        {
            return base.GetOtherUrls(umbracoContext, id, current);
        }

        public override UrlInfo GetUrl(UmbracoContext umbracoContext, IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
          
            //Just in case
            if (content == null)
            {
                //otherwise return the base GetUrl result:
                return base.GetUrl(umbracoContext, content, mode, culture, current);
            }

            //If this is a virtual node itself, no need to handle it - should return normal URL

            bool hasVirtualNodeInPath = false;
            foreach (IPublishedContent item in content.Ancestors()) //.Union(content.Children())
            {
                if (item.IsVirtualNode())
                {
                    hasVirtualNodeInPath = true;
                    break;
                }
            }

            return (hasVirtualNodeInPath ? ConstructUrl(umbracoContext, content.Id, current, mode, content, culture) : null);
        }

        private UrlInfo ConstructUrl(UmbracoContext umbracoContext, int id, Uri current, UrlMode mode, IPublishedContent content, string culture)
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
            UrlInfo url = base.GetUrl(umbracoContext, content, mode, culture, current);
            
            string newUrl="";

            //If we come from an absolute URL, strip the host part and keep it so that we can append
            //it again when returing the URL.
            string hostPart = "";
            if (url.Text.StartsWith("http"))
            {
                Uri u = new Uri(url.Text);

                newUrl = url.Text.Replace(u.GetLeftPart(UriPartial.Authority), "");

                hostPart = u.GetLeftPart(UriPartial.Authority);
            }
            else
            {
                newUrl = url.Text;
            }

            //Strip leading and trailing slashes
            if ((newUrl.EndsWith("/")))
            {
                newUrl = newUrl.Substring(0, newUrl.Length - 1);
            }
            if ((newUrl.StartsWith("/")))
            {
                newUrl = newUrl.Substring(1, newUrl.Length - 1);
            }

            //Now split the url. We should have as many elements as those in pathIds.
            string[] urlParts = newUrl.Split('/').Reverse().ToArray();

            //Iterate the url parts. Check the corresponding path id and if the document that corresponds there
            //is of a type that must be excluded from the path, just make that url part an empty string.
            int cnt = 0;
            foreach (string p in urlParts)
            {
                IPublishedContent currItem = umbracoContext.Content.GetById(int.Parse(pathIds[cnt]));

                //Omit any virtual node unless it's leaf level (we still need this otherwise it will be pointing to parent's URL)
                if (currItem.IsVirtualNode() && cnt > 0)
                {
                    urlParts[cnt] = "";
                }
                cnt++;
            }

            //Reconstruct the url, leaving out all parts that we emptied above. This
            //will be our final url, without the parts that correspond to excluded nodes.
            string finalUrl = string.Join("/", urlParts.Reverse().Where(x => x != "").ToArray());

            //Just in case - check if there are trailing and leading slashes and add them if not.
            //Also check for the "AddTrailingSlash" setting and append trailing slash or not accordingly.
            finalUrl = _requestSettings.AddTrailingSlash 
                ? finalUrl.EnsureEndsWith("/").EnsureStartsWith("/")
                : finalUrl.EnsureStartsWith("/");
            
            finalUrl = string.Concat(hostPart, finalUrl);

            //Voila.
            return (new UrlInfo(finalUrl, true, culture));
        }
    }
}