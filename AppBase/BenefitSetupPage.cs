using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class BenefitItem
    {
        public int BenefitID { get; set; }
        public string BenefitCode { get; set; } = "";
        public string BenefitName { get; set; } = "";
        public string BenefitType { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class BenefitSetupPage : AppBasePage
    {
        public static readonly string[] BenefitTypes =
        {
            "Medical", "Leave", "Allowance", "Insurance", "Vehicle",
            "Housing", "Education", "Pension", "Bonus", "Fuel", "Other"
        };

        public IReadOnlyList<string> BenefitTypeOptions => BenefitTypes;

        public string PageTitle => "Benefit Setup";
        public List<BenefitItem> Records { get; set; } = new List<BenefitItem>();
        public BenefitItem Input { get; set; } = new BenefitItem { IsActive = true };
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
            LoadRecords();

            if (editId.HasValue && editId > 0)
                LoadForEdit(editId.Value);
            else
                Input.BenefitCode = GenerateNextCode();
        }

        private void Save()
        {
            var benefitId = FormInt("benefitId");
            var benefitCode = FormString("benefitCode");
            var benefitName = FormString("benefitName");
            var benefitType = FormString("benefitType");
            var description = FormString("description");
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(benefitName))
            {
                SetAlert("Benefit name is required.", "error");
                Response.Redirect("~/BenefitSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();

                    if (benefitId > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblBenefit SET
    BenefitCode = @Code,
    BenefitName = @Name,
    BenefitType = @Type,
    Description = @Desc,
    IsActive = @IsActive,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE BenefitID = @Id;", conn))
                        {
                            BindSaveParams(cmd, benefitId, benefitCode, benefitName, benefitType, description, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Benefit updated successfully.");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(benefitCode))
                            benefitCode = GenerateNextCode();

                        using (var cmd = new SqlCommand(@"
INSERT INTO tblBenefit
    (BenefitCode, BenefitName, BenefitType, Description, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@Code, @Name, @Type, @Desc, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            BindSaveParams(cmd, 0, benefitCode, benefitName, benefitType, description, isActive, isInsert: true);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Benefit added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("Benefit name already exists.", "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/BenefitSetup.aspx");
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblBenefit SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE BenefitID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Benefit deactivated.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/BenefitSetup.aspx");
        }

        private static void BindSaveParams(
            SqlCommand cmd, int benefitId, string benefitCode, string benefitName,
            string benefitType, string description, bool isActive, bool isInsert = false)
        {
            if (!isInsert) cmd.Parameters.AddWithValue("@Id", benefitId);
            cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(benefitCode) ? (object)DBNull.Value : benefitCode.Trim());
            cmd.Parameters.AddWithValue("@Name", benefitName.Trim());
            cmd.Parameters.AddWithValue("@Type", string.IsNullOrWhiteSpace(benefitType) ? (object)DBNull.Value : benefitType.Trim());
            cmd.Parameters.AddWithValue("@Desc", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT BenefitID, BenefitCode, BenefitName, BenefitType, Description, IsActive
FROM tblBenefit
ORDER BY IsActive DESC, BenefitName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new BenefitItem
                        {
                            BenefitID = Convert.ToInt32(dr["BenefitID"]),
                            BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString(),
                            BenefitName = dr["BenefitName"].ToString(),
                            BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString(),
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT BenefitID, BenefitCode, BenefitName, BenefitType, Description, IsActive
FROM tblBenefit WHERE BenefitID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        Input = new BenefitItem
                        {
                            BenefitID = Convert.ToInt32(dr["BenefitID"]),
                            BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString(),
                            BenefitName = dr["BenefitName"].ToString(),
                            BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString(),
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        };
                    }
                }
            }
        }

        private string GenerateNextCode()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 BenefitCode FROM tblBenefit
WHERE BenefitCode LIKE 'GB-BN-%'
ORDER BY BenefitCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last) && last.Length >= 9
                    && int.TryParse(last.Substring(6), out int num))
                    return "GB-BN-" + (num + 1).ToString("D5");
                return "GB-BN-00001";
            }
        }

    }
}
