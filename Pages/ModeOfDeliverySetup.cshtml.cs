using HRMS.Services;
namespace HRMS.Pages;

public class ModeOfDeliverySetupModel : LookupSetupPageModel
{
    public ModeOfDeliverySetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblModeOfDelivery";
    protected override string IdColumn => "ModeOfDeliveryID";
    protected override string NameColumn => "ModeOfDeliveryName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Mode of Delivery Setup";
    public override string ItemLabel => "Mode of Delivery";
    public override string PagePath => "/ModeOfDeliverySetup";
}
