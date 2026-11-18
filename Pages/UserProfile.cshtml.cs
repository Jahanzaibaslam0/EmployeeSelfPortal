using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class UserProfileModel : PageModel
{
    private readonly EmployeeProfileAccessService _profileAccess;

    public UserProfileModel(EmployeeProfileAccessService profileAccess)
    {
        _profileAccess = profileAccess;
    }

    public string PageTitle => "My Profile";
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "info";

    public IActionResult OnGet()
    {
        if (!_profileAccess.CanAccessEmployeeMasterPage())
            return Redirect("/Home?accessDenied=1");

        var employeeId = _profileAccess.GetLinkedEmployeeId();
        if (employeeId.HasValue && employeeId.Value > 0)
            return RedirectToPage("/EmployeeMaster", new { editId = employeeId.Value });

        if (TempData.ContainsKey("Alert"))
        {
            AlertMessage = TempData["Alert"]?.ToString() ?? "";
            AlertType = TempData["AlertType"]?.ToString() ?? "info";
        }
        else
        {
            AlertMessage = "No employee record is linked to your user account. Please contact HR to assign your User ID in Employee Master.";
            AlertType = "warning";
        }

        return Page();
    }
}
