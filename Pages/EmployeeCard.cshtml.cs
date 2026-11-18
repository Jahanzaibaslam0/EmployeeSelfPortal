using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class EmployeeCardData
{
    public int EmployeeID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Designation { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string OfficeLocation { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string BloodGroup { get; set; } = "";
    public string DateOfJoining { get; set; } = "";
    public string ValidityPeriod { get; set; } = "";
    public bool IsVisitorCard { get; set; }
    public string CardTitle { get; set; } = "Employee ID Card";
    public string PhotoUrl { get; set; } = "";
    public string Initials { get; set; } = "?";
    public string QrPayload { get; set; } = "";
    public string Status { get; set; } = "";
}

public class EmployeeCardPickerItem
{
    public int EmployeeID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
}

public class EmployeeCardModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly EmployeeProfileAccessService _profileAccess;

    public EmployeeCardModel(IConfiguration config, AuthService auth, EmployeeProfileAccessService profileAccess)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _profileAccess = profileAccess;
    }

    public string PageTitle => "Employee / Visitor Card";
    public EmployeeCardData? Card { get; set; }
    public List<EmployeeCardPickerItem> Employees { get; set; } = new();
    public bool ShowPicker { get; set; }
    public bool CanBrowseCards { get; set; }
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(int? id)
    {
        if (!_auth.IsLoggedIn)
            return RedirectToPage("/Login");

        if (!_profileAccess.CanAccessEmployeeCard())
        {
            TempData["Alert"] = EmployeeProfileAccessService.NotSynchronizedMessage;
            TempData["AlertType"] = "warning";
            return RedirectToPage("/UserProfile");
        }

        CanBrowseCards = _profileAccess.HasFullEmployeeMasterAccess();

        if (id.HasValue && id > 0)
        {
            if (!_profileAccess.CanViewEmployee(id.Value))
            {
                TempData["Alert"] = "You do not have permission to view this employee card.";
                TempData["AlertType"] = "error";
                return RedirectToPage("/UserProfile");
            }

            Card = LoadCard(id.Value);
            if (Card == null)
            {
                ErrorMessage = "Employee record not found.";
                return Page();
            }

            return Page();
        }

        if (_profileAccess.HasFullEmployeeMasterAccess())
        {
            ShowPicker = true;
            Employees = LoadEmployeePickerList();
            return Page();
        }

        var ownId = _profileAccess.GetLinkedEmployeeId();
        if (ownId is > 0)
            return RedirectToPage(new { id = ownId.Value });

        TempData["Alert"] = EmployeeProfileAccessService.NotSynchronizedMessage;
        TempData["AlertType"] = "warning";
        return RedirectToPage("/UserProfile");
    }

    private List<EmployeeCardPickerItem> LoadEmployeePickerList()
    {
        var list = new List<EmployeeCardPickerItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT e.EmployeeID, e.EmployeeCode,
                   LTRIM(RTRIM(ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,''))) AS FullName,
                   ISNULL(d.DepartmentName, '') AS DepartmentName
            FROM tblEmployee e
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            WHERE e.Status = 'Active'
            ORDER BY e.EmployeeCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            list.Add(new EmployeeCardPickerItem
            {
                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                FullName = dr["FullName"].ToString() ?? "",
                DepartmentName = dr["DepartmentName"].ToString() ?? ""
            });
        }
        return list;
    }

    private EmployeeCardData? LoadCard(int employeeId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                e.EmployeeID,
                e.EmployeeCode,
                LTRIM(RTRIM(ISNULL(NULLIF(LTRIM(RTRIM(e.DisplayName)), ''),
                    ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'')))) AS FullName,
                ISNULL(e.FirstName, '') AS FirstName,
                ISNULL(e.LastName, '') AS LastName,
                ISNULL(e.Designation, '') AS Designation,
                ISNULL(d.DepartmentName, '') AS DepartmentName,
                ISNULL(le.LegalEntityName, '') AS CompanyName,
                ISNULL(loc.LocationName,
                    ISNULL(wlLoc.LocationName,
                        ISNULL(NULLIF(LTRIM(RTRIM(city.CityName)), '') + CASE WHEN city.CityName IS NOT NULL AND p.ProvinceName IS NOT NULL THEN ', ' ELSE '' END + ISNULL(p.ProvinceName, ''), ''))) AS OfficeLocation,
                ISNULL(cEmail.ContactValue, ISNULL(e.OfficialEmail, ISNULL(e.Email, ''))) AS Email,
                ISNULL(
                    NULLIF(LTRIM(RTRIM(cCell.ContactValue)), ''),
                    ISNULL(NULLIF(LTRIM(RTRIM(e.OfficialMobile)), ''),
                           ISNULL(NULLIF(LTRIM(RTRIM(e.PersonalMobile)), ''),
                                  ISNULL(e.Phone, '')))) AS Phone,
                ISNULL(bg.BloodGroupName, '') AS BloodGroup,
                e.DateOfJoining,
                e.ProbationEndDate,
                e.EmploymentStartDate,
                ISNULL(wc.WorkerCategoryName, '') AS WorkerCategoryName,
                ISNULL(et.EmploymentTypeName, '') AS EmploymentTypeName,
                ISNULL(es.EmploymentStatusName, '') AS EmploymentStatusName,
                ISNULL(e.Status, '') AS Status,
                ISNULL(e.PhotoPath, '') AS PhotoPath
            FROM tblEmployee e
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblLegalEntity le ON le.LegalEntityID = e.LegalEntityID
            LEFT JOIN tblLocation loc ON loc.LocationID = e.LocationID
            LEFT JOIN tblWorkerLocation wl ON wl.EmployeeID = e.EmployeeID
            LEFT JOIN tblLocation wlLoc ON wlLoc.LocationID = wl.PrimaryLocationID
            LEFT JOIN tblCity city ON city.CityID = e.CityID
            LEFT JOIN tblProvince p ON p.ProvinceID = e.ProvinceID
            LEFT JOIN tblBloodGroup bg ON bg.BloodGroupID = e.BloodGroupID
            LEFT JOIN tblWorkerCategory wc ON wc.WorkerCategoryID = e.WorkerCategoryID
            LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = e.EmploymentTypeID
            LEFT JOIN tblEmploymentStatus es ON es.EmploymentStatusID = e.EmploymentStatusID
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID AND ContactType = 'OfficialEmail'
                  AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
                ORDER BY IsPrimary DESC, ContactID DESC
            ) cEmail
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID
                  AND ContactType IN ('OfficialMobile', 'PersonalMobile', 'WhatsApp')
                  AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
                ORDER BY
                    CASE
                        WHEN ContactType = 'OfficialMobile' AND IsPrimary = 1 THEN 0
                        WHEN ContactType = 'OfficialMobile' THEN 1
                        WHEN IsPrimary = 1 THEN 2
                        WHEN ContactType = 'PersonalMobile' THEN 3
                        ELSE 4
                    END,
                    ContactID DESC
            ) cCell
            WHERE e.EmployeeID = @EmployeeID;", conn);

        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read())
            return null;

        var firstName = dr["FirstName"].ToString() ?? "";
        var lastName = dr["LastName"].ToString() ?? "";
        var workerCategory = dr["WorkerCategoryName"].ToString() ?? "";
        var employmentType = dr["EmploymentTypeName"].ToString() ?? "";
        var employmentStatus = dr["EmploymentStatusName"].ToString() ?? "";
        var isVisitor = IsVisitorRecord(workerCategory, employmentType, employmentStatus);

        var photoPath = dr["PhotoPath"].ToString() ?? "";
        var photoUrl = ResolvePhotoUrl(photoPath);

        DateTime? dateOfJoining = dr["DateOfJoining"] == DBNull.Value ? null : Convert.ToDateTime(dr["DateOfJoining"]);
        DateTime? probationEnd = dr["ProbationEndDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["ProbationEndDate"]);
        DateTime? employmentStart = dr["EmploymentStartDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["EmploymentStartDate"]);

        var employeeCode = dr["EmployeeCode"].ToString() ?? "";

        return new EmployeeCardData
        {
            EmployeeID = employeeId,
            EmployeeCode = employeeCode,
            FullName = dr["FullName"].ToString() ?? "",
            Designation = dr["Designation"].ToString() ?? "",
            DepartmentName = dr["DepartmentName"].ToString() ?? "",
            CompanyName = dr["CompanyName"].ToString() ?? "",
            OfficeLocation = dr["OfficeLocation"].ToString() ?? "",
            Email = dr["Email"].ToString() ?? "",
            Phone = dr["Phone"].ToString() ?? "",
            BloodGroup = dr["BloodGroup"].ToString() ?? "",
            DateOfJoining = dateOfJoining?.ToString("dd MMM yyyy") ?? "",
            ValidityPeriod = BuildValidityPeriod(isVisitor, employmentStart, dateOfJoining, probationEnd),
            IsVisitorCard = isVisitor,
            CardTitle = isVisitor ? "Visitor ID Card" : "Employee ID Card",
            PhotoUrl = photoUrl,
            Initials = BuildInitials(firstName, lastName),
            QrPayload = employeeCode,
            Status = dr["Status"].ToString() ?? ""
        };
    }

    private static bool IsVisitorRecord(string workerCategory, string employmentType, string employmentStatus)
    {
        static bool ContainsVisitor(string value)
            => !string.IsNullOrWhiteSpace(value)
               && value.Contains("visitor", StringComparison.OrdinalIgnoreCase);

        return ContainsVisitor(workerCategory)
               || ContainsVisitor(employmentType)
               || ContainsVisitor(employmentStatus);
    }

    private static string BuildValidityPeriod(bool isVisitor, DateTime? employmentStart, DateTime? dateOfJoining, DateTime? probationEnd)
    {
        if (!isVisitor)
            return "";

        if (probationEnd.HasValue)
            return $"Valid until {probationEnd.Value:dd MMM yyyy}";

        var start = employmentStart ?? dateOfJoining;
        if (start.HasValue)
            return $"Valid until {start.Value.AddDays(90):dd MMM yyyy}";

        return "Validity as per HR approval";
    }

    private static string ResolvePhotoUrl(string photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
            return "";

        return photoPath.StartsWith('/') ? photoPath : "/" + photoPath.TrimStart('/');
    }

    private static string BuildInitials(string firstName, string lastName)
    {
        var parts = new[] { firstName, lastName }
            .Select(p => string.IsNullOrWhiteSpace(p) ? "" : p.Trim()[0].ToString().ToUpperInvariant())
            .Where(c => c.Length > 0)
            .Take(2);
        var initials = string.Concat(parts);
        return string.IsNullOrEmpty(initials) ? "?" : initials;
    }
}
