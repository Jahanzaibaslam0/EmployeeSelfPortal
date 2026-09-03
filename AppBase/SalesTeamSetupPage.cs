using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class SalesTeamRecord
    {
        public int SalesTeamID { get; set; }
        public string SalesTeamCode { get; set; } = "";
        public string SalesTeamName { get; set; } = "";
        public int DivisionID { get; set; }
        public string DivisionName { get; set; } = "";
        public string AliasName { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class SalesTeamSetupPage : AppBasePage
    {
        public string PageTitle => "Sales Team Setup";
        public SalesTeamRecord Input { get; private set; } = new SalesTeamRecord { IsActive = true };
        public List<SalesTeamRecord> Records { get; private set; } = new List<SalesTeamRecord>();
        public List<LookupItem> Divisions { get; private set; } = new List<LookupItem>();
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
            LoadDivisions();

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);

            LoadRecords();
        }

        private void OnPostSave()
        {
            var salesTeamID = 0;
            int.TryParse(FormString("salesTeamID"), out salesTeamID);
            var salesTeamCode = FormString("salesTeamCode");
            var salesTeamName = FormString("salesTeamName");
            var divisionID = 0;
            int.TryParse(FormString("divisionID"), out divisionID);
            var aliasName = FormString("aliasName");
            var description = FormString("description");
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(salesTeamCode))
            {
                SetAlert("Sales Team Code is required.", "error");
                Response.Redirect("~/SalesTeamSetup.aspx");
                return;
            }
            if (string.IsNullOrWhiteSpace(salesTeamName))
            {
                SetAlert("Sales Team Name is required.", "error");
                Response.Redirect("~/SalesTeamSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (salesTeamID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblSalesTeam
                    SET SalesTeamCode = @Code,
                        SalesTeamName = @Name,
                        DivisionID    = @DivisionID,
                        AliasName     = @AliasName,
                        Description   = @Description,
                        IsActive      = @IsActive,
                        ModifiedOn    = GETDATE()
                    WHERE SalesTeamID = @ID;", conn))
                        {
                            AddParams(cmd, salesTeamID, salesTeamCode, salesTeamName, divisionID, aliasName, description, isActive);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Sales Team updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblSalesTeam
                        (SalesTeamCode, SalesTeamName, DivisionID, AliasName, Description, IsActive)
                    VALUES
                        (@Code, @Name, @DivisionID, @AliasName, @Description, @IsActive);", conn))
                        {
                            AddParams(cmd, 0, salesTeamCode, salesTeamName, divisionID, aliasName, description, isActive);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Sales Team added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    SetAlert("A Sales Team with this code already exists.", "error");
                else
                    SetAlert("Error: " + ex.Message, "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/SalesTeamSetup.aspx");
        }

        private void OnPostDelete()
        {
            var deleteId = 0;
            int.TryParse(FormString("deleteId"), out deleteId);
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
                UPDATE tblSalesTeam
                SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE SalesTeamID = @ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Sales Team removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/SalesTeamSetup.aspx");
        }

        private void LoadDivisions()
        {
            Divisions.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT DivisionID, DivisionName
            FROM tblDivision
            WHERE IsActive = 1
            ORDER BY DivisionName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Divisions.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["DivisionID"]),
                            Name = dr["DivisionName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT s.SalesTeamID, s.SalesTeamCode, s.SalesTeamName,
                   ISNULL(s.DivisionID, 0) AS DivisionID,
                   ISNULL(d.DivisionName, '') AS DivisionName,
                   s.AliasName, s.Description, s.IsActive
            FROM tblSalesTeam s
            LEFT JOIN tblDivision d ON d.DivisionID = s.DivisionID
            WHERE s.SalesTeamID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read()) Input = ReadRecord(dr);
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT s.SalesTeamID, s.SalesTeamCode, s.SalesTeamName,
                   ISNULL(s.DivisionID, 0) AS DivisionID,
                   ISNULL(d.DivisionName, '') AS DivisionName,
                   s.AliasName, s.Description, s.IsActive
            FROM tblSalesTeam s
            LEFT JOIN tblDivision d ON d.DivisionID = s.DivisionID
            ORDER BY s.IsActive DESC, s.SalesTeamCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Records.Add(ReadRecord(dr));
                }
            }
        }

        private static SalesTeamRecord ReadRecord(SqlDataReader dr)
        {
            return new SalesTeamRecord
            {
                SalesTeamID = Convert.ToInt32(dr["SalesTeamID"]),
                SalesTeamCode = dr["SalesTeamCode"].ToString() ?? "",
                SalesTeamName = dr["SalesTeamName"].ToString() ?? "",
                DivisionID = Convert.ToInt32(dr["DivisionID"]),
                DivisionName = dr["DivisionName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }

        private static void AddParams(SqlCommand cmd, int id, string code, string name,
            int divisionID, string alias, string description, bool isActive)
        {
            cmd.Parameters.AddWithValue("@ID", id);
            cmd.Parameters.AddWithValue("@Code", code.Trim());
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@DivisionID", divisionID <= 0 ? (object)DBNull.Value : divisionID);
            cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }
    }
}
