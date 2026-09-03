using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public partial class ExpenseMasterPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public string PageTitle => "Expense Master";
        public List<ExpenseListItem> Expenses { get; set; } = new List<ExpenseListItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> Locations { get; set; } = new List<LookupItem>();
        public List<LookupItem> ExpenseCategories { get; set; } = new List<LookupItem>();
        public ExpenseInput Input { get; set; } = new ExpenseInput();
        public List<ExpenseDetailRecord> DetailRecords { get; set; } = new List<ExpenseDetailRecord>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public string CategoriesJson => WebFormsJson.Serialize(ExpenseCategories);
        public string DetailsJsonInit => WebFormsJson.Serialize(DetailRecords);

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                OnPostSave();
                return;
            }

            var newExpense = Request.QueryString["newExpense"] == "1" || Request.QueryString["newExpense"] == "true";
            OnGet(QueryInt("editId"), newExpense);
        }

        private void OnGet(int? editId, bool newExpense)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newExpense;
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
                    if (DetailRecords.Count == 0) DetailRecords.Add(new ExpenseDetailRecord());
                }
            }
            else LoadExpenses();
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new ExpenseInput
            {
                ExpenseID = int.TryParse(Request.Form["ExpenseID"], out var eid) ? eid : 0,
                EmployeeID = int.TryParse(Request.Form["EmployeeID"], out var empid) ? empid : 0,
                ExpenseDate = FormString("ExpenseDate"),
                LocationID = int.TryParse(Request.Form["LocationID"], out var lid) ? lid : 0,
                ExpensePurpose = FormString("ExpensePurpose"),
                WorkflowStatus = string.IsNullOrWhiteSpace(FormString("WorkflowStatus")) ? "Draft" : FormString("WorkflowStatus"),
                VehicleNo = FormString("VehicleNo"),
                MeterReading = FormString("MeterReading"),
                DocumentStatus = string.IsNullOrWhiteSpace(FormString("DocumentStatus")) ? "Pending" : FormString("DocumentStatus")
            };

            var details = ParseDetails(Request.Form["DetailsJson"]);
            DetailRecords = details;

            if (Input.EmployeeID <= 0)
            {
                SetFormError("Employee is required.");
                return;
            }
            if (details.Count == 0)
            {
                SetFormError("Add at least one expense line item.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var expenseId = SaveExpenseHeader(conn, tx, Input);
                        ReplaceExpenseDetails(conn, tx, expenseId, details);
                        tx.Commit();
                        Input.ExpenseID = expenseId;
                    }
                }
                SetAlert(EditMode ? "Expense updated successfully." : "Expense saved successfully.");
                Response.Redirect("~/ExpenseMaster.aspx?editId=" + Input.ExpenseID);
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
            }
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var delDetails = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID=@Id;", conn))
                    {
                        delDetails.Parameters.AddWithValue("@Id", deleteId);
                        delDetails.ExecuteNonQuery();
                    }
                    using (var delHeader = new SqlCommand("DELETE FROM tblExpense WHERE ExpenseID=@Id;", conn))
                    {
                        delHeader.Parameters.AddWithValue("@Id", deleteId);
                        delHeader.ExecuteNonQuery();
                    }
                }
                SetAlert("Expense deleted successfully.");
            }
            catch (Exception ex) { SetAlert("Error deleting expense: " + ex.Message, "error"); }
            Response.Redirect("~/ExpenseMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message; AlertType = "error";
            LoadLookups(); ShowForm = true;
            if (DetailRecords.Count == 0) DetailRecords.Add(new ExpenseDetailRecord());
        }

        private int SaveExpenseHeader(SqlConnection conn, SqlTransaction tx, ExpenseInput input)
        {
            object expenseDate = string.IsNullOrWhiteSpace(input.ExpenseDate) ? DBNull.Value : (object)DateTime.Parse(input.ExpenseDate);
            object locationId = input.LocationID > 0 ? (object)input.LocationID : DBNull.Value;

            if (input.ExpenseID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblExpense SET EmployeeID=@EmployeeID, ExpenseDate=@ExpenseDate, LocationID=@LocationID,
  ExpensePurpose=@ExpensePurpose, WorkflowStatus=@WorkflowStatus, VehicleNo=@VehicleNo,
  MeterReading=@MeterReading, DocumentStatus=@DocumentStatus, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ExpenseID=@ExpenseID;", conn, tx))
                {
                    BindHeaderParams(cmd, input, expenseDate, locationId);
                    cmd.Parameters.AddWithValue("@ExpenseID", input.ExpenseID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                    return input.ExpenseID;
                }
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblExpense (EmployeeID, ExpenseDate, LocationID, ExpensePurpose, WorkflowStatus,
  VehicleNo, MeterReading, DocumentStatus, CreatedOn, CreatedByUserID)
VALUES (@EmployeeID, @ExpenseDate, @LocationID, @ExpensePurpose, @WorkflowStatus,
  @VehicleNo, @MeterReading, @DocumentStatus, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
            {
                BindHeaderParams(ins, input, expenseDate, locationId);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                return (int)ins.ExecuteScalar();
            }
        }

        private static void BindHeaderParams(SqlCommand cmd, ExpenseInput input, object expenseDate, object locationId)
        {
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
            cmd.Parameters.AddWithValue("@ExpenseDate", expenseDate);
            cmd.Parameters.AddWithValue("@LocationID", locationId);
            cmd.Parameters.AddWithValue("@ExpensePurpose", string.IsNullOrWhiteSpace(input.ExpensePurpose) ? (object)DBNull.Value : input.ExpensePurpose);
            cmd.Parameters.AddWithValue("@WorkflowStatus", input.WorkflowStatus);
            cmd.Parameters.AddWithValue("@VehicleNo", string.IsNullOrWhiteSpace(input.VehicleNo) ? (object)DBNull.Value : input.VehicleNo);
            cmd.Parameters.AddWithValue("@MeterReading", string.IsNullOrWhiteSpace(input.MeterReading) ? (object)DBNull.Value : input.MeterReading);
            cmd.Parameters.AddWithValue("@DocumentStatus", string.IsNullOrWhiteSpace(input.DocumentStatus) ? (object)DBNull.Value : input.DocumentStatus);
        }

        private void ReplaceExpenseDetails(SqlConnection conn, SqlTransaction tx, int expenseId, List<ExpenseDetailRecord> details)
        {
            using (var del = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID=@ExpenseID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ExpenseID", expenseId);
                del.ExecuteNonQuery();
            }
            int sort = 0;
            foreach (var line in details)
            {
                // File upload skipped — store empty / existing path only
                var docPath = line.OriginalReceiptDocPath ?? "";
                using (var ins = new SqlCommand(@"
INSERT INTO tblExpenseDetail
 (ExpenseID, ExpenseCategoryID, Description, PaymentMethod, TransactionDate,
  Currency, TransactionAmount, Amount, ApprovalStatus, OriginalReceiptID,
  OriginalReceiptDocPath, SortOrder, CreatedOn, CreatedByUserID)
VALUES
 (@ExpenseID, @ExpenseCategoryID, @Description, @PaymentMethod, @TransactionDate,
  @Currency, @TransactionAmount, @Amount, @ApprovalStatus, @OriginalReceiptID,
  @OriginalReceiptDocPath, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@ExpenseID", expenseId);
                    ins.Parameters.AddWithValue("@ExpenseCategoryID", line.ExpenseCategoryID > 0 ? (object)line.ExpenseCategoryID : DBNull.Value);
                    ins.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(line.Description) ? (object)DBNull.Value : line.Description);
                    ins.Parameters.AddWithValue("@PaymentMethod", string.IsNullOrWhiteSpace(line.PaymentMethod) ? (object)DBNull.Value : line.PaymentMethod);
                    ins.Parameters.AddWithValue("@TransactionDate", ParseDateObj(line.TransactionDate));
                    ins.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(line.Currency) ? "PKR" : line.Currency);
                    ins.Parameters.AddWithValue("@TransactionAmount", ParseDecimalObj(line.TransactionAmount));
                    ins.Parameters.AddWithValue("@Amount", ParseDecimalObj(line.Amount));
                    ins.Parameters.AddWithValue("@ApprovalStatus", string.IsNullOrWhiteSpace(line.ApprovalStatus) ? "Pending" : line.ApprovalStatus);
                    ins.Parameters.AddWithValue("@OriginalReceiptID", string.IsNullOrWhiteSpace(line.OriginalReceiptID) ? (object)DBNull.Value : line.OriginalReceiptID);
                    ins.Parameters.AddWithValue("@OriginalReceiptDocPath", string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
                    ins.Parameters.AddWithValue("@SortOrder", sort);
                    AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                    ins.ExecuteNonQuery();
                    sort++;
                }
            }
        }

        private static List<ExpenseDetailRecord> ParseDetails(string json)
        {
            return WebFormsJson.DeserializeList<ExpenseDetailRecord>(json)
                .Where(d => d.ExpenseCategoryID > 0 || !string.IsNullOrWhiteSpace(d.Description) || ParseDecimal(d.Amount) > 0)
                .ToList();
        }

        private void LoadExpenses()
        {
            Expenses.Clear();
            var scope = _dataScope.GetEmployeeFilter("emp");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
SELECT e.ExpenseID, emp.EmployeeCode, emp.FirstName + ' ' + emp.LastName AS EmployeeName,
       e.ExpenseDate, ISNULL(loc.LocationName,''), ISNULL(e.ExpensePurpose,''),
       (SELECT COUNT(*) FROM tblExpenseDetail d WHERE d.ExpenseID=e.ExpenseID),
       ISNULL((SELECT SUM(d.Amount) FROM tblExpenseDetail d WHERE d.ExpenseID=e.ExpenseID),0),
       e.WorkflowStatus, ISNULL(e.DocumentStatus,'')
FROM tblExpense e
INNER JOIN tblEmployee emp ON emp.EmployeeID=e.EmployeeID
LEFT JOIN tblLocation loc ON loc.LocationID=e.LocationID
WHERE 1=1 {scope.Sql}
ORDER BY e.ExpenseID DESC;", conn))
            {
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Expenses.Add(new ExpenseListItem
                        {
                            ExpenseID = dr.GetInt32(0),
                            EmployeeCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            EmployeeName = dr.GetString(2),
                            ExpenseDate = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3),
                            LocationName = dr.GetString(4),
                            ExpensePurpose = dr.GetString(5),
                            LineCount = dr.GetInt32(6),
                            TotalAmount = dr.GetDecimal(7),
                            WorkflowStatus = dr.GetString(8),
                            DocumentStatus = dr.GetString(9)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int expenseId)
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
SELECT ExpenseID, EmployeeID, ExpenseDate, LocationID, ExpensePurpose,
       WorkflowStatus, VehicleNo, MeterReading, DocumentStatus
FROM tblExpense WHERE ExpenseID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", expenseId);
                    using (var dr = cmd.ExecuteReader())
                    {
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
                }
                using (var cmd = new SqlCommand(@"
SELECT DetailID, ExpenseCategoryID, Description, PaymentMethod, TransactionDate,
       Currency, TransactionAmount, Amount, ApprovalStatus, OriginalReceiptID, OriginalReceiptDocPath
FROM tblExpenseDetail WHERE ExpenseID=@Id ORDER BY SortOrder, DetailID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", expenseId);
                    using (var dr = cmd.ExecuteReader())
                    {
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
            }
            if (DetailRecords.Count == 0) DetailRecords.Add(new ExpenseDetailRecord());
        }

        private void LoadLookups()
        {
            Employees = new List<LookupItem>();
            Locations = new List<LookupItem>();
            ExpenseCategories = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                var scope = _dataScope.GetEmployeeFilter("e");
                using (var cmd = new SqlCommand($@"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
FROM tblEmployee e WHERE e.Status='Active' {scope.Sql} ORDER BY e.FirstName, e.LastName;", conn))
                {
                    scope.ApplyTo(cmd);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                            var name = (dr.GetString(2) + " " + dr.GetString(3)).Trim();
                            Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = string.IsNullOrEmpty(code) ? name : code + " – " + name });
                        }
                    }
                }
                using (var cmd = new SqlCommand("SELECT LocationID, LocationName FROM tblLocation WHERE IsActive=1 ORDER BY LocationName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Locations.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
                using (var cmd = new SqlCommand("SELECT ExpenseCategoryID, ExpenseCategoryName FROM tblExpenseCategory WHERE IsActive=1 ORDER BY ExpenseCategoryName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        ExpenseCategories.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }

        private static object ParseDateObj(string value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)DateTime.Parse(value);
        private static object ParseDecimalObj(string value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)ParseDecimal(value);
        private static decimal ParseDecimal(string value) =>
            decimal.TryParse(value, out var d) ? d : 0m;
    }
}
