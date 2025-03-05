using Microsoft.Playwright.NUnit;

namespace BoGLWeb.PlaywrightTests {
    public class BoglFileLoadTests : PageTest {

        private readonly int timeout = 2000;

        [SetUp]
        public void Setup() {

        }

        [Test]
        public async Task TestSimpleBoglFileLoad() {
            await Page.GotoAsync("http:///localhost:5006/simplesystemdiagramfromboglfiletest");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
            await Expect(Page.Locator("text=System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Spring")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Force_Input")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Spring to System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass to System_MT_Spring")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass to System_MT_Force_Input")).ToBeVisibleAsync();
        }

        [Test]
        public async Task TestComplexBoglFileLoad() {
            await Page.GotoAsync("http:///localhost:5006/complexsystemdiagramfromboglfiletest");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
            await Expect(Page.Locator("text=System_MT_Ground")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Spring")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Damper")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Force_Input")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Spring to System_MT_Ground")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass to System_MT_Spring")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Ground to System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Spring to System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Damper to System_MT_Spring has velocity 4")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Damper to System_MT_Mass")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass to System_MT_Damper")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Mass to System_MT_Ground")).ToBeVisibleAsync();
            await Expect(Page.Locator("text=System_MT_Force_Input to System_MT_Mass")).ToBeVisibleAsync();
        }

    }
}
