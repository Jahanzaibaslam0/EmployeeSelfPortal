using System;
using HRMS.Services;

namespace HRMS
{
    public partial class MyWorkspacePage : AppBasePage
    {
        private readonly MyWorkspaceService _svc = new MyWorkspaceService();

        public string PageTitle => "My Workspace";
        public MyWorkspaceModel Model { get; private set; } = new MyWorkspaceModel();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            if (!Auth.CurrentUserId.HasValue)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            Model = _svc.Load();
        }

        /// <summary>ASCII-only SVG icons so IIS/Windows-1252 aspx encoding cannot garble glyphs.</summary>
        protected string WsIcon(string name)
        {
            const string box = "xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\"";
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "check":
                    return "<svg " + box + "><rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"3\"/><path d=\"M8 12.5l2.5 2.5L16 9\"/></svg>";
                case "doc":
                    return "<svg " + box + "><path d=\"M8 4h6l6 6v10a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z\"/><path d=\"M14 4v6h6\"/><path d=\"M9 14h6M9 18h4\"/></svg>";
                case "clock":
                    return "<svg " + box + "><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/></svg>";
                case "megaphone":
                    return "<svg " + box + "><path d=\"M4 10v4h3l5 4V6L7 10H4z\"/><path d=\"M16 9.5a3.5 3.5 0 0 1 0 5\"/><path d=\"M18.5 7a7 7 0 0 1 0 10\"/></svg>";
                case "plus":
                    return "<svg " + box + "><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 8v8M8 12h8\"/></svg>";
                case "user":
                    return "<svg " + box + "><circle cx=\"12\" cy=\"8\" r=\"3.5\"/><path d=\"M5.5 19a6.5 6.5 0 0 1 13 0\"/></svg>";
                case "book":
                    return "<svg " + box + "><path d=\"M5 5h11a3 3 0 0 1 3 3v11H8a3 3 0 0 0-3 3V5z\"/><path d=\"M5 19a3 3 0 0 1 3-3h11\"/></svg>";
                case "folder":
                    return "<svg " + box + "><path d=\"M3 7h6l2 2h10v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7z\"/></svg>";
                case "chevron":
                    return "<svg " + box + "><path d=\"M9 6l6 6-6 6\"/></svg>";
                default:
                    return "<svg " + box + "><circle cx=\"12\" cy=\"12\" r=\"9\"/></svg>";
            }
        }
    }
}
