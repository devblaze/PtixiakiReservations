using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PtixiakiReservations.Models; // Change if your ApplicationUser is elsewhere

namespace PtixiakiReservations.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class VerifyCodeModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<VerifyCodeModel> _logger;

        public VerifyCodeModel(SignInManager<ApplicationUser> signInManager, ILogger<VerifyCodeModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Security Code")]
            public string Code { get; set; }

            public bool RememberMe { get; set; }
        }

        // This runs when they arrive from the Login page
        public void OnGet(string returnUrl = null, bool rememberMe = false)
        {
            ReturnUrl = returnUrl;
            Input = new InputModel { RememberMe = rememberMe };
        }

        // This runs when they click "Verify and Log in"
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            ReturnUrl = ReturnUrl ?? Url.Content("~/");

            // Verify the 6-digit code
            var result = await _signInManager.TwoFactorSignInAsync(
                "Email", 
                Input.Code, 
                Input.RememberMe, 
                rememberClient: false); // rememberClient skips 2FA on this browser next time if true

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with 2FA.");
                return LocalRedirect(ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid security code.");
                return Page();
            }
        }
    }
}