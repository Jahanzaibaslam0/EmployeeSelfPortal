using HRMS.Services;
namespace HRMS.Pages;

public class CustomerClassSetupModel : LookupSetupPageModel
{
    public CustomerClassSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblCustomerClass";
    protected override string IdColumn => "CustomerClassID";
    protected override string NameColumn => "CustomerClassName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Customer Class Setup";
    public override string ItemLabel => "Customer Class";
    public override string PagePath => "/CustomerClassSetup";
}
