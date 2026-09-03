using HRMS.Services;

namespace HRMS
{
    public partial class NationalitySetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblNationality";
        protected override string IdColumn => "NationalityID";
        protected override string NameColumn => "NationalityName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Nationality Setup";
        public override string ItemLabel => "Nationality";
        public override string PagePath => "/NationalitySetup";
    }
}
