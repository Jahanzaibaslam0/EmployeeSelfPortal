using System;
using System.Collections.Generic;
using System.Web;

namespace HRMS.Services
{
    public class WorkstationNavItem
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string Access { get; set; } = "Form";
        public bool Visible { get; set; }
        public bool Active { get; set; }
    }

    public class WorkstationModuleView
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<WorkstationNavItem> Items { get; set; } = new List<WorkstationNavItem>();
        public bool Visible { get; set; }
        public bool Expanded { get; set; }
        public int VisibleCount { get { return Items.FindAll(i => i.Visible).Count; } }
    }

    /// <summary>
    /// Display-only module catalog. Does not change permissions or process URLs.
    /// </summary>
    public static class WorkstationNav
    {
        public static List<WorkstationModuleView> BuildVisible(string currentPath = null)
        {
            var path = AppForms.NormalizePath(currentPath ?? CurrentPath());
            var auth = new AuthService();
            var perms = new PermissionService();
            var profile = new EmployeeProfileAccessService();
            var modules = Catalog();

            foreach (var mod in modules)
            {
                foreach (var item in mod.Items)
                {
                    item.Visible = CanSee(item, auth, perms, profile);
                    item.Active = item.Visible && IsActive(item.Url, path);
                }
                mod.Visible = mod.Items.Exists(i => i.Visible) || string.Equals(mod.Id, "qms", StringComparison.OrdinalIgnoreCase);
                mod.Expanded = mod.Items.Exists(i => i.Active);
            }

            return modules.FindAll(m => m.Visible);
        }

        private static string CurrentPath()
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx == null || ctx.Request == null) return "";
                return ctx.Request.Path ?? "";
            }
            catch { return ""; }
        }

        private static bool IsActive(string url, string currentPath)
        {
            var itemPath = AppForms.NormalizePath(url);
            if (string.IsNullOrEmpty(itemPath) || string.IsNullOrEmpty(currentPath)) return false;
            if (itemPath.IndexOf('#') >= 0)
                itemPath = itemPath.Substring(0, itemPath.IndexOf('#'));
            return currentPath.Equals(itemPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanSee(WorkstationNavItem item, AuthService auth, PermissionService perms, EmployeeProfileAccessService profile)
        {
            if (!auth.IsLoggedIn) return false;
            switch ((item.Access ?? "Form").Trim())
            {
                case "Always":
                    return true;
                case "Dashboard":
                    return auth.IsAdmin || perms.CanRead("Dashboard");
                case "Leave":
                    return new LeaveApprovalService().CanAccessModule();
                case "Attendance":
                    return auth.IsAdmin || perms.CanRead("EmployeeAttendance") || new LeaveApprovalService().CanAccessModule();
                case "Request":
                    return new EmployeeRequestService().CanAccessModule();
                case "Discipline":
                    return new DisciplineService().CanAccessModule();
                case "ProfileReview":
                    return new ProfileChangeRequestService().CanReview();
                case "MyDocuments":
                    return profile.CanAccessMyDocuments();
                case "Lms":
                    return new LmsDocumentService().CanAccessLibrary();
                case "EmployeeCard":
                    return profile.HasFullEmployeeMasterAccess() || profile.IsEmployeeProfileSynchronized();
                case "ContactReview":
                    return new ContactChangeWorkflowService().CanReview();
                case "MasterSetup":
                    return perms.CanAccessMasterSetup();
                case "Admin":
                    return auth.IsAdmin;
                default:
                    return string.IsNullOrEmpty(item.Key) || perms.CanRead(item.Key);
            }
        }

        private static List<WorkstationModuleView> Catalog()
        {
            return new List<WorkstationModuleView>
            {
                new WorkstationModuleView
                {
                    Id = "hrms",
                    Title = "HRMS",
                    Subtitle = "Human Resource Management System",
                    Icon = "HR",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("MyWorkspace", "My Workspace", "/MyWorkspace.aspx", "Personal dashboard for requests, tasks, leave, and attendance", "Always"),
                        Item("UserProfile", "My Profile", "/UserProfile.aspx", "View and update your employee profile", "Always"),
                        Item("EmployeeMaster", "Employee Master", "/EmployeeMaster.aspx", "Maintain employee records"),
                        Item("ProfileChange", "Profile Request", "/ProfileChangeReview.aspx", "HR review of profile change requests", "ProfileReview"),
                        Item("EmployeeRequest", "Employee Request", "/EmployeeRequest.aspx", "Submit and track employee requests", "Request"),
                        Item("EmployeeDiscipline", "Discipline", "/EmployeeDiscipline.aspx", "Disciplinary cases and corrective actions", "Discipline"),
                        Item("LeaveMaster", "Leave Application", "/LeaveMaster.aspx", "Apply for and approve leave", "Leave"),
                        Item("EmployeeAttendance", "Employee Attendance", "/EmployeeAttendance.aspx", "Attendance dashboard records", "Attendance"),
                        Item("PositionMaster", "Position Master", "/PositionMaster.aspx", "Positions and reporting structure"),
                        Item("ExpenseMaster", "Expense Master", "/ExpenseMaster.aspx", "Employee expense claims"),
                        Item("EmployeeMasterSetup", "Employee Master Setup", "/MasterSetup.aspx#setup-employee-setup", "Employee configuration pages", "MasterSetup"),
                        Item("Dashboard", "HR Dashboard", "/Dashboard.aspx", "HR metrics and summaries", "Dashboard"),
                        Item("PositionHierarchy", "Position Hierarchy", "/PositionHierarchy.aspx", "Position reporting hierarchy"),
                        Item("PerformanceMaster", "Employee Performance", "/PerformanceMaster.aspx", "Performance cycles"),
                        Item("TrainingMaster", "Employee Training", "/TrainingMaster.aspx", "Training records"),
                        Item("RecruitmentMaster", "Recruitment", "/RecruitmentMaster.aspx", "Recruitment workflows"),
                        Item("EmployeeReport", "Employee Directory", "/EmployeeReport.aspx", "Internal employee directory"),
                        Item("MyDocuments", "My Documents", "/MyDocuments.aspx", "Uploaded employee documents", "MyDocuments"),
                        Item("LmsLibrary", "Knowledge Library", "/LmsLibrary.aspx", "Manuals, SOPs, and policies", "Lms"),
                        Item("EmployeeCard", "Employee / Visitor Card", "/EmployeeCard.aspx", "Identity card preview and print", "EmployeeCard")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "contact",
                    Title = "Contact Hub",
                    Subtitle = "People and organization contacts",
                    Icon = "CH",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("ContactMaster", "All Contacts", "/ContactMaster.aspx", "Complete contact list with filters"),
                        Item("ContactChangeReview", "Contact Approvals", "/ContactChangeReview.aspx", "Approve or reject contact create and update requests", "ContactReview"),
                        Item("ContactMaster", "My Contacts", "/MyContacts.aspx", "Contacts created by you"),
                        Item("ContactMaster", "Customer Contacts", "/CustomerContacts.aspx", "Contacts linked to customers"),
                        Item("ContactMaster", "Vendor Contacts", "/VendorContacts.aspx", "Contacts linked to vendors"),
                        Item("ContactMaster", "Employee / Staff Contacts", "/EmployeeContacts.aspx", "Contacts linked to employees"),
                        Item("ContactMaster", "No Business Link", "/NoBusinessLinkContacts.aspx", "Guest and other contacts with no customer, vendor, or employee link"),
                        Item("ContactMasterSetup", "Contact Master Setup", "/MasterSetup.aspx#setup-customer-setup", "Contact and address lookup setup", "MasterSetup")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "product",
                    Title = "Product Hub",
                    Subtitle = "Catalog and product configuration",
                    Icon = "PH",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("ProductMaster", "Product Master", "/ProductMaster.aspx", "Product catalog and item information"),
                        Item("ProductBatchMaster", "Batch Management", "/ProductBatchMaster.aspx", "Product batches, expiry, and batch stock"),
                        Item("ProductSerialization", "Product Serialization", "/ProductSerialization.aspx", "DRAP / GS1 hexacodes by product and batch"),
                        Item("ProductMasterSetup", "Product Master Setup", "/MasterSetup.aspx#setup-product-setup", "Product setup pages", "MasterSetup")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "sales",
                    Title = "Sales Receivable",
                    Subtitle = "Customers, orders, and invoicing",
                    Icon = "SR",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("CustomerMaster", "Customer Master", "/CustomerMaster.aspx", "Customer accounts"),
                        Item("InvoiceMaster", "Invoice Master", "/InvoiceMaster.aspx", "Invoices and billing"),
                        Item("SalesOrderMaster", "Sales Order", "/SalesOrderMaster.aspx", "Sales orders")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "purchase",
                    Title = "Purchase Payable",
                    Subtitle = "Vendors and purchasing",
                    Icon = "PP",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("VendorMaster", "Vendor Master", "/VendorMaster.aspx", "Vendor accounts"),
                        Item("PurchaseOrderMaster", "Purchase Order", "/PurchaseOrderMaster.aspx", "Purchase orders"),
                        Item("GoodsReceiptMaster", "Goods Receipt", "/GoodsReceiptMaster.aspx", "Receive goods against purchase orders")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "qms",
                    Title = "QMS",
                    Subtitle = "Quality Management System",
                    Icon = "QM",
                    Items = new List<WorkstationNavItem>()
                },
                new WorkstationModuleView
                {
                    Id = "trade",
                    Title = "Trade Management",
                    Subtitle = "Inventory and stock movement",
                    Icon = "TM",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("InventoryMaster", "Inventory Management", "/InventoryMaster.aspx", "Stock levels, receipts, issues, and valuation"),
                        Item("GoodsIssueMaster", "Goods Issue", "/GoodsIssueMaster.aspx", "Issue stock from inventory")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "crm",
                    Title = "Engagement CRM",
                    Subtitle = "Tasks, communication, and follow-up",
                    Icon = "CR",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("TaskMaster", "Task Management", "/TaskMaster.aspx", "Tasks linked to people, sales, and purchases"),
                        Item("Notifications", "Announcements", "/Notifications.aspx", "Company announcements"),
                        Item("Memorandums", "Memorandums", "/Memorandums.aspx", "Active memorandums and policies"),
                        Item("QuickLinks", "Quick Links", "/QuickLinks.aspx", "Process and software shortcuts")
                    }
                },
                new WorkstationModuleView
                {
                    Id = "admin",
                    Title = "Administration",
                    Subtitle = "Security, setup, and workspace",
                    Icon = "AD",
                    Items = new List<WorkstationNavItem>
                    {
                        Item("Home", "Workstation Home", "/Home.aspx", "Module dashboard", "Always"),
                        Item("Inbox", "Notifications Inbox", "/Inbox.aspx", "In-application notifications", "Always"),
                        Item("AccountSettings", "Account Settings", "/AccountSettings.aspx", "Password and authenticator", "Always"),
                        Item("MasterSetup", "Master Setup", "/MasterSetup.aspx", "Configuration catalog", "MasterSetup"),
                        Item("UserSetup", "User Setup", "/UserSetup.aspx", "Application users"),
                        Item("UserRightsSetup", "User Rights", "/UserRightsSetup.aspx", "Page and action permissions"),
                        Item("AuditReport", "Audit Log Report", "/AuditReport.aspx", "Audit history", "Admin")
                    }
                }
            };
        }

        private static WorkstationNavItem Item(string key, string title, string url, string description, string access = "Form")
        {
            return new WorkstationNavItem
            {
                Key = key,
                Title = title,
                Url = url,
                Description = description,
                Access = access
            };
        }
    }
}
