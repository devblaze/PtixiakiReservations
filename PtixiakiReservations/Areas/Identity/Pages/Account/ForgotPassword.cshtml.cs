using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using PtixiakiReservations.Models;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

       public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null) return RedirectToPage("./ForgotPasswordConfirmation");

            // 1. Generate 2FA Code
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            string subject = "Your EventSphere Security Code";
            string message = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Security Verification</h2>
                    <p>You requested a verification code.</p>
                    <p>Your code is: <strong style='font-size: 24px; color: #4F46E5;'>{code}</strong></p>
                    <p style='font-size: 12px; color: #666;'>If you did not request this code, please ignore this email.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, message);
                TempData["ResetEmail"] = Input.Email;
                return RedirectToPage("./ResetPasswordWith2fa");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL ERROR: {ex.Message}");
                
                ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again later.");
                return Page();
            } 
        }
    }
}
