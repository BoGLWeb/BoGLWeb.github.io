using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NuGet.Frameworks;
using NUnit.Framework;

namespace PlaywrightTests {

    [TestFixture]
    public class RuleRuleSetLoadTests : PageTest{

        private readonly int timeout = 100000;
       
        [SetUp]
        public void setup() {

        }

        [Test]
        public async Task testNumLoadedRuleSets() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            await Expect(Page.Locator("text=Num Rulesets: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=Num Rulesets: 19")).ToBeVisibleAsync();
        }

        [Test]
        public async Task testNumLoadedRulets() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();

            await Expect(Page.Locator("text=Total Rules : 202")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedBondGraphRulesetRules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=BondGraphRuleset : 58")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
        }

        [Test]
        public async Task testNumLoadedSimplicifationRulesetRules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=SimplificationRuleset: 27")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
        }

        [Test]
        public async Task testNumLoadedDirRulesetRules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=DirRuleset: 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
        }

        [Test]
        public async Task testNumLoadedNewDirectionRuleSet_2Rules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=newDirectionRuleSet_2 : 15")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
        }

        [Test]
        public async Task testNumLoadedDirRuleset3() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=DirRuleset3 : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedSimplification2Rules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=Simplification2 : 32")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedNewCausalityMethodRuleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=NewCausalityMethodRuleset : 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedNewCausalityMethodRuleset_2() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=NewCausalityMethodRuleset_2 : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedNewCausalityMethodRuleset_3() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=NewCausalityMethod_3 : 2")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedINVDMarkerRules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=INVDMarkerRules : 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedINVDMarkerRules_2() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=INVDMarkerRules_2: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedCalibrationNewRuleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=CalubrationNewRuleset : 12")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedCalibrationNewRuleset_2() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=CalibrationNewRuleset_2 : 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedRFlagCleanRuleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=RFlagCleanRuleset : 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedICFixTotalRuleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=ICFixTotalRuleset : 12")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedTransformerFlipRuleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=TransformerFlipRuleset: 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedTransformerFlipRuleset2() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=TransformerFlipRuleset2 : 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedClean23Ruleset() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=Clearn23Ruleset : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }

        [Test]
        public async Task testNumLoadedBeforeBGVerifyDirRuleSet() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=BeforeBG-VerifyDirRuleSet : 8")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }
    }
}
