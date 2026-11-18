using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class TaskListItem
{
    public int TaskID { get; set; }
    public string TaskCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Priority { get; set; } = "";
    public string TaskStatus { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string AssignedToName { get; set; } = "";
    public string ReferenceType { get; set; } = "";
    public string ReferenceDisplay { get; set; } = "";
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TaskHistoryItem
{
    public string ActionType { get; set; } = "";
    public string OldStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string CreatedByName { get; set; } = "";
    public DateTime CreatedOn { get; set; }
}

public class TaskInput
{
    public int TaskID { get; set; }
    public string TaskCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string TaskStatus { get; set; } = "Open";
    public string DueDate { get; set; } = "";
    public int AssignedToEmployeeID { get; set; }
    public string ReferenceType { get; set; } = "None";
    public int ReferenceEmployeeID { get; set; }
    public int ReferenceCustomerID { get; set; }
    public int ReferenceVendorID { get; set; }
    public int ReferenceSalesOrderID { get; set; }
    public int ReferencePurchaseOrderID { get; set; }
}

public class TaskMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly AuditService _audit;

    public static readonly string[] PriorityOptions = { "Low", "Medium", "High", "Urgent" };
    public static readonly string[] StatusOptions = { "Open", "In Progress", "On Hold", "Completed", "Cancelled" };
    public static readonly string[] ReferenceTypeOptions = { "None", "Employee", "Customer", "Vendor", "Sales", "Purchase" };

    public TaskMasterModel(IConfiguration config, AuthService auth, AuditService audit)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _audit = audit;
    }

    public List<TaskListItem> Tasks { get; set; } = new();
    public List<TaskHistoryItem> History { get; set; } = new();
    public TaskInput Input { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> Customers { get; set; } = new();
    public List<LookupItem> Vendors { get; set; } = new();
    public List<LookupItem> SalesOrders { get; set; } = new();
    public List<LookupItem> PurchaseOrders { get; set; } = new();

    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public string FilterReferenceType { get; set; } = "";
    public string FilterStatus { get; set; } = "";
    public int FilterAssignedTo { get; set; }
    public string FilterSearch { get; set; } = "";

    public int CountOpen { get; set; }
    public int CountInProgress { get; set; }
    public int CountCompleted { get; set; }
    public int CountOverdue { get; set; }

    public bool IsStatusLocked =>
        Input.TaskStatus is "Completed" or "Cancelled";

    public IReadOnlyList<string> PriorityOptionList => PriorityOptions;
    public IReadOnlyList<string> StatusOptionList => StatusOptions;
    public IReadOnlyList<string> ReferenceTypeOptionList => ReferenceTypeOptions;

    public IActionResult OnGet(
        [FromQuery] int? editId,
        [FromQuery] bool? newTask,
        [FromQuery] string? refType,
        [FromQuery] string? status,
        [FromQuery] int? assignedTo,
        [FromQuery] string? q)
    {
        LoadAlert();
        FilterReferenceType = refType?.Trim() ?? "";
        FilterStatus = status?.Trim() ?? "";
        FilterAssignedTo = assignedTo ?? 0;
        FilterSearch = q?.Trim() ?? "";

        ShowForm = (editId.HasValue && editId > 0) || newTask == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                if (!LoadForEdit(editId.Value))
                {
                    TempData["Alert"] = "Task not found.";
                    TempData["AlertType"] = "error";
                    return RedirectToPage();
                }
                EditMode = true;
                LoadHistory(editId.Value);
            }
            else
            {
                Input.TaskCode = GenerateNextTaskCode();
                Input.DueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(FilterReferenceType) &&
                    ReferenceTypeOptions.Contains(FilterReferenceType, StringComparer.OrdinalIgnoreCase))
                {
                    Input.ReferenceType = FilterReferenceType;
                }
            }
        }
        else
        {
            LoadEmployeesLookup();
            LoadTasks();
            LoadStats();
        }

        return Page();
    }

    public IActionResult OnPost(
        int TaskID,
        bool EditMode,
        string Title,
        string Description,
        string Priority,
        string TaskStatus,
        string DueDate,
        int AssignedToEmployeeID,
        string ReferenceType,
        int ReferenceEmployeeID,
        int ReferenceCustomerID,
        int ReferenceVendorID,
        int ReferenceSalesOrderID,
        int ReferencePurchaseOrderID)
    {
        this.EditMode = EditMode;
        Input = new TaskInput
        {
            TaskID = TaskID,
            Title = Title?.Trim() ?? "",
            Description = Description?.Trim() ?? "",
            Priority = string.IsNullOrWhiteSpace(Priority) ? "Medium" : Priority.Trim(),
            TaskStatus = string.IsNullOrWhiteSpace(TaskStatus) ? "Open" : TaskStatus.Trim(),
            DueDate = DueDate?.Trim() ?? "",
            AssignedToEmployeeID = AssignedToEmployeeID,
            ReferenceType = string.IsNullOrWhiteSpace(ReferenceType) ? "None" : ReferenceType.Trim(),
            ReferenceEmployeeID = ReferenceEmployeeID,
            ReferenceCustomerID = ReferenceCustomerID,
            ReferenceVendorID = ReferenceVendorID,
            ReferenceSalesOrderID = ReferenceSalesOrderID,
            ReferencePurchaseOrderID = ReferencePurchaseOrderID
        };

        NormalizeReferenceIds();

        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            TempData["Alert"] = "Task title is required.";
            TempData["AlertType"] = "error";
            return RedirectToForm(TaskID, EditMode);
        }

        if (!PriorityOptions.Contains(Input.Priority))
            Input.Priority = "Medium";
        if (!StatusOptions.Contains(Input.TaskStatus))
            Input.TaskStatus = "Open";
        if (!ReferenceTypeOptions.Contains(Input.ReferenceType))
            Input.ReferenceType = "None";

        if (!ValidateReferenceSelection(out var refError))
        {
            TempData["Alert"] = refError;
            TempData["AlertType"] = "error";
            return RedirectToForm(TaskID, EditMode);
        }

        DateTime? dueDate = null;
        if (!string.IsNullOrWhiteSpace(Input.DueDate))
        {
            if (!DateTime.TryParse(Input.DueDate, out var parsedDue))
            {
                TempData["Alert"] = "Invalid due date.";
                TempData["AlertType"] = "error";
                return RedirectToForm(TaskID, EditMode);
            }
            dueDate = parsedDue.Date;
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            string oldStatus = "";
            if (EditMode && TaskID > 0)
            {
                using (var chk = new SqlCommand(
                    "SELECT TaskStatus, TaskCode FROM tblTask WHERE TaskID = @ID AND IsActive = 1;", conn, tx))
                {
                    chk.Parameters.AddWithValue("@ID", TaskID);
                    using var r = chk.ExecuteReader();
                    if (!r.Read())
                    {
                        TempData["Alert"] = "Task not found or inactive.";
                        TempData["AlertType"] = "error";
                        return RedirectToPage();
                    }
                    oldStatus = r.GetString(0);
                    Input.TaskCode = r.GetString(1);
                    if (oldStatus is "Completed" or "Cancelled")
                    {
                        TempData["Alert"] = $"Cannot edit a {oldStatus.ToLower()} task.";
                        TempData["AlertType"] = "error";
                        return RedirectToPage();
                    }
                }

                using var upd = new SqlCommand(@"
                    UPDATE tblTask SET
                        Title = @Title,
                        Description = @Description,
                        Priority = @Priority,
                        TaskStatus = @TaskStatus,
                        DueDate = @DueDate,
                        AssignedToEmployeeID = @AssignedToEmployeeID,
                        ReferenceType = @ReferenceType,
                        ReferenceEmployeeID = @ReferenceEmployeeID,
                        ReferenceCustomerID = @ReferenceCustomerID,
                        ReferenceVendorID = @ReferenceVendorID,
                        ReferenceSalesOrderID = @ReferenceSalesOrderID,
                        ReferencePurchaseOrderID = @ReferencePurchaseOrderID,
                        CompletedOn = CASE WHEN @TaskStatus = 'Completed' AND CompletedOn IS NULL THEN GETDATE()
                                           WHEN @TaskStatus <> 'Completed' THEN NULL
                                           ELSE CompletedOn END,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE TaskID = @TaskID;", conn, tx);
                BindTaskParams(upd, Input, dueDate);
                upd.Parameters.AddWithValue("@TaskID", TaskID);
                AuditHelper.AddModifiedBy(upd, _auth.CurrentUserId);
                upd.ExecuteNonQuery();
            }
            else
            {
                Input.TaskCode = GenerateNextTaskCode(conn, tx);
                using var ins = new SqlCommand(@"
                    INSERT INTO tblTask (
                        TaskCode, Title, Description, Priority, TaskStatus, DueDate,
                        AssignedToEmployeeID, ReferenceType,
                        ReferenceEmployeeID, ReferenceCustomerID, ReferenceVendorID,
                        ReferenceSalesOrderID, ReferencePurchaseOrderID,
                        CompletedOn, IsActive, CreatedOn, CreatedByUserID)
                    OUTPUT INSERTED.TaskID
                    VALUES (
                        @TaskCode, @Title, @Description, @Priority, @TaskStatus, @DueDate,
                        @AssignedToEmployeeID, @ReferenceType,
                        @ReferenceEmployeeID, @ReferenceCustomerID, @ReferenceVendorID,
                        @ReferenceSalesOrderID, @ReferencePurchaseOrderID,
                        CASE WHEN @TaskStatus = 'Completed' THEN GETDATE() ELSE NULL END,
                        1, GETDATE(), @CreatedByUserID);", conn, tx);
                ins.Parameters.AddWithValue("@TaskCode", Input.TaskCode);
                BindTaskParams(ins, Input, dueDate);
                AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
                TaskID = (int)ins.ExecuteScalar()!;
                Input.TaskID = TaskID;
            }

            if (!EditMode || !string.Equals(oldStatus, Input.TaskStatus, StringComparison.OrdinalIgnoreCase))
            {
                InsertHistory(conn, tx, TaskID,
                    EditMode ? "StatusChange" : "Create",
                    EditMode ? oldStatus : null,
                    Input.TaskStatus,
                    EditMode ? $"Status changed to {Input.TaskStatus}" : "Task created");
            }
            else if (EditMode)
            {
                InsertHistory(conn, tx, TaskID, "Update", oldStatus, Input.TaskStatus, "Task details updated");
            }

            tx.Commit();

            _audit.Log(
                actionType: EditMode ? "Update" : "Create",
                entityType: "Task",
                entityId: TaskID,
                entityName: Input.TaskCode,
                formKey: "TaskMaster",
                pagePath: "/TaskMaster",
                handlerName: "OnPost",
                details: $"Status: {Input.TaskStatus}, Ref: {Input.ReferenceType}, Assignee: {Input.AssignedToEmployeeID}");

            TempData["Alert"] = EditMode ? "Task updated successfully." : "Task created successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = TaskID });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving task: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToForm(TaskID, EditMode);
        }
    }

    public IActionResult OnPostComplete(int completeId)
    {
        return ChangeStatus(completeId, "Completed", "Task marked as completed.");
    }

    public IActionResult OnPostCancel(int cancelId)
    {
        return ChangeStatus(cancelId, "Cancelled", "Task cancelled.");
    }

    public IActionResult OnPostReopen(int reopenId)
    {
        return ChangeStatus(reopenId, "Open", "Task reopened.");
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            string? code = null;
            using (var get = new SqlCommand("SELECT TaskCode FROM tblTask WHERE TaskID = @ID;", conn))
            {
                get.Parameters.AddWithValue("@ID", deleteId);
                code = get.ExecuteScalar()?.ToString();
            }

            using var cmd = new SqlCommand(@"
                UPDATE tblTask SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE TaskID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();

            InsertHistory(conn, null, deleteId, "Delete", null, null, "Task removed");
            _audit.Log("Delete", "Task", deleteId, code, "TaskMaster", "/TaskMaster", "OnPostDelete");

            TempData["Alert"] = "Task removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing task: " + ex.Message;
            TempData["AlertType"] = "error";
        }
        return RedirectToPage();
    }

    private IActionResult ChangeStatus(int taskId, string newStatus, string successMessage)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            string oldStatus;
            string code;
            using (var get = new SqlCommand(
                "SELECT TaskStatus, TaskCode FROM tblTask WHERE TaskID = @ID AND IsActive = 1;", conn, tx))
            {
                get.Parameters.AddWithValue("@ID", taskId);
                using var r = get.ExecuteReader();
                if (!r.Read())
                {
                    TempData["Alert"] = "Task not found.";
                    TempData["AlertType"] = "error";
                    return RedirectToPage();
                }
                oldStatus = r.GetString(0);
                code = r.GetString(1);
            }

            if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Alert"] = $"Task is already {newStatus}.";
                TempData["AlertType"] = "error";
                return RedirectToPage(new { editId = taskId });
            }

            using var upd = new SqlCommand(@"
                UPDATE tblTask SET
                    TaskStatus = @Status,
                    CompletedOn = CASE WHEN @Status = 'Completed' THEN GETDATE() ELSE NULL END,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE TaskID = @ID;", conn, tx);
            upd.Parameters.AddWithValue("@Status", newStatus);
            upd.Parameters.AddWithValue("@ID", taskId);
            AuditHelper.AddModifiedBy(upd, _auth.CurrentUserId);
            upd.ExecuteNonQuery();

            InsertHistory(conn, tx, taskId, "StatusChange", oldStatus, newStatus, successMessage);
            tx.Commit();

            _audit.Log("StatusChange", "Task", taskId, code, "TaskMaster", "/TaskMaster",
                "OnPostStatus", $"{oldStatus} → {newStatus}");

            TempData["Alert"] = successMessage;
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = taskId });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error updating status: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }
    }

    private void NormalizeReferenceIds()
    {
        switch (Input.ReferenceType)
        {
            case "Employee":
                Input.ReferenceCustomerID = 0;
                Input.ReferenceVendorID = 0;
                Input.ReferenceSalesOrderID = 0;
                Input.ReferencePurchaseOrderID = 0;
                break;
            case "Customer":
                Input.ReferenceEmployeeID = 0;
                Input.ReferenceVendorID = 0;
                Input.ReferenceSalesOrderID = 0;
                Input.ReferencePurchaseOrderID = 0;
                break;
            case "Vendor":
                Input.ReferenceEmployeeID = 0;
                Input.ReferenceCustomerID = 0;
                Input.ReferenceSalesOrderID = 0;
                Input.ReferencePurchaseOrderID = 0;
                break;
            case "Sales":
                Input.ReferenceEmployeeID = 0;
                Input.ReferenceCustomerID = 0;
                Input.ReferenceVendorID = 0;
                Input.ReferencePurchaseOrderID = 0;
                break;
            case "Purchase":
                Input.ReferenceEmployeeID = 0;
                Input.ReferenceCustomerID = 0;
                Input.ReferenceVendorID = 0;
                Input.ReferenceSalesOrderID = 0;
                break;
            default:
                Input.ReferenceType = "None";
                Input.ReferenceEmployeeID = 0;
                Input.ReferenceCustomerID = 0;
                Input.ReferenceVendorID = 0;
                Input.ReferenceSalesOrderID = 0;
                Input.ReferencePurchaseOrderID = 0;
                break;
        }
    }

    private bool ValidateReferenceSelection(out string error)
    {
        error = Input.ReferenceType switch
        {
            "Employee" when Input.ReferenceEmployeeID <= 0 => "Select an employee reference.",
            "Customer" when Input.ReferenceCustomerID <= 0 => "Select a customer reference.",
            "Vendor" when Input.ReferenceVendorID <= 0 => "Select a vendor reference.",
            "Sales" when Input.ReferenceSalesOrderID <= 0 => "Select a sales order reference.",
            "Purchase" when Input.ReferencePurchaseOrderID <= 0 => "Select a purchase order reference.",
            _ => ""
        };
        return string.IsNullOrEmpty(error);
    }

    private static void BindTaskParams(SqlCommand cmd, TaskInput input, DateTime? dueDate)
    {
        cmd.Parameters.AddWithValue("@Title", input.Title);
        cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(input.Description) ? DBNull.Value : input.Description);
        cmd.Parameters.AddWithValue("@Priority", input.Priority);
        cmd.Parameters.AddWithValue("@TaskStatus", input.TaskStatus);
        cmd.Parameters.AddWithValue("@DueDate", dueDate.HasValue ? dueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@AssignedToEmployeeID", input.AssignedToEmployeeID > 0 ? input.AssignedToEmployeeID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReferenceType", input.ReferenceType);
        cmd.Parameters.AddWithValue("@ReferenceEmployeeID", input.ReferenceEmployeeID > 0 ? input.ReferenceEmployeeID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReferenceCustomerID", input.ReferenceCustomerID > 0 ? input.ReferenceCustomerID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReferenceVendorID", input.ReferenceVendorID > 0 ? input.ReferenceVendorID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReferenceSalesOrderID", input.ReferenceSalesOrderID > 0 ? input.ReferenceSalesOrderID : DBNull.Value);
        cmd.Parameters.AddWithValue("@ReferencePurchaseOrderID", input.ReferencePurchaseOrderID > 0 ? input.ReferencePurchaseOrderID : DBNull.Value);
    }

    private void InsertHistory(SqlConnection conn, SqlTransaction? tx, int taskId,
        string actionType, string? oldStatus, string? newStatus, string? remarks)
    {
        using var cmd = tx == null
            ? new SqlCommand(@"
                INSERT INTO tblTaskHistory (TaskID, ActionType, OldStatus, NewStatus, Remarks, CreatedOn, CreatedByUserID, CreatedByName)
                VALUES (@TaskID, @ActionType, @OldStatus, @NewStatus, @Remarks, GETDATE(), @CreatedByUserID, @CreatedByName);", conn)
            : new SqlCommand(@"
                INSERT INTO tblTaskHistory (TaskID, ActionType, OldStatus, NewStatus, Remarks, CreatedOn, CreatedByUserID, CreatedByName)
                VALUES (@TaskID, @ActionType, @OldStatus, @NewStatus, @Remarks, GETDATE(), @CreatedByUserID, @CreatedByName);", conn, tx);

        cmd.Parameters.AddWithValue("@TaskID", taskId);
        cmd.Parameters.AddWithValue("@ActionType", actionType);
        cmd.Parameters.AddWithValue("@OldStatus", (object?)oldStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NewStatus", (object?)newStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedByUserID", _auth.CurrentUserId.HasValue && _auth.CurrentUserId.Value > 0 ? _auth.CurrentUserId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedByName", _auth.CurrentUsername ?? "");
        cmd.ExecuteNonQuery();
    }

    private void LoadTasks()
    {
        Tasks.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT t.TaskID, t.TaskCode, t.Title, t.Priority, t.TaskStatus, t.DueDate, t.ReferenceType, t.CreatedOn, t.IsActive,
                   ISNULL(LTRIM(RTRIM(ISNULL(ae.DisplayName, ISNULL(ae.FirstName,'') + ' ' + ISNULL(ae.LastName,'')))), '') AS AssignedToName,
                   CASE t.ReferenceType
                     WHEN 'Employee' THEN ISNULL(re.EmployeeCode,'') + ' – ' + ISNULL(LTRIM(RTRIM(ISNULL(re.DisplayName, ISNULL(re.FirstName,'') + ' ' + ISNULL(re.LastName,'')))), '')
                     WHEN 'Customer' THEN ISNULL(c.CustomerCode,'') + ' – ' + ISNULL(c.Name,'')
                     WHEN 'Vendor'   THEN ISNULL(v.VendorCode,'') + ' – ' + ISNULL(v.Name,'')
                     WHEN 'Sales'    THEN ISNULL(so.SalesOrderCode,'') + ' – ' + ISNULL(so.CustomerName,'')
                     WHEN 'Purchase' THEN ISNULL(po.PurchaseOrderCode,'') + ' – ' + ISNULL(po.VendorName,'')
                     ELSE '—'
                   END AS ReferenceDisplay
            FROM tblTask t
            LEFT JOIN tblEmployee ae ON ae.EmployeeID = t.AssignedToEmployeeID
            LEFT JOIN tblEmployee re ON re.EmployeeID = t.ReferenceEmployeeID
            LEFT JOIN tblCustomer c ON c.CustomerID = t.ReferenceCustomerID
            LEFT JOIN tblVendor v ON v.VendorID = t.ReferenceVendorID
            LEFT JOIN tblSalesOrder so ON so.SalesOrderID = t.ReferenceSalesOrderID
            LEFT JOIN tblPurchaseOrder po ON po.PurchaseOrderID = t.ReferencePurchaseOrderID
            WHERE t.IsActive = 1
              AND (@RefType = '' OR t.ReferenceType = @RefType)
              AND (@Status = '' OR t.TaskStatus = @Status)
              AND (@AssignedTo = 0 OR t.AssignedToEmployeeID = @AssignedTo)
              AND (@Search = '' OR t.TaskCode LIKE '%' + @Search + '%' OR t.Title LIKE '%' + @Search + '%'
                   OR ISNULL(c.Name,'') LIKE '%' + @Search + '%'
                   OR ISNULL(v.Name,'') LIKE '%' + @Search + '%'
                   OR ISNULL(so.SalesOrderCode,'') LIKE '%' + @Search + '%'
                   OR ISNULL(po.PurchaseOrderCode,'') LIKE '%' + @Search + '%')
            ORDER BY
                CASE t.TaskStatus
                    WHEN 'Open' THEN 1 WHEN 'In Progress' THEN 2 WHEN 'On Hold' THEN 3
                    WHEN 'Completed' THEN 4 ELSE 5 END,
                CASE t.Priority WHEN 'Urgent' THEN 1 WHEN 'High' THEN 2 WHEN 'Medium' THEN 3 ELSE 4 END,
                t.DueDate ASC, t.TaskID DESC;", conn);

        cmd.Parameters.AddWithValue("@RefType", FilterReferenceType);
        cmd.Parameters.AddWithValue("@Status", FilterStatus);
        cmd.Parameters.AddWithValue("@AssignedTo", FilterAssignedTo);
        cmd.Parameters.AddWithValue("@Search", FilterSearch);
        conn.Open();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            Tasks.Add(new TaskListItem
            {
                TaskID = r.GetInt32(0),
                TaskCode = r.GetString(1),
                Title = r.GetString(2),
                Priority = r.GetString(3),
                TaskStatus = r.GetString(4),
                DueDate = r.IsDBNull(5) ? null : r.GetDateTime(5),
                ReferenceType = r.GetString(6),
                CreatedOn = r.GetDateTime(7),
                IsActive = r.GetBoolean(8),
                AssignedToName = r.IsDBNull(9) ? "" : r.GetString(9).Trim(),
                ReferenceDisplay = r.IsDBNull(10) ? "—" : r.GetString(10).Trim()
            });
        }
    }

    private void LoadStats()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                SUM(CASE WHEN TaskStatus = 'Open' THEN 1 ELSE 0 END),
                SUM(CASE WHEN TaskStatus = 'In Progress' THEN 1 ELSE 0 END),
                SUM(CASE WHEN TaskStatus = 'Completed' THEN 1 ELSE 0 END),
                SUM(CASE WHEN TaskStatus IN ('Open','In Progress','On Hold')
                          AND DueDate IS NOT NULL AND DueDate < CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END)
            FROM tblTask WHERE IsActive = 1;", conn);
        conn.Open();
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            CountOpen = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
            CountInProgress = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
            CountCompleted = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
            CountOverdue = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
        }
    }

    private bool LoadForEdit(int taskId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TaskID, TaskCode, Title, Description, Priority, TaskStatus, DueDate,
                   AssignedToEmployeeID, ReferenceType,
                   ReferenceEmployeeID, ReferenceCustomerID, ReferenceVendorID,
                   ReferenceSalesOrderID, ReferencePurchaseOrderID
            FROM tblTask WHERE TaskID = @ID AND IsActive = 1;", conn);
        cmd.Parameters.AddWithValue("@ID", taskId);
        conn.Open();
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return false;

        Input = new TaskInput
        {
            TaskID = r.GetInt32(0),
            TaskCode = r.GetString(1),
            Title = r.GetString(2),
            Description = r.IsDBNull(3) ? "" : r.GetString(3),
            Priority = r.GetString(4),
            TaskStatus = r.GetString(5),
            DueDate = r.IsDBNull(6) ? "" : r.GetDateTime(6).ToString("yyyy-MM-dd"),
            AssignedToEmployeeID = r.IsDBNull(7) ? 0 : r.GetInt32(7),
            ReferenceType = r.GetString(8),
            ReferenceEmployeeID = r.IsDBNull(9) ? 0 : r.GetInt32(9),
            ReferenceCustomerID = r.IsDBNull(10) ? 0 : r.GetInt32(10),
            ReferenceVendorID = r.IsDBNull(11) ? 0 : r.GetInt32(11),
            ReferenceSalesOrderID = r.IsDBNull(12) ? 0 : r.GetInt32(12),
            ReferencePurchaseOrderID = r.IsDBNull(13) ? 0 : r.GetInt32(13)
        };
        return true;
    }

    private void LoadHistory(int taskId)
    {
        History.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ActionType, OldStatus, NewStatus, Remarks, CreatedByName, CreatedOn
            FROM tblTaskHistory WHERE TaskID = @ID
            ORDER BY CreatedOn DESC, TaskHistoryID DESC;", conn);
        cmd.Parameters.AddWithValue("@ID", taskId);
        conn.Open();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            History.Add(new TaskHistoryItem
            {
                ActionType = r.GetString(0),
                OldStatus = r.IsDBNull(1) ? "" : r.GetString(1),
                NewStatus = r.IsDBNull(2) ? "" : r.GetString(2),
                Remarks = r.IsDBNull(3) ? "" : r.GetString(3),
                CreatedByName = r.IsDBNull(4) ? "" : r.GetString(4),
                CreatedOn = r.GetDateTime(5)
            });
        }
    }

    private void LoadLookups()
    {
        LoadEmployeesLookup();
        Customers = LoadSimpleLookup(
            "SELECT CustomerID, CustomerCode + ' – ' + Name FROM tblCustomer WHERE IsActive = 1 ORDER BY CustomerCode;");
        Vendors = LoadSimpleLookup(
            "SELECT VendorID, VendorCode + ' – ' + Name FROM tblVendor WHERE IsActive = 1 ORDER BY VendorCode;");
        SalesOrders = LoadSimpleLookup(@"
            SELECT SalesOrderID, SalesOrderCode + ' – ' + ISNULL(CustomerName,'')
            FROM tblSalesOrder
            WHERE OrderStatus NOT IN ('Cancelled')
            ORDER BY SalesOrderID DESC;");
        PurchaseOrders = LoadSimpleLookup(@"
            SELECT PurchaseOrderID, PurchaseOrderCode + ' – ' + ISNULL(VendorName,'')
            FROM tblPurchaseOrder
            WHERE OrderStatus NOT IN ('Cancelled')
            ORDER BY PurchaseOrderID DESC;");
    }

    private void LoadEmployeesLookup()
    {
        Employees = LoadSimpleLookup(@"
            SELECT EmployeeID,
                   EmployeeCode + ' – ' + LTRIM(RTRIM(ISNULL(DisplayName, ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''))))
            FROM tblEmployee
            WHERE Status = 'Active'
            ORDER BY EmployeeCode;");
    }

    private List<LookupItem> LoadSimpleLookup(string sql)
    {
        var list = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new LookupItem
            {
                Id = r.GetInt32(0),
                Name = r.IsDBNull(1) ? "" : r.GetString(1)
            });
        }
        return list;
    }

    private string GenerateNextTaskCode(SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        bool own = conn == null;
        conn ??= new SqlConnection(_conn);
        if (own) conn.Open();
        try
        {
            using var cmd = tx == null
                ? new SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(TaskCode, 4, 20) AS INT)), 0) + 1 FROM tblTask WHERE TaskCode LIKE 'TSK%';", conn)
                : new SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(TaskCode, 4, 20) AS INT)), 0) + 1 FROM tblTask WHERE TaskCode LIKE 'TSK%';", conn, tx);
            var next = Convert.ToInt32(cmd.ExecuteScalar());
            return $"TSK{next:D6}";
        }
        finally
        {
            if (own) conn.Dispose();
        }
    }

    private IActionResult RedirectToForm(int taskId, bool editMode)
        => editMode && taskId > 0
            ? RedirectToPage(new { editId = taskId })
            : RedirectToPage(new { newTask = true });

    private void LoadAlert()
    {
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
