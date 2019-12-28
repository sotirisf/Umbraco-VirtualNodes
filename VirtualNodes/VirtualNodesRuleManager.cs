using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Web;

namespace DotSee.VirtualNodes
{
    /// <summary>
    /// Loads rules for VirtualNodesUrlProvider
    /// </summary>
    public sealed class VirtualNodesRuleManager
    {

        #region Private Members

        /// <summary>
        /// Lazy singleton instance member
        /// </summary>
        private static readonly Lazy<VirtualNodesRuleManager> _instance = new Lazy<VirtualNodesRuleManager>(() => new VirtualNodesRuleManager());

        /// <summary>
        /// The list of rule objects
        /// </summary>
        private List<string> _rules;

        private List<string> _notPageRules;

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the list of rules
        /// </summary>
        public List<string> Rules { get { return _rules; } }

        /// <summary>
        /// Gets the list of rules
        /// </summary>
        public List<string> NotPageRules { get { return _notPageRules; } }

        #endregion

        #region Constructors

        /// <summary>
        /// Returns a (singleton) VirtualNodesRuleManager instance
        /// </summary>
        public static VirtualNodesRuleManager Instance { get { return _instance.Value; } }

        /// <summary>
        /// Private constructor for Singleton
        /// </summary>
        private VirtualNodesRuleManager()
        {
            ///This is the prefix web.config keys should have to be included 
            string key = "virtualnode";
            string notPageKey = "virtualnode-notpage";

            _rules = new List<string>();
            _notPageRules = new List<string>();

            //Get all entries with keys starting with specified prefix
            string rules = ConfigurationManager.AppSettings.Get(key);
            string notPageRules = ConfigurationManager.AppSettings.Get(notPageKey);

            if (string.IsNullOrEmpty(rules) && string.IsNullOrEmpty(notPageRules)) { return; }
            
           //Register a rule for each item
            foreach (string rule in rules.Split(','))
            {
                _rules.Add(rule.Trim());
            }

            foreach (string rule in notPageRules.Split(','))
            {
                _notPageRules.Add(rule.Trim());
            }
        }

        #endregion
    }
}
