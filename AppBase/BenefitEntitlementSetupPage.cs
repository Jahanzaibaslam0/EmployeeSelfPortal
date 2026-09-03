using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class BenefitLinkRecord
    {
        public int DetailID { get; set; }
        public int BenefitID { get; set; }
        public string BenefitCode { get; set; } = "";
        public string BenefitName { get; set; } = "";
        public string BenefitType { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class BenefitEntitlementSetupPage : AppBasePage
    {
        public string PageTitle => "Benefit Entitlement Setup";

        public List<LookupRecord> Entitlements { get; set; } = new List<LookupRecord>();
        public LookupRecord EntitlementInput { get; set; } = new LookupRecord { IsActive = true };
        public List<BenefitLinkRecord> LinkedBenefits { get; set; } = new List<BenefitLinkRecord>();
        public List<BenefitItem> AvailableBenefits { get; set; } = new List<BenefitItem>();

        public int ManageEntitlementID { get; set; }
        public string ManageEntitlementName { get; set; } = "";
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public Dictionary<int, int> EntitlementBenefitCounts { get; set; } = new Dictionary<int, int>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = (Request.Form["__handler"] ?? "SaveEntitlement").Trim();
                switch (handler.ToLowerInvariant())
                {
                    case "deleteentitlement":
                        DeleteEntitlement(FormInt("deleteId"));
                        return;
                    case "linkbenefit":
                        LinkBenefit(FormInt("benefitEntitlementID"), FormInt("benefitID"));
                        return;
                    case "unlinkbenefit":
                        UnlinkBenefit(FormInt("detailID"), FormInt("benefitEntitlementID"));
                        return;
                    default:
                        SaveEntitlement();
                        return;
                }
            }

            LoadPage(QueryInt("editId"), QueryInt("manageId"));
        }

        private void LoadPage(int? editId, int? manageId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadEntitlements();

            if (editId.HasValue && editId > 0)
                LoadEntitlementForEdit(editId.Value);

            int targetId = manageId ?? editId ?? 0;
            if (targetId > 0)
            {
                ManageEntitlementID = targetId;
                ManageEntitlementName = GetEntitlementName(targetId);
                LoadLinkedBenefits(targetId);
                LoadAvailableBenefits(targetId);
            }
        }

        private void SaveEntitlement()
        {
            var itemId = FormInt("itemId");
            var itemName = FormString("itemName");
            var aliasName = FormString("aliasName");
            var isActive = FormBool("isActive");
            var manageId = FormInt("manageId");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                SetAlert("Benefit Entitlement name is required.", "error");
                RedirectManage(manageId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    int savedId;

                    if (itemId > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblBenefitEntitlement SET
    BenefitEntitlementName = @Name,
    AliasName              = @Alias,
    IsActive               = @IsActive,
    ModifiedOn             = GETDATE(),
    ModifiedByUserID       = @ModifiedByUserID
WHERE BenefitEntitlementID = @Id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", itemId);
                            cmd.Parameters.AddWithValue("@Name", itemName.Trim());
                            cmd.Parameters.AddWithValue("@Alias", string.IsNullOrWhiteSpace(aliasName) ? (object)DBNull.Value : aliasName.Trim());
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        savedId = itemId;
                        SetAlert("Benefit Entitlement updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblBenefitEntitlement (BenefitEntitlementName, AliasName, IsActive, CreatedOn, CreatedByUserID)
VALUES (@Name, @Alias, @IsActive, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", itemName.Trim());
                            cmd.Parameters.AddWithValue("@Alias", string.IsNullOrWhiteSpace(aliasName) ? (object)DBNull.Value : aliasName.Trim());
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            savedId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        SetAlert("Benefit Entitlement added successfully.");
                    }

                    Response.Redirect("~/BenefitEntitlementSetup.aspx?manageId=" + savedId);
                    return;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("Entitlement name already exists.", "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            RedirectManage(manageId);
        }

        private void DeleteEntitlement(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblBenefitEntitlement SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE BenefitEntitlementID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Benefit Entitlement removed.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/BenefitEntitlementSetup.aspx");
        }

        private void LinkBenefit(int benefitEntitlementID, int benefitID)
        {
            if (benefitID <= 0)
            {
                SetAlert("Please select a benefit to add.", "error");
                RedirectManage(benefitEntitlementID);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();

                    using (var checkCmd = new SqlCommand(@"
SELECT COUNT(*) FROM tblBenefitEntitlementDetail
WHERE BenefitEntitlementID = @EntID AND BenefitID = @BenID;", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@EntID", benefitEntitlementID);
                        checkCmd.Parameters.AddWithValue("@BenID", benefitID);
                        var exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (exists > 0)
                        {
                            SetAlert("This benefit is already added to this entitlement.", "error");
                            RedirectManage(benefitEntitlementID);
                            return;
                        }
                    }

                    using (var cmd = new SqlCommand(@"
INSERT INTO tblBenefitEntitlementDetail (BenefitEntitlementID, BenefitID)
VALUES (@EntID, @BenID);", conn))
                    {
                        cmd.Parameters.AddWithValue("@EntID", benefitEntitlementID);
                        cmd.Parameters.AddWithValue("@BenID", benefitID);
                        cmd.ExecuteNonQuery();
                    }
                }
                SetAlert("Benefit added to entitlement.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            RedirectManage(benefitEntitlementID);
        }

        private void UnlinkBenefit(int detailID, int benefitEntitlementID)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
DELETE FROM tblBenefitEntitlementDetail WHERE DetailID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", detailID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Benefit removed from entitlement.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            RedirectManage(benefitEntitlementID);
        }

        private void LoadEntitlements()
        {
            Entitlements.Clear();
            EntitlementBenefitCounts.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT e.BenefitEntitlementID, e.BenefitEntitlementName, e.AliasName, e.IsActive,
       (SELECT COUNT(*) FROM tblBenefitEntitlementDetail d
        WHERE d.BenefitEntitlementID = e.BenefitEntitlementID) AS BenefitCount
FROM tblBenefitEntitlement e
ORDER BY e.IsActive DESC, e.BenefitEntitlementName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var rec = new LookupRecord
                        {
                            Id = Convert.ToInt32(dr["BenefitEntitlementID"]),
                            Name = dr["BenefitEntitlementName"].ToString() ?? "",
                            AliasName = dr["AliasName"] == DBNull.Value ? "" : (dr["AliasName"].ToString() ?? ""),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        };
                        Entitlements.Add(rec);
                        EntitlementBenefitCounts[rec.Id] = Convert.ToInt32(dr["BenefitCount"]);
                    }
                }
            }
        }

        private void LoadEntitlementForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT BenefitEntitlementID, BenefitEntitlementName, AliasName, IsActive
FROM tblBenefitEntitlement WHERE BenefitEntitlementID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        EntitlementInput = new LookupRecord
                        {
                            Id = Convert.ToInt32(dr["BenefitEntitlementID"]),
                            Name = dr["BenefitEntitlementName"].ToString() ?? "",
                            AliasName = dr["AliasName"] == DBNull.Value ? "" : (dr["AliasName"].ToString() ?? ""),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        };
                    }
                }
            }
        }

        private void LoadLinkedBenefits(int entitlementId)
        {
            LinkedBenefits.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DetailID, b.BenefitID, b.BenefitCode, b.BenefitName, b.BenefitType, b.Description
FROM tblBenefitEntitlementDetail d
INNER JOIN tblBenefit b ON b.BenefitID = d.BenefitID
WHERE d.BenefitEntitlementID = @Id
ORDER BY b.BenefitName;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", entitlementId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        LinkedBenefits.Add(new BenefitLinkRecord
                        {
                            DetailID = Convert.ToInt32(dr["DetailID"]),
                            BenefitID = Convert.ToInt32(dr["BenefitID"]),
                            BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString(),
                            BenefitName = dr["BenefitName"].ToString(),
                            BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString(),
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString()
                        });
                    }
                }
            }
        }

        private void LoadAvailableBenefits(int entitlementId)
        {
            AvailableBenefits.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT BenefitID, BenefitCode, BenefitName, BenefitType
FROM tblBenefit
WHERE IsActive = 1
  AND BenefitID NOT IN (
        SELECT BenefitID FROM tblBenefitEntitlementDetail
        WHERE BenefitEntitlementID = @Id
  )
ORDER BY BenefitName;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", entitlementId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        AvailableBenefits.Add(new BenefitItem
                        {
                            BenefitID = Convert.ToInt32(dr["BenefitID"]),
                            BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString(),
                            BenefitName = dr["BenefitName"].ToString(),
                            BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString()
                        });
                    }
                }
            }
        }

        private string GetEntitlementName(int id)
        {
            var found = Entitlements.FirstOrDefault(e => e.Id == id);
            return found != null ? found.Name : "";
        }

        private void RedirectManage(int manageId)
        {
            if (manageId > 0)
                Response.Redirect("~/BenefitEntitlementSetup.aspx?manageId=" + manageId);
            else
                Response.Redirect("~/BenefitEntitlementSetup.aspx");
        }

    }
}
