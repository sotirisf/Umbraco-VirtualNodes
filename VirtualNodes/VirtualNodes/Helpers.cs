using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace DotSee.VirtualNodes
{
    public static class Helpers
    {

        public static bool MatchDuplicateName(string potentialDuplicateName, string currNodeName)
        {
            return (potentialDuplicateName.LastIndexOf('(') != -1
                        && potentialDuplicateName.LastIndexOf(')') == potentialDuplicateName.Length - 1
                        && potentialDuplicateName.Substring(0, potentialDuplicateName.LastIndexOf('(') - 1).Equals(currNodeName)
                    );

        }

        public static int GetMaxNodeNameNumbering(string potentialDuplicateName, string currNodeName, int maxNumber)
        {
            //Anything goes wrong, we return the same maxNumber as provided
            try
            {
                int newNumber = int.Parse(potentialDuplicateName.Substring(potentialDuplicateName.LastIndexOf('(') + 1, potentialDuplicateName.LastIndexOf(')') - 1 - potentialDuplicateName.LastIndexOf('(')));
                maxNumber = (maxNumber < newNumber) ? newNumber : maxNumber;
            }
            catch { }

            return (maxNumber);
        }

        public static bool IsVirtualNode(this IPublishedContent item) {
            foreach (VirtualNodesRule rule in VirtualNodesRuleManager.Instance.Rules)
            {
                if (MatchContentTypeAlias(item.DocumentTypeAlias, rule.DocTypeAlias))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVirtualNode(this IContent item) {
            foreach (VirtualNodesRule rule in VirtualNodesRuleManager.Instance.Rules) {
                if (MatchContentTypeAlias(item.ContentType.Alias, rule.DocTypeAlias)) {
                    return true;
                }
            }
            return false;
           
        }

        private static bool MatchContentTypeAlias(string nodeContentTypeAlias, string contentTypeAliasFromSettings)
        {

            if (contentTypeAliasFromSettings.EndsWith("*") && contentTypeAliasFromSettings.StartsWith("*")) {
                return (nodeContentTypeAlias.Contains(contentTypeAliasFromSettings.ToLower().Replace("*", "")));
            }
            else if (contentTypeAliasFromSettings.EndsWith("*"))
            {
                return (nodeContentTypeAlias.ToLower().StartsWith(contentTypeAliasFromSettings.ToLower().Replace("*", "")));
            }
            else if (contentTypeAliasFromSettings.StartsWith("*"))
            {
                return (nodeContentTypeAlias.ToLower().EndsWith(contentTypeAliasFromSettings.ToLower().Replace("*", "")));
            }
            else
            {
                return (nodeContentTypeAlias.ToLower().Equals(contentTypeAliasFromSettings.ToLower()));
            }
        }
    }
}
