using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    /// <summary>Employee-facing Knowledge Library with RBAC-filtered document list.</summary>
    public class LmsLibraryPage : AppBasePage
    {
        private readonly LmsDocumentService _lms = new LmsDocumentService();

        public string PageTitle => "Knowledge Library";
        public List<LmsDocumentItem> Documents { get; private set; } = new List<LmsDocumentItem>();
        public string SelectedCategory { get; private set; } = "";
        public string SearchTerm { get; private set; } = "";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "info";
        public bool CanManage { get; private set; }
        public string ScopeNote { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!_lms.CanAccessLibrary())
            {
                SetAlert("You do not have access to the Knowledge Library. Contact HR if you need access.", "warning");
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            _lms.EnsureSchema();
            CanManage = _lms.CanManageDocuments();

            var ctx = _lms.GetViewerContext();
            ScopeNote = ctx.IsAdmin || ctx.CanManage
                ? "Administrator view: all active LMS documents are listed."
                : "Showing manuals and reference materials authorized for your profile, department, and role.";

            SelectedCategory = (Request.QueryString["category"] ?? "").Trim();
            SearchTerm = (Request.QueryString["q"] ?? "").Trim();

            // Secure download gate: only return file path if authorized
            var viewId = QueryInt("viewId");
            if (viewId.HasValue && viewId.Value > 0)
            {
                OpenDocument(viewId.Value);
                return;
            }

            try
            {
                var category = LmsCategories.All.Any(c => c.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                    ? SelectedCategory
                    : null;
                Documents = _lms.ListVisibleDocuments(category, SearchTerm);
            }
            catch (Exception ex)
            {
                AlertMessage = "Error loading library: " + ex.Message;
                AlertType = "error";
            }
        }

        private void OpenDocument(int documentId)
        {
            if (!_lms.CanViewDocument(documentId))
            {
                SetAlert("You are not authorized to open this document.", "error");
                Response.Redirect("~/LmsLibrary.aspx");
                return;
            }

            var doc = _lms.GetById(documentId, forSetup: false);
            if (doc == null || string.IsNullOrWhiteSpace(doc.DocumentPath))
            {
                SetAlert("Document file not found.", "error");
                Response.Redirect("~/LmsLibrary.aspx");
                return;
            }

            var href = doc.DocumentPath.StartsWith("~", StringComparison.Ordinal)
                ? ResolveUrl(doc.DocumentPath)
                : (doc.DocumentPath.StartsWith("/", StringComparison.Ordinal)
                    ? ResolveUrl("~" + doc.DocumentPath)
                    : doc.DocumentPath);
            Response.Redirect(href);
        }

        public string FileHref(LmsDocumentItem doc)
        {
            if (doc == null || !doc.HasFile) return "";
            // Force authorization check via viewId rather than raw path exposure in UI if preferred:
            return ResolveUrl("~/LmsLibrary.aspx?viewId=" + doc.DocumentID);
        }
    }
}
