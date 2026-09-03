using HRMS.Services;

namespace HRMS
{
    public partial class TermsOfPaymentSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblTermsOfPayment";
        protected override string IdColumn => "TermsOfPaymentID";
        protected override string NameColumn => "TermsOfPaymentName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Terms of Payment Setup";
        public override string ItemLabel => "Terms of Payment";
        public override string PagePath => "/TermsOfPaymentSetup";
    }
}
