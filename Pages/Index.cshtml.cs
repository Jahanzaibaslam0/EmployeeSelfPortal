using HRMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

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

public class IndexModel : PageModel
{
    private readonly string _conn;
    private readonly PermissionService _perms;
    private readonly EmployeeProfileAccessService _profileAccess;

    public IndexModel(IConfiguration config, PermissionService perms, EmployeeProfileAccessService profileAccess)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _perms = perms;
        _profileAccess = profileAccess;
    }

    public string PageTitle => "Home";
    public List<GallerySlide> Slides { get; set; } = new();
    public List<HomeProcessLink> ProcessLinks { get; set; } = new();
    public bool ShowProfileSyncWarning { get; set; }
    public string ProfileSyncMessage { get; set; } = "Your user account is not linked to an employee profile. Some self-service features may be unavailable.";
    public bool ShowAccessDeniedPopup { get; set; }
    public string AccessDeniedMessage { get; set; } = "";

    public void OnGet(bool? accessDenied)
    {
        ShowProfileSyncWarning = HttpContext.Session.GetInt32("ShowProfileSyncWarning") == 1;
        if (ShowProfileSyncWarning)
            HttpContext.Session.Remove("ShowProfileSyncWarning");

        ShowAccessDeniedPopup = HttpContext.Session.GetInt32("ShowAccessDeniedPopup") == 1;
        if (ShowAccessDeniedPopup)
        {
            AccessDeniedMessage = HttpContext.Session.GetString("AccessDeniedMessage") ?? PermissionService.AccessRestrictedMessage;
            HttpContext.Session.Remove("ShowAccessDeniedPopup");
            HttpContext.Session.Remove("AccessDeniedMessage");
        }
        else if (accessDenied == true || Request.Query["accessDenied"] == "1")
        {
            ShowAccessDeniedPopup = true;
            AccessDeniedMessage = PermissionService.AccessRestrictedMessage;
        }

        LoadSlides();
        ProcessLinks = GetProcessLinks()
            .Where(CanShowHomeLink)
            .ToList();
    }

    private bool CanShowHomeLink(HomeProcessLink link)
    {
        return link.FormKey switch
        {
            "UserProfile"  => _perms.CanRead("UserProfile"),
            "MyDocuments"  => _profileAccess.CanAccessMyDocuments(),
            "EmployeeCard" => _profileAccess.CanAccessEmployeeCard(),
            "EmployeeMaster" => _profileAccess.HasFullEmployeeMasterAccess(),
            _ => string.IsNullOrEmpty(link.FormKey) || _perms.CanRead(link.FormKey)
        };
    }

    private static List<HomeProcessLink> GetProcessLinks() => new()
    {
        new() { FormKey = "UserProfile",       Title = "My Profile",              Description = "View and update your personal employee information",   Url = "/UserProfile",        Category = "Self Service",  Icon = "MP" },
        new() { FormKey = "EmployeeMaster",    Title = "Employee Master",         Description = "Maintain employee records and employment details",      Url = "/EmployeeMaster",     Category = "Master Data",   Icon = "EM" },
        new() { FormKey = "PositionMaster",    Title = "Position Master",         Description = "Manage positions, assignments, and reporting structure",  Url = "/PositionMaster",     Category = "Master Data",   Icon = "PM" },
        new() { FormKey = "PositionHierarchy", Title = "Position Hierarchy",      Description = "Visualize position reporting hierarchy",                Url = "/PositionHierarchy",  Category = "Master Data",   Icon = "PH" },
        new() { FormKey = "CustomerMaster",    Title = "Customer Master",         Description = "Manage customer accounts and business relationships",   Url = "/CustomerMaster",     Category = "Master Data",   Icon = "CM" },
        new() { FormKey = "VendorMaster",      Title = "Vendor Master",           Description = "Manage vendor accounts and supplier relationships",     Url = "/VendorMaster",       Category = "Master Data",   Icon = "VN" },
        new() { FormKey = "ContactMaster",     Title = "Contact Master",          Description = "Maintain contact details for customers and vendors",    Url = "/ContactMaster",      Category = "Master Data",   Icon = "CT" },
        new() { FormKey = "ProductMaster",     Title = "Product Master",          Description = "Manage product catalog and item information",           Url = "/ProductMaster",      Category = "Master Data",   Icon = "PD" },
        new() { FormKey = "InvoiceMaster",     Title = "Invoice Master",          Description = "Create and manage invoices and billing records",        Url = "/InvoiceMaster",      Category = "Master Data",   Icon = "IN" },
        new() { FormKey = "PurchaseOrderMaster", Title = "Purchase Order",          Description = "Create and manage purchase orders with line items",   Url = "/PurchaseOrderMaster", Category = "Master Data",  Icon = "PO" },
        new() { FormKey = "SalesOrderMaster",    Title = "Sales Order",             Description = "Create and manage sales orders with line items",      Url = "/SalesOrderMaster",    Category = "Master Data",  Icon = "SO" },
        new() { FormKey = "InventoryMaster",     Title = "Inventory Management",    Description = "Track stock levels, receipts, issues, and valuation", Url = "/InventoryMaster",     Category = "Master Data",  Icon = "IV" },
        new() { FormKey = "TaskMaster",          Title = "Task Management",         Description = "Create and track tasks linked to employees, customers, sales, purchases, and vendors", Url = "/TaskMaster", Category = "Master Data", Icon = "TK" },
        new() { FormKey = "Notifications",     Title = "Announcements",           Description = "View company announcements and HR notifications",      Url = "/Notifications",      Category = "Communication", Icon = "AN" },
        new() { FormKey = "Memorandums",       Title = "Memorandums",             Description = "Read active memorandums and policy documents",          Url = "/Memorandums",        Category = "Communication", Icon = "MM" },
        new() { FormKey = "PerformanceMaster", Title = "Employee Performance",    Description = "Record and review employee performance cycles",        Url = "/PerformanceMaster",  Category = "HR Process",    Icon = "PF" },
        new() { FormKey = "TrainingMaster",    Title = "Employee Training",       Description = "Track employee training and development records",      Url = "/TrainingMaster",     Category = "HR Process",    Icon = "TR" },
        new() { FormKey = "ExpenseMaster",     Title = "Expense Process",         Description = "Submit and manage employee expense claims",            Url = "/ExpenseMaster",      Category = "HR Process",    Icon = "EP" },
        new() { FormKey = "LeaveMaster",       Title = "Leave Management",        Description = "Apply for and manage employee leave requests",         Url = "/LeaveMaster",        Category = "HR Process",    Icon = "LM" },
        new() { FormKey = "RecruitmentMaster", Title = "Recruitment Process",     Description = "Manage hiring and recruitment workflows",              Url = "/RecruitmentMaster",  Category = "HR Process",    Icon = "RC" },
        new() { FormKey = "EmployeeReport",    Title = "Employee Directory",      Description = "Search, view, and export the internal employee directory", Url = "/EmployeeReport", Category = "HR Process",    Icon = "ED" },
        new() { FormKey = "MyDocuments",       Title = "My Documents",            Description = "View and access your uploaded documents",              Url = "/MyDocuments",        Category = "Self Service",  Icon = "DC" },
        new() { FormKey = "EmployeeCard",      Title = "Employee / Visitor Card", Description = "Preview, print, or download employee identity cards",  Url = "/EmployeeCard",       Category = "Self Service",  Icon = "ID" },
        new() { FormKey = "QuickLinks",        Title = "Quick Links",             Description = "HR processes and organization software shortcuts",     Url = "/QuickLinks",         Category = "Shortcuts",     Icon = "QL" },
    };

    private void LoadSlides()
    {
        Slides.Clear();
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                SELECT GalleryImageID, Title, Description, ImagePath
                FROM tblGalleryImage
                WHERE IsActive = 1
                ORDER BY SortOrder, GalleryImageID;", conn);
            conn.Open();
            using var dr = cmd.ExecuteReader();
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
        catch
        {
            // Gallery table may be missing on older DBs — show welcome banner instead
            Slides.Clear();
        }
    }
}
