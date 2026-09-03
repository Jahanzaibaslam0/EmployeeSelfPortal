using HRMS.Services;

namespace HRMS
{
    public partial class ReligionSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblReligion";
        protected override string IdColumn => "ReligionID";
        protected override string NameColumn => "ReligionName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Religion Setup";
        public override string ItemLabel => "Religion";
        public override string PagePath => "/ReligionSetup";
    }
}
