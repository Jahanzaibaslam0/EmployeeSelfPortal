using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class JobRecord
    {
        public int JobID { get; set; }
        public string JobTitle { get; set; } = "";
        public string JobCode { get; set; } = "";
        public int GradeID { get; set; }
        public string GradeName { get; set; } = "";
        public string JobLevel { get; set; } = "";
        public string PositionNumber { get; set; } = "";
        public int ReportsToEmployeeID { get; set; }
        public string ReportsToName { get; set; } = "";
        public int FunctionalManagerEmployeeID { get; set; }
        public string FunctionalManagerName { get; set; } = "";
        public int DottedLineManagerEmployeeID { get; set; }
        public string DottedLineManagerName { get; set; } = "";
        public int BackupApproverEmployeeID { get; set; }
        public string BackupApproverName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class JobSetupPage : AppBasePage
    {
        public static readonly string[] JobLevels =
        {
            "Entry", "Standard", "Senior", "Lead", "Manager", "Director", "Executive"
        };

        public string PageTitle => "Job Setup";
        public JobRecord Input { get; private set; } = new JobRecord { IsActive = true };
        public List<JobRecord> Records { get; private set; } = new List<JobRecord>();
        public List<LookupItem> Grades { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Employees { get; private set; } = new List<LookupItem>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete();
                    return;
                }
                OnPostSave();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadGrades();
            LoadEmployees();

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);
            else
            {
                Input.JobCode = GenerateNextJobCode();
                Input.PositionNumber = GenerateNextPositionNumber();
            }

            LoadRecords();
        }

        private void OnPostSave()
        {
            var jobID = ParseInt(FormString("jobID"));
            var jobTitle = FormString("jobTitle");
            var jobCode = FormString("jobCode");
            var gradeID = ParseInt(FormString("gradeID"));
            var jobLevel = FormString("jobLevel");
            var positionNumber = FormString("positionNumber");
            var reportsToEmployeeID = ParseInt(FormString("reportsToEmployeeID"));
            var functionalManagerEmployeeID = ParseInt(FormString("functionalManagerEmployeeID"));
            var dottedLineManagerEmployeeID = ParseInt(FormString("dottedLineManagerEmployeeID"));
            var backupApproverEmployeeID = ParseInt(FormString("backupApproverEmployeeID"));
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(jobTitle))
            {
                SetAlert("Job Title is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (string.IsNullOrWhiteSpace(jobCode))
            {
                SetAlert("Job Code is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (gradeID <= 0)
            {
                SetAlert("Job Grade is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (string.IsNullOrWhiteSpace(jobLevel))
            {
                SetAlert("Job Level is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (string.IsNullOrWhiteSpace(positionNumber))
            {
                SetAlert("Position Number is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (reportsToEmployeeID <= 0)
            {
                SetAlert("Reports To (Supervisor) is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            if (backupApproverEmployeeID <= 0)
            {
                SetAlert("Backup Approver is required.", "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (jobID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblJob SET
                        JobTitle                    = @JobTitle,
                        JobCode                     = @JobCode,
                        GradeID                     = @GradeID,
                        JobLevel                    = @JobLevel,
                        PositionNumber              = @PositionNumber,
                        ReportsToEmployeeID         = @ReportsToEmployeeID,
                        FunctionalManagerEmployeeID = @FunctionalManagerEmployeeID,
                        DottedLineManagerEmployeeID = @DottedLineManagerEmployeeID,
                        BackupApproverEmployeeID    = @BackupApproverEmployeeID,
                        IsActive                    = @IsActive,
                        ModifiedOn                  = GETDATE(),
                        ModifiedByUserID            = @ModifiedByUserID
                    WHERE JobID = @JobID;", conn))
                        {
                            AddParams(cmd, jobID, jobTitle, jobCode, gradeID, jobLevel, positionNumber,
                                reportsToEmployeeID, functionalManagerEmployeeID,
                                dottedLineManagerEmployeeID, backupApproverEmployeeID, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Job updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblJob
                        (JobTitle, JobCode, GradeID, JobLevel, PositionNumber,
                         ReportsToEmployeeID, FunctionalManagerEmployeeID,
                         DottedLineManagerEmployeeID, BackupApproverEmployeeID, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@JobTitle, @JobCode, @GradeID, @JobLevel, @PositionNumber,
                         @ReportsToEmployeeID, @FunctionalManagerEmployeeID,
                         @DottedLineManagerEmployeeID, @BackupApproverEmployeeID, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, jobTitle, jobCode, gradeID, jobLevel, positionNumber,
                                reportsToEmployeeID, functionalManagerEmployeeID,
                                dottedLineManagerEmployeeID, backupApproverEmployeeID, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Job added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    SetAlert("Job Code or Position Number already exists.", "error");
                else
                    SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(jobID));
                return;
            }

            Response.Redirect("~/JobSetup.aspx");
        }

        private void OnPostDelete()
        {
            var deleteId = ParseInt(FormString("deleteId"));
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
                UPDATE tblJob SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE JobID = @JobID;", conn))
                {
                    cmd.Parameters.AddWithValue("@JobID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Job deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/JobSetup.aspx");
        }

        private string RedirectEdit(int jobID)
            => jobID > 0 ? "~/JobSetup.aspx?editId=" + jobID : "~/JobSetup.aspx";

        private static int ParseInt(string value)
        {
            int n;
            return int.TryParse(value, out n) ? n : 0;
        }

        private static void AddParams(
            SqlCommand cmd, int jobID, string jobTitle, string jobCode, int gradeID,
            string jobLevel, string positionNumber,
            int reportsToEmployeeID, int functionalManagerEmployeeID,
            int dottedLineManagerEmployeeID, int backupApproverEmployeeID, bool isActive)
        {
            if (jobID > 0)
                cmd.Parameters.AddWithValue("@JobID", jobID);

            cmd.Parameters.AddWithValue("@JobTitle", jobTitle.Trim());
            cmd.Parameters.AddWithValue("@JobCode", jobCode.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@GradeID", gradeID);
            cmd.Parameters.AddWithValue("@JobLevel", jobLevel.Trim());
            cmd.Parameters.AddWithValue("@PositionNumber", positionNumber.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@ReportsToEmployeeID", reportsToEmployeeID);
            cmd.Parameters.AddWithValue("@FunctionalManagerEmployeeID",
                functionalManagerEmployeeID <= 0 ? (object)DBNull.Value : functionalManagerEmployeeID);
            cmd.Parameters.AddWithValue("@DottedLineManagerEmployeeID",
                dottedLineManagerEmployeeID <= 0 ? (object)DBNull.Value : dottedLineManagerEmployeeID);
            cmd.Parameters.AddWithValue("@BackupApproverEmployeeID", backupApproverEmployeeID);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadGrades()
        {
            Grades.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT GradeID, GradeName FROM tblGrade
            WHERE IsActive = 1 ORDER BY GradeName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Grades.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["GradeID"]),
                            Name = dr["GradeName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode,
                   FirstName + ' ' + LastName AS FullName
            FROM   tblEmployee
            WHERE  Status = 'Active'
            ORDER BY FirstName, LastName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr["EmployeeCode"].ToString() ?? "";
                        var name = dr["FullName"].ToString() ?? "";
                        Employees.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["EmployeeID"]),
                            Name = code + " – " + name
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int jobId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT JobID, JobTitle, JobCode, GradeID, JobLevel, PositionNumber,
                   ReportsToEmployeeID, FunctionalManagerEmployeeID,
                   DottedLineManagerEmployeeID, BackupApproverEmployeeID, IsActive
            FROM tblJob WHERE JobID = @JobID;", conn))
            {
                cmd.Parameters.AddWithValue("@JobID", jobId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new JobRecord
                    {
                        JobID = jobId,
                        JobTitle = dr["JobTitle"].ToString() ?? "",
                        JobCode = dr["JobCode"].ToString() ?? "",
                        GradeID = IntOrZero(dr["GradeID"]),
                        JobLevel = dr["JobLevel"].ToString() ?? "",
                        PositionNumber = dr["PositionNumber"].ToString() ?? "",
                        ReportsToEmployeeID = IntOrZero(dr["ReportsToEmployeeID"]),
                        FunctionalManagerEmployeeID = IntOrZero(dr["FunctionalManagerEmployeeID"]),
                        DottedLineManagerEmployeeID = IntOrZero(dr["DottedLineManagerEmployeeID"]),
                        BackupApproverEmployeeID = IntOrZero(dr["BackupApproverEmployeeID"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT j.JobID, j.JobTitle, j.JobCode, j.GradeID, g.GradeName,
                   j.JobLevel, j.PositionNumber,
                   j.ReportsToEmployeeID,
                   rt.FirstName + ' ' + rt.LastName AS ReportsToName,
                   j.FunctionalManagerEmployeeID,
                   fm.FirstName + ' ' + fm.LastName AS FunctionalManagerName,
                   j.DottedLineManagerEmployeeID,
                   dm.FirstName + ' ' + dm.LastName AS DottedLineManagerName,
                   j.BackupApproverEmployeeID,
                   ba.FirstName + ' ' + ba.LastName AS BackupApproverName,
                   j.IsActive
            FROM tblJob j
            LEFT JOIN tblGrade g ON g.GradeID = j.GradeID
            LEFT JOIN tblEmployee rt ON rt.EmployeeID = j.ReportsToEmployeeID
            LEFT JOIN tblEmployee fm ON fm.EmployeeID = j.FunctionalManagerEmployeeID
            LEFT JOIN tblEmployee dm ON dm.EmployeeID = j.DottedLineManagerEmployeeID
            LEFT JOIN tblEmployee ba ON ba.EmployeeID = j.BackupApproverEmployeeID
            ORDER BY j.IsActive DESC, j.JobTitle;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new JobRecord
                        {
                            JobID = Convert.ToInt32(dr["JobID"]),
                            JobTitle = dr["JobTitle"].ToString() ?? "",
                            JobCode = dr["JobCode"].ToString() ?? "",
                            GradeID = IntOrZero(dr["GradeID"]),
                            GradeName = Str(dr["GradeName"]),
                            JobLevel = dr["JobLevel"].ToString() ?? "",
                            PositionNumber = dr["PositionNumber"].ToString() ?? "",
                            ReportsToEmployeeID = IntOrZero(dr["ReportsToEmployeeID"]),
                            ReportsToName = Str(dr["ReportsToName"]),
                            FunctionalManagerEmployeeID = IntOrZero(dr["FunctionalManagerEmployeeID"]),
                            FunctionalManagerName = Str(dr["FunctionalManagerName"]),
                            DottedLineManagerEmployeeID = IntOrZero(dr["DottedLineManagerEmployeeID"]),
                            DottedLineManagerName = Str(dr["DottedLineManagerName"]),
                            BackupApproverEmployeeID = IntOrZero(dr["BackupApproverEmployeeID"]),
                            BackupApproverName = Str(dr["BackupApproverName"]),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }
        }

        private string GenerateNextJobCode()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT TOP 1 JobCode FROM tblJob
            WHERE JobCode LIKE 'JC-GEN-%'
            ORDER BY JobCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last) && last.Length >= 10)
                {
                    int num;
                    if (int.TryParse(last.Substring(7), out num))
                        return "JC-GEN-" + (num + 1).ToString("D3");
                }
                return "JC-GEN-001";
            }
        }

        private string GenerateNextPositionNumber()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT TOP 1 PositionNumber FROM tblJob
            WHERE PositionNumber LIKE 'POS-%'
            ORDER BY PositionNumber DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last) && last.Length >= 10)
                {
                    int num;
                    if (int.TryParse(last.Substring(4), out num))
                        return "POS-" + (num + 1).ToString("D6");
                }
                return "POS-000001";
            }
        }

        private static int IntOrZero(object v)
            => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);

        private static string Str(object v)
            => v == null || v == DBNull.Value ? "" : v.ToString() ?? "";
    }
}
