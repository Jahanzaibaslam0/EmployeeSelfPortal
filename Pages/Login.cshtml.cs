using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class LoginModel : PageModel
{
    private readonly AuthService _auth;
    private readonly AuditService _audit;

    public LoginModel(AuthService auth, AuditService audit)
    {
        _auth = auth;
        _audit = audit;
    }

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string ErrorMessage { get; set; } = "";
    public string ReturnUrl { get; set; } = "/Home";

    public IActionResult OnGet(string? returnUrl)
    {
        try
        {
            // Ensure DB has admin account before showing login (review mode)
            _auth.EnsureUserTableAndAdmin();
        }
        catch
        {
            // Connection issues will show on Sign In
        }

        if (_auth.IsLoggedIn)
            return RedirectToPage("Index");

        ReturnUrl = NormalizeReturnUrl(returnUrl);
        return Page();
    }

    public IActionResult OnPost()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl);

        var result = _auth.Login(Username, Password);
        if (!result.Success)
        {
            _audit.LogLogin(Username.Trim(), false, message: result.Message);
            ErrorMessage = result.Message;
            return Page();
        }

        HttpContext.Session.SetInt32("ShowNotificationPopup", 1);
        HttpContext.Session.SetInt32("ShowMemorandumPopup", 1);

        if (!_auth.IsAdmin && !_auth.LinkedEmployeeId.HasValue)
            HttpContext.Session.SetInt32("ShowProfileSyncWarning", 1);

        return RedirectToPage("Index");
    }

    public IActionResult OnGetLogout()
    {
        _audit.LogLogout();
        _auth.Logout();
        return RedirectToPage("/Login");
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/" || returnUrl.StartsWith("/EmployeeMaster", StringComparison.OrdinalIgnoreCase))
            return "/Home";
        return returnUrl;
    }
}
