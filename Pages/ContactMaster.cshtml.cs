using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace HRMS.Pages;

public class ContactListItem
{
    public int ContactID { get; set; }
    public string ContactCode { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Name { get; set; } = "";
    public string ContactType { get; set; } = "";
    public string ContactStatus { get; set; } = "Active";
    public string Mobile { get; set; } = "";
    public string Email { get; set; } = "";
}

public class GhaziResponsibleInput
{
    public int ContactGhaziResponsibleID { get; set; }
    public int PositionID { get; set; }
    public int EmployeeID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? Expiration { get; set; }
    public string Purpose { get; set; } = "";
}

public class ContactInput
{
    public int ContactID { get; set; }
    public string ContactCode { get; set; } = "";
    public int CustomerID { get; set; }
    public string ContactFor { get; set; } = "";
    public string ContactType { get; set; } = "Customer";
    public string ContactStatus { get; set; } = "Active";
    public string Name { get; set; } = "";
    public string SearchName { get; set; } = "";
    public int GenderID { get; set; }
    public string MaritalStatus { get; set; } = "";
    public string ProfessionalTitle { get; set; } = "";
    public string Department { get; set; } = "";
    public string OfficeLocation { get; set; } = "";
    public string AvailableFrom { get; set; } = "";
    public string AvailableTo { get; set; } = "";
    public string ReportToManagerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Email { get; set; } = "";
    public string Whatsapp { get; set; } = "";
    public string URL { get; set; } = "";
    public string Fax { get; set; } = "";
}

public class ContactMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    private static readonly Regex NameRegex = new(@"^[a-zA-Z0-9\s\-_.&]+$", RegexOptions.Compiled);

    public static readonly string[] ContactTypes =
        { "Customer", "Vendor", "Prospect", "Consultant", "Guest", "Others" };

    public static readonly string[] ContactStatuses = { "Active", "Inactive" };

    public static readonly string[] MaritalStatuses =
        { "Single", "Married", "Divorced", "Widowed", "Other" };

    public ContactMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public List<ContactListItem> Contacts { get; set; } = new();
    public ContactInput Input { get; set; } = new();
    public List<LookupItem> Customers { get; set; } = new();
    public List<LookupItem> Genders { get; set; } = new();
    public List<LookupItem> Positions { get; set; } = new();
    public List<GhaziResponsibleInput> GhaziResponsibles { get; set; } = new();
    public IReadOnlyList<string> ContactTypeOptions => ContactTypes;
    public IReadOnlyList<string> ContactStatusOptions => ContactStatuses;
    public IReadOnlyList<string> MaritalStatusOptions => MaritalStatuses;
    public string[] GhaziPurposeOptions { get; } =
        { "Primary", "Secondary", "Backup", "Escalation", "Other" };
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newContact)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newContact == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                LoadGhaziPositions();
                LoadGhaziResponsibles(editId.Value);
                EditMode = true;
            }
            else
            {
                Input.ContactCode = GenerateNextContactCode();
            }
        }
        else
        {
            LoadContacts();
        }
    }

    public IActionResult OnPost(
        int ContactID,
        bool EditMode,
        int CustomerID,
        string ContactFor,
        string ContactType,
        string ContactStatus,
        string Name,
        string SearchName,
        int GenderID,
        string MaritalStatus,
        string ProfessionalTitle,
        string Department,
        string OfficeLocation,
        string AvailableFrom,
        string AvailableTo,
        string ReportToManagerName,
        string Phone,
        string Mobile,
        string Email,
        string Whatsapp,
        string URL,
        string Fax)
    {
        this.EditMode = EditMode;
        Input = new ContactInput
        {
            ContactID = ContactID,
            CustomerID = CustomerID,
            ContactFor = ContactFor?.Trim() ?? "",
            ContactType = string.IsNullOrWhiteSpace(ContactType) ? "Customer" : ContactType.Trim(),
            ContactStatus = string.IsNullOrWhiteSpace(ContactStatus) ? "Active" : ContactStatus.Trim(),
            Name = Name?.Trim() ?? "",
            SearchName = SearchName?.Trim() ?? "",
            GenderID = GenderID,
            MaritalStatus = MaritalStatus?.Trim() ?? "",
            ProfessionalTitle = ProfessionalTitle?.Trim() ?? "",
            Department = Department?.Trim() ?? "",
            OfficeLocation = OfficeLocation?.Trim() ?? "",
            AvailableFrom = AvailableFrom?.Trim() ?? "",
            AvailableTo = AvailableTo?.Trim() ?? "",
            ReportToManagerName = ReportToManagerName?.Trim() ?? "",
            Phone = Phone?.Trim() ?? "",
            Mobile = Mobile?.Trim() ?? "",
            Email = Email?.Trim() ?? "",
            Whatsapp = Whatsapp?.Trim() ?? "",
            URL = URL?.Trim() ?? "",
            Fax = Fax?.Trim() ?? ""
        };

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            SetFormError("Contact name is required.");
            return Page();
        }

        if (!NameRegex.IsMatch(Input.Name))
        {
            SetFormError("Contact name must be alphanumeric (letters, numbers, spaces, - _ . & allowed).");
            return Page();
        }

        if (!ContactTypes.Contains(Input.ContactType, StringComparer.OrdinalIgnoreCase))
        {
            SetFormError("Invalid contact type.");
            return Page();
        }

        if (!ContactStatuses.Contains(Input.ContactStatus, StringComparer.OrdinalIgnoreCase))
        {
            SetFormError("Invalid contact status.");
            return Page();
        }

        if (!InputValidators.TryValidateContactMasterFields(
                Input.Email, Input.Phone, Input.Mobile, Input.Whatsapp, Input.Fax, out var fieldError))
        {
            SetFormError(fieldError!);
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input, EditMode);
            TempData["Alert"] = EditMode ? "Contact updated successfully." : "Contact created successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.ContactID });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            SetFormError("A contact with this ID already exists.");
            return Page();
        }
        catch (Exception ex)
        {
            SetFormError("Error: " + ex.Message);
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblContactMaster
                SET ContactStatus = 'Inactive',
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ContactID = @ContactID;", conn);
            cmd.Parameters.AddWithValue("@ContactID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Contact deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing contact: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SetFormError(string message)
    {
        AlertMessage = message;
        AlertType = "error";
        LoadLookups();
        ShowForm = true;
        if (!EditMode)
            Input.ContactCode = GenerateNextContactCode();
    }

    private void SaveRecord(SqlConnection conn, ContactInput input, bool editMode)
    {
        if (editMode && input.ContactID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblContactMaster
                SET CustomerID = @CustomerID,
                    ContactFor = @ContactFor,
                    ContactType = @ContactType,
                    ContactStatus = @ContactStatus,
                    Name = @Name,
                    SearchName = @SearchName,
                    GenderID = @GenderID,
                    MaritalStatus = @MaritalStatus,
                    ProfessionalTitle = @ProfessionalTitle,
                    Department = @Department,
                    OfficeLocation = @OfficeLocation,
                    AvailableFrom = @AvailableFrom,
                    AvailableTo = @AvailableTo,
                    ReportToManagerName = @ReportToManagerName,
                    Phone = @Phone,
                    Mobile = @Mobile,
                    Email = @Email,
                    Whatsapp = @Whatsapp,
                    URL = @URL,
                    Fax = @Fax,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ContactID = @ContactID;", conn);
            BindParams(cmd, input);
            cmd.Parameters.AddWithValue("@ContactID", input.ContactID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        input.ContactCode = GenerateNextContactCode(conn);
        using var ins = new SqlCommand(@"
            INSERT INTO tblContactMaster
                (ContactCode, CustomerID, ContactFor, ContactType, ContactStatus, Name, SearchName,
                 GenderID, MaritalStatus, ProfessionalTitle, Department, OfficeLocation,
                 AvailableFrom, AvailableTo, ReportToManagerName,
                 Phone, Mobile, Email, Whatsapp, URL, Fax,
                 CreatedOn, CreatedByUserID)
            VALUES
                (@ContactCode, @CustomerID, @ContactFor, @ContactType, @ContactStatus, @Name, @SearchName,
                 @GenderID, @MaritalStatus, @ProfessionalTitle, @Department, @OfficeLocation,
                 @AvailableFrom, @AvailableTo, @ReportToManagerName,
                 @Phone, @Mobile, @Email, @Whatsapp, @URL, @Fax,
                 GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        BindParams(ins, input);
        ins.Parameters.AddWithValue("@ContactCode", input.ContactCode);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.ContactID = (int)ins.ExecuteScalar()!;
    }

    private static void BindParams(SqlCommand cmd, ContactInput input)
    {
        cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID > 0 ? input.CustomerID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ContactFor", string.IsNullOrWhiteSpace(input.ContactFor) ? DBNull.Value : input.ContactFor);
        cmd.Parameters.AddWithValue("@ContactType", input.ContactType);
        cmd.Parameters.AddWithValue("@ContactStatus", input.ContactStatus);
        cmd.Parameters.AddWithValue("@Name", input.Name);
        cmd.Parameters.AddWithValue("@SearchName", string.IsNullOrWhiteSpace(input.SearchName) ? DBNull.Value : input.SearchName);
        cmd.Parameters.AddWithValue("@GenderID", input.GenderID > 0 ? input.GenderID : DBNull.Value);
        cmd.Parameters.AddWithValue("@MaritalStatus", string.IsNullOrWhiteSpace(input.MaritalStatus) ? DBNull.Value : input.MaritalStatus);
        cmd.Parameters.AddWithValue("@ProfessionalTitle", string.IsNullOrWhiteSpace(input.ProfessionalTitle) ? DBNull.Value : input.ProfessionalTitle);
        cmd.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(input.Department) ? DBNull.Value : input.Department);
        cmd.Parameters.AddWithValue("@OfficeLocation", string.IsNullOrWhiteSpace(input.OfficeLocation) ? DBNull.Value : input.OfficeLocation);
        cmd.Parameters.AddWithValue("@AvailableFrom", TimeParam(input.AvailableFrom));
        cmd.Parameters.AddWithValue("@AvailableTo", TimeParam(input.AvailableTo));
        cmd.Parameters.AddWithValue("@ReportToManagerName", string.IsNullOrWhiteSpace(input.ReportToManagerName) ? DBNull.Value : input.ReportToManagerName);
        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(input.Phone) ? DBNull.Value : input.Phone);
        cmd.Parameters.AddWithValue("@Mobile", string.IsNullOrWhiteSpace(input.Mobile) ? DBNull.Value : input.Mobile);
        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(input.Email) ? DBNull.Value : input.Email);
        cmd.Parameters.AddWithValue("@Whatsapp", string.IsNullOrWhiteSpace(input.Whatsapp) ? DBNull.Value : input.Whatsapp);
        cmd.Parameters.AddWithValue("@URL", string.IsNullOrWhiteSpace(input.URL) ? DBNull.Value : input.URL);
        cmd.Parameters.AddWithValue("@Fax", string.IsNullOrWhiteSpace(input.Fax) ? DBNull.Value : input.Fax);
    }

    private static object TimeParam(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return TimeSpan.TryParse(value, out var t) ? t : DBNull.Value;
    }

    private static string FormatTime(object? value)
    {
        if (value == null || value == DBNull.Value) return "";
        if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
        return value.ToString() ?? "";
    }

    private string GenerateNextContactCode(SqlConnection? conn = null)
    {
        var ownsConnection = conn == null;
        conn ??= new SqlConnection(_conn);
        if (ownsConnection) conn.Open();

        try
        {
            using var cmd = new SqlCommand(@"
                SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(ContactCode, 4, 10) AS INT)), 0)
                FROM tblContactMaster
                WHERE ContactCode LIKE 'CNT[0-9]%';", conn);
            var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"CNT{next:D6}";
        }
        finally
        {
            if (ownsConnection) conn.Dispose();
        }
    }

    private void LoadContacts()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT c.ContactID, c.ContactCode, ISNULL(cu.CustomerCode, ''), ISNULL(cu.Name, ''),
                   c.Name, c.ContactType, c.ContactStatus, ISNULL(c.Mobile, ''), ISNULL(c.Email, '')
            FROM tblContactMaster c
            LEFT JOIN tblCustomer cu ON cu.CustomerID = c.CustomerID
            ORDER BY CASE WHEN c.ContactStatus = 'Active' THEN 0 ELSE 1 END, c.ContactCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Contacts.Add(new ContactListItem
            {
                ContactID = dr.GetInt32(0),
                ContactCode = dr.GetString(1),
                CustomerCode = dr.GetString(2),
                CustomerName = dr.GetString(3),
                Name = dr.GetString(4),
                ContactType = dr.GetString(5),
                ContactStatus = dr.GetString(6),
                Mobile = dr.GetString(7),
                Email = dr.GetString(8)
            });
        }
    }

    private void LoadForEdit(int contactId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ContactID, ContactCode, CustomerID, ContactFor, ContactType, ContactStatus,
                   Name, SearchName, GenderID, MaritalStatus, ProfessionalTitle, Department,
                   OfficeLocation, AvailableFrom, AvailableTo, ReportToManagerName,
                   Phone, Mobile, Email, Whatsapp, URL, Fax
            FROM tblContactMaster
            WHERE ContactID = @ContactID;", conn);
        cmd.Parameters.AddWithValue("@ContactID", contactId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return;

        Input = new ContactInput
        {
            ContactID = dr.GetInt32(0),
            ContactCode = dr.GetString(1),
            CustomerID = dr.IsDBNull(2) ? 0 : dr.GetInt32(2),
            ContactFor = dr.IsDBNull(3) ? "" : dr.GetString(3),
            ContactType = dr.GetString(4),
            ContactStatus = dr.GetString(5),
            Name = dr.GetString(6),
            SearchName = dr.IsDBNull(7) ? "" : dr.GetString(7),
            GenderID = dr.IsDBNull(8) ? 0 : dr.GetInt32(8),
            MaritalStatus = dr.IsDBNull(9) ? "" : dr.GetString(9),
            ProfessionalTitle = dr.IsDBNull(10) ? "" : dr.GetString(10),
            Department = dr.IsDBNull(11) ? "" : dr.GetString(11),
            OfficeLocation = dr.IsDBNull(12) ? "" : dr.GetString(12),
            AvailableFrom = FormatTime(dr.IsDBNull(13) ? null : dr.GetValue(13)),
            AvailableTo = FormatTime(dr.IsDBNull(14) ? null : dr.GetValue(14)),
            ReportToManagerName = dr.IsDBNull(15) ? "" : dr.GetString(15),
            Phone = dr.IsDBNull(16) ? "" : dr.GetString(16),
            Mobile = dr.IsDBNull(17) ? "" : dr.GetString(17),
            Email = dr.IsDBNull(18) ? "" : dr.GetString(18),
            Whatsapp = dr.IsDBNull(19) ? "" : dr.GetString(19),
            URL = dr.IsDBNull(20) ? "" : dr.GetString(20),
            Fax = dr.IsDBNull(21) ? "" : dr.GetString(21)
        };
    }

    private void LoadLookups()
    {
        Customers = LoadCustomerLookup();
        Genders = LoadLookup("tblGender", "GenderID", "GenderName");
        LoadGhaziPositions();
    }

    public JsonResult OnGetActivePositionWorker(int positionId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 e.EmployeeID, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   ISNULL(d.DepartmentName, '') AS DepartmentName,
                   ISNULL(e.Designation, '') AS Designation,
                   ISNULL(l.LocationName, '') AS LocationName
            FROM tblPositionWorkerAssignment a
            INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            WHERE a.PositionID = @PositionID AND a.IsActive = 1
              AND (a.AssignmentEndDate IS NULL OR a.AssignmentEndDate >= CAST(GETDATE() AS DATE))
            ORDER BY a.AssignmentStartDate DESC;", conn);
        cmd.Parameters.AddWithValue("@PositionID", positionId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read())
            return new JsonResult(new { employeeID = 0, message = "No active employee is assigned to this position." });

        return new JsonResult(new
        {
            employeeID = dr.GetInt32(0),
            employeeName = dr.GetString(1),
            departmentName = dr.GetString(2),
            designation = dr.GetString(3),
            locationName = dr.GetString(4)
        });
    }

    public IActionResult OnPostSaveGhaziResponsibles(int contactID, List<GhaziResponsibleInput> ghaziResponsibles)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();
            using (var del = new SqlCommand("DELETE FROM tblContactGhaziResponsible WHERE ContactID = @ContactID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ContactID", contactID);
                del.ExecuteNonQuery();
            }

            int sort = 1;
            foreach (var row in ghaziResponsibles.Where(r => r.PositionID > 0 && r.EmployeeID > 0))
            {
                using var ins = new SqlCommand(@"
                    INSERT INTO tblContactGhaziResponsible
                        (ContactID, PositionID, EmployeeID, EffectiveFrom, Expiration, Purpose, SortOrder, CreatedOn, CreatedByUserID)
                    VALUES
                        (@ContactID, @PositionID, @EmployeeID, @EffectiveFrom, @Expiration, @Purpose, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);
                ins.Parameters.AddWithValue("@ContactID", contactID);
                ins.Parameters.AddWithValue("@PositionID", row.PositionID);
                ins.Parameters.AddWithValue("@EmployeeID", row.EmployeeID);
                ins.Parameters.AddWithValue("@EffectiveFrom", row.EffectiveFrom ?? DateTime.Today);
                ins.Parameters.AddWithValue("@Expiration", row.Expiration.HasValue ? row.Expiration.Value : DBNull.Value);
                ins.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(row.Purpose) ? "Primary" : row.Purpose);
                ins.Parameters.AddWithValue("@SortOrder", sort++);
                AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
                ins.ExecuteNonQuery();
            }

            tx.Commit();
            TempData["Alert"] = "Ghazi responsible records saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving Ghazi responsibles: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = contactID });
    }

    private void LoadGhaziPositions()
    {
        Positions.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT PositionID, PositionNo + ISNULL(' – ' + NULLIF(Description, ''), '')
            FROM tblPosition WHERE IsActive = 1 ORDER BY PositionNo;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Positions.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
    }

    private void LoadGhaziResponsibles(int contactId)
    {
        GhaziResponsibles.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT g.ContactGhaziResponsibleID, g.PositionID, g.EmployeeID,
                   ISNULL(e.EmployeeCode, '') AS EmployeeCode,
                   e.FirstName + ' ' + e.LastName AS EmployeeName,
                   ISNULL(d.DepartmentName, '') AS Department,
                   ISNULL(e.Designation, '') AS Designation,
                   ISNULL(l.LocationName, '') AS Location,
                   g.EffectiveFrom, g.Expiration, g.Purpose
            FROM tblContactGhaziResponsible g
            INNER JOIN tblEmployee e ON e.EmployeeID = g.EmployeeID
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            WHERE g.ContactID = @ContactID
            ORDER BY g.SortOrder, g.ContactGhaziResponsibleID;", conn);
        cmd.Parameters.AddWithValue("@ContactID", contactId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            GhaziResponsibles.Add(new GhaziResponsibleInput
            {
                ContactGhaziResponsibleID = dr.GetInt32(0),
                PositionID = dr.GetInt32(1),
                EmployeeID = dr.GetInt32(2),
                EmployeeCode = dr.GetString(3),
                EmployeeName = dr.GetString(4),
                Department = dr.GetString(5),
                Designation = dr.GetString(6),
                Location = dr.GetString(7),
                EffectiveFrom = dr.GetDateTime(8),
                Expiration = dr.IsDBNull(9) ? null : dr.GetDateTime(9),
                Purpose = dr.IsDBNull(10) ? "" : dr.GetString(10)
            });
        }
    }

    private List<LookupItem> LoadCustomerLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CustomerID, CustomerCode, Name
            FROM tblCustomer
            WHERE IsActive = 1
            ORDER BY CustomerCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            items.Add(new LookupItem
            {
                Id = dr.GetInt32(0),
                Name = $"{dr.GetString(1)} – {dr.GetString(2)}"
            });
        }
        return items;
    }

    private List<LookupItem> LoadLookup(string table, string idCol, string nameCol)
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idCol}, {nameCol}
            FROM {table}
            WHERE IsActive = 1
            ORDER BY {nameCol};", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        return items;
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }
}
