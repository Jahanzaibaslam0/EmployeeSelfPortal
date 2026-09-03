using HRMS.Services;
namespace HRMS.Pages;

public class BrandCodeSetupModel : LookupSetupPageModel
{
    public BrandCodeSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblBrandCode";
    protected override string IdColumn => "BrandCodeID";
    protected override string NameColumn => "BrandCode";
    protected override string? AliasColumn => "BrandName";

    public override string PageTitle => "Brand Code Setup";
    public override string ItemLabel => "Brand Code";
    public override string PagePath => "/BrandCodeSetup";
    public override string AliasLabel => "Brand Name";
}
