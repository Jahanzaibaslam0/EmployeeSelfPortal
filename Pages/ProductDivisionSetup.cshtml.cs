using HRMS.Services;
namespace HRMS.Pages;

public class ProductDivisionSetupModel : LookupSetupPageModel
{
    public ProductDivisionSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblProductDivision";
    protected override string IdColumn => "ProductDivisionID";
    protected override string NameColumn => "ProductDivisionName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Division Setup";
    public override string ItemLabel => "Division";
    public override string PagePath => "/ProductDivisionSetup";
}
