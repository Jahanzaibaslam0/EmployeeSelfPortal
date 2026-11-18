using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS.Controls
{
    public partial class ContactHubList : UserControl
    {
        public string HubView { get; set; } = ContactHubService.ViewAll;
        public string ListPath { get; set; } = "/ContactMaster.aspx";
        public string PageHeading { get; set; } = "Contact List";
        public bool LockTypeFilter { get; set; }

        protected ContactHubFilter Filter { get; private set; } = new ContactHubFilter();
        protected ContactHubResult Result { get; private set; } = new ContactHubResult();
        protected bool CanWrite { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            var perms = new PermissionService();
            CanWrite = auth.IsAdmin || perms.CanWrite("ContactMaster");
            var svc = new ContactHubService();
            svc.EnsureSchema();
            new ContactChangeWorkflowService().EnsureSchema();
            Filter = svc.FromRequest(Request, HubView);
            if (LockTypeFilter)
            {
                if (string.Equals(HubView, ContactHubService.ViewCustomer, StringComparison.OrdinalIgnoreCase))
                    Filter.ContactType = "Customer";
                if (string.Equals(HubView, ContactHubService.ViewVendor, StringComparison.OrdinalIgnoreCase))
                    Filter.ContactType = "Vendor";
                if (string.Equals(HubView, ContactHubService.ViewEmployee, StringComparison.OrdinalIgnoreCase))
                    Filter.ContactType = "Employee";
                if (string.Equals(HubView, ContactHubService.ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase))
                    Filter.ContactType = "NoBusinessLink";
            }
            Result = svc.Search(Filter, auth.CurrentUserId);
        }

        protected string SortUrl(string col)
        {
            var dir = string.Equals(Filter.Sort, col, StringComparison.OrdinalIgnoreCase) && Filter.Dir == "asc" ? "desc" : "asc";
            return ListPath + "?" + ContactHubService.Query(Filter, 1, col, dir);
        }

        protected string PageUrl(int page)
            => ListPath + "?" + ContactHubService.Query(Filter, page, Filter.Sort, Filter.Dir);

        protected string ChipUrl(string chip)
        {
            var f = new ContactHubFilter
            {
                View = Filter.View,
                Search = Filter.Search,
                Position = Filter.Position,
                Branch = Filter.Branch,
                Sort = Filter.Sort,
                Dir = Filter.Dir,
                ContactType = "All",
                Verification = "All",
                AccessStatus = "All",
                RecordType = "All"
            };
            switch ((chip ?? "").Trim())
            {
                case "Customers": f.ContactType = "Customer"; break;
                case "Vendors": f.ContactType = "Vendor"; break;
                case "Employees": f.ContactType = "Employee"; break;
                case "ContactPersons": f.RecordType = "People"; break;
                case "NoBusinessLink": f.ContactType = "NoBusinessLink"; break;
                case "ToVerify": f.Verification = "ToVerify"; break;
                case "Restricted": f.AccessStatus = "Restricted"; break;
                case "Quarantined": f.AccessStatus = "Quarantined"; break;
            }
            return ListPath + "?" + ContactHubService.Query(f, 1, f.Sort, f.Dir);
        }

        protected bool ChipActive(string chip)
        {
            var type = Filter.ContactType ?? "All";
            var verify = Filter.Verification ?? "All";
            var access = Filter.AccessStatus ?? "All";
            var record = Filter.RecordType ?? "All";
            var typeAll = string.Equals(type, "All", StringComparison.OrdinalIgnoreCase);
            var verifyAll = string.Equals(verify, "All", StringComparison.OrdinalIgnoreCase);
            var accessAll = string.Equals(access, "All", StringComparison.OrdinalIgnoreCase);
            var recordAll = string.Equals(record, "All", StringComparison.OrdinalIgnoreCase);
            switch ((chip ?? "").Trim())
            {
                case "All":
                    return typeAll && verifyAll && accessAll && recordAll;
                case "Customers":
                    return type.Equals("Customer", StringComparison.OrdinalIgnoreCase) || type.Equals("Customers", StringComparison.OrdinalIgnoreCase);
                case "Vendors":
                    return type.Equals("Vendor", StringComparison.OrdinalIgnoreCase) || type.Equals("Vendors", StringComparison.OrdinalIgnoreCase);
                case "Employees":
                    return type.Equals("Employee", StringComparison.OrdinalIgnoreCase) || type.Equals("Employees", StringComparison.OrdinalIgnoreCase);
                case "ContactPersons":
                    return record.Equals("People", StringComparison.OrdinalIgnoreCase) && typeAll && verifyAll && accessAll;
                case "NoBusinessLink":
                    return ContactHubService.IsNoBusinessLink(type);
                case "ToVerify":
                    return verify.Equals("ToVerify", StringComparison.OrdinalIgnoreCase) || verify.Equals("Pending", StringComparison.OrdinalIgnoreCase);
                case "Restricted":
                    return access.Equals("Restricted", StringComparison.OrdinalIgnoreCase);
                case "Quarantined":
                    return access.Equals("Quarantined", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        protected string NewContactUrl()
        {
            if (string.Equals(HubView, ContactHubService.ViewCustomer, StringComparison.OrdinalIgnoreCase))
                return "/ContactMaster.aspx?newContact=1&presetType=Customer";
            if (string.Equals(HubView, ContactHubService.ViewVendor, StringComparison.OrdinalIgnoreCase))
                return "/ContactMaster.aspx?newContact=1&presetType=Vendor";
            if (string.Equals(HubView, ContactHubService.ViewEmployee, StringComparison.OrdinalIgnoreCase))
                return "/ContactMaster.aspx?newContact=1&presetType=Employee";
            if (string.Equals(HubView, ContactHubService.ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase))
                return "/ContactMaster.aspx?newContact=1&presetType=" + Server.UrlEncode("Guest/Others");
            return "/ContactMaster.aspx?newContact=1";
        }

        protected string ExportUrl(int id)
            => "/ContactExport.aspx?id=" + id + "&view=" + HubView;

        protected static string H(string value)
            => System.Web.HttpUtility.HtmlEncode(value ?? "");

        protected static string B64(string value)
            => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value ?? ""));
    }
}
