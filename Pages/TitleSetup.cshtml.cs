using HRMS.Services;

namespace HRMS.Pages;

public class TitleSetupModel : LookupSetupPageModel
{
    public TitleSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblTitle";
    protected override string IdColumn => "TitleID";
    protected override string NameColumn => "TitleName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Title Setup";
    public override string ItemLabel => "Title";
    public override string PagePath => "/TitleSetup";
}
