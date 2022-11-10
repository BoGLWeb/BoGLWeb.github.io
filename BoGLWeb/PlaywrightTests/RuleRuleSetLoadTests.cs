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

        private readonly int timeout = 20000;
       
        [SetUp]
        public void setup() {

        }

        [Test]
        public async Task testNumLoaded() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var countButton = Page.Locator("text=Count Rules");
            await Page.WaitForTimeoutAsync(timeout);
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=Num Rulesets : 19")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=Total Rules : 202")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=BondGraphRuleset : 58")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
            await Expect(Page.Locator("text=SimplificationRuleset : 28")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
            await Expect(Page.Locator("text=DirRuleset : 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
            await Expect(Page.Locator("text=newDirectionRuleSet_2 : 17")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout 
            });
            await Expect(Page.Locator("text=DirRuleset3 : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=Simplification2 : 33")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=NewCausalityMethodRuleset : 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=NewCausalityMethodRuleset_2 : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=NewCausalityMethodRuleset_3 : 2")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=INVDMarkerRules : 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=INVDMarkerRules_2 : 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=CalibrationNewRuleset : 12")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=CalibrationNewRuleset_2 : 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=RFlagCleanRuleset : 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=ICFixTotalRuleset : 12")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=TransformerFlipRuleset : 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=TransformerFlipRuleset2 : 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
            await Expect(Page.Locator("text=Clean23Ruleset : 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
           await Expect(Page.Locator("text=BeforeBG-VerifyDirRuleSet : 8")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions {
                Timeout = timeout
            });
        }
    }
}
