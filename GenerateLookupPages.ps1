$root = "D:\Project\HRMS"
$data = "D:\Project\DATA"
New-Item -ItemType Directory -Force -Path $data | Out-Null
Copy-Item "$root\Database\Script.sql" $data -Force
Copy-Item "$root\Database\UserSecurity_Script.sql" $data -Force -ErrorAction SilentlyContinue
foreach ($f in @('css','js','images')) {
    $src = Join-Path $root "wwwroot\$f"
    $dst = Join-Path $root $f
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path $dst | Out-Null
        Copy-Item "$src\*" $dst -Recurse -Force
    }
}
$lookups = @(
  @{N="GenderSetup";T="tblGender";I="GenderID";M="GenderName";Title="Gender Setup";Label="Gender"},
  @{N="DivisionSetup";T="tblDivision";I="DivisionID";M="DivisionName";Title="Division Setup";Label="Division";Alias="AliasName"},
  @{N="BusinessUnitSetup";T="tblBusinessUnit";I="BusinessUnitID";M="BusinessUnitName";Title="Business Unit Setup";Label="Business Unit";Alias="AliasName"},
  @{N="WorkforceSegmentSetup";T="tblWorkforceSegment";I="WorkforceSegmentID";M="WorkforceSegmentName";Title="Workforce Segment Setup";Label="Workforce Segment";Alias="AliasName"},
  @{N="UnitSetup";T="tblUnit";I="UnitID";M="UnitName";Title="Unit Setup";Label="Unit";Alias="AliasName"},
  @{N="WingSetup";T="tblWing";I="WingID";M="WingName";Title="Wing Setup";Label="Wing";Alias="AliasName"},
  @{N="ReligionSetup";T="tblReligion";I="ReligionID";M="ReligionName";Title="Religion Setup";Label="Religion";Alias="AliasName"},
  @{N="NationalitySetup";T="tblNationality";I="NationalityID";M="NationalityName";Title="Nationality Setup";Label="Nationality";Alias="AliasName"},
  @{N="RegionSetup";T="tblRegion";I="RegionID";M="RegionName";Title="Region Setup";Label="Region";Alias="AliasName"},
  @{N="BusinessSegmentSetup";T="tblBusinessSegment";I="BusinessSegmentID";M="BusinessSegmentName";Title="Business Segment Setup";Label="Business Segment";Alias="AliasName"},
  @{N="GradeSetup";T="tblGrade";I="GradeID";M="GradeName";Title="Grade Setup";Label="Grade";Alias="AliasName"},
  @{N="EmploymentTypeSetup";T="tblEmploymentType";I="EmploymentTypeID";M="EmploymentTypeName";Title="Employment Type Setup";Label="Employment Type";Alias="AliasName"},
  @{N="DesignationLevelSetup";T="tblDesignationLevel";I="DesignationLevelID";M="DesignationLevelName";Title="Designation Level Setup";Label="Designation Level";Alias="AliasName"},
  @{N="TitleSetup";T="tblTitle";I="TitleID";M="TitleName";Title="Title Setup";Label="Title";Alias="AliasName"},
  @{N="EmploymentStatusSetup";T="tblEmploymentStatus";I="EmploymentStatusID";M="EmploymentStatusName";Title="Employment Status Setup";Label="Employment Status";Alias="AliasName"},
  @{N="ExpenseCategorySetup";T="tblExpenseCategory";I="ExpenseCategoryID";M="ExpenseCategoryName";Title="Expense Category Setup";Label="Expense Category"},
  @{N="LeaveCategorySetup";T="tblLeaveCategory";I="LeaveCategoryID";M="LeaveCategoryName";Title="Leave Category Setup";Label="Leave Category"},
  @{N="BloodGroupSetup";T="tblBloodGroup";I="BloodGroupID";M="BloodGroupName";Title="Blood Group Setup";Label="Blood Group";Alias="AliasName"},
  @{N="DocumentTypeSetup";T="tblDocumentType";I="DocumentTypeID";M="DocumentTypeName";Title="Document Type Setup";Label="Document Type";Alias="AliasName"},
  @{N="LocationSetup";T="tblLocation";I="LocationID";M="LocationName";Title="Location Setup";Label="Location";Alias="AliasName"},
  @{N="CostCenterSetup";T="tblCostCenter";I="CostCenterID";M="CostCenterName";Title="Cost Center Setup";Label="Cost Center";Alias="AliasName"},
  @{N="UnitOfMeasureSetup";T="tblUnitOfMeasure";I="UnitOfMeasureID";M="UnitOfMeasureName";Title="Unit of Measure Setup";Label="Unit of Measure";Alias="AliasName"},
  @{N="ModeOfDeliverySetup";T="tblModeOfDelivery";I="ModeOfDeliveryID";M="ModeOfDeliveryName";Title="Mode of Delivery Setup";Label="Mode of Delivery";Alias="AliasName"},
  @{N="MethodOfPaymentSetup";T="tblMethodOfPayment";I="MethodOfPaymentID";M="MethodOfPaymentName";Title="Method of Payment Setup";Label="Method of Payment";Alias="AliasName"},
  @{N="CustomerGroupSetup";T="tblCustomerGroup";I="CustomerGroupID";M="CustomerGroupName";Title="Customer Group Setup";Label="Customer Group";Alias="AliasName"},
  @{N="TermsOfPaymentSetup";T="tblTermsOfPayment";I="TermsOfPaymentID";M="TermsOfPaymentName";Title="Terms of Payment Setup";Label="Terms of Payment";Alias="AliasName"},
  @{N="CustomerClassSetup";T="tblCustomerClass";I="CustomerClassID";M="CustomerClassName";Title="Customer Class Setup";Label="Customer Class";Alias="AliasName"},
  @{N="BillPreferenceSetup";T="tblBillPreference";I="BillPreferenceID";M="BillPreferenceName";Title="Bill Preference Setup";Label="Bill Preference";Alias="AliasName"},
  @{N="TaxGroupSetup";T="tblTaxGroup";I="TaxGroupID";M="TaxGroupName";Title="Tax Group Setup";Label="Tax Group";Alias="AliasName"},
  @{N="FBRStatusSetup";T="tblFBRStatus";I="FBRStatusID";M="FBRStatusName";Title="FBR Status Setup";Label="FBR Status";Alias="AliasName"},
  @{N="ProductNatureSetup";T="tblProductNature";I="ProductNatureID";M="ProductNatureName";Title="Product Nature Setup";Label="Product Nature";Alias="AliasName"},
  @{N="InventoryTypeSetup";T="tblInventoryType";I="InventoryTypeID";M="InventoryTypeName";Title="Inventory Type Setup";Label="Inventory Type";Alias="AliasName"},
  @{N="ItemRegisteredSetup";T="tblItemRegistered";I="ItemRegisteredID";M="ItemRegisteredName";Title="Item Registered Setup";Label="Item Registered";Alias="AliasName"},
  @{N="BrandCodeSetup";T="tblBrandCode";I="BrandCodeID";M="BrandCode";Title="Brand Code Setup";Label="Brand Code";Alias="BrandName"},
  @{N="BrandGroupSetup";T="tblBrandGroup";I="BrandGroupID";M="BrandGroupName";Title="Brand Group Setup";Label="Brand Group";Alias="AliasName"},
  @{N="ProductGroupSetup";T="tblProductGroup";I="ProductGroupID";M="ProductGroupName";Title="Product Group Setup";Label="Product Group";Alias="AliasName"},
  @{N="ProductSalesGroupSetup";T="tblProductSalesGroup";I="ProductSalesGroupID";M="ProductSalesGroupName";Title="Sales Group Setup";Label="Sales Group";Alias="AliasName"},
  @{N="ItemGroupSetup";T="tblItemGroup";I="ItemGroupID";M="ItemGroupName";Title="Item Group Setup";Label="Item Group";Alias="AliasName"},
  @{N="SalesCategorySetup";T="tblSalesCategory";I="SalesCategoryID";M="SalesCategoryName";Title="Sales Category Setup";Label="Sales Category";Alias="AliasName"},
  @{N="ProductDivisionSetup";T="tblProductDivision";I="ProductDivisionID";M="ProductDivisionName";Title="Division Setup";Label="Division";Alias="AliasName"},
  @{N="ProductTeamSetup";T="tblProductTeam";I="ProductTeamID";M="ProductTeamName";Title="Team Setup";Label="Team";Alias="AliasName"},
  @{N="HSCodeSetup";T="tblHSCode";I="HSCodeID";M="HSCode";Title="HS Code Setup";Label="HS Code";Alias="Description"},
  @{N="BankGroupSetup";T="tblBankGroup";I="BankGroupID";M="BankGroupName";Title="Bank Group Setup";Label="Bank Group";Alias="AliasName"}
)
foreach ($l in $lookups) {
  $name = $l.N
  "<%@ Page Language=`"C#`" MasterPageFile=`"~/LookupSetup.Master`" AutoEventWireup=`"true`" CodeBehind=`"$name.aspx.cs`" Inherits=`"HRMS.${name}Page`" %>" | Set-Content "$root\$name.aspx" -Encoding UTF8
  $aliasLine = if ($l.Alias) { "protected override string AliasColumn => `"$($l.Alias)`";" } else { "" }
  @"
using HRMS.Services;

namespace HRMS
{
    public partial class ${name}Page : LookupSetupBasePage
    {
        protected override string TableName => `"$($l.T)`";
        protected override string IdColumn => `"$($l.I)`";
        protected override string NameColumn => `"$($l.M)`";
        $aliasLine
        public override string PageTitle => `"$($l.Title)`";
        public override string ItemLabel => `"$($l.Label)`";
        public override string PagePath => `"/$name`";
    }
}
"@ | Set-Content "$root\$name.aspx.cs" -Encoding UTF8
}
Write-Host "Done. Aspx count:" (Get-ChildItem $root -Filter *.aspx -Recurse).Count
