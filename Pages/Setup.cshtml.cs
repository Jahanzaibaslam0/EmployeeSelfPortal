using HRMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class SetupLinkItem
{
    public string FormKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class SetupCategoryGroup
{
    public string Category { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<SetupLinkItem> Links { get; set; } = new();
}

public class SetupModel : PageModel
{
    private readonly AuthService _auth;
    private readonly PermissionService _perms;

    public SetupModel(AuthService auth, PermissionService perms)
    {
        _auth = auth;
        _perms = perms;
    }

    public string PageTitle => "Setup";
    public List<SetupCategoryGroup> Categories { get; set; } = new();

    public void OnGet()
    {
        var allCategories = GetAllSetupCategories();

        foreach (var cat in allCategories)
        {
            var visible = cat.Links
                .Where(l => string.IsNullOrEmpty(l.FormKey) || _auth.IsAdmin || _perms.CanRead(l.FormKey))
                .ToList();

            if (visible.Count > 0)
            {
                Categories.Add(new SetupCategoryGroup
                {
                    Category = cat.Category,
                    Icon = cat.Icon,
                    Links = visible
                });
            }
        }
    }

    private static List<SetupCategoryGroup> GetAllSetupCategories() => new()
    {
        new()
        {
            Category = "Organization Setup",
            Icon = "ORG",
            Links = new()
            {
                new() { FormKey = "DivisionSetup",         Title = "Division",            Url = "/DivisionSetup",         Icon = "DV" },
                new() { FormKey = "BusinessSegmentSetup",  Title = "Business Segment",    Url = "/BusinessSegmentSetup",  Icon = "BS" },
                new() { FormKey = "BusinessUnitSetup",     Title = "Business Unit",       Url = "/BusinessUnitSetup",     Icon = "BU" },
                new() { FormKey = "WorkforceSegmentSetup", Title = "Workforce Segment",   Url = "/WorkforceSegmentSetup", Icon = "WS" },
                new() { FormKey = "UnitSetup",             Title = "Unit",                Url = "/UnitSetup",             Icon = "UN" },
                new() { FormKey = "WingSetup",             Title = "Wing",                Url = "/WingSetup",             Icon = "WG" },
                new() { FormKey = "GenderSetup",           Title = "Gender",              Url = "/GenderSetup",           Icon = "GN" },
                new() { FormKey = "ReligionSetup",         Title = "Religion",            Url = "/ReligionSetup",         Icon = "RL" },
                new() { FormKey = "NationalitySetup",      Title = "Nationality",         Url = "/NationalitySetup",      Icon = "NT" },
                new() { FormKey = "LanguageSetup",         Title = "Language",            Url = "/LanguageSetup",         Icon = "LN" },
                new() { FormKey = "BankSetup",             Title = "Bank Master",         Url = "/BankSetup",             Icon = "BK" },
                new() { FormKey = "BankGroupSetup",        Title = "Bank Group",          Url = "/BankGroupSetup",        Icon = "BG" },
                new() { FormKey = "CurrencySetup",         Title = "Currency",            Url = "/CurrencySetup",         Icon = "CR" },
                new() { FormKey = "UnitOfMeasureSetup",    Title = "Unit of Measure",     Url = "/UnitOfMeasureSetup",    Icon = "UM" },
                new() { FormKey = "CostCenterSetup",       Title = "Cost Center",         Url = "/CostCenterSetup",       Icon = "CC" },
                new() { FormKey = "SkillSetup",            Title = "Skill",               Url = "/SkillSetup",            Icon = "SK" },
                new() { FormKey = "LegalEntitySetup",      Title = "Legal Entity",        Url = "/LegalEntitySetup",      Icon = "LE" },
                new() { FormKey = "SalesTeamSetup",        Title = "Sales Team",          Url = "/SalesTeamSetup",        Icon = "ST" },
                new() { FormKey = "WorkLocationTypeSetup", Title = "Work Location Type",  Url = "/WorkLocationTypeSetup", Icon = "WL" },
                new() { FormKey = "WorkArrangementSetup",  Title = "Work Arrangement",    Url = "/WorkArrangementSetup",  Icon = "WA" },
                new() { FormKey = "ExtensionSetup",        Title = "Extension",           Url = "/ExtensionSetup",        Icon = "EX" },
                new() { FormKey = "CitySetup",             Title = "City",                Url = "/CitySetup",             Icon = "CT" },
                new() { FormKey = "ProvinceSetup",         Title = "Province",            Url = "/ProvinceSetup",         Icon = "PV" },
                new() { FormKey = "SalesGroupSetup",       Title = "Sales Group",         Url = "/SalesGroupSetup",       Icon = "SG" },
                new() { FormKey = "DepartmentSetup",       Title = "Department",          Url = "/DepartmentSetup",       Icon = "DP" },
                new() { FormKey = "RegionSetup",           Title = "Region",              Url = "/RegionSetup",           Icon = "RG" },
                new() { FormKey = "LocationSetup",         Title = "Location",            Url = "/LocationSetup",         Icon = "LC" },
                new() { FormKey = "SoftwareLinkSetup",     Title = "Software Link",       Url = "/SoftwareLinkSetup",     Icon = "SL" },
                new() { FormKey = "NotificationSetup",     Title = "Notification Setup",  Url = "/NotificationSetup",     Icon = "NS" },
                new() { FormKey = "MemorandumSetup",       Title = "Memorandum Setup",    Url = "/MemorandumSetup",       Icon = "MS" },
                new() { FormKey = "ImageGallerySetup",     Title = "Image Gallery Setup", Url = "/ImageGallerySetup",     Icon = "IG" },
            }
        },
        new()
        {
            Category = "Employee Setup",
            Icon = "EMP",
            Links = new()
            {
                new() { FormKey = "GradeSetup",              Title = "Grade",              Url = "/GradeSetup",              Icon = "GR" },
                new() { FormKey = "EmploymentTypeSetup",     Title = "Employment Type",    Url = "/EmploymentTypeSetup",     Icon = "ET" },
                new() { FormKey = "DesignationLevelSetup",   Title = "Designation Level",  Url = "/DesignationLevelSetup",   Icon = "DL" },
                new() { FormKey = "DesignationSetup",        Title = "Designation",        Url = "/DesignationSetup",        Icon = "DS" },
                new() { FormKey = "TitleSetup",              Title = "Title",              Url = "/TitleSetup",              Icon = "TT" },
                new() { FormKey = "EmployeeCategorySetup",   Title = "Employee Category",  Url = "/EmployeeCategorySetup",   Icon = "EY" },
                new() { FormKey = "EmploymentStatusSetup",   Title = "Employment Status",  Url = "/EmploymentStatusSetup",   Icon = "ES" },
                new() { FormKey = "BenefitSetup",            Title = "Benefit",            Url = "/BenefitSetup",            Icon = "BF" },
                new() { FormKey = "BenefitEntitlementSetup", Title = "Benefit Entitlement",Url = "/BenefitEntitlementSetup", Icon = "BE" },
                new() { FormKey = "ExpenseCategorySetup",    Title = "Expense Category",   Url = "/ExpenseCategorySetup",    Icon = "EC" },
                new() { FormKey = "LeaveCategorySetup",      Title = "Leave Category",     Url = "/LeaveCategorySetup",      Icon = "LV" },
                new() { FormKey = "BloodGroupSetup",         Title = "Blood Group",        Url = "/BloodGroupSetup",         Icon = "BL" },
                new() { FormKey = "WorkerCategorySetup",     Title = "Worker Category",    Url = "/WorkerCategorySetup",     Icon = "WC" },
                new() { FormKey = "JobSetup",                Title = "Job",                Url = "/JobSetup",                Icon = "JB" },
                new() { FormKey = "WorkerLocationSetup",     Title = "Worker Location",    Url = "/WorkerLocationSetup",     Icon = "WK" },
                new() { FormKey = "DocumentTypeSetup",       Title = "Document Type",      Url = "/DocumentTypeSetup",       Icon = "DT" },
            }
        },
        new()
        {
            Category = "Customer Setup",
            Icon = "CST",
            Links = new()
            {
                new() { FormKey = "ModeOfDeliverySetup",  Title = "Mode of Delivery",   Url = "/ModeOfDeliverySetup",  Icon = "MD" },
                new() { FormKey = "MethodOfPaymentSetup", Title = "Method of Payment",  Url = "/MethodOfPaymentSetup", Icon = "MP" },
                new() { FormKey = "CustomerGroupSetup",   Title = "Customer Group",     Url = "/CustomerGroupSetup",   Icon = "CG" },
                new() { FormKey = "TermsOfPaymentSetup",  Title = "Terms of Payment",   Url = "/TermsOfPaymentSetup",  Icon = "TP" },
                new() { FormKey = "CustomerClassSetup",   Title = "Customer Class",     Url = "/CustomerClassSetup",   Icon = "CL" },
                new() { FormKey = "BillPreferenceSetup",  Title = "Bill Preference",    Url = "/BillPreferenceSetup",  Icon = "BP" },
                new() { FormKey = "TaxGroupSetup",        Title = "Tax Group",          Url = "/TaxGroupSetup",        Icon = "TG" },
                new() { FormKey = "FBRStatusSetup",       Title = "FBR Status",         Url = "/FBRStatusSetup",       Icon = "FB" },
                new() { FormKey = "CountrySetup",         Title = "Country",            Url = "/CountrySetup",         Icon = "CY" },
                new() { FormKey = "StateSetup",           Title = "State",              Url = "/StateSetup",           Icon = "ST" },
                new() { FormKey = "ContactCitySetup",     Title = "City",               Url = "/ContactCitySetup",     Icon = "CI" },
                new() { FormKey = "PostalSetup",          Title = "Postal",             Url = "/PostalSetup",          Icon = "PC" },
                new() { FormKey = "StreetSetup",          Title = "Street",             Url = "/StreetSetup",          Icon = "SR" },
                new() { FormKey = "ContactLocationSetup", Title = "Address Lookups",    Url = "/ContactLocationSetup", Icon = "CA" },
            }
        },
        new()
        {
            Category = "Product Setup",
            Icon = "PRD",
            Links = new()
            {
                new() { FormKey = "ProductNatureSetup",     Title = "Product Nature",  Url = "/ProductNatureSetup",     Icon = "PN" },
                new() { FormKey = "InventoryTypeSetup",     Title = "Inventory Type",  Url = "/InventoryTypeSetup",     Icon = "IT" },
                new() { FormKey = "ItemRegisteredSetup",    Title = "Item Registered", Url = "/ItemRegisteredSetup",    Icon = "IR" },
                new() { FormKey = "BrandCodeSetup",         Title = "Brand Code",      Url = "/BrandCodeSetup",         Icon = "BC" },
                new() { FormKey = "BrandGroupSetup",        Title = "Brand Group",     Url = "/BrandGroupSetup",        Icon = "BR" },
                new() { FormKey = "ProductGroupSetup",      Title = "Product Group",   Url = "/ProductGroupSetup",      Icon = "PG" },
                new() { FormKey = "ProductSalesGroupSetup", Title = "Sales Group",     Url = "/ProductSalesGroupSetup", Icon = "PS" },
                new() { FormKey = "ItemGroupSetup",         Title = "Item Group",      Url = "/ItemGroupSetup",         Icon = "IG" },
                new() { FormKey = "SalesCategorySetup",     Title = "Sales Category",  Url = "/SalesCategorySetup",     Icon = "SC" },
                new() { FormKey = "ProductDivisionSetup",   Title = "Division",        Url = "/ProductDivisionSetup",   Icon = "PD" },
                new() { FormKey = "ProductTeamSetup",       Title = "Team",            Url = "/ProductTeamSetup",       Icon = "PT" },
                new() { FormKey = "HSCodeSetup",            Title = "HS Code",         Url = "/HSCodeSetup",            Icon = "HS" },
            }
        },
        new()
        {
            Category = "Security",
            Icon = "SEC",
            Links = new()
            {
                new() { FormKey = "UserSetup",       Title = "User Setup",       Url = "/UserSetup",       Icon = "US" },
                new() { FormKey = "UserRightsSetup", Title = "User Rights",      Url = "/UserRightsSetup", Icon = "UR" },
                new() { FormKey = "AuditReport",     Title = "Audit Log Report", Url = "/AuditReport",     Icon = "AL" },
            }
        },
    };
}
