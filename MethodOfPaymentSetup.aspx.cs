using HRMS.Services;

namespace HRMS
{
    public partial class MethodOfPaymentSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblMethodOfPayment";
        protected override string IdColumn => "MethodOfPaymentID";
        protected override string NameColumn => "MethodOfPaymentName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Method of Payment Setup";
        public override string ItemLabel => "Method of Payment";
        public override string PagePath => "/MethodOfPaymentSetup";
    }
}
