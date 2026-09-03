using HRMS.Services;

namespace HRMS
{
    public partial class GenderSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblGender";
        protected override string IdColumn => "GenderID";
        protected override string NameColumn => "GenderName";

        public override string PageTitle => "Gender Setup";
        public override string ItemLabel => "Gender";
        public override string PagePath => "/GenderSetup";
    }
}
