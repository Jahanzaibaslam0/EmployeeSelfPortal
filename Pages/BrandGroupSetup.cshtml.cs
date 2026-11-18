using HRMS.Services;
namespace HRMS.Pages;

public class BrandGroupSetupModel : LookupSetupPageModel
{
    public BrandGroupSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblBrandGroup";
    protected override string IdColumn => "BrandGroupID";
    protected override string NameColumn => "BrandGroupName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Brand Group Setup";
    public override string ItemLabel => "Brand Group";
    public override string PagePath => "/BrandGroupSetup";
}
