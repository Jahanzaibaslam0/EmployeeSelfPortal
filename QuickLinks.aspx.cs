using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;

namespace HRMS
{
    public partial class QuickLinksPage : AppBasePage
    {
        public string PageTitle => "Quick Links";
        public List<QuickLinkItem> HrProcessLinks { get; private set; } = new List<QuickLinkItem>();
        public List<QuickLinkItem> SoftwareLinks { get; private set; } = new List<QuickLinkItem>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            HrProcessLinks = new List<QuickLinkItem>
            {
                new QuickLinkItem { Title = "Expense Process", Description = "Submit and manage employee expense claims", Url = "/ExpenseMaster.aspx", Category = "HR Process", Icon = "EP" },
                new QuickLinkItem { Title = "Employee Performance", Description = "Record and review employee performance cycles", Url = "/PerformanceMaster.aspx", Category = "HR Process", Icon = "PF" },
                new QuickLinkItem { Title = "Recruitment Process", Description = "Manage hiring and recruitment workflows", Url = "/RecruitmentMaster.aspx", Category = "HR Process", Icon = "RC" },
                new QuickLinkItem { Title = "Employee Training", Description = "Track employee training and development", Url = "/TrainingMaster.aspx", Category = "HR Process", Icon = "TR" },
            };
            LoadSoftwareLinks();
        }

        private void LoadSoftwareLinks()
        {
            SoftwareLinks.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT SoftwareName, SoftwareUrl, Category, Description
            FROM tblSoftwareLink WHERE IsActive = 1 ORDER BY SortOrder, SoftwareName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var name = dr["SoftwareName"].ToString() ?? "";
                        SoftwareLinks.Add(new QuickLinkItem
                        {
                            Title = name,
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                            Url = dr["SoftwareUrl"].ToString() ?? "",
                            Category = dr["Category"] == DBNull.Value ? "Software" : dr["Category"].ToString() ?? "Software",
                            Icon = name.Length >= 2 ? name.Substring(0, 2).ToUpperInvariant() : name.ToUpperInvariant(),
                            External = true
                        });
                    }
                }
            }
        }
    }
}
