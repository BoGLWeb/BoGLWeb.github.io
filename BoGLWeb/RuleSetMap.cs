using GraphSynth.Representation;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace BoGLWeb {
    public sealed class RuleSetMap{

        //Singleton Setup
        private static RuleSetMap instance = null;
        private static readonly object padlock = new object();

        //Class variables
        private Dictionary<string, ruleSet> ruleSetMap;
        private int numLoaded;

        private RuleSetMap() {
            ruleSetMap = new Dictionary<string, ruleSet>();
            numLoaded = 0;
        }

        /// <summary>
        /// Returns the singleton instance of the RuleSetMap
        /// </summary>
        /// <returns>An instance of RuleSetMap</returns>
        public static RuleSetMap getInstance() {
            lock (padlock) {
                if (instance == null) {
                    instance = new RuleSetMap();
                }
                return instance;
            }            
        }

        /// <summary>
        /// Loads the ruleset with a given name
        /// </summary>
        /// <param name="name">The name of the ruleset</param>
        /// <returns>The completed Task</returns>
        public async Task loadRuleSet(string name) {
            //Ensure that we only load each rule once
            if (ruleSetMap.ContainsKey(name)) {
                Console.WriteLine("Rule " + name + " already loaded.");
                return;
            }

            //Setup HTTP client that we will use to load the file
            HttpClient client = new HttpClient();

            //Load the file as plain text
            //TODO Figure out if this URL is okay, or is there something else that it should be
            HttpResponseMessage ruleSetResponse = await client.GetAsync("http://localhost:5006/Rules/" + name + ".rsxml");
            var ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            var ruleSetFileContent = await ruleSetResponse.Content.ReadAsStreamAsync();

            //Deserialize the ruleset
            ruleSetMap.Add(name, (ruleSet)ruleDeserializer.Deserialize(ruleSetFileContent));
            var numRules = ruleSetMap[name].ruleFileNames.Count;
            string ruleDir = ruleSetMap[name].rulesDir;

            //Load rules for the ruleset
            List<string> ruleFileNames = ruleSetMap[name].ruleFileNames;

            var progStart = 5;
            var progStep = (double)(100 - progStart) / ruleFileNames.Count;
            var rules = new List<grammarRule>();
            numLoaded = 0;
            while (numLoaded < ruleFileNames.Count)
            {
                var rulePath = "/Rules/" + ruleFileNames[numLoaded];

                HttpResponseMessage ruleResponse = await client.GetAsync("http://localhost:5006/" + rulePath);
                string ruleText = await ruleResponse.Content.ReadAsStringAsync();

                var xeRule = XElement.Parse(ruleText);
                var temp = xeRule.Element("{ignorableUri}" + "grammarRule");
                var openRule = new grammarRule();
                if (temp != null)
                {
                    openRule = DeSerializeRuleFromXML(RemoveXAMLns(RemoveIgnorablePrefix(temp.ToString())));
                }

                removeNullWhiteSpaceEmptyLabels(openRule.L);
                removeNullWhiteSpaceEmptyLabels(openRule.R);

                object ruleObj = new object[] { openRule, rulePath };
                if (ruleObj is grammarRule)
                {
                    rules.Add((grammarRule)ruleObj);
                }
                else if (ruleObj is object[])
                {
                    foreach (object o in (object[])ruleObj)
                    {
                        if (o is grammarRule)
                        {
                            rules.Add((grammarRule)o);
                        }
                    }
                }
                numLoaded++;
            }

            ruleSetMap[name].rules = rules;
        }

        /// <summary>
        /// Returns the number of loaded rules
        /// </summary>
        /// <returns>number of loaded rules as an int</returns>
        public int getNumRules() {
            return ruleSetMap.Count;
        }

        /// <summary>
        /// Returns a ruleset
        /// </summary>
        /// <param name="name">The name of the ruleset</param>
        /// <returns>A ruleset</returns>
        public ruleSet getRuleSet(string name) {
            return ruleSetMap[name];
        }

        //Helper methods from BoGL Desktop
        private grammarRule DeSerializeRuleFromXML(string xmlString)
        {
        var stringReader = new StringReader(xmlString);
        var ruleDeserializer = new XmlSerializer(typeof(grammarRule));
        var newGrammarRule = (grammarRule)ruleDeserializer.Deserialize(stringReader);
        if (newGrammarRule.L == null)
        {
            newGrammarRule.L = new designGraph();
        }
        else
        {
            newGrammarRule.L.internallyConnectGraph();
        }

        if (newGrammarRule.R == null)
        {
            newGrammarRule.R = new designGraph();
        }
        else
        {
            newGrammarRule.R.internallyConnectGraph();
        }

        return newGrammarRule;
        }

        private string RemoveXAMLns(string str)
        {
            return str.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");
        }

        private string RemoveIgnorablePrefix(string str)
        {
            return str.Replace("GraphSynth:", "").Replace("xmlns=\"ignorableUri\"", "");
        }

        private void removeNullWhiteSpaceEmptyLabels(designGraph g)
        {
        g.globalLabels.RemoveAll(string.IsNullOrWhiteSpace);
        foreach (var a in g.arcs)
        {
            a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
        }
        foreach (var a in g.nodes)
        {
            a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
        }
        foreach (var a in g.hyperarcs)
        {
            a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
        }
        }
    }
}
