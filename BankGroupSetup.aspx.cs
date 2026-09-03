using HRMS.Services;

namespace HRMS
{
    public partial class BankGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblBankGroup";
        protected override string IdColumn => "BankGroupID";
        protected override string NameColumn => "BankGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Bank Group Setup";
        public override string ItemLabel => "Bank Group";
        public override string PagePath => "/BankGroupSetup";
    }
}
