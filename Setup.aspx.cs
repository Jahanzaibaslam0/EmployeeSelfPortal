using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class SetupPage : AppBasePage
    {
        public string PageTitle => "Setup";
        public List<SetupCategoryGroup> Categories { get; private set; } = new List<SetupCategoryGroup>();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                OnGet();
        }

        private void OnGet()
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            foreach (var cat in GetAllSetupCategories())
            {
                var visible = cat.Links
                    .Where(l => string.IsNullOrEmpty(l.FormKey) || Auth.IsAdmin || Perms.CanRead(l.FormKey))
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

        private static List<SetupCategoryGroup> GetAllSetupCategories()
        {
            return new List<SetupCategoryGroup>
            {
                new SetupCategoryGroup
                {
                    Category = "Organization Setup",
                    Icon = "ORG",
                    Links = new List<SetupLinkItem>
                    {
                        new SetupLinkItem { FormKey = "DivisionSetup", Title = "Division", Url = "/DivisionSetup.aspx", Icon = "DV" },
                        new SetupLinkItem { FormKey = "BusinessSegmentSetup", Title = "Business Segment", Url = "/BusinessSegmentSetup.aspx", Icon = "BS" },
                        new SetupLinkItem { FormKey = "BusinessUnitSetup", Title = "Business Unit", Url = "/BusinessUnitSetup.aspx", Icon = "BU" },
                        new SetupLinkItem { FormKey = "WorkforceSegmentSetup", Title = "Workforce Segment", Url = "/WorkforceSegmentSetup.aspx", Icon = "WS" },
                        new SetupLinkItem { FormKey = "UnitSetup", Title = "Unit", Url = "/UnitSetup.aspx", Icon = "UN" },
                        new SetupLinkItem { FormKey = "WingSetup", Title = "Wing", Url = "/WingSetup.aspx", Icon = "WG" },
                        new SetupLinkItem { FormKey = "GenderSetup", Title = "Gender", Url = "/GenderSetup.aspx", Icon = "GN" },
                        new SetupLinkItem { FormKey = "ReligionSetup", Title = "Religion", Url = "/ReligionSetup.aspx", Icon = "RL" },
                        new SetupLinkItem { FormKey = "NationalitySetup", Title = "Nationality", Url = "/NationalitySetup.aspx", Icon = "NT" },
                        new SetupLinkItem { FormKey = "LanguageSetup", Title = "Language", Url = "/LanguageSetup.aspx", Icon = "LN" },
                        new SetupLinkItem { FormKey = "BankSetup", Title = "Bank Master", Url = "/BankSetup.aspx", Icon = "BK" },
                        new SetupLinkItem { FormKey = "BankGroupSetup", Title = "Bank Group", Url = "/BankGroupSetup.aspx", Icon = "BG" },
                        new SetupLinkItem { FormKey = "CurrencySetup", Title = "Currency", Url = "/CurrencySetup.aspx", Icon = "CR" },
                        new SetupLinkItem { FormKey = "UnitOfMeasureSetup", Title = "Unit of Measure", Url = "/UnitOfMeasureSetup.aspx", Icon = "UM" },
                        new SetupLinkItem { FormKey = "CostCenterSetup", Title = "Cost Center", Url = "/CostCenterSetup.aspx", Icon = "CC" },
                        new SetupLinkItem { FormKey = "SkillSetup", Title = "Skill", Url = "/SkillSetup.aspx", Icon = "SK" },
                        new SetupLinkItem { FormKey = "LegalEntitySetup", Title = "Legal Entity", Url = "/LegalEntitySetup.aspx", Icon = "LE" },
                        new SetupLinkItem { FormKey = "SalesTeamSetup", Title = "Sales Team", Url = "/SalesTeamSetup.aspx", Icon = "ST" },
                        new SetupLinkItem { FormKey = "WorkLocationTypeSetup", Title = "Work Location Type", Url = "/WorkLocationTypeSetup.aspx", Icon = "WL" },
                        new SetupLinkItem { FormKey = "WorkArrangementSetup", Title = "Work Arrangement", Url = "/WorkArrangementSetup.aspx", Icon = "WA" },
                        new SetupLinkItem { FormKey = "ExtensionSetup", Title = "Extension", Url = "/ExtensionSetup.aspx", Icon = "EX" },
                        new SetupLinkItem { FormKey = "CitySetup", Title = "City", Url = "/CitySetup.aspx", Icon = "CT" },
                        new SetupLinkItem { FormKey = "ProvinceSetup", Title = "Province", Url = "/ProvinceSetup.aspx", Icon = "PV" },
                        new SetupLinkItem { FormKey = "SalesGroupSetup", Title = "Sales Group", Url = "/SalesGroupSetup.aspx", Icon = "SG" },
                        new SetupLinkItem { FormKey = "DepartmentSetup", Title = "Department", Url = "/DepartmentSetup.aspx", Icon = "DP" },
                        new SetupLinkItem { FormKey = "RegionSetup", Title = "Region", Url = "/RegionSetup.aspx", Icon = "RG" },
                        new SetupLinkItem { FormKey = "LocationSetup", Title = "Location", Url = "/LocationSetup.aspx", Icon = "LC" },
                        new SetupLinkItem { FormKey = "SoftwareLinkSetup", Title = "Software Link", Url = "/SoftwareLinkSetup.aspx", Icon = "SL" },
                        new SetupLinkItem { FormKey = "NotificationSetup", Title = "Notification Setup", Url = "/NotificationSetup.aspx", Icon = "NS" },
                        new SetupLinkItem { FormKey = "MemorandumSetup", Title = "Memorandum Setup", Url = "/MemorandumSetup.aspx", Icon = "MS" },
                        new SetupLinkItem { FormKey = "ImageGallerySetup", Title = "Image Gallery Setup", Url = "/ImageGallerySetup.aspx", Icon = "IG" },
                    }
                },
                new SetupCategoryGroup
                {
                    Category = "Employee Setup",
                    Icon = "EMP",
                    Links = new List<SetupLinkItem>
                    {
                        new SetupLinkItem { FormKey = "GradeSetup", Title = "Grade", Url = "/GradeSetup.aspx", Icon = "GR" },
                        new SetupLinkItem { FormKey = "EmploymentTypeSetup", Title = "Employment Type", Url = "/EmploymentTypeSetup.aspx", Icon = "ET" },
                        new SetupLinkItem { FormKey = "DesignationLevelSetup", Title = "Designation Level", Url = "/DesignationLevelSetup.aspx", Icon = "DL" },
                        new SetupLinkItem { FormKey = "TitleSetup", Title = "Title", Url = "/TitleSetup.aspx", Icon = "TT" },
                        new SetupLinkItem { FormKey = "EmploymentStatusSetup", Title = "Employment Status", Url = "/EmploymentStatusSetup.aspx", Icon = "ES" },
                        new SetupLinkItem { FormKey = "BenefitSetup", Title = "Benefit", Url = "/BenefitSetup.aspx", Icon = "BF" },
                        new SetupLinkItem { FormKey = "BenefitEntitlementSetup", Title = "Benefit Entitlement", Url = "/BenefitEntitlementSetup.aspx", Icon = "BE" },
                        new SetupLinkItem { FormKey = "ExpenseCategorySetup", Title = "Expense Category", Url = "/ExpenseCategorySetup.aspx", Icon = "EC" },
                        new SetupLinkItem { FormKey = "LeaveCategorySetup", Title = "Leave Category", Url = "/LeaveCategorySetup.aspx", Icon = "LV" },
                        new SetupLinkItem { FormKey = "BloodGroupSetup", Title = "Blood Group", Url = "/BloodGroupSetup.aspx", Icon = "BL" },
                        new SetupLinkItem { FormKey = "WorkerCategorySetup", Title = "Worker Category", Url = "/WorkerCategorySetup.aspx", Icon = "WC" },
                        new SetupLinkItem { FormKey = "JobSetup", Title = "Job", Url = "/JobSetup.aspx", Icon = "JB" },
                        new SetupLinkItem { FormKey = "WorkerLocationSetup", Title = "Worker Location", Url = "/WorkerLocationSetup.aspx", Icon = "WK" },
                        new SetupLinkItem { FormKey = "DocumentTypeSetup", Title = "Document Type", Url = "/DocumentTypeSetup.aspx", Icon = "DT" },
                    }
                },
                new SetupCategoryGroup
                {
                    Category = "Customer Setup",
                    Icon = "CST",
                    Links = new List<SetupLinkItem>
                    {
                        new SetupLinkItem { FormKey = "ModeOfDeliverySetup", Title = "Mode of Delivery", Url = "/ModeOfDeliverySetup.aspx", Icon = "MD" },
                        new SetupLinkItem { FormKey = "MethodOfPaymentSetup", Title = "Method of Payment", Url = "/MethodOfPaymentSetup.aspx", Icon = "MP" },
                        new SetupLinkItem { FormKey = "CustomerGroupSetup", Title = "Customer Group", Url = "/CustomerGroupSetup.aspx", Icon = "CG" },
                        new SetupLinkItem { FormKey = "TermsOfPaymentSetup", Title = "Terms of Payment", Url = "/TermsOfPaymentSetup.aspx", Icon = "TP" },
                        new SetupLinkItem { FormKey = "CustomerClassSetup", Title = "Customer Class", Url = "/CustomerClassSetup.aspx", Icon = "CL" },
                        new SetupLinkItem { FormKey = "BillPreferenceSetup", Title = "Bill Preference", Url = "/BillPreferenceSetup.aspx", Icon = "BP" },
                        new SetupLinkItem { FormKey = "TaxGroupSetup", Title = "Tax Group", Url = "/TaxGroupSetup.aspx", Icon = "TG" },
                        new SetupLinkItem { FormKey = "FBRStatusSetup", Title = "FBR Status", Url = "/FBRStatusSetup.aspx", Icon = "FB" },
                    }
                },
                new SetupCategoryGroup
                {
                    Category = "Product Setup",
                    Icon = "PRD",
                    Links = new List<SetupLinkItem>
                    {
                        new SetupLinkItem { FormKey = "ProductNatureSetup", Title = "Product Nature", Url = "/ProductNatureSetup.aspx", Icon = "PN" },
                        new SetupLinkItem { FormKey = "InventoryTypeSetup", Title = "Inventory Type", Url = "/InventoryTypeSetup.aspx", Icon = "IT" },
                        new SetupLinkItem { FormKey = "ItemRegisteredSetup", Title = "Item Registered", Url = "/ItemRegisteredSetup.aspx", Icon = "IR" },
                        new SetupLinkItem { FormKey = "BrandCodeSetup", Title = "Brand Code", Url = "/BrandCodeSetup.aspx", Icon = "BC" },
                        new SetupLinkItem { FormKey = "BrandGroupSetup", Title = "Brand Group", Url = "/BrandGroupSetup.aspx", Icon = "BR" },
                        new SetupLinkItem { FormKey = "ProductGroupSetup", Title = "Product Group", Url = "/ProductGroupSetup.aspx", Icon = "PG" },
                        new SetupLinkItem { FormKey = "ProductSalesGroupSetup", Title = "Sales Group", Url = "/ProductSalesGroupSetup.aspx", Icon = "PS" },
                        new SetupLinkItem { FormKey = "ItemGroupSetup", Title = "Item Group", Url = "/ItemGroupSetup.aspx", Icon = "IG" },
                        new SetupLinkItem { FormKey = "SalesCategorySetup", Title = "Sales Category", Url = "/SalesCategorySetup.aspx", Icon = "SC" },
                        new SetupLinkItem { FormKey = "ProductDivisionSetup", Title = "Division", Url = "/ProductDivisionSetup.aspx", Icon = "PD" },
                        new SetupLinkItem { FormKey = "ProductTeamSetup", Title = "Team", Url = "/ProductTeamSetup.aspx", Icon = "PT" },
                        new SetupLinkItem { FormKey = "HSCodeSetup", Title = "HS Code", Url = "/HSCodeSetup.aspx", Icon = "HS" },
                    }
                },
                new SetupCategoryGroup
                {
                    Category = "Security",
                    Icon = "SEC",
                    Links = new List<SetupLinkItem>
                    {
                        new SetupLinkItem { FormKey = "UserSetup", Title = "User Setup", Url = "/UserSetup.aspx", Icon = "US" },
                        new SetupLinkItem { FormKey = "UserRightsSetup", Title = "User Rights", Url = "/UserRightsSetup.aspx", Icon = "UR" },
                        new SetupLinkItem { FormKey = "AuditReport", Title = "Audit Log Report", Url = "/AuditReport.aspx", Icon = "AL" },
                    }
                },
            };
        }
    }
}
