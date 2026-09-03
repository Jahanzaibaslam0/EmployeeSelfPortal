using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public partial class TaskMasterPage : AppBasePage
    {
        public static readonly string[] PriorityOptions = { "Low", "Medium", "High", "Urgent" };
        public static readonly string[] StatusOptions = { "Open", "In Progress", "On Hold", "Completed", "Cancelled" };
        public static readonly string[] ReferenceTypeOptions = { "None", "Employee", "Customer", "Vendor", "Sales", "Purchase" };

        public string PageTitle => "Task Master";
        public List<TaskListItem> Tasks { get; set; } = new List<TaskListItem>();
        public List<TaskHistoryItem> History { get; set; } = new List<TaskHistoryItem>();
        public TaskInput Input { get; set; } = new TaskInput();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> Customers { get; set; } = new List<LookupItem>();
        public List<LookupItem> Vendors { get; set; } = new List<LookupItem>();
        public List<LookupItem> SalesOrders { get; set; } = new List<LookupItem>();
        public List<LookupItem> PurchaseOrders { get; set; } = new List<LookupItem>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public bool IsStatusLocked => Input.TaskStatus == "Completed" || Input.TaskStatus == "Cancelled";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    SoftDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                if (string.Equals(handler, "Complete", StringComparison.OrdinalIgnoreCase))
                {
                    ChangeStatus(int.TryParse(Request.Form["statusId"], out var id) ? id : 0, "Completed", "Task marked as completed.");
                    return;
                }
                if (string.Equals(handler, "Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    ChangeStatus(int.TryParse(Request.Form["statusId"], out var id) ? id : 0, "Cancelled", "Task cancelled.");
                    return;
                }
                if (string.Equals(handler, "Reopen", StringComparison.OrdinalIgnoreCase))
                {
                    ChangeStatus(int.TryParse(Request.Form["statusId"], out var id) ? id : 0, "Open", "Task reopened.");
                    return;
                }
                OnPostSave();
                return;
            }

            var newTask = Request.QueryString["newTask"] == "1" || Request.QueryString["newTask"] == "true";
            OnGet(QueryInt("editId"), newTask);
        }

        private void OnGet(int? editId, bool newTask)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newTask;
            if (ShowForm)
            {
                LoadLookups();
                if (editId.HasValue && editId > 0)
                {
                    if (!LoadForEdit(editId.Value))
                    {
                        SetAlert("Task not found.", "error");
                        Response.Redirect("~/TaskMaster.aspx");
                        return;
                    }
                    EditMode = true;
                    LoadHistory(editId.Value);
                }
                else
                {
                    Input.TaskCode = GenerateNextTaskCode();
                    Input.DueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
                }
            }
            else LoadTasks();
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new TaskInput
            {
                TaskID = int.TryParse(Request.Form["TaskID"], out var tid) ? tid : 0,
                Title = FormString("Title"),
                Description = FormString("Description"),
                Priority = string.IsNullOrWhiteSpace(FormString("Priority")) ? "Medium" : FormString("Priority"),
                TaskStatus = string.IsNullOrWhiteSpace(FormString("TaskStatus")) ? "Open" : FormString("TaskStatus"),
                DueDate = FormString("DueDate"),
                AssignedToEmployeeID = int.TryParse(Request.Form["AssignedToEmployeeID"], out var a) ? a : 0,
                ReferenceType = string.IsNullOrWhiteSpace(FormString("ReferenceType")) ? "None" : FormString("ReferenceType"),
                ReferenceEmployeeID = int.TryParse(Request.Form["ReferenceEmployeeID"], out var re) ? re : 0,
                ReferenceCustomerID = int.TryParse(Request.Form["ReferenceCustomerID"], out var rc) ? rc : 0,
                ReferenceVendorID = int.TryParse(Request.Form["ReferenceVendorID"], out var rv) ? rv : 0,
                ReferenceSalesOrderID = int.TryParse(Request.Form["ReferenceSalesOrderID"], out var rs) ? rs : 0,
                ReferencePurchaseOrderID = int.TryParse(Request.Form["ReferencePurchaseOrderID"], out var rp) ? rp : 0
            };
            NormalizeReferenceIds();

            if (string.IsNullOrWhiteSpace(Input.Title))
            {
                SetAlert("Task title is required.", "error");
                Response.Redirect(EditMode && Input.TaskID > 0 ? "~/TaskMaster.aspx?editId=" + Input.TaskID : "~/TaskMaster.aspx?newTask=1");
                return;
            }
            if (!PriorityOptions.Contains(Input.Priority)) Input.Priority = "Medium";
            if (!StatusOptions.Contains(Input.TaskStatus)) Input.TaskStatus = "Open";
            if (!ReferenceTypeOptions.Contains(Input.ReferenceType)) Input.ReferenceType = "None";
            if (!ValidateReferenceSelection(out var refError))
            {
                SetAlert(refError, "error");
                Response.Redirect(EditMode && Input.TaskID > 0 ? "~/TaskMaster.aspx?editId=" + Input.TaskID : "~/TaskMaster.aspx?newTask=1");
                return;
            }

            DateTime? dueDate = null;
            if (!string.IsNullOrWhiteSpace(Input.DueDate))
            {
                if (!DateTime.TryParse(Input.DueDate, out var parsedDue))
                {
                    SetAlert("Invalid due date.", "error");
                    Response.Redirect(EditMode && Input.TaskID > 0 ? "~/TaskMaster.aspx?editId=" + Input.TaskID : "~/TaskMaster.aspx?newTask=1");
                    return;
                }
                dueDate = parsedDue.Date;
            }

            try
            {
                int taskId = Input.TaskID;
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        string oldStatus = "";
                        if (EditMode && taskId > 0)
                        {
                            using (var chk = new SqlCommand("SELECT TaskStatus, TaskCode FROM tblTask WHERE TaskID=@ID AND IsActive=1;", conn, tx))
                            {
                                chk.Parameters.AddWithValue("@ID", taskId);
                                using (var r = chk.ExecuteReader())
                                {
                                    if (!r.Read())
                                    {
                                        SetAlert("Task not found or inactive.", "error");
                                        Response.Redirect("~/TaskMaster.aspx");
                                        return;
                                    }
                                    oldStatus = r.GetString(0);
                                    Input.TaskCode = r.GetString(1);
                                    if (oldStatus == "Completed" || oldStatus == "Cancelled")
                                    {
                                        SetAlert("Cannot edit a " + oldStatus.ToLower() + " task.", "error");
                                        Response.Redirect("~/TaskMaster.aspx");
                                        return;
                                    }
                                }
                            }
                            using (var upd = new SqlCommand(@"
UPDATE tblTask SET Title=@Title, Description=@Description, Priority=@Priority, TaskStatus=@TaskStatus,
  DueDate=@DueDate, AssignedToEmployeeID=@AssignedToEmployeeID, ReferenceType=@ReferenceType,
  ReferenceEmployeeID=@ReferenceEmployeeID, ReferenceCustomerID=@ReferenceCustomerID,
  ReferenceVendorID=@ReferenceVendorID, ReferenceSalesOrderID=@ReferenceSalesOrderID,
  ReferencePurchaseOrderID=@ReferencePurchaseOrderID,
  CompletedOn=CASE WHEN @TaskStatus='Completed' AND CompletedOn IS NULL THEN GETDATE()
                   WHEN @TaskStatus<>'Completed' THEN NULL ELSE CompletedOn END,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE TaskID=@TaskID;", conn, tx))
                            {
                                BindTaskParams(upd, Input, dueDate);
                                upd.Parameters.AddWithValue("@TaskID", taskId);
                                AuditHelper.AddModifiedBy(upd, Auth.CurrentUserId);
                                upd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            Input.TaskCode = GenerateNextTaskCode(conn, tx);
                            using (var ins = new SqlCommand(@"
INSERT INTO tblTask (TaskCode, Title, Description, Priority, TaskStatus, DueDate,
  AssignedToEmployeeID, ReferenceType, ReferenceEmployeeID, ReferenceCustomerID, ReferenceVendorID,
  ReferenceSalesOrderID, ReferencePurchaseOrderID, CompletedOn, IsActive, CreatedOn, CreatedByUserID)
OUTPUT INSERTED.TaskID
VALUES (@TaskCode, @Title, @Description, @Priority, @TaskStatus, @DueDate,
  @AssignedToEmployeeID, @ReferenceType, @ReferenceEmployeeID, @ReferenceCustomerID, @ReferenceVendorID,
  @ReferenceSalesOrderID, @ReferencePurchaseOrderID,
  CASE WHEN @TaskStatus='Completed' THEN GETDATE() ELSE NULL END, 1, GETDATE(), @CreatedByUserID);", conn, tx))
                            {
                                ins.Parameters.AddWithValue("@TaskCode", Input.TaskCode);
                                BindTaskParams(ins, Input, dueDate);
                                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                                taskId = (int)ins.ExecuteScalar();
                                Input.TaskID = taskId;
                            }
                        }

                        if (!EditMode || !string.Equals(oldStatus, Input.TaskStatus, StringComparison.OrdinalIgnoreCase))
                            InsertHistory(conn, tx, taskId, EditMode ? "StatusChange" : "Create", EditMode ? oldStatus : null, Input.TaskStatus,
                                EditMode ? "Status changed to " + Input.TaskStatus : "Task created");
                        else if (EditMode)
                            InsertHistory(conn, tx, taskId, "Update", oldStatus, Input.TaskStatus, "Task details updated");

                        tx.Commit();
                    }
                }

                Audit.Log(EditMode ? "Update" : "Create", "Task", taskId, Input.TaskCode, "TaskMaster", "/TaskMaster.aspx", "OnPostSave",
                    "Status: " + Input.TaskStatus + ", Ref: " + Input.ReferenceType);
                SetAlert(EditMode ? "Task updated successfully." : "Task created successfully.");
                Response.Redirect("~/TaskMaster.aspx?editId=" + taskId);
            }
            catch (Exception ex)
            {
                SetAlert("Error saving task: " + ex.Message, "error");
                Response.Redirect(EditMode && Input.TaskID > 0 ? "~/TaskMaster.aspx?editId=" + Input.TaskID : "~/TaskMaster.aspx?newTask=1");
            }
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                string code = null;
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var get = new SqlCommand("SELECT TaskCode FROM tblTask WHERE TaskID=@ID;", conn))
                    {
                        get.Parameters.AddWithValue("@ID", deleteId);
                        code = get.ExecuteScalar() as string;
                    }
                    using (var cmd = new SqlCommand("UPDATE tblTask SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE TaskID=@ID;", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", deleteId);
                        AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                    InsertHistory(conn, null, deleteId, "Delete", null, null, "Task removed");
                }
                Audit.Log("Delete", "Task", deleteId, code, "TaskMaster", "/TaskMaster.aspx", "SoftDelete");
                SetAlert("Task removed successfully.");
            }
            catch (Exception ex) { SetAlert("Error removing task: " + ex.Message, "error"); }
            Response.Redirect("~/TaskMaster.aspx");
        }

        private void ChangeStatus(int taskId, string newStatus, string successMessage)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        string oldStatus, code;
                        using (var get = new SqlCommand("SELECT TaskStatus, TaskCode FROM tblTask WHERE TaskID=@ID AND IsActive=1;", conn, tx))
                        {
                            get.Parameters.AddWithValue("@ID", taskId);
                            using (var r = get.ExecuteReader())
                            {
                                if (!r.Read())
                                {
                                    SetAlert("Task not found.", "error");
                                    Response.Redirect("~/TaskMaster.aspx");
                                    return;
                                }
                                oldStatus = r.GetString(0);
                                code = r.GetString(1);
                            }
                        }
                        using (var upd = new SqlCommand(@"
UPDATE tblTask SET TaskStatus=@Status,
  CompletedOn=CASE WHEN @Status='Completed' THEN GETDATE() ELSE NULL END,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE TaskID=@ID;", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@Status", newStatus);
                            upd.Parameters.AddWithValue("@ID", taskId);
                            AuditHelper.AddModifiedBy(upd, Auth.CurrentUserId);
                            upd.ExecuteNonQuery();
                        }
                        InsertHistory(conn, tx, taskId, "StatusChange", oldStatus, newStatus, successMessage);
                        tx.Commit();
                        Audit.Log("StatusChange", "Task", taskId, code, "TaskMaster", "/TaskMaster.aspx", "ChangeStatus", oldStatus + " → " + newStatus);
                    }
                }
                SetAlert(successMessage);
                Response.Redirect("~/TaskMaster.aspx?editId=" + taskId);
            }
            catch (Exception ex)
            {
                SetAlert("Error updating status: " + ex.Message, "error");
                Response.Redirect("~/TaskMaster.aspx");
            }
        }

        private void NormalizeReferenceIds()
        {
            switch (Input.ReferenceType)
            {
                case "Employee":
                    Input.ReferenceCustomerID = Input.ReferenceVendorID = Input.ReferenceSalesOrderID = Input.ReferencePurchaseOrderID = 0; break;
                case "Customer":
                    Input.ReferenceEmployeeID = Input.ReferenceVendorID = Input.ReferenceSalesOrderID = Input.ReferencePurchaseOrderID = 0; break;
                case "Vendor":
                    Input.ReferenceEmployeeID = Input.ReferenceCustomerID = Input.ReferenceSalesOrderID = Input.ReferencePurchaseOrderID = 0; break;
                case "Sales":
                    Input.ReferenceEmployeeID = Input.ReferenceCustomerID = Input.ReferenceVendorID = Input.ReferencePurchaseOrderID = 0; break;
                case "Purchase":
                    Input.ReferenceEmployeeID = Input.ReferenceCustomerID = Input.ReferenceVendorID = Input.ReferenceSalesOrderID = 0; break;
                default:
                    Input.ReferenceType = "None";
                    Input.ReferenceEmployeeID = Input.ReferenceCustomerID = Input.ReferenceVendorID = Input.ReferenceSalesOrderID = Input.ReferencePurchaseOrderID = 0;
                    break;
            }
        }

        private bool ValidateReferenceSelection(out string error)
        {
            error = "";
            if (Input.ReferenceType == "Employee" && Input.ReferenceEmployeeID <= 0) error = "Select an employee reference.";
            else if (Input.ReferenceType == "Customer" && Input.ReferenceCustomerID <= 0) error = "Select a customer reference.";
            else if (Input.ReferenceType == "Vendor" && Input.ReferenceVendorID <= 0) error = "Select a vendor reference.";
            else if (Input.ReferenceType == "Sales" && Input.ReferenceSalesOrderID <= 0) error = "Select a sales order reference.";
            else if (Input.ReferenceType == "Purchase" && Input.ReferencePurchaseOrderID <= 0) error = "Select a purchase order reference.";
            return string.IsNullOrEmpty(error);
        }

        private static void BindTaskParams(SqlCommand cmd, TaskInput input, DateTime? dueDate)
        {
            cmd.Parameters.AddWithValue("@Title", input.Title);
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(input.Description) ? (object)DBNull.Value : input.Description);
            cmd.Parameters.AddWithValue("@Priority", input.Priority);
            cmd.Parameters.AddWithValue("@TaskStatus", input.TaskStatus);
            cmd.Parameters.AddWithValue("@DueDate", dueDate.HasValue ? (object)dueDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@AssignedToEmployeeID", input.AssignedToEmployeeID > 0 ? (object)input.AssignedToEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceType", input.ReferenceType);
            cmd.Parameters.AddWithValue("@ReferenceEmployeeID", input.ReferenceEmployeeID > 0 ? (object)input.ReferenceEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceCustomerID", input.ReferenceCustomerID > 0 ? (object)input.ReferenceCustomerID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceVendorID", input.ReferenceVendorID > 0 ? (object)input.ReferenceVendorID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceSalesOrderID", input.ReferenceSalesOrderID > 0 ? (object)input.ReferenceSalesOrderID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferencePurchaseOrderID", input.ReferencePurchaseOrderID > 0 ? (object)input.ReferencePurchaseOrderID : DBNull.Value);
        }

        private void InsertHistory(SqlConnection conn, SqlTransaction tx, int taskId, string actionType, string oldStatus, string newStatus, string remarks)
        {
            using (var cmd = tx == null
                ? new SqlCommand(@"INSERT INTO tblTaskHistory (TaskID, ActionType, OldStatus, NewStatus, Remarks, CreatedOn, CreatedByUserID, CreatedByName)
VALUES (@TaskID, @ActionType, @OldStatus, @NewStatus, @Remarks, GETDATE(), @CreatedByUserID, @CreatedByName);", conn)
                : new SqlCommand(@"INSERT INTO tblTaskHistory (TaskID, ActionType, OldStatus, NewStatus, Remarks, CreatedOn, CreatedByUserID, CreatedByName)
VALUES (@TaskID, @ActionType, @OldStatus, @NewStatus, @Remarks, GETDATE(), @CreatedByUserID, @CreatedByName);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@TaskID", taskId);
                cmd.Parameters.AddWithValue("@ActionType", actionType);
                cmd.Parameters.AddWithValue("@OldStatus", (object)oldStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NewStatus", (object)newStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedByUserID", Auth.CurrentUserId.HasValue && Auth.CurrentUserId.Value > 0 ? (object)Auth.CurrentUserId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedByName", Auth.CurrentUsername ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        private void LoadTasks()
        {
            Tasks.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT t.TaskID, t.TaskCode, t.Title, t.Priority, t.TaskStatus, t.DueDate, t.ReferenceType, t.IsActive,
       ISNULL(LTRIM(RTRIM(ISNULL(ae.DisplayName, ISNULL(ae.FirstName,'')+' '+ISNULL(ae.LastName,'')))), '') AS AssignedToName,
       CASE t.ReferenceType
         WHEN 'Employee' THEN ISNULL(re.EmployeeCode,'')+' – '+ISNULL(LTRIM(RTRIM(ISNULL(re.DisplayName, ISNULL(re.FirstName,'')+' '+ISNULL(re.LastName,'')))), '')
         WHEN 'Customer' THEN ISNULL(c.CustomerCode,'')+' – '+ISNULL(c.Name,'')
         WHEN 'Vendor' THEN ISNULL(v.VendorCode,'')+' – '+ISNULL(v.Name,'')
         WHEN 'Sales' THEN ISNULL(so.SalesOrderCode,'')+' – '+ISNULL(so.CustomerName,'')
         WHEN 'Purchase' THEN ISNULL(po.PurchaseOrderCode,'')+' – '+ISNULL(po.VendorName,'')
         ELSE '—' END AS ReferenceDisplay
FROM tblTask t
LEFT JOIN tblEmployee ae ON ae.EmployeeID=t.AssignedToEmployeeID
LEFT JOIN tblEmployee re ON re.EmployeeID=t.ReferenceEmployeeID
LEFT JOIN tblCustomer c ON c.CustomerID=t.ReferenceCustomerID
LEFT JOIN tblVendor v ON v.VendorID=t.ReferenceVendorID
LEFT JOIN tblSalesOrder so ON so.SalesOrderID=t.ReferenceSalesOrderID
LEFT JOIN tblPurchaseOrder po ON po.PurchaseOrderID=t.ReferencePurchaseOrderID
WHERE t.IsActive=1
ORDER BY t.TaskID DESC;", conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Tasks.Add(new TaskListItem
                        {
                            TaskID = r.GetInt32(0),
                            TaskCode = r.GetString(1),
                            Title = r.GetString(2),
                            Priority = r.GetString(3),
                            TaskStatus = r.GetString(4),
                            DueDate = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5),
                            ReferenceType = r.GetString(6),
                            IsActive = r.GetBoolean(7),
                            AssignedToName = r.IsDBNull(8) ? "" : r.GetString(8).Trim(),
                            ReferenceDisplay = r.IsDBNull(9) ? "—" : r.GetString(9).Trim()
                        });
                    }
                }
            }
        }

        private bool LoadForEdit(int taskId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT TaskID, TaskCode, Title, Description, Priority, TaskStatus, DueDate,
       AssignedToEmployeeID, ReferenceType, ReferenceEmployeeID, ReferenceCustomerID,
       ReferenceVendorID, ReferenceSalesOrderID, ReferencePurchaseOrderID
FROM tblTask WHERE TaskID=@ID AND IsActive=1;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", taskId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadHistory(int taskId)
        {
            History.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ActionType, OldStatus, NewStatus, Remarks, CreatedByName, CreatedOn
FROM tblTaskHistory WHERE TaskID=@ID ORDER BY CreatedOn DESC, TaskHistoryID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", taskId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadLookups()
        {
            Employees = LoadSimpleLookup(@"
SELECT EmployeeID, EmployeeCode+' – '+LTRIM(RTRIM(ISNULL(DisplayName, ISNULL(FirstName,'')+' '+ISNULL(LastName,''))))
FROM tblEmployee WHERE Status='Active' ORDER BY EmployeeCode;");
            Customers = LoadSimpleLookup("SELECT CustomerID, CustomerCode+' – '+Name FROM tblCustomer WHERE IsActive=1 ORDER BY CustomerCode;");
            Vendors = LoadSimpleLookup("SELECT VendorID, VendorCode+' – '+Name FROM tblVendor WHERE IsActive=1 ORDER BY VendorCode;");
            SalesOrders = LoadSimpleLookup("SELECT SalesOrderID, SalesOrderCode+' – '+ISNULL(CustomerName,'') FROM tblSalesOrder WHERE OrderStatus NOT IN ('Cancelled') ORDER BY SalesOrderID DESC;");
            PurchaseOrders = LoadSimpleLookup("SELECT PurchaseOrderID, PurchaseOrderCode+' – '+ISNULL(VendorName,'') FROM tblPurchaseOrder WHERE OrderStatus NOT IN ('Cancelled') ORDER BY PurchaseOrderID DESC;");
        }

        private List<LookupItem> LoadSimpleLookup(string sql)
        {
            var list = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new LookupItem { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
            }
            return list;
        }

        private string GenerateNextTaskCode(SqlConnection conn = null, SqlTransaction tx = null)
        {
            var own = conn == null;
            if (own) { conn = new SqlConnection(Conn); conn.Open(); }
            try
            {
                using (var cmd = tx == null
                    ? new SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(TaskCode,4,20) AS INT)),0)+1 FROM tblTask WHERE TaskCode LIKE 'TSK%';", conn)
                    : new SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(TaskCode,4,20) AS INT)),0)+1 FROM tblTask WHERE TaskCode LIKE 'TSK%';", conn, tx))
                {
                    return "TSK" + Convert.ToInt32(cmd.ExecuteScalar()).ToString("D6");
                }
            }
            finally { if (own) conn.Dispose(); }
        }
    }
}
