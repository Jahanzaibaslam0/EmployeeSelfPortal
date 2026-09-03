using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class GallerySlide
    {
        public int GalleryImageID { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
    }

    public class HomeProcessLink
    {
        public string FormKey { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public partial class HomePage : AppBasePage
    {
        public List<GallerySlide> Slides { get; private set; } = new List<GallerySlide>();
        public List<HomeProcessLink> ProcessLinks { get; private set; } = new List<HomeProcessLink>();
        public bool ShowProfileSyncWarning { get; private set; }
        public string ProfileSyncMessage { get; private set; } =
            "Your user account is not linked to an employee profile. Some self-service features may be unavailable.";

        protected void Page_Load(object sender, EventArgs e)
        {
            ShowProfileSyncWarning = Session["ShowProfileSyncWarning"] != null;
            if (ShowProfileSyncWarning) Session.Remove("ShowProfileSyncWarning");

            LoadSlides();
            var profileAccess = new EmployeeProfileAccessService();
            ProcessLinks = GetProcessLinks()
                .Where(l =>
                {
                    if (string.Equals(l.FormKey, EmployeeProfileAccessService.MyDocumentsFormKey, StringComparison.OrdinalIgnoreCase))
                        return profileAccess.CanAccessMyDocuments();
                    if (string.Equals(l.FormKey, LmsDocumentService.LibraryFormKey, StringComparison.OrdinalIgnoreCase))
                        return new LmsDocumentService().CanAccessLibrary();
                    return string.IsNullOrEmpty(l.FormKey) || Perms.CanRead(l.FormKey);
                })
                .ToList();
        }

        private static List<HomeProcessLink> GetProcessLinks()
        {
            return new List<HomeProcessLink>
            {
                new HomeProcessLink { FormKey = "UserProfile", Title = "My Profile", Description = "View and update your personal employee information", Url = "/UserProfile.aspx", Category = "Self Service", Icon = "MP" },
                new HomeProcessLink { FormKey = "MyDocuments", Title = "My Documents", Description = "View and access your uploaded documents", Url = "/MyDocuments.aspx", Category = "Self Service", Icon = "DC" },
                new HomeProcessLink { FormKey = "LmsLibrary", Title = "Knowledge Library", Description = "Access manuals, SOPs, policies, and department reference materials", Url = "/LmsLibrary.aspx", Category = "Self Service", Icon = "KL" },
                new HomeProcessLink { FormKey = "Notifications", Title = "Announcements", Description = "View company announcements and HR notifications", Url = "/Notifications.aspx", Category = "Communication", Icon = "AN" },
                new HomeProcessLink { FormKey = "Memorandums", Title = "Memorandums", Description = "Read active memorandums and policy documents", Url = "/Memorandums.aspx", Category = "Communication", Icon = "MM" },
                new HomeProcessLink { FormKey = "PerformanceMaster", Title = "Employee Performance", Description = "Record and review employee performance cycles", Url = "/PerformanceMaster.aspx", Category = "HR Process", Icon = "PF" },
                new HomeProcessLink { FormKey = "TrainingMaster", Title = "Employee Training", Description = "Track employee training and development records", Url = "/TrainingMaster.aspx", Category = "HR Process", Icon = "TR" },
                new HomeProcessLink { FormKey = "ExpenseMaster", Title = "Expense Process", Description = "Submit and manage employee expense claims", Url = "/ExpenseMaster.aspx", Category = "HR Process", Icon = "EP" },
                new HomeProcessLink { FormKey = "RecruitmentMaster", Title = "Recruitment Process", Description = "Manage hiring and recruitment workflows", Url = "/RecruitmentMaster.aspx", Category = "HR Process", Icon = "RC" },
                new HomeProcessLink { FormKey = "EmployeeReport", Title = "Employee Directory", Description = "Search, view, and export the internal employee directory", Url = "/EmployeeReport.aspx", Category = "HR Process", Icon = "ED" },
                new HomeProcessLink { FormKey = "EmployeeMaster", Title = "Employee Master", Description = "Maintain employee records and employment details", Url = "/EmployeeMaster.aspx", Category = "Master Data", Icon = "EM" },
                new HomeProcessLink { FormKey = "PositionMaster", Title = "Position Master", Description = "Manage positions, assignments, and reporting structure", Url = "/PositionMaster.aspx", Category = "Master Data", Icon = "PM" },
                new HomeProcessLink { FormKey = "QuickLinks", Title = "Quick Links", Description = "HR processes and organization software shortcuts", Url = "/QuickLinks.aspx", Category = "Shortcuts", Icon = "QL" },
            };
        }

        private void LoadSlides()
        {
            Slides.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
                SELECT GalleryImageID, Title, Description, ImagePath
                FROM tblGalleryImage WHERE IsActive = 1
                ORDER BY SortOrder, GalleryImageID;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Slides.Add(new GallerySlide
                        {
                            GalleryImageID = Convert.ToInt32(dr["GalleryImageID"]),
                            Title = dr["Title"].ToString() ?? "",
                            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                            ImagePath = dr["ImagePath"].ToString() ?? ""
                        });
                    }
                }
            }
        }
    }
}
