using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class MemorandumRecord
    {
        public int MemorandumID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime ValidTillDate { get; set; } = DateTime.Today.AddMonths(1);
        public bool IsActive { get; set; } = true;
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
    }

    public class MemorandumSetupPage : AppBasePage
    {
        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg"
        };

        public string PageTitle => "Memorandum Setup";
        public MemorandumRecord Input { get; set; } = new MemorandumRecord();
        public List<MemorandumRecord> Records { get; set; } = new List<MemorandumRecord>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            Form.Enctype = "multipart/form-data";

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
            var memorandumID = FormInt("memorandumID");
            var memorandumName = FormString("memorandumName");
            var description = FormString("description");
            var departmentID = FormInt("departmentID");
            var isActive = FormBool("isActive");

            DateTime startDate;
            DateTime validTillDate;
            if (!DateTime.TryParse(FormString("startDate"), out startDate))
                startDate = DateTime.Today;
            if (!DateTime.TryParse(FormString("validTillDate"), out validTillDate))
                validTillDate = DateTime.Today.AddMonths(1);

            if (string.IsNullOrWhiteSpace(memorandumName))
            {
                SetAlert("Memorandum name is required.", "error");
                Response.Redirect("~/MemorandumSetup.aspx");
                return;
            }

            if (validTillDate.Date < startDate.Date)
            {
                SetAlert("Valid till date cannot be before start date.", "error");
                Response.Redirect("~/MemorandumSetup.aspx");
                return;
            }

            try
            {
                string docPath = null;
                string originalName = null;

                if (memorandumID > 0)
                {
                    var existing = LoadRecordById(memorandumID);
                    docPath = existing != null ? existing.DocumentPath : null;
                    originalName = existing != null ? existing.OriginalFileName : null;
                }

                var documentFile = Request.Files["documentFile"];
                var hasNewFile = documentFile != null && documentFile.ContentLength > 0;

                if (hasNewFile && memorandumID > 0)
                {
                    var saved = SaveDocumentFile(documentFile, memorandumID);
                    docPath = saved.Item1;
                    originalName = saved.Item2;
                }

                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    var deptParam = departmentID > 0 ? (object)departmentID : DBNull.Value;

                    if (memorandumID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblMemorandum
SET MemorandumName = @Name,
    Description = @Description,
    DepartmentID = @DepartmentID,
    StartDate = @StartDate,
    ValidTillDate = @ValidTillDate,
    IsActive = @IsActive,
    DocumentPath = @DocumentPath,
    OriginalFileName = @OriginalFileName,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE MemorandumID = @Id;", conn))
                        {
                            BindParams(cmd, memorandumID, memorandumName, description, deptParam,
                                startDate, validTillDate, isActive, docPath, originalName);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Memorandum updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblMemorandum
    (MemorandumName, Description, DepartmentID, StartDate, ValidTillDate, IsActive,
     DocumentPath, OriginalFileName, CreatedOn, CreatedByUserID)
VALUES
    (@Name, @Description, @DepartmentID, @StartDate, @ValidTillDate, @IsActive,
     @DocumentPath, @OriginalFileName, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                        {
                            BindParams(cmd, 0, memorandumName, description, deptParam,
                                startDate, validTillDate, isActive, docPath, originalName, isInsert: true);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            var newId = Convert.ToInt32(cmd.ExecuteScalar());

                            if (hasNewFile && newId > 0)
                            {
                                var saved = SaveDocumentFile(documentFile, newId);
                                using (var upd = new SqlCommand(@"
UPDATE tblMemorandum
SET DocumentPath = @Path, OriginalFileName = @Orig
WHERE MemorandumID = @Id;", conn))
                                {
                                    upd.Parameters.AddWithValue("@Path", string.IsNullOrWhiteSpace(saved.Item1) ? (object)DBNull.Value : saved.Item1);
                                    upd.Parameters.AddWithValue("@Orig", string.IsNullOrWhiteSpace(saved.Item2) ? (object)DBNull.Value : saved.Item2);
                                    upd.Parameters.AddWithValue("@Id", newId);
                                    upd.ExecuteNonQuery();
                                }
                            }
                        }
                        SetAlert("Memorandum added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/MemorandumSetup.aspx");
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblMemorandum
SET IsActive = 0,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE MemorandumID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Memorandum deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/MemorandumSetup.aspx");
        }

        private static void BindParams(
            SqlCommand cmd, int id, string name, string description, object deptParam,
            DateTime startDate, DateTime validTillDate, bool isActive,
            string docPath, string originalName, bool isInsert = false)
        {
            if (!isInsert) cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
            cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
            cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
            cmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? (object)DBNull.Value : originalName);
        }

        private Tuple<string, string> SaveDocumentFile(HttpPostedFile file, int memorandumId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("File type not allowed. Use PDF, Word, Excel, or image files.");

            var uploads = Server.MapPath("~/uploads/memorandums");
            Directory.CreateDirectory(uploads);

            var safeName = "memo_" + memorandumId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            var fullPath = Path.Combine(uploads, safeName);
            file.SaveAs(fullPath);

            return Tuple.Create("/uploads/memorandums/" + safeName, Path.GetFileName(file.FileName));
        }

        private MemorandumRecord LoadRecordById(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT MemorandumID, MemorandumName, Description,
       ISNULL(DepartmentID, 0) AS DepartmentID,
       StartDate, ValidTillDate, IsActive, DocumentPath, OriginalFileName
FROM tblMemorandum WHERE MemorandumID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return new MemorandumRecord
                    {
                        MemorandumID = Convert.ToInt32(dr["MemorandumID"]),
                        Name = dr["MemorandumName"].ToString() ?? "",
                        Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                        DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                        StartDate = Convert.ToDateTime(dr["StartDate"]),
                        ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                        OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? ""
                    };
                }
            }
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DepartmentID, DepartmentName FROM tblDepartment
WHERE IsActive = 1 ORDER BY DepartmentName;", conn))
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
            Input = LoadRecordById(id) ?? new MemorandumRecord();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName
FROM tblMemorandum m
LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
WHERE m.MemorandumID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var name = cmd.ExecuteScalar();
                Input.DepartmentName = name != null ? name.ToString() : "";
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT m.MemorandumID, m.MemorandumName, m.Description,
       ISNULL(m.DepartmentID, 0) AS DepartmentID,
       ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName,
       m.StartDate, m.ValidTillDate, m.IsActive,
       m.DocumentPath, m.OriginalFileName
FROM tblMemorandum m
LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
ORDER BY m.IsActive DESC, m.StartDate DESC, m.MemorandumID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new MemorandumRecord
                        {
                            MemorandumID = Convert.ToInt32(dr["MemorandumID"]),
                            Name = dr["MemorandumName"].ToString() ?? "",
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                            DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                            DepartmentName = dr["DepartmentName"].ToString() ?? "",
                            StartDate = Convert.ToDateTime(dr["StartDate"]),
                            ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                            OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

    }
}
