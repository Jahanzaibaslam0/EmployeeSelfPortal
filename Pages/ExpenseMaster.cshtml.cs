using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace HRMS.Pages;

public class ExpenseListItem
{
    public int ExpenseID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime? ExpenseDate { get; set; }
    public string LocationName { get; set; } = "";
    public string ExpensePurpose { get; set; } = "";
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string WorkflowStatus { get; set; } = "";
    public string DocumentStatus { get; set; } = "";
}

public class ExpenseInput
{
    public int ExpenseID { get; set; }
    public int EmployeeID { get; set; }
    public string ExpenseDate { get; set; } = "";
    public int LocationID { get; set; }
    public string ExpensePurpose { get; set; } = "";
    public string WorkflowStatus { get; set; } = "Draft";
    public string VehicleNo { get; set; } = "";
    public string MeterReading { get; set; } = "";
    public string DocumentStatus { get; set; } = "Pending";
}

public class ExpenseDetailRecord
{
    public int DetailID { get; set; }
    public int ExpenseCategoryID { get; set; }
    public string Description { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string TransactionDate { get; set; } = "";
    public string Currency { get; set; } = "PKR";
    public string TransactionAmount { get; set; } = "";
    public string Amount { get; set; } = "";
    public string ApprovalStatus { get; set; } = "Pending";
    public string OriginalReceiptID { get; set; } = "";
    public string OriginalReceiptDocPath { get; set; } = "";
}

public class ExpenseMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly DataAccessScopeService _dataScope;
    private readonly IWebHostEnvironment _env;

    public ExpenseMasterModel(IConfiguration config, AuthService auth, DataAccessScopeService dataScope, IWebHostEnvironment env)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _dataScope = dataScope;
        _env = env;
    }

    public List<ExpenseListItem> Expenses { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> Locations { get; set; } = new();
    public List<LookupItem> ExpenseCategories { get; set; } = new();
    public ExpenseInput Input { get; set; } = new();
    public List<ExpenseDetailRecord> DetailRecords { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newExpense)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newExpense == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
            else
            {
                Input.ExpenseDate = DateTime.Today.ToString("yyyy-MM-dd");
            }
        }
        else
        {
            LoadExpenses();
        }
    }

    public async Task<IActionResult> OnPostAsync(
        int ExpenseID, string EditMode, int EmployeeID, string ExpenseDate, int LocationID,
        string ExpensePurpose, string WorkflowStatus, string VehicleNo, string MeterReading,
        string DocumentStatus, string DetailsJson)
    {
        this.EditMode = EditMode == "true";
        Input = new ExpenseInput
        {
            ExpenseID = ExpenseID,
            EmployeeID = EmployeeID,
            ExpenseDate = ExpenseDate ?? "",
            LocationID = LocationID,
            ExpensePurpose = ExpensePurpose?.Trim() ?? "",
            WorkflowStatus = string.IsNullOrWhiteSpace(WorkflowStatus) ? "Draft" : WorkflowStatus,
            VehicleNo = VehicleNo?.Trim() ?? "",
            MeterReading = MeterReading?.Trim() ?? "",
            DocumentStatus = DocumentStatus ?? "Pending"
        };

        if (EmployeeID <= 0)
        {
            AlertMessage = "Employee is required.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        var details = ParseDetails(DetailsJson);
        if (details.Count == 0)
        {
            AlertMessage = "Add at least one expense line item.";
            AlertType = "error";
            DetailRecords = details;
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var expenseId = SaveExpenseHeader(conn, tx, Input);
            await ReplaceExpenseDetailsAsync(conn, tx, expenseId, details);

            tx.Commit();

            TempData["Alert"] = this.EditMode ? "Expense updated successfully." : "Expense saved successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = expenseId });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            DetailRecords = details;
            LoadLookups();
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using (var delDetails = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID = @Id;", conn))
            {
                delDetails.Parameters.AddWithValue("@Id", deleteId);
                delDetails.ExecuteNonQuery();
            }
            using (var delHeader = new SqlCommand("DELETE FROM tblExpense WHERE ExpenseID = @Id;", conn))
            {
                delHeader.Parameters.AddWithValue("@Id", deleteId);
                delHeader.ExecuteNonQuery();
            }

            TempData["Alert"] = "Expense deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting expense: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private int SaveExpenseHeader(SqlConnection conn, SqlTransaction tx, ExpenseInput input)
    {
        var expenseDate = string.IsNullOrWhiteSpace(input.ExpenseDate) ? DBNull.Value : (object)DateTime.Parse(input.ExpenseDate);
        var locationId = input.LocationID > 0 ? (object)input.LocationID : DBNull.Value;

        if (input.ExpenseID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblExpense
                SET EmployeeID = @EmployeeID,
                    ExpenseDate = @ExpenseDate,
                    LocationID = @LocationID,
                    ExpensePurpose = @ExpensePurpose,
                    WorkflowStatus = @WorkflowStatus,
                    VehicleNo = @VehicleNo,
                    MeterReading = @MeterReading,
                    DocumentStatus = @DocumentStatus,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ExpenseID = @ExpenseID;", conn, tx);

            BindHeaderParams(cmd, input, expenseDate, locationId);
            cmd.Parameters.AddWithValue("@ExpenseID", input.ExpenseID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return input.ExpenseID;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblExpense
                (EmployeeID, ExpenseDate, LocationID, ExpensePurpose, WorkflowStatus,
                 VehicleNo, MeterReading, DocumentStatus, CreatedOn, CreatedByUserID)
            VALUES
                (@EmployeeID, @ExpenseDate, @LocationID, @ExpensePurpose, @WorkflowStatus,
                 @VehicleNo, @MeterReading, @DocumentStatus, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);

        BindHeaderParams(ins, input, expenseDate, locationId);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        return (int)ins.ExecuteScalar()!;
    }

    private static void BindHeaderParams(SqlCommand cmd, ExpenseInput input, object expenseDate, object locationId)
    {
        cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
        cmd.Parameters.AddWithValue("@ExpenseDate", expenseDate);
        cmd.Parameters.AddWithValue("@LocationID", locationId);
        cmd.Parameters.AddWithValue("@ExpensePurpose", string.IsNullOrWhiteSpace(input.ExpensePurpose) ? DBNull.Value : input.ExpensePurpose);
        cmd.Parameters.AddWithValue("@WorkflowStatus", input.WorkflowStatus);
        cmd.Parameters.AddWithValue("@VehicleNo", string.IsNullOrWhiteSpace(input.VehicleNo) ? DBNull.Value : input.VehicleNo);
        cmd.Parameters.AddWithValue("@MeterReading", string.IsNullOrWhiteSpace(input.MeterReading) ? DBNull.Value : input.MeterReading);
        cmd.Parameters.AddWithValue("@DocumentStatus", string.IsNullOrWhiteSpace(input.DocumentStatus) ? DBNull.Value : input.DocumentStatus);
    }

    private async Task ReplaceExpenseDetailsAsync(SqlConnection conn, SqlTransaction tx, int expenseId, List<ExpenseDetailRecord> details)
    {
        using (var del = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID = @ExpenseID;", conn, tx))
        {
            del.Parameters.AddWithValue("@ExpenseID", expenseId);
            del.ExecuteNonQuery();
        }

        int sort = 0;
        foreach (var line in details)
        {
            var docPath = line.OriginalReceiptDocPath;
            var fileKey = $"receiptDoc_{sort}";
            if (Request.Form.Files[fileKey] is { Length: > 0 } file)
                docPath = await SaveReceiptFileAsync(file);

            using var ins = new SqlCommand(@"
                INSERT INTO tblExpenseDetail
                    (ExpenseID, ExpenseCategoryID, Description, PaymentMethod, TransactionDate,
                     Currency, TransactionAmount, Amount, ApprovalStatus, OriginalReceiptID,
                     OriginalReceiptDocPath, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@ExpenseID, @ExpenseCategoryID, @Description, @PaymentMethod, @TransactionDate,
                     @Currency, @TransactionAmount, @Amount, @ApprovalStatus, @OriginalReceiptID,
                     @OriginalReceiptDocPath, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            ins.Parameters.AddWithValue("@ExpenseID", expenseId);
            ins.Parameters.AddWithValue("@ExpenseCategoryID", line.ExpenseCategoryID > 0 ? line.ExpenseCategoryID : DBNull.Value);
            ins.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(line.Description) ? DBNull.Value : line.Description);
            ins.Parameters.AddWithValue("@PaymentMethod", string.IsNullOrWhiteSpace(line.PaymentMethod) ? DBNull.Value : line.PaymentMethod);
            ins.Parameters.AddWithValue("@TransactionDate", ParseDateObj(line.TransactionDate));
            ins.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(line.Currency) ? "PKR" : line.Currency);
            ins.Parameters.AddWithValue("@TransactionAmount", ParseDecimalObj(line.TransactionAmount));
            ins.Parameters.AddWithValue("@Amount", ParseDecimalObj(line.Amount));
            ins.Parameters.AddWithValue("@ApprovalStatus", string.IsNullOrWhiteSpace(line.ApprovalStatus) ? "Pending" : line.ApprovalStatus);
            ins.Parameters.AddWithValue("@OriginalReceiptID", string.IsNullOrWhiteSpace(line.OriginalReceiptID) ? DBNull.Value : line.OriginalReceiptID);
            ins.Parameters.AddWithValue("@OriginalReceiptDocPath", string.IsNullOrWhiteSpace(docPath) ? DBNull.Value : docPath);
            ins.Parameters.AddWithValue("@SortOrder", sort);
            AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
            ins.ExecuteNonQuery();
            sort++;
        }
    }

    private async Task<string> SaveReceiptFileAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "expenses");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName);
        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, name);
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
        return $"/uploads/expenses/{name}";
    }

    private static List<ExpenseDetailRecord> ParseDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<ExpenseDetailRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?
                .Where(d => d.ExpenseCategoryID > 0 || !string.IsNullOrWhiteSpace(d.Description) || ParseDecimal(d.Amount) > 0)
                .ToList() ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void LoadExpenses()
    {
        var scope = _dataScope.GetEmployeeFilter("emp");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT e.ExpenseID, emp.EmployeeCode,
                   emp.FirstName + ' ' + emp.LastName AS EmployeeName,
                   e.ExpenseDate, ISNULL(loc.LocationName, '') AS LocationName,
                   ISNULL(e.ExpensePurpose, '') AS ExpensePurpose,
                   (SELECT COUNT(*) FROM tblExpenseDetail d WHERE d.ExpenseID = e.ExpenseID) AS LineCount,
                   ISNULL((SELECT SUM(d.Amount) FROM tblExpenseDetail d WHERE d.ExpenseID = e.ExpenseID), 0) AS TotalAmount,
                   e.WorkflowStatus, ISNULL(e.DocumentStatus, '') AS DocumentStatus
            FROM tblExpense e
            INNER JOIN tblEmployee emp ON emp.EmployeeID = e.EmployeeID
            LEFT JOIN tblLocation loc ON loc.LocationID = e.LocationID
            WHERE 1=1 {scope.Sql}
            ORDER BY e.ExpenseID DESC;", conn);
        scope.ApplyTo(cmd);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Expenses.Add(new ExpenseListItem
            {
                ExpenseID = dr.GetInt32(0),
                EmployeeCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                EmployeeName = dr.GetString(2),
                ExpenseDate = dr.IsDBNull(3) ? null : dr.GetDateTime(3),
                LocationName = dr.GetString(4),
                ExpensePurpose = dr.GetString(5),
                LineCount = dr.GetInt32(6),
                TotalAmount = dr.GetDecimal(7),
                WorkflowStatus = dr.GetString(8),
                DocumentStatus = dr.GetString(9)
            });
        }
    }

    private void LoadForEdit(int expenseId)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT ExpenseID, EmployeeID, ExpenseDate, LocationID, ExpensePurpose,
                   WorkflowStatus, VehicleNo, MeterReading, DocumentStatus
            FROM tblExpense WHERE ExpenseID = @Id;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", expenseId);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Input = new ExpenseInput
                {
                    ExpenseID = dr.GetInt32(0),
                    EmployeeID = dr.GetInt32(1),
                    ExpenseDate = dr.IsDBNull(2) ? "" : dr.GetDateTime(2).ToString("yyyy-MM-dd"),
                    LocationID = dr.IsDBNull(3) ? 0 : dr.GetInt32(3),
                    ExpensePurpose = dr.IsDBNull(4) ? "" : dr.GetString(4),
                    WorkflowStatus = dr.GetString(5),
                    VehicleNo = dr.IsDBNull(6) ? "" : dr.GetString(6),
                    MeterReading = dr.IsDBNull(7) ? "" : dr.GetString(7),
                    DocumentStatus = dr.IsDBNull(8) ? "" : dr.GetString(8)
                };
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT DetailID, ExpenseCategoryID, Description, PaymentMethod, TransactionDate,
                   Currency, TransactionAmount, Amount, ApprovalStatus, OriginalReceiptID, OriginalReceiptDocPath
            FROM tblExpenseDetail WHERE ExpenseID = @Id ORDER BY SortOrder, DetailID;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", expenseId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                DetailRecords.Add(new ExpenseDetailRecord
                {
                    DetailID = dr.GetInt32(0),
                    ExpenseCategoryID = dr.IsDBNull(1) ? 0 : dr.GetInt32(1),
                    Description = dr.IsDBNull(2) ? "" : dr.GetString(2),
                    PaymentMethod = dr.IsDBNull(3) ? "" : dr.GetString(3),
                    TransactionDate = dr.IsDBNull(4) ? "" : dr.GetDateTime(4).ToString("yyyy-MM-dd"),
                    Currency = dr.IsDBNull(5) ? "PKR" : dr.GetString(5),
                    TransactionAmount = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.##"),
                    Amount = dr.IsDBNull(7) ? "" : dr.GetDecimal(7).ToString("0.##"),
                    ApprovalStatus = dr.IsDBNull(8) ? "Pending" : dr.GetString(8),
                    OriginalReceiptID = dr.IsDBNull(9) ? "" : dr.GetString(9),
                    OriginalReceiptDocPath = dr.IsDBNull(10) ? "" : dr.GetString(10)
                });
            }
        }
    }

    private void LoadLookups()
    {
        Employees = new List<LookupItem>();
        Locations = new List<LookupItem>();
        ExpenseCategories = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        conn.Open();

        var scope = _dataScope.GetEmployeeFilter("e");
        using (var cmd = new SqlCommand($@"
            SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
            FROM tblEmployee e WHERE e.Status = 'Active' {scope.Sql} ORDER BY e.FirstName, e.LastName;", conn))
        {
            scope.ApplyTo(cmd);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                var name = $"{dr.GetString(2)} {dr.GetString(3)}".Trim();
                Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}" });
            }
        }

        using (var cmd = new SqlCommand(@"SELECT LocationID, LocationName FROM tblLocation WHERE IsActive = 1 ORDER BY LocationName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Locations.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }

        using (var cmd = new SqlCommand(@"SELECT ExpenseCategoryID, ExpenseCategoryName FROM tblExpenseCategory WHERE IsActive = 1 ORDER BY ExpenseCategoryName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                ExpenseCategories.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }

    private static object ParseDateObj(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)DateTime.Parse(value);

    private static object ParseDecimalObj(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)ParseDecimal(value);

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, out var d) ? d : 0m;
}
