using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class AuditReportPage : AppBasePage
    {
        public string PageTitle => "Audit Log Report";
        public List<AuditLogRow> Records { get; private set; } = new List<AuditLogRow>();
        public DateTime? DateFrom { get; private set; }
        public DateTime? DateTo { get; private set; }
        public int TotalRecords { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            if (!Auth.IsAdmin)
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }
            DateFrom = DateTime.Today.AddDays(-30);
            DateTo = DateTime.Today;
            if (DateTime.TryParse(Request.QueryString["dateFrom"], out var df)) DateFrom = df;
            if (DateTime.TryParse(Request.QueryString["dateTo"], out var dt)) DateTo = dt;
            LoadRecords();
        }

        private void LoadRecords()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 200 AuditLogID, ActionAt, Username, ActionType, EntityType, EntityName, Details, Success
FROM tblAuditLog WHERE ActionAt >= @DateFrom AND ActionAt < DATEADD(day,1,@DateTo)
ORDER BY ActionAt DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@DateFrom", DateFrom.Value.Date);
                cmd.Parameters.AddWithValue("@DateTo", DateTo.Value.Date);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new AuditLogRow
                        {
                            Id = Convert.ToInt64(dr["AuditLogID"]),
                            ActionAt = Convert.ToDateTime(dr["ActionAt"]),
                            Username = dr["Username"]?.ToString() ?? "",
                            ActionType = dr["ActionType"].ToString() ?? "",
                            EntityType = dr["EntityType"]?.ToString() ?? "",
                            EntityName = dr["EntityName"]?.ToString() ?? "",
                            Details = dr["Details"]?.ToString() ?? "",
                            Success = Convert.ToBoolean(dr["Success"])
                        });
                    }
                }
            }
            TotalRecords = Records.Count;
        }
    }

    public class AuditLogRow
    {
        public long Id { get; set; }
        public DateTime ActionAt { get; set; }
        public string Username { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string Details { get; set; } = "";
        public bool Success { get; set; }
    }

    public class SkillSetupPage : SimpleListMasterPage
    {
        public override string PageTitle => "Skill Setup";
        public override string ListSql => "SELECT TOP 500 SkillID, SkillCode, SkillName, FieldType, IsActive FROM tblSkill ORDER BY IsActive DESC, SkillCode;";
    }

    public class SoftwareLinkSetupPage : SimpleListMasterPage
    {
        public override string PageTitle => "Software Link Setup";
        public override string ListSql => "SELECT TOP 500 SoftwareLinkID, SoftwareName, SoftwareUrl, Category, IsActive FROM tblSoftwareLink ORDER BY SortOrder, SoftwareName;";
    }

    public class ImageGallerySetupPage : SimpleListMasterPage
    {
        public override string PageTitle => "Image Gallery Setup";
        public override string ListSql => "SELECT TOP 500 GalleryImageID, Title, ImagePath, SortOrder, IsActive FROM tblGalleryImage ORDER BY SortOrder, GalleryImageID;";
    }

    public class EmployeeReportPage : SimpleListMasterPage
    {
        public override string PageTitle => "Employee Directory";
        public override string ListSql => @"
SELECT TOP 500 e.EmployeeCode, e.FirstName + ' ' + e.LastName AS FullName, d.DepartmentName, e.Designation, e.Status
FROM tblEmployee e LEFT JOIN tblDepartment d ON d.DepartmentID=e.DepartmentID
ORDER BY e.FirstName, e.LastName;";
    }

    public class InventoryReportPage : SimpleListMasterPage
    {
        public override string PageTitle => "Inventory Report";
        public override string ListSql => @"
SELECT TOP 500 p.ProductCode, p.ProductName, SUM(CASE WHEN t.TransactionType IN ('IN','GRN') THEN t.Quantity ELSE -t.Quantity END) AS OnHand
FROM tblProduct p LEFT JOIN tblInventoryTransaction t ON t.ProductID=p.ProductID
GROUP BY p.ProductCode, p.ProductName ORDER BY p.ProductCode;";
    }

}
