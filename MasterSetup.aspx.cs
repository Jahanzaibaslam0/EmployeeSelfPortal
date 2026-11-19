using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class MasterSetupPage : AppBasePage
    {
        public string PageTitle => "Master Setup";
        public List<SetupCategoryGroup> Categories { get; private set; } = new List<SetupCategoryGroup>();
        public int VisibleLinkCount { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                OnGet();
        }

        private void OnGet()
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!Auth.IsAdmin && !Perms.CanAccessMasterSetup())
            {
                Response.Redirect("~/Home.aspx");
                return;
            }

            var writeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<AppForms.FormDef> visibleForms = AppForms.All.Where(AppForms.IsSetupCatalogForm);
            if (Auth.IsAdmin)
            {
                foreach (var f in visibleForms) writeKeys.Add(f.Key);
            }
            else if (Auth.CurrentUserId.HasValue)
            {
                var granted = Perms.GetUserPermissions(Auth.CurrentUserId.Value)
                    .Where(p => p.CanRead)
                    .ToList();
                var readKeys = new HashSet<string>(granted.Select(p => p.FormKey), StringComparer.OrdinalIgnoreCase);
                foreach (var p in granted.Where(x => x.CanWrite)) writeKeys.Add(p.FormKey);
                visibleForms = visibleForms.Where(f => readKeys.Contains(f.Key));
            }
            else
            {
                visibleForms = Enumerable.Empty<AppForms.FormDef>();
            }

            var grouped = visibleForms
                .GroupBy(f => f.Category)
                .OrderBy(g => g.Min(f => f.SortOrder));

            foreach (var g in grouped)
            {
                var links = g
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .Select(f => ToLink(f, writeKeys.Contains(f.Key)))
                    .ToList();
                if (links.Count == 0) continue;

                Categories.Add(new SetupCategoryGroup
                {
                    Category = g.Key,
                    Icon = CategoryIcon(g.Key),
                    Links = links
                });
            }

            VisibleLinkCount = Categories.Sum(c => c.Links.Count);
        }

        private static SetupLinkItem ToLink(AppForms.FormDef form, bool canWrite)
        {
            return new SetupLinkItem
            {
                FormKey = form.Key,
                Title = DisplayTitle(form.Name),
                Url = ToPageUrl(form.Path),
                Icon = IconFromName(form.Name),
                CanWrite = canWrite,
                AccessLabel = canWrite ? "Edit" : "View"
            };
        }

        public static string CategoryAnchor(string category)
        {
            if (string.IsNullOrEmpty(category)) return "setup-section";
            return "setup-" + category.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        private static string DisplayTitle(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.EndsWith(" Setup", StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - 6);
            return name;
        }

        private static string ToPageUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            if (path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)) return path;
            return path + ".aspx";
        }

        private static string CategoryIcon(string category)
        {
            if (string.Equals(category, "Organization Setup", StringComparison.OrdinalIgnoreCase)) return "ORG";
            if (string.Equals(category, "Employee Setup", StringComparison.OrdinalIgnoreCase)) return "EMP";
            if (string.Equals(category, "Customer Setup", StringComparison.OrdinalIgnoreCase)) return "CST";
            if (string.Equals(category, "Product Setup", StringComparison.OrdinalIgnoreCase)) return "PRD";
            if (string.Equals(category, "Security", StringComparison.OrdinalIgnoreCase)) return "SEC";
            if (string.IsNullOrEmpty(category) || category.Length < 2) return "ST";
            return category.Substring(0, 2).ToUpperInvariant();
        }

        private static string IconFromName(string name)
        {
            var core = DisplayTitle(name ?? "");
            var parts = core.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (char.ToUpperInvariant(parts[0][0]).ToString() + char.ToUpperInvariant(parts[1][0]));
            if (core.Length >= 2) return core.Substring(0, 2).ToUpperInvariant();
            return string.IsNullOrEmpty(core) ? "ST" : core.ToUpperInvariant();
        }
    }
}
