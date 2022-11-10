using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NuGet.Frameworks;
using System.Runtime.ConstrainedExecution;
/// NuGet\Install-Package Microsoft.Playwright -Version 1.27.1
/// NuGet\Install-Package Microsoft.Playwright.NUnit -Version 1.27.1

namespace PlaywrightTests {
    /// <summary>
    /// Runs unit tests for all areas of this program.
    /// </summary>
    public class Tests : PageTest {
        private readonly int timeout = 20000;

        [SetUp]
        public void Setup() {
        }

        /// <summary>
        /// Tests the EditionList object used as the undo/redo stack.
        /// </summary>
        [Test]
        public async Task TestEditionHelper() {
            await Page.GotoAsync("http://localhost:5006/undoredotest");
            var undoButton = Page.Locator("text=UndoAction");
            var redoButton = Page.Locator("text=RedoAction");
            var editButton = Page.Locator("text=Edit");
            var clearButton = Page.Locator("text=Clear");
            await Expect(Page.Locator("text=Current index: -1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 0; i < 10; i++) {
                await editButton.ClickAsync();
                await Expect(Page.Locator("text=Current index: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current size: " + (i + 1))).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current element: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            }
            for (int i = 8; i >= 2; i--) {
                await undoButton.ClickAsync();
                await Expect(Page.Locator("text=Current index: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current size: 10")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current element: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            }
            for (int i = 3; i <= 7; i++) {
                await redoButton.ClickAsync();
                await Expect(Page.Locator("text=Current index: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current size: 10")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current element: " + i)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            }
            await editButton.ClickAsync();
            await Expect(Page.Locator("text=Current index: 8")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 9")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 10")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await clearButton.ClickAsync();
            await Expect(Page.Locator("text=Current index: -1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await editButton.ClickAsync();
            await Expect(Page.Locator("text=Current index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 11")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await undoButton.ClickAsync();
            await Expect(Page.Locator("text=Current index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 11")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await redoButton.ClickAsync();
            await Expect(Page.Locator("text=Current index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 11")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 0; i < 4; i++) {
                await editButton.ClickAsync();
            }
            await Expect(Page.Locator("text=Current index: 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 15")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 0; i < 5; i++) {
                await redoButton.ClickAsync();
                await Expect(Page.Locator("text=Current index: 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current size: 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
                await Expect(Page.Locator("text=Current element: 15")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            }
            for (int i = 0; i < 13; i++) {
                await undoButton.ClickAsync();
            }
            await Expect(Page.Locator("text=Current index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 11")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 0; i < 13; i++) {
                await redoButton.ClickAsync();
            }
            await Expect(Page.Locator("text=Current index: 4")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current size: 5")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current element: 15")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
        }
    }
}

