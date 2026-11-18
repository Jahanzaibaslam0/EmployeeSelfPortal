using HRMS.Services;
namespace HRMS.Pages;

public class MethodOfPaymentSetupModel : LookupSetupPageModel
{
    public MethodOfPaymentSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblMethodOfPayment";
    protected override string IdColumn => "MethodOfPaymentID";
    protected override string NameColumn => "MethodOfPaymentName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Method of Payment Setup";
    public override string ItemLabel => "Method of Payment";
    public override string PagePath => "/MethodOfPaymentSetup";
}
