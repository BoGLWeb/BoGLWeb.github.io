using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BoGLWeb;
using Microsoft.Playwright.NUnit;
using NuGet.Frameworks;
using NUnit.Framework;

namespace PlaywrightTests {
    public class RuleRuleSetLoadTests{
       
        [SetUp]
        public void setup() {
            RuleSetMap.getInstance().loadRuleSet("BondGraphRuleset");
            RuleSetMap.getInstance().loadRuleSet("SimplificationRuleset");
            RuleSetMap.getInstance().loadRuleSet("DirRuleset");
            RuleSetMap.getInstance().loadRuleSet("newDirectionRuleSet_2");
            RuleSetMap.getInstance().loadRuleSet("DirRuleset3");
            RuleSetMap.getInstance().loadRuleSet("Simplification2");
            RuleSetMap.getInstance().loadRuleSet("NewCausalityMethodRuleset");
            RuleSetMap.getInstance().loadRuleSet("NewCausalityMethodRuleset_2");
            RuleSetMap.getInstance().loadRuleSet("NewCausalityMethodRuleset_3");
            RuleSetMap.getInstance().loadRuleSet("INVDMarkerRules");
            RuleSetMap.getInstance().loadRuleSet("INVDMarkerRules_2");
            RuleSetMap.getInstance().loadRuleSet("CalibrationNewRuleset");
            RuleSetMap.getInstance().loadRuleSet("CalibrationNewRuleset_2");
            RuleSetMap.getInstance().loadRuleSet("RFlagCleanRuleset");
            RuleSetMap.getInstance().loadRuleSet("ICFixTotalRuleset");
            RuleSetMap.getInstance().loadRuleSet("TransformerFlipRuleset");
            RuleSetMap.getInstance().loadRuleSet("TransformerFlipRuleset2");
            RuleSetMap.getInstance().loadRuleSet("Clean23Ruleset");
            RuleSetMap.getInstance().loadRuleSet("BeforeBG-VerifyDirRuleSet");
        }

        [Test]
        public void testNumLoadedRules() {
            Assert.Pass();
        }
    }
}
