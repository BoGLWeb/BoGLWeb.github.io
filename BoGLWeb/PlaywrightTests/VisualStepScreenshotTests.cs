using Microsoft.Playwright;
using NUnit.Framework;

namespace BoGLWeb.PlaywrightTests {
    [TestFixture]
    [Explicit("Requires local BoGLWeb server running on http://localhost:5006")]
    public class VisualStepScreenshotTests {
        private const string BaseUrl = "http://localhost:5006";
        private static readonly string OutDir = Path.Combine(
            @"C:\Users\p_a_d\.gemini\antigravity\brain\ec83403d-fae2-49af-8f4b-f2a15d540709",
            "visual_test_screenshots");

        private static string FindExamplesDir() {
            string[] candidates = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "wwwroot", "examples"),
                Path.Combine(Directory.GetCurrentDirectory(), "BoGLWeb", "wwwroot", "examples"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "examples")
            };
            foreach (string c in candidates) {
                string full = Path.GetFullPath(c);
                if (Directory.Exists(full)) return full;
            }
            throw new DirectoryNotFoundException("Could not find wwwroot/examples");
        }

        [Test]
        public async Task CaptureStepByStepEquationsWithBondGraphs() {
            Directory.CreateDirectory(OutDir);
            string examplesDir = FindExamplesDir();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
                ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
            });
            var page = await context.NewPageAsync();
            List<string> consoleMessages = new();
            page.Console += (_, msg) => {
                consoleMessages.Add($"[{msg.Type}] {msg.Text}");
            };
            page.PageError += (_, err) => {
                consoleMessages.Add($"[PageError] {err}");
            };

            await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Wait for rules to load and #generateButton to become enabled
            var generateBtn = page.Locator("#generateButton");
            await generateBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60000 });
            await page.WaitForFunctionAsync("() => !document.getElementById('generateButton')?.disabled", null,
                new PageWaitForFunctionOptions { Timeout = 90000 });

            // Widen the left panel via the .resizer drag handle so steps are comfortably readable
            var resizer = page.Locator(".resizer");
            var box = await resizer.BoundingBoxAsync();
            if (box != null) {
                await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
                await page.Mouse.DownAsync();
                await page.Mouse.MoveAsync(box.X + 180, box.Y + box.Height / 2, new MouseMoveOptions { Steps = 10 });
                await page.Mouse.UpAsync();
            }

            // 1. Rack and Pinion (Derivative Causality + TF + Se)
            await LoadGenerateAndOpenEquationsAsync(page, Path.Combine(examplesDir, "rack_pinion.bogl"));
            await SelectEquationAsync(page, "Equation for h10'");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutDir, "rack_pinion_L10.png") });

            await SelectEquationAsync(page, "Equation for x0'");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutDir, "rack_pinion_x5.png") });

            // 2. Spring & Damper with Force (Derivative Causality on C with source derivative F5')
            await LoadGenerateAndOpenEquationsAsync(page, Path.Combine(examplesDir, "spring_&_damper.bogl"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutDir, "spring_damper_x4.png") });

            // 3. LRC Circuit (No Derivative Causality baseline)
            await LoadGenerateAndOpenEquationsAsync(page, Path.Combine(examplesDir, "lrc_circuit.bogl"));
            await SelectEquationAsync(page, "Equation for ϕ2'");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutDir, "lrc_circuit_phi2.png") });

            // 4. Motor, Shaft and Gear Pair (GY + TF + Derivative Causality + auto-selected valid causal option)
            await LoadGenerateAndOpenEquationsAsync(page, Path.Combine(examplesDir, "motor-gear-pair.bogl"));
            await SelectEquationAsync(page, "Equation for h6'");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutDir, "motor_gear_L6.png") });

            TestContext.WriteLine("Screenshots saved to: " + OutDir);
            if (consoleMessages.Count > 0) {
                TestContext.WriteLine("Console logs:\n" + string.Join("\n", consoleMessages));
            }
            Assert.That(consoleMessages.Where(m => m.StartsWith("[error]") || m.StartsWith("[PageError]")), Is.Empty);
            Assert.That(File.Exists(Path.Combine(OutDir, "rack_pinion_L10.png")), Is.True);
        }

        private static async Task LoadGenerateAndOpenEquationsAsync(IPage page, string boglPath) {
            // Ensure we're on the System tab
            await page.Locator(".ant-tabs-tab:has-text('System')").ClickAsync();
            await page.WaitForTimeoutAsync(200);

            // Open File menu once to ensure #fileUpload listener is initialized, then set file
            await page.Locator(".topMenu:has-text('File')").ClickAsync();
            await page.WaitForTimeoutAsync(200);
            await page.SetInputFilesAsync("#fileUpload", boglPath);
            await page.WaitForTimeoutAsync(400);
            // Close menu if still open by clicking canvas area
            await page.Mouse.ClickAsync(900, 450);
            await page.WaitForTimeoutAsync(200);

            // Click Generate and wait for Causal BG tab to become enabled
            await page.Locator("#generateButton").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => { const el = document.querySelector('.tabsDisabled'); return !el; }",
                null,
                new PageWaitForFunctionOptions { Timeout = 60000 });

            // Switch to Causal BG tab
            await page.Locator(".ant-tabs-tab:has-text('Causal BG')").ClickAsync();
            await page.WaitForTimeoutAsync(400);

            // Click Show State Equations if currently showing State Variables
            var showEqBtn = page.Locator("#stateVarMenu button:has-text('Show State Equations')");
            if (await showEqBtn.IsVisibleAsync()) {
                await showEqBtn.ClickAsync();
            }

            // Wait for KaTeX finalEquation to render
            await page.Locator("#finalEquation .katex-mathml").WaitForAsync(new LocatorWaitForOptions {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });
            await page.WaitForTimeoutAsync(300);
        }

        private static async Task SelectEquationAsync(IPage page, string optionLabel) {
            await page.Locator("#equationSelect").ClickAsync();
            await page.WaitForTimeoutAsync(150);
            await page.Locator($".ant-select-item-option:has-text(\"{optionLabel}\")").First.ClickAsync();
            await page.WaitForTimeoutAsync(300);
            await page.Locator("#finalEquation .katex-mathml").WaitForAsync(new LocatorWaitForOptions {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });
            await page.EvaluateAsync("() => { const m = document.getElementById('diffeqMenu'); if (m) m.scrollTop = m.scrollHeight; }");
            await page.WaitForTimeoutAsync(150);
        }
    }
}
