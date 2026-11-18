using HRMS.Services;
namespace HRMS.Pages;

public class SalesCategorySetupModel : LookupSetupPageModel
{
    public SalesCategorySetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblSalesCategory";
    protected override string IdColumn => "SalesCategoryID";
    protected override string NameColumn => "SalesCategoryName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Sales Category Setup";
    public override string ItemLabel => "Sales Category";
    public override string PagePath => "/SalesCategorySetup";
}
