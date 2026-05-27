using System.Threading.Tasks;                
using Microsoft.AspNetCore.Identity;         
using Microsoft.AspNetCore.Mvc;              
using Microsoft.AspNetCore.Mvc.RazorPages;    
using PtixiakiReservations.Models;

public class ResetPasswordWith2faModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordWith2faModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string twoFactorCode)
    {
        var email = TempData["ResetEmail"] as string;
        if (string.IsNullOrEmpty(email)) return RedirectToPage("./ForgotPassword");

        var user = await _userManager.FindByEmailAsync(email);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", twoFactorCode);

        if (isValid)
        {
            // 1. Generate the actual Reset Token (usually in the link)
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 2. Pass everything to the final reset page via TempData
            TempData["ResetEmail"] = email;
            TempData["ResetToken"] = resetToken;

            return RedirectToPage("./ResetPassword");
        }

        ModelState.AddModelError(string.Empty, "Invalid code.");
        TempData.Keep(); 
        return Page();
    }
}