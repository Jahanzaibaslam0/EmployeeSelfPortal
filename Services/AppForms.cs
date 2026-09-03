using System;
using System.Linq;

namespace HRMS.Services
{
/// <summary>All application forms/pages that can be permission-controlled.</summary>
public static class AppForms
{
    public class FormDef
    {
        public FormDef(string key, string name, string path, string category, int sortOrder)
        {
            Key = key;
            Name = name;
            Path = path;
            Category = category;
            SortOrder = sortOrder;
        }

        public string Key { get; }
        public string Name { get; }
        public string Path { get; }
        public string Category { get; }
        public int SortOrder { get; }
    }

    public static readonly FormDef[] All = new FormDef[]
    {
        new("Home",                    "Home",                     "/Home",                    "Transactions",       0),
        new("Dashboard",               "HRMS Dashboard",           "/Dashboard",               "Transactions",       1),
        new("UserProfile",             "My Profile",               "/UserProfile",             "Transactions",       2),
        new("MyDocuments",             "My Documents",             "/MyDocuments",             "Transactions",       3),
        new("LmsLibrary",              "Knowledge Library",        "/LmsLibrary",              "Transactions",       4),
        new("EmployeeMaster",          "Employee Master",          "/EmployeeMaster",          "Transactions",       5),
        new("PositionMaster",          "Position Master",          "/PositionMaster",          "Transactions",       6),
        new("PositionHierarchy",       "Position Hierarchy",       "/PositionHierarchy",       "Transactions",       7),
        new("EmployeeReport",          "Internal Employee Directory","/EmployeeReport",          "Transactions",       8),
        new("QuickLinks",              "Quick Links",              "/QuickLinks",              "Transactions",       9),
        new("Notifications",           "Notifications",            "/Notifications",           "Transactions",      10),
        new("Memorandums",             "Memorandums",              "/Memorandums",             "Transactions",      11),
        new("ExpenseMaster",           "Expense Process",          "/ExpenseMaster",           "Transactions",      12),
        new("PerformanceMaster",       "Employee Performance",     "/PerformanceMaster",       "Transactions",      13),
        new("TrainingMaster",          "Employee Training",        "/TrainingMaster",          "Transactions",      14),
        new("RecruitmentMaster",       "Recruitment Process",      "/RecruitmentMaster",       "Transactions",      15),
        new("LeaveMaster",             "Leave Management",         "/LeaveMaster",             "Transactions",      16),
        new("CustomerMaster",          "Customer Master",          "/CustomerMaster",          "Transactions",      17),
        new("ContactMaster",           "Contact Master",           "/ContactMaster",           "Transactions",      18),
        new("ProductMaster",           "Product Master",           "/ProductMaster",           "Transactions",      19),
        new("InvoiceMaster",           "Invoice Master",           "/InvoiceMaster",           "Transactions",      20),

        new("DivisionSetup",           "Division Setup",           "/DivisionSetup",           "Organization Setup", 10),
        new("BusinessSegmentSetup",    "Business Segment Setup",   "/BusinessSegmentSetup",    "Organization Setup", 11),
        new("BusinessUnitSetup",       "Business Unit Setup",      "/BusinessUnitSetup",       "Organization Setup", 12),
        new("WorkforceSegmentSetup",   "Workforce Segment Setup",  "/WorkforceSegmentSetup",   "Organization Setup", 13),
        new("UnitSetup",               "Unit Setup",               "/UnitSetup",               "Organization Setup", 14),
        new("WingSetup",               "Wing Setup",               "/WingSetup",               "Organization Setup", 15),
        new("GenderSetup",             "Gender Setup",             "/GenderSetup",             "Organization Setup", 16),
        new("ReligionSetup",           "Religion Setup",           "/ReligionSetup",           "Organization Setup", 17),
        new("NationalitySetup",        "Nationality Setup",        "/NationalitySetup",        "Organization Setup", 18),
        new("LanguageSetup",           "Language Setup",           "/LanguageSetup",           "Organization Setup", 19),
        new("BankSetup",               "Bank Master Setup",        "/BankSetup",               "Organization Setup", 20),
        new("BankGroupSetup",          "Bank Group Setup",         "/BankGroupSetup",          "Organization Setup", 21),
        new("CurrencySetup",           "Currency Setup",           "/CurrencySetup",           "Organization Setup", 22),
        new("UnitOfMeasureSetup",      "Unit of Measure Setup",    "/UnitOfMeasureSetup",      "Organization Setup", 23),
        new("CostCenterSetup",         "Cost Center Setup",        "/CostCenterSetup",         "Organization Setup", 24),
        new("SkillSetup",              "Skill Setup",              "/SkillSetup",              "Organization Setup", 25),
        new("LegalEntitySetup",        "Legal Entity Setup",       "/LegalEntitySetup",        "Organization Setup", 26),
        new("SalesTeamSetup",          "Sales Team Setup",         "/SalesTeamSetup",          "Organization Setup", 27),
        new("WorkLocationTypeSetup",   "Work Location Type Setup", "/WorkLocationTypeSetup",   "Organization Setup", 28),
        new("WorkArrangementSetup",    "Work Arrangement Setup",   "/WorkArrangementSetup",    "Organization Setup", 29),
        new("ExtensionSetup",          "Extension Master Setup",   "/ExtensionSetup",          "Organization Setup", 30),
        new("CitySetup",               "City Setup",               "/CitySetup",               "Organization Setup", 31),
        new("ProvinceSetup",           "Province Setup",           "/ProvinceSetup",           "Organization Setup", 32),
        new("SalesGroupSetup",         "Sales Group Setup",        "/SalesGroupSetup",         "Organization Setup", 33),
        new("DepartmentSetup",         "Department Setup",         "/DepartmentSetup",         "Organization Setup", 34),
        new("RegionSetup",             "Region Setup",             "/RegionSetup",             "Organization Setup", 35),
        new("LocationSetup",           "Location Setup",           "/LocationSetup",           "Organization Setup", 36),
        new("SoftwareLinkSetup",       "Software Link Setup",      "/SoftwareLinkSetup",       "Organization Setup", 37),
        new("NotificationSetup",       "Notification Setup",       "/NotificationSetup",       "Organization Setup", 38),
        new("MemorandumSetup",         "Memorandum Setup",         "/MemorandumSetup",         "Organization Setup", 39),
        new("ImageGallerySetup",       "Image Gallery Setup",      "/ImageGallerySetup",       "Organization Setup", 40),
        new("LmsDocumentSetup",        "LMS Document Setup",       "/LmsDocumentSetup",        "Organization Setup", 41),

        new("GradeSetup",              "Grade Setup",              "/GradeSetup",              "Employee Setup",     33),
        new("EmploymentTypeSetup",     "Employment Type Setup",    "/EmploymentTypeSetup",     "Employee Setup",     34),
        new("DesignationLevelSetup",   "Designation Level Setup",  "/DesignationLevelSetup",   "Employee Setup",     35),
        new("TitleSetup",              "Title Setup",              "/TitleSetup",              "Employee Setup",     36),
        new("EmploymentStatusSetup",   "Employment Status Setup",  "/EmploymentStatusSetup",   "Employee Setup",     37),
        new("BenefitSetup",            "Benefit Setup",            "/BenefitSetup",            "Employee Setup",     38),
        new("BenefitEntitlementSetup", "Benefit Entitlement Setup","/BenefitEntitlementSetup", "Employee Setup",     39),
        new("ExpenseCategorySetup",    "Expense Category Setup",   "/ExpenseCategorySetup",    "Employee Setup",     40),
        new("LeaveCategorySetup",      "Leave Category Setup",     "/LeaveCategorySetup",      "Employee Setup",     41),
        new("BloodGroupSetup",         "Blood Group Setup",        "/BloodGroupSetup",         "Employee Setup",     42),
        new("WorkerCategorySetup",     "Worker Category Setup",    "/WorkerCategorySetup",     "Employee Setup",     43),
        new("JobSetup",                "Job Setup",                "/JobSetup",                "Employee Setup",     44),
        new("WorkerLocationSetup",     "Worker Location Setup",    "/WorkerLocationSetup",     "Employee Setup",     45),
        new("DocumentTypeSetup",       "Document Type Setup",      "/DocumentTypeSetup",       "Employee Setup",     46),

        new("ModeOfDeliverySetup",     "Mode of Delivery Setup",   "/ModeOfDeliverySetup",     "Customer Setup",     60),
        new("MethodOfPaymentSetup",    "Method of Payment Setup",  "/MethodOfPaymentSetup",    "Customer Setup",     61),
        new("CustomerGroupSetup",      "Customer Group Setup",     "/CustomerGroupSetup",      "Customer Setup",     62),
        new("TermsOfPaymentSetup",     "Terms of Payment Setup",   "/TermsOfPaymentSetup",     "Customer Setup",     63),
        new("CustomerClassSetup",      "Customer Class Setup",     "/CustomerClassSetup",      "Customer Setup",     64),
        new("BillPreferenceSetup",     "Bill Preference Setup",    "/BillPreferenceSetup",     "Customer Setup",     65),
        new("TaxGroupSetup",           "Tax Group Setup",          "/TaxGroupSetup",           "Customer Setup",     66),
        new("FBRStatusSetup",          "FBR Status Setup",         "/FBRStatusSetup",          "Customer Setup",     67),

        new("ProductNatureSetup",      "Product Nature Setup",     "/ProductNatureSetup",      "Product Setup",      70),
        new("InventoryTypeSetup",      "Inventory Type Setup",     "/InventoryTypeSetup",      "Product Setup",      71),
        new("ItemRegisteredSetup",     "Item Registered Setup",    "/ItemRegisteredSetup",     "Product Setup",      72),
        new("BrandCodeSetup",          "Brand Code Setup",         "/BrandCodeSetup",          "Product Setup",      73),
        new("BrandGroupSetup",         "Brand Group Setup",        "/BrandGroupSetup",         "Product Setup",      74),
        new("ProductGroupSetup",       "Product Group Setup",      "/ProductGroupSetup",       "Product Setup",      75),
        new("ProductSalesGroupSetup",  "Sales Group Setup",        "/ProductSalesGroupSetup",  "Product Setup",      76),
        new("ItemGroupSetup",          "Item Group Setup",         "/ItemGroupSetup",          "Product Setup",      77),
        new("SalesCategorySetup",      "Sales Category Setup",     "/SalesCategorySetup",      "Product Setup",      78),
        new("ProductDivisionSetup",    "Division Setup",           "/ProductDivisionSetup",    "Product Setup",      79),
        new("ProductTeamSetup",        "Team Setup",               "/ProductTeamSetup",        "Product Setup",      80),
        new("HSCodeSetup",             "HS Code Setup",            "/HSCodeSetup",             "Product Setup",      81),

        new("UserSetup",               "User Setup",               "/UserSetup",               "Security",           50),
        new("UserRightsSetup",         "User Rights Setup",        "/UserRightsSetup",         "Security",           51),
        new("AuditReport",             "Audit Log Report",         "/AuditReport",             "Security",           52),
    };

    public static FormDef FindByPath(string path)
    {
        var p = NormalizePath(path);
        if (string.IsNullOrEmpty(p) || p == "/index" || p == "/home")
            return All.FirstOrDefault(f => f.Key.Equals("Home", StringComparison.OrdinalIgnoreCase));
        return All.FirstOrDefault(f => NormalizePath(f.Path).Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Normalizes request/app paths so /DepartmentSetup.aspx matches /DepartmentSetup.
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var p = path.Trim();
        var q = p.IndexOf('?');
        if (q >= 0) p = p.Substring(0, q);
        var hash = p.IndexOf('#');
        if (hash >= 0) p = p.Substring(0, hash);
        p = p.Replace('\\', '/').TrimEnd('/');
        if (p.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            p = p.Substring(0, p.Length - 5);
        if (!p.StartsWith("/"))
            p = "/" + p;
        return p.ToLowerInvariant();
    }

    public static FormDef FindByKey(string key)
        => All.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}
}
