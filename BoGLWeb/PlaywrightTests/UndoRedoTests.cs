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

namespace BoGLWeb.PlaywrightTests {
    /// <summary>
    /// Runs unit tests for all areas of this program.
    /// </summary>
    public class Tests : PageTest {
        private readonly int timeout = 20000;

        [SetUp]
        public void Setup() {
        }

        /// <summary>
        /// <br>Tests the following functions:</br>
        ///     <br>- The <c>EditionList</c> object used as the undo/redo stack.</br>
        ///     <br>- The <c>HashList</c> object as a <c>LinkedList HashMap</c>
        ///     implementation.</br>
        /// </summary>
        [Test]
        public async Task TestEditionHelper() {
            await Page.GotoAsync("http://localhost:5006/undoredotest");
            var undoButton = Page.Locator("text=Undo EditionList");
            var redoButton = Page.Locator("text=Redo EditionList");
            var editButton = Page.Locator("text=Edit EditionList");
            var clearButton = Page.Locator("text=Clear EditionList");
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
            // Begin HashList testing
            var addToHashListButton = Page.Locator("text=Add Element to HashList");
            var clearHashListButton = Page.Locator("text=Clear HashList");
            var incrementHashListCounterButton = Page.Locator("text=Increment HashList Counter");
            var decrementHashListCounterButton = Page.Locator("text=Decrement HashList Counter");
            var updateCurrentHashListElementButton = Page.Locator("text=Update Current HashList Element");
            var removeHashListElementButton = Page.Locator("text=Remove HashList Element");
            await Expect(Page.Locator("text=Current HashList size: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList: []")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await addToHashListButton.ClickAsync();
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Current HashList size: 1")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList: [0]")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Item at current HashList index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 1; i < 10; i++) {
                await addToHashListButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Current HashList size: 10")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Item at current HashList index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 0; i < 6; i++) {
                await incrementHashListCounterButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Item at current HashList index: 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 6; i > 3; i--) {
                await decrementHashListCounterButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Item at current HashList index: 3")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 3; i < 7; i++) {
                await removeHashListElementButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Item at current HashList index: 7")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList: [0, 1, 2, 7, 8, 9]")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList size: 6")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 3; i < 13; i++) {
                await incrementHashListCounterButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Item at current HashList index: 9")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            for (int i = 5; i > -5; i--) {
                await decrementHashListCounterButton.ClickAsync();
            }
            await updateCurrentHashListElementButton.ClickAsync();
            await Expect(Page.Locator("text=Item at current HashList index: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await clearHashListButton.ClickAsync();
            await Expect(Page.Locator("text=Current HashList: []")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
            await Expect(Page.Locator("text=Current HashList size: 0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = timeout });
        }
    }
}

