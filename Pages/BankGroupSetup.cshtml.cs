using HRMS.Services;
namespace HRMS.Pages;

public class BankGroupSetupModel : LookupSetupPageModel
{
    public BankGroupSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblBankGroup";
    protected override string IdColumn => "BankGroupID";
    protected override string NameColumn => "BankGroupName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Bank Group Setup";
    public override string ItemLabel => "Bank Group";
    public override string PagePath => "/BankGroupSetup";
}
