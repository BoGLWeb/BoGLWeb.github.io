using GraphSynth.Representation;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace BoGLWeb {
    public sealed class RuleSetMap{

        //Singleton
        private static RuleSetMap instance = null;
        private static readonly object padlock = new object();

        //Other Stuff
        private Dictionary<string, ruleSet> ruleSetMap;
        private int numLoaded;

        RuleSetMap() {
            ruleSetMap = new Dictionary<string, ruleSet>();
            numLoaded = 0;
        }

        public static RuleSetMap getInstance() {
            lock (padlock) {
                if (instance == null) {
                    instance = new RuleSetMap();
                }
                return instance;
            }            
        }

        public async Task loadRuleSet(string name) {
            if (ruleSetMap.ContainsKey(name)) {
                Console.WriteLine("Rule " + name + " already loaded.");
                return;
            }

            HttpClient client = new HttpClient();

            //TODO Figure out if this URL is okay, or is there something else that it should be
            HttpResponseMessage ruleSetResponse = await client.GetAsync("http://localhost:5006/Rules/" + name + ".rsxml");
            var ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            var ruleSetFileContent = await ruleSetResponse.Content.ReadAsStreamAsync();
            ruleSetMap.Add(name, (ruleSet)ruleDeserializer.Deserialize(ruleSetFileContent));
            var numRules = ruleSetMap[name].ruleFileNames.Count;
            string ruleDir = ruleSetMap[name].rulesDir;
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

        public int getNumRules() {
            return ruleSetMap.Count;
        }

        public ruleSet getRuleSet(string name) {
            return ruleSetMap[name];
        }

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
