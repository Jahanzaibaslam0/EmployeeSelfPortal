using HRMS.Services;
namespace HRMS.Pages;

public class BillPreferenceSetupModel : LookupSetupPageModel
{
    public BillPreferenceSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblBillPreference";
    protected override string IdColumn => "BillPreferenceID";
    protected override string NameColumn => "BillPreferenceName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Bill Preference Setup";
    public override string ItemLabel => "Bill Preference";
    public override string PagePath => "/BillPreferenceSetup";
}
