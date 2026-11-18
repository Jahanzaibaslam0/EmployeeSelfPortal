using HRMS.Services;
namespace HRMS.Pages;

public class HSCodeSetupModel : LookupSetupPageModel
{
    public HSCodeSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblHSCode";
    protected override string IdColumn => "HSCodeID";
    protected override string NameColumn => "HSCode";
    protected override string? AliasColumn => "HSCodeDescription";

    public override string PageTitle => "HS Code Setup";
    public override string ItemLabel => "HS Code";
    public override string PagePath => "/HSCodeSetup";
    public override string AliasLabel => "Description";
}
