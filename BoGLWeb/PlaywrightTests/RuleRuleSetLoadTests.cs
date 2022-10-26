using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NuGet.Frameworks;
using NUnit.Framework;

namespace PlaywrightTests {

    [TestFixture]
    public class RuleRuleSetLoadTests : PageTest{
       
        [SetUp]
        public void setup() {

        }

        [Test]
        public async Task testNumLoadedRules() {
            await Page.GotoAsync("http://localhost:5006/rulerulesettest");
            await Expect(Page.Locator("text=Num Rulesets: 0")).ToBeVisibleAsync();
            var countButton = Page.Locator("text=Count Rules");
            await countButton.ClickAsync();
            await Expect(Page.Locator("text=Num Rulesets: 19")).ToBeVisibleAsync();
        }
    }
}
