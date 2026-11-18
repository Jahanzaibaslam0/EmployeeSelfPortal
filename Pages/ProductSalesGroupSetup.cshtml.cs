using HRMS.Services;
namespace HRMS.Pages;

public class ProductSalesGroupSetupModel : LookupSetupPageModel
{
    public ProductSalesGroupSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblProductSalesGroup";
    protected override string IdColumn => "ProductSalesGroupID";
    protected override string NameColumn => "ProductSalesGroupName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Sales Group Setup";
    public override string ItemLabel => "Sales Group";
    public override string PagePath => "/ProductSalesGroupSetup";
}
