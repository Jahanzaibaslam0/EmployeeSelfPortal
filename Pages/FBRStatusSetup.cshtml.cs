using HRMS.Services;
namespace HRMS.Pages;

public class FBRStatusSetupModel : LookupSetupPageModel
{
    public FBRStatusSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblFBRStatus";
    protected override string IdColumn => "FBRStatusID";
    protected override string NameColumn => "FBRStatusName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "FBR Status Setup";
    public override string ItemLabel => "FBR Status";
    public override string PagePath => "/FBRStatusSetup";
}
