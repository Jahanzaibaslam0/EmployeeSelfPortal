using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class NotificationRecord
    {
        public int NotificationID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime ValidTillDate { get; set; } = DateTime.Today.AddMonths(1);
        public bool IsActive { get; set; } = true;
    }

    public class NotificationSetupPage : AppBasePage
    {
        public string PageTitle => "Notification Setup";
        public NotificationRecord Input { get; set; } = new NotificationRecord();
        public List<NotificationRecord> Records { get; set; } = new List<NotificationRecord>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    SoftDelete(FormInt("deleteId"));
                    return;
                }
                Save();
                return;
            }

            LoadPage(QueryInt("editId"));
        }

        private void LoadPage(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadDepartments();
            if (editId.HasValue && editId > 0)
                LoadForEdit(editId.Value);
            LoadRecords();
        }

        private void Save()
        {
            var notificationID = FormInt("notificationID");
            var notificationName = FormString("notificationName");
            var description = FormString("description");
            var departmentID = FormInt("departmentID");
            var isActive = FormBool("isActive");

            DateTime startDate;
            DateTime validTillDate;
            if (!DateTime.TryParse(FormString("startDate"), out startDate))
                startDate = DateTime.Today;
            if (!DateTime.TryParse(FormString("validTillDate"), out validTillDate))
                validTillDate = DateTime.Today.AddMonths(1);

            if (string.IsNullOrWhiteSpace(notificationName))
            {
                SetAlert("Notification name is required.", "error");
                Response.Redirect("~/NotificationSetup.aspx");
                return;
            }

            if (validTillDate.Date < startDate.Date)
            {
                SetAlert("Valid till date cannot be before start date.", "error");
                Response.Redirect("~/NotificationSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    var deptParam = departmentID > 0 ? (object)departmentID : DBNull.Value;

                    if (notificationID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblNotification
SET NotificationName = @Name,
    Description = @Description,
    DepartmentID = @DepartmentID,
    StartDate = @StartDate,
    ValidTillDate = @ValidTillDate,
    IsActive = @IsActive,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE NotificationID = @Id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", notificationID);
                            cmd.Parameters.AddWithValue("@Name", notificationName.Trim());
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
                            cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                            cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Notification updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblNotification
    (NotificationName, Description, DepartmentID, StartDate, ValidTillDate, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@Name, @Description, @DepartmentID, @StartDate, @ValidTillDate, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", notificationName.Trim());
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
                            cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                            cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Notification added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/NotificationSetup.aspx");
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblNotification
SET IsActive = 0,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE NotificationID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Notification deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/NotificationSetup.aspx");
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DepartmentID, DepartmentName
FROM tblDepartment
WHERE IsActive = 1
ORDER BY DepartmentName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Departments.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["DepartmentID"]),
                            Name = dr["DepartmentName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT n.NotificationID, n.NotificationName, n.Description,
       ISNULL(n.DepartmentID, 0) AS DepartmentID,
       d.DepartmentName, n.StartDate, n.ValidTillDate, n.IsActive
FROM tblNotification n
LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
WHERE n.NotificationID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        Input = ReadRecord(dr);
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT n.NotificationID, n.NotificationName, n.Description,
       ISNULL(n.DepartmentID, 0) AS DepartmentID,
       ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName,
       n.StartDate, n.ValidTillDate, n.IsActive
FROM tblNotification n
LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
ORDER BY n.IsActive DESC, n.StartDate DESC, n.NotificationID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Records.Add(ReadRecord(dr));
                }
            }
        }

        private static NotificationRecord ReadRecord(SqlDataReader dr) => new NotificationRecord
        {
            NotificationID = Convert.ToInt32(dr["NotificationID"]),
            Name = dr["NotificationName"].ToString() ?? "",
            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
            DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
            DepartmentName = dr["DepartmentName"] == DBNull.Value ? "" : dr["DepartmentName"].ToString() ?? "",
            StartDate = Convert.ToDateTime(dr["StartDate"]),
            ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };
    }
}
