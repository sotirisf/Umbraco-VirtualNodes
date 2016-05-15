using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Web;

namespace DotSee.VirtualNodes
{
    /// <summary>
    /// Loads rules for OmitSegmentsUrlProvider
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
        private List<VirtualNodesRule> _rules;

        #endregion

        #region Public Members
        
        /// <summary>
        /// Gets the list of rules
        /// </summary>
        public List<VirtualNodesRule> Rules { get { return _rules; } }
        
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
            string keyPrefix = "virtualnodes:";

            _rules = new List<VirtualNodesRule>();

            //Get all entries with keys starting with specified prefix
            string[] ruleKeys =
                ConfigurationManager.AppSettings.AllKeys
                .Where(x => x.StartsWith(keyPrefix)).ToArray();
             
            //Register a rule for each item
            foreach (string ruleKey in ruleKeys)
            {
                RegisterRule(new VirtualNodesRule(ruleKey.Replace(keyPrefix, ""), ConfigurationManager.AppSettings[ruleKey]));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new rule for url segment replacement
        /// </summary>
        /// <param name="rule">An VirtualNodesRule object</param>
        public void RegisterRule(VirtualNodesRule rule)
        {
            _rules.Add(rule);
        }
        
        #endregion  

    }
}
