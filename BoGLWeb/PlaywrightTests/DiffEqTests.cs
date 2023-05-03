using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NuGet.Frameworks;
using System.Runtime.ConstrainedExecution;

namespace BoGLWeb.PlaywrightTests {
    public class DiffEqTests : PageTest {
        private readonly int timeout = 20000;
        private String[] testFunctions;

        [SetUp]
        public void Setup() {
        }

        /// <summary>
        /// Tests the <c>Expression</c> object as used for differential equation
        /// conversion.
        /// </summary>
        [Test]
        public async Task TestFunctionClass() {
            await Page.GotoAsync("http://localhost:5006/diffeqtest");
            var displayAllFunctionsButton = Page.Locator("text=Display All Functions");
            await displayAllFunctionsButton.ClickAsync();
            var allFunctionsText = Page.Locator("text=Available functions: ");
            testFunctions = allFunctionsText.InnerTextAsync().ToString().Split(',');
            var parseNextFunctionButton = Page.Locator("text=Parse Next Expression");
            foreach (String fn in testFunctions) {
                await parseNextFunctionButton.ClickAsync();
                await Expect(Page.Locator("text=Parsed test Expression: " + fn)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            }
        }
    }
}
