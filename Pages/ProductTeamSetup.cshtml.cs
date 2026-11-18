using HRMS.Services;
namespace HRMS.Pages;

public class ProductTeamSetupModel : LookupSetupPageModel
{
    public ProductTeamSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblProductTeam";
    protected override string IdColumn => "ProductTeamID";
    protected override string NameColumn => "ProductTeamName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Team Setup";
    public override string ItemLabel => "Team";
    public override string PagePath => "/ProductTeamSetup";
}
