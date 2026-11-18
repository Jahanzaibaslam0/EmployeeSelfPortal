using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class PositionListItem
{
    public int PositionID { get; set; }
    public string PositionNo { get; set; } = "";
    public string Description { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string TitleName { get; set; } = "";
    public string PositionTypeName { get; set; } = "";
    public string PositionDuration { get; set; } = "";
    public DateTime? PositionStartDate { get; set; }
    public DateTime? PositionEndDate { get; set; }
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
}

public class PositionInput
{
    public int PositionID { get; set; }
    public string PositionNo { get; set; } = "";
    public string Description { get; set; } = "";
    public int EmailEmployeeID { get; set; }
    public int JobID { get; set; }
    public int DepartmentID { get; set; }
    public int ReportsToPositionID { get; set; }
    public int TitleID { get; set; }
    public int PositionTypeID { get; set; }
    public string PositionDuration { get; set; } = "";
    public DateTime? PositionStartDate { get; set; }
    public DateTime? PositionEndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PositionAssignmentInput
{
    public int AssignmentID { get; set; }
    public int EmployeeID { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime? AssignmentStartDate { get; set; }
    public DateTime? AssignmentEndDate { get; set; }
    public string Reason { get; set; } = "";
}

public class EmailLookupItem
{
    public int EmployeeID { get; set; }
    public string Email { get; set; } = "";
    public string Label { get; set; } = "";
}

public class LinkedEntitlementItem
{
    public int LinkID { get; set; }
    public int BenefitEntitlementID { get; set; }
    public string EntitlementName { get; set; } = "";
    public string AliasName { get; set; } = "";
}

public class WorkerEntitlementSourceItem
{
    public string EmployeeName { get; set; } = "";
    public string EntitlementName { get; set; } = "";
}

public class PositionMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public static readonly string[] PositionDurations =
        { "Permanent", "Fixed Term", "Temporary", "Contract", "Internship" };

    public PositionMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public bool ShowForm { get; set; }
    public bool EditMode => Input.PositionID > 0;

    public PositionInput Input { get; set; } = new();
    public List<PositionListItem> Positions { get; set; } = new();
    public List<PositionAssignmentInput> Assignments { get; set; } = new();
    public List<LinkedEntitlementItem> LinkedEntitlements { get; set; } = new();

    public List<EmailLookupItem> EmailLookups { get; set; } = new();
    public List<LookupItem> Jobs { get; set; } = new();
    public List<LookupItem> Departments { get; set; } = new();
    public List<LookupItem> ReportToPositions { get; set; } = new();
    public List<LookupItem> Titles { get; set; } = new();
    public List<LookupItem> PositionTypes { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> AvailableEntitlements { get; set; } = new();
    public List<WorkerEntitlementSourceItem> WorkerEntitlementSources { get; set; } = new();

    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";
    public int SelectedEntitlementID { get; set; }

    public void OnGet(bool? newPosition, int? editId)
    {
        LoadAlert();
        LoadLookupLists();

        if (newPosition == true || (editId.HasValue && editId > 0))
        {
            ShowForm = true;
            if (editId.HasValue && editId > 0)
            {
                LoadPosition(editId.Value);
                LoadAssignments(editId.Value);
                LoadLinkedEntitlements(editId.Value);
                LoadAvailableEntitlements(editId.Value);
                LoadWorkerEntitlementSources(editId.Value);
            }
            else
            {
                Input.PositionNo = GenerateNextPositionNo();
                Input.PositionStartDate = DateTime.Today;
            }
        }
        else
        {
            LoadPositionList();
        }
    }

    public IActionResult OnPostSavePosition(
        int positionID, string positionNo, string description,
        int emailEmployeeID, int jobID, int departmentID, int reportsToPositionID,
        int titleID, int positionTypeID, string positionDuration,
        DateTime? positionStartDate, DateTime? positionEndDate, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(positionNo))
        {
            TempData["Alert"] = "Position number is required.";
            TempData["AlertType"] = "error";
            return RedirectToForm(positionID);
        }

        if (reportsToPositionID > 0 && reportsToPositionID == positionID)
        {
            TempData["Alert"] = "A position cannot report to itself.";
            TempData["AlertType"] = "error";
            return RedirectToForm(positionID);
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (positionID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblPosition SET
                        PositionNo = @PositionNo,
                        Description = @Description,
                        EmailEmployeeID = @EmailEmployeeID,
                        JobID = @JobID,
                        DepartmentID = @DepartmentID,
                        ReportsToPositionID = @ReportsToPositionID,
                        TitleID = @TitleID,
                        PositionTypeID = @PositionTypeID,
                        PositionDuration = @PositionDuration,
                        PositionStartDate = @PositionStartDate,
                        PositionEndDate = @PositionEndDate,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE PositionID = @PositionID;", conn);
                BindPositionParams(cmd, positionID, positionNo, description, emailEmployeeID, jobID,
                    departmentID, reportsToPositionID, titleID, positionTypeID, positionDuration,
                    positionStartDate, positionEndDate, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Position updated successfully.";
                positionID = positionID;
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblPosition
                        (PositionNo, Description, EmailEmployeeID, JobID, DepartmentID,
                         ReportsToPositionID, TitleID, PositionTypeID, PositionDuration,
                         PositionStartDate, PositionEndDate, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@PositionNo, @Description, @EmailEmployeeID, @JobID, @DepartmentID,
                         @ReportsToPositionID, @TitleID, @PositionTypeID, @PositionDuration,
                         @PositionStartDate, @PositionEndDate, @IsActive, GETDATE(), @CreatedByUserID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                BindPositionParams(cmd, 0, positionNo, description, emailEmployeeID, jobID,
                    departmentID, reportsToPositionID, titleID, positionTypeID, positionDuration,
                    positionStartDate, positionEndDate, isActive, isInsert: true);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                positionID = Convert.ToInt32(cmd.ExecuteScalar());
                TempData["Alert"] = "Position created successfully.";
            }

            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = positionID });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "Position number already exists.";
            TempData["AlertType"] = "error";
            return RedirectToForm(positionID);
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToForm(positionID);
        }
    }

    public IActionResult OnPostSaveAssignments(int positionID, List<PositionAssignmentInput> assignments)
    {
        if (positionID <= 0)
        {
            TempData["Alert"] = "Save the position before adding worker assignments.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { newPosition = true });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            using (var del = new SqlCommand("DELETE FROM tblPositionWorkerAssignment WHERE PositionID = @PositionID;", conn, tx))
            {
                del.Parameters.AddWithValue("@PositionID", positionID);
                del.ExecuteNonQuery();
            }

            if (assignments != null)
            {
                int sort = 0;
                foreach (var row in assignments.Where(a => a.EmployeeID > 0))
                {
                    using var ins = new SqlCommand(@"
                        INSERT INTO tblPositionWorkerAssignment
                            (PositionID, EmployeeID, AssignmentStartDate, AssignmentEndDate, Reason, SortOrder, CreatedOn, CreatedByUserID)
                        VALUES
                            (@PositionID, @EmployeeID, @StartDate, @EndDate, @Reason, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);
                    ins.Parameters.AddWithValue("@PositionID", positionID);
                    ins.Parameters.AddWithValue("@EmployeeID", row.EmployeeID);
                    ins.Parameters.AddWithValue("@StartDate", (object?)row.AssignmentStartDate?.Date ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@EndDate", (object?)row.AssignmentEndDate?.Date ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(row.Reason) ? DBNull.Value : row.Reason.Trim());
                    ins.Parameters.AddWithValue("@SortOrder", ++sort);
                    AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
                    ins.ExecuteNonQuery();
                }
            }

            tx.Commit();
            TempData["Alert"] = "Worker assignments saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving assignments: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = positionID });
    }

    public IActionResult OnPostLinkEntitlement(int positionID, int benefitEntitlementID)
    {
        if (positionID <= 0 || benefitEntitlementID <= 0)
        {
            TempData["Alert"] = "Select a benefit entitlement to link.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = positionID });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS (
                    SELECT 1 FROM tblPositionBenefitEntitlement
                    WHERE PositionID = @PositionID AND BenefitEntitlementID = @EntitlementID)
                INSERT INTO tblPositionBenefitEntitlement
                    (PositionID, BenefitEntitlementID, CreatedOn, CreatedByUserID)
                VALUES (@PositionID, @EntitlementID, GETDATE(), @CreatedByUserID);", conn);
            cmd.Parameters.AddWithValue("@PositionID", positionID);
            cmd.Parameters.AddWithValue("@EntitlementID", benefitEntitlementID);
            AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Benefit entitlement linked successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = positionID });
    }

    public IActionResult OnPostUnlinkEntitlement(int positionID, int linkID)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                DELETE FROM tblPositionBenefitEntitlement
                WHERE PositionBenefitEntitlementID = @LinkID AND PositionID = @PositionID;", conn);
            cmd.Parameters.AddWithValue("@LinkID", linkID);
            cmd.Parameters.AddWithValue("@PositionID", positionID);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Benefit entitlement removed.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = positionID });
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblPosition SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE PositionID = @PositionID;", conn);
            cmd.Parameters.AddWithValue("@PositionID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Position deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void BindPositionParams(SqlCommand cmd, int positionID, string positionNo, string description,
        int emailEmployeeID, int jobID, int departmentID, int reportsToPositionID,
        int titleID, int positionTypeID, string positionDuration,
        DateTime? positionStartDate, DateTime? positionEndDate, bool isActive, bool isInsert = false)
    {
        if (!isInsert) cmd.Parameters.AddWithValue("@PositionID", positionID);
        cmd.Parameters.AddWithValue("@PositionNo", positionNo.Trim());
        cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.Parameters.AddWithValue("@EmailEmployeeID", emailEmployeeID > 0 ? emailEmployeeID : DBNull.Value);
        cmd.Parameters.AddWithValue("@JobID", jobID > 0 ? jobID : DBNull.Value);
        cmd.Parameters.AddWithValue("@DepartmentID", departmentID > 0 ? departmentID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReportsToPositionID", reportsToPositionID > 0 ? reportsToPositionID : DBNull.Value);
        cmd.Parameters.AddWithValue("@TitleID", titleID > 0 ? titleID : DBNull.Value);
        cmd.Parameters.AddWithValue("@PositionTypeID", positionTypeID > 0 ? positionTypeID : DBNull.Value);
        cmd.Parameters.AddWithValue("@PositionDuration", string.IsNullOrWhiteSpace(positionDuration) ? DBNull.Value : positionDuration);
        cmd.Parameters.AddWithValue("@PositionStartDate", (object?)positionStartDate?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PositionEndDate", (object?)positionEndDate?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private IActionResult RedirectToForm(int positionID)
        => RedirectToPage(positionID > 0 ? new { editId = positionID } : new { newPosition = true });

    private string GenerateNextPositionNo()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 PositionNo FROM tblPosition
            WHERE PositionNo LIKE 'EPS-%'
            ORDER BY PositionNo DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 10)
        {
            var numPart = last[4..];
            if (int.TryParse(numPart, out int num))
                return $"EPS-{(num + 1):D6}";
        }
        return "EPS-000001";
    }

    private void LoadLookupLists()
    {
        Jobs.Clear();
        Departments.Clear();
        Titles.Clear();
        PositionTypes.Clear();
        Employees.Clear();
        EmailLookups.Clear();
        ReportToPositions.Clear();

        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT JobID, JobTitle + ' (' + JobCode + ')' AS JobLabel
            FROM tblJob WHERE IsActive = 1 ORDER BY JobTitle;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Jobs.Add(new LookupItem { Id = Convert.ToInt32(dr["JobID"]), Name = dr["JobLabel"].ToString()! });
        }

        using (var cmd = new SqlCommand(@"
            SELECT DepartmentID, DepartmentName FROM tblDepartment
            WHERE IsActive = 1 ORDER BY DepartmentName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Departments.Add(new LookupItem { Id = Convert.ToInt32(dr["DepartmentID"]), Name = dr["DepartmentName"].ToString()! });
        }

        using (var cmd = new SqlCommand(@"
            SELECT DesignationLevelID, DesignationLevelName FROM tblDesignationLevel
            WHERE IsActive = 1 ORDER BY DesignationLevelName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Titles.Add(new LookupItem { Id = Convert.ToInt32(dr["DesignationLevelID"]), Name = dr["DesignationLevelName"].ToString()! });
        }

        using (var cmd = new SqlCommand(@"
            SELECT EmploymentTypeID, EmploymentTypeName FROM tblEmploymentType
            WHERE IsActive = 1 ORDER BY EmploymentTypeName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                PositionTypes.Add(new LookupItem { Id = Convert.ToInt32(dr["EmploymentTypeID"]), Name = dr["EmploymentTypeName"].ToString()! });
        }

        using (var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName + ' ' + LastName AS FullName
            FROM tblEmployee WHERE Status = 'Active'
            ORDER BY FirstName, LastName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                var code = dr["EmployeeCode"].ToString() ?? "";
                var name = dr["FullName"].ToString() ?? "";
                Employees.Add(new LookupItem { Id = Convert.ToInt32(dr["EmployeeID"]), Name = $"{code} – {name}" });
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT e.EmployeeID, c.ContactValue AS Email,
                   e.EmployeeCode + ' – ' + e.FirstName + ' ' + e.LastName AS EmpLabel
            FROM tblEmployee e
            INNER JOIN tblEmployeeContact c ON c.EmployeeID = e.EmployeeID
            WHERE e.Status = 'Active' AND c.ContactType = 'OfficialEmail'
              AND c.ContactValue IS NOT NULL AND LTRIM(RTRIM(c.ContactValue)) <> ''
            ORDER BY e.FirstName, e.LastName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                var email = dr["Email"].ToString() ?? "";
                EmailLookups.Add(new EmailLookupItem
                {
                    EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                    Email = email,
                    Label = $"{dr["EmpLabel"]} ({email})"
                });
            }
        }

        var excludeId = Input.PositionID;
        using (var cmd = new SqlCommand(@"
            SELECT PositionID, PositionNo + ISNULL(' – ' + NULLIF(Description,''), '') AS PositionLabel
            FROM tblPosition
            WHERE IsActive = 1 AND (@ExcludeId = 0 OR PositionID <> @ExcludeId)
            ORDER BY PositionNo;", conn))
        {
            cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                ReportToPositions.Add(new LookupItem { Id = Convert.ToInt32(dr["PositionID"]), Name = dr["PositionLabel"].ToString()! });
        }
    }

    private void LoadPosition(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT PositionID, PositionNo, Description,
                   ISNULL(EmailEmployeeID, 0) AS EmailEmployeeID,
                   ISNULL(JobID, 0) AS JobID,
                   ISNULL(DepartmentID, 0) AS DepartmentID,
                   ISNULL(ReportsToPositionID, 0) AS ReportsToPositionID,
                   ISNULL(TitleID, 0) AS TitleID,
                   ISNULL(PositionTypeID, 0) AS PositionTypeID,
                   PositionDuration, PositionStartDate, PositionEndDate, IsActive
            FROM tblPosition WHERE PositionID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return;

        static int Int(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
        Input = new PositionInput
        {
            PositionID = id,
            PositionNo = dr["PositionNo"].ToString() ?? "",
            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
            EmailEmployeeID = Int(dr["EmailEmployeeID"]),
            JobID = Int(dr["JobID"]),
            DepartmentID = Int(dr["DepartmentID"]),
            ReportsToPositionID = Int(dr["ReportsToPositionID"]),
            TitleID = Int(dr["TitleID"]),
            PositionTypeID = Int(dr["PositionTypeID"]),
            PositionDuration = dr["PositionDuration"] == DBNull.Value ? "" : dr["PositionDuration"].ToString() ?? "",
            PositionStartDate = dr["PositionStartDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["PositionStartDate"]),
            PositionEndDate = dr["PositionEndDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["PositionEndDate"]),
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private void LoadAssignments(int positionId)
    {
        Assignments.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT a.PositionWorkerAssignmentID, a.EmployeeID,
                   e.EmployeeCode + ' – ' + e.FirstName + ' ' + e.LastName AS EmployeeName,
                   a.AssignmentStartDate, a.AssignmentEndDate, a.Reason
            FROM tblPositionWorkerAssignment a
            INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
            WHERE a.PositionID = @PositionID
            ORDER BY a.SortOrder, a.PositionWorkerAssignmentID;", conn);
        cmd.Parameters.AddWithValue("@PositionID", positionId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Assignments.Add(new PositionAssignmentInput
            {
                AssignmentID = Convert.ToInt32(dr["PositionWorkerAssignmentID"]),
                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                EmployeeName = dr["EmployeeName"].ToString() ?? "",
                AssignmentStartDate = dr["AssignmentStartDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["AssignmentStartDate"]),
                AssignmentEndDate = dr["AssignmentEndDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["AssignmentEndDate"]),
                Reason = dr["Reason"] == DBNull.Value ? "" : dr["Reason"].ToString() ?? ""
            });
        }
    }

    private void LoadLinkedEntitlements(int positionId)
    {
        LinkedEntitlements.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT l.PositionBenefitEntitlementID, l.BenefitEntitlementID,
                   be.BenefitEntitlementName, be.AliasName
            FROM tblPositionBenefitEntitlement l
            INNER JOIN tblBenefitEntitlement be ON be.BenefitEntitlementID = l.BenefitEntitlementID
            WHERE l.PositionID = @PositionID
            ORDER BY be.BenefitEntitlementName;", conn);
        cmd.Parameters.AddWithValue("@PositionID", positionId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            LinkedEntitlements.Add(new LinkedEntitlementItem
            {
                LinkID = Convert.ToInt32(dr["PositionBenefitEntitlementID"]),
                BenefitEntitlementID = Convert.ToInt32(dr["BenefitEntitlementID"]),
                EntitlementName = dr["BenefitEntitlementName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? ""
            });
        }
    }

    private void LoadAvailableEntitlements(int positionId)
    {
        AvailableEntitlements.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT be.BenefitEntitlementID, be.BenefitEntitlementName
            FROM tblBenefitEntitlement be
            WHERE be.IsActive = 1
              AND NOT EXISTS (
                  SELECT 1 FROM tblPositionBenefitEntitlement l
                  WHERE l.PositionID = @PositionID AND l.BenefitEntitlementID = be.BenefitEntitlementID)
            ORDER BY be.BenefitEntitlementName;", conn);
        cmd.Parameters.AddWithValue("@PositionID", positionId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            AvailableEntitlements.Add(new LookupItem
            {
                Id = Convert.ToInt32(dr["BenefitEntitlementID"]),
                Name = dr["BenefitEntitlementName"].ToString() ?? ""
            });
        }
    }

    private void LoadWorkerEntitlementSources(int positionId)
    {
        WorkerEntitlementSources.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT e.FirstName + ' ' + e.LastName AS EmployeeName,
                   ISNULL(be.BenefitEntitlementName, '(none in Employee Master)') AS EntitlementName
            FROM tblPositionWorkerAssignment a
            INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
            LEFT JOIN tblBenefitEntitlement be ON be.BenefitEntitlementID = e.BenefitEntitlementID
            WHERE a.PositionID = @PositionID
              AND a.IsActive = 1
              AND (a.AssignmentEndDate IS NULL OR a.AssignmentEndDate >= CAST(GETDATE() AS DATE))
            ORDER BY e.FirstName, e.LastName;", conn);
        cmd.Parameters.AddWithValue("@PositionID", positionId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            WorkerEntitlementSources.Add(new WorkerEntitlementSourceItem
            {
                EmployeeName = dr.GetString(0),
                EntitlementName = dr.GetString(1)
            });
        }
    }

    private void LoadPositionList()
    {
        Positions.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT p.PositionID, p.PositionNo, p.Description,
                   ISNULL(j.JobTitle, '') AS JobTitle,
                   ISNULL(d.DepartmentName, '') AS DepartmentName,
                   ISNULL(t.DesignationLevelName, '') AS TitleName,
                   ISNULL(et.EmploymentTypeName, '') AS PositionTypeName,
                   ISNULL(p.PositionDuration, '') AS PositionDuration,
                   p.PositionStartDate, p.PositionEndDate,
                   ISNULL(ec.ContactValue, '') AS Email,
                   p.IsActive
            FROM tblPosition p
            LEFT JOIN tblJob j ON j.JobID = p.JobID
            LEFT JOIN tblDepartment d ON d.DepartmentID = p.DepartmentID
            LEFT JOIN tblDesignationLevel t ON t.DesignationLevelID = p.TitleID
            LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = p.PositionTypeID
            LEFT JOIN tblEmployeeContact ec ON ec.EmployeeID = p.EmailEmployeeID AND ec.ContactType = 'OfficialEmail'
            ORDER BY p.IsActive DESC, p.PositionNo;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Positions.Add(new PositionListItem
            {
                PositionID = Convert.ToInt32(dr["PositionID"]),
                PositionNo = dr["PositionNo"].ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                JobTitle = dr["JobTitle"].ToString() ?? "",
                DepartmentName = dr["DepartmentName"].ToString() ?? "",
                TitleName = dr["TitleName"].ToString() ?? "",
                PositionTypeName = dr["PositionTypeName"].ToString() ?? "",
                PositionDuration = dr["PositionDuration"].ToString() ?? "",
                PositionStartDate = dr["PositionStartDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["PositionStartDate"]),
                PositionEndDate = dr["PositionEndDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["PositionEndDate"]),
                Email = dr["Email"].ToString() ?? "",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            });
        }
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
