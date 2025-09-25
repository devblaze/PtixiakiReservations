using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    public class TestBase
    {
        protected IPlaywright Playwright = null!;
        protected IBrowser Browser = null!;
        protected IBrowserContext Context = null!;
        protected IPage Page = null!;

        protected string BaseUrl = "https://localhost:5001"; // Update this to match your local development URL

        [SetUp]
        public async Task Setup()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            // You can change the browser here (Chromium, Firefox, Webkit)
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // Set to false if you want to see the browser
                SlowMo = 100 // Slow down actions by 100ms for debugging
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true, // For local development
                RecordVideoDir = "videos/", // Optional: Record videos of tests
                RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 }
            });

            Page = await Context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            // Take screenshot on failure
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = $"screenshots/{TestContext.CurrentContext.Test.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    FullPage = true
                });
            }

            await Context.CloseAsync();
            await Browser.CloseAsync();
        }

        protected async Task LoginAsync(string email, string password)
        {
            await Page.GotoAsync($"{BaseUrl}/Identity/Account/Login");
            await Page.FillAsync("input[name='Input.Email']", email);
            await Page.FillAsync("input[name='Input.Password']", password);
            await Page.ClickAsync("button[type='submit']:has-text('Log in')");

            // Wait for navigation after login
            await Page.WaitForURLAsync(url => !url.Contains("/Identity/Account/Login"));
        }

        protected async Task RegisterUserAsync(string email, string password, string firstName, string lastName)
        {
            await Page.GotoAsync($"{BaseUrl}/Identity/Account/Register");

            await Page.FillAsync("input[name='Input.Email']", email);
            await Page.FillAsync("input[name='Input.Password']", password);
            await Page.FillAsync("input[name='Input.ConfirmPassword']", password);

            // Fill additional fields if they exist
            var firstNameInput = await Page.QuerySelectorAsync("input[name='Input.FirstName']");
            if (firstNameInput != null)
            {
                await Page.FillAsync("input[name='Input.FirstName']", firstName);
            }

            var lastNameInput = await Page.QuerySelectorAsync("input[name='Input.LastName']");
            if (lastNameInput != null)
            {
                await Page.FillAsync("input[name='Input.LastName']", lastName);
            }

            await Page.ClickAsync("button[type='submit']:has-text('Register')");

            // Wait for registration to complete
            await Page.WaitForURLAsync(url => !url.Contains("/Identity/Account/Register"));
        }

        protected async Task WaitForToastOrAlert(string message = null)
        {
            // Wait for common notification patterns
            var toastSelector = ".toast, .alert, .notification, .message";

            if (message != null)
            {
                await Page.WaitForSelectorAsync($"{toastSelector}:has-text('{message}')");
            }
            else
            {
                await Page.WaitForSelectorAsync(toastSelector);
            }
        }

        protected string GenerateTestEmail()
        {
            return $"test_{Guid.NewGuid():N}@example.com";
        }

        protected async Task<bool> ElementExistsAsync(string selector)
        {
            var element = await Page.QuerySelectorAsync(selector);
            return element != null;
        }

        protected async Task SelectOptionByTextAsync(string selector, string text)
        {
            await Page.SelectOptionAsync(selector, new SelectOptionValue { Label = text });
        }

        protected async Task ScrollToElementAsync(string selector)
        {
            await Page.EvalOnSelectorAsync(selector, "element => element.scrollIntoView()");
        }
    }
}