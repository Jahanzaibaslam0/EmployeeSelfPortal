using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        public string DisplayUrl => GalleryImagePage.UrlFor(GalleryImageID);
    }

    public partial class HomePage : AppBasePage
    {
        public List<GallerySlide> Slides { get; private set; } = new List<GallerySlide>();
        public List<WorkstationModuleView> Modules { get; private set; } = new List<WorkstationModuleView>();
        public bool ShowProfileSyncWarning { get; private set; }
        public string ProfileSyncMessage { get; private set; } =
            "Your user account is not linked to an employee profile. Some self-service features may be unavailable.";

        protected void Page_Load(object sender, EventArgs e)
        {
            ShowProfileSyncWarning = Session["ShowProfileSyncWarning"] != null;
            if (ShowProfileSyncWarning) Session.Remove("ShowProfileSyncWarning");

            LoadSlides();
            Modules = WorkstationNav.BuildVisible()
                .FindAll(m => !string.Equals(m.Id, "admin", StringComparison.OrdinalIgnoreCase));
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = "Home";
        }

        private void LoadSlides()
        {
            Slides.Clear();
            try
            {
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
                                ImagePath = dr["ImagePath"] == DBNull.Value ? "" : dr["ImagePath"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch
            {
                Slides.Clear();
            }
        }
    }
}
