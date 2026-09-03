using HRMS.Services;

namespace HRMS
{
    public partial class CustomerGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblCustomerGroup";
        protected override string IdColumn => "CustomerGroupID";
        protected override string NameColumn => "CustomerGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Customer Group Setup";
        public override string ItemLabel => "Customer Group";
        public override string PagePath => "/CustomerGroupSetup";
    }
}
