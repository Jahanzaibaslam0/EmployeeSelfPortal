using HRMS.Services;

namespace HRMS
{
    public partial class HSCodeSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblHSCode";
        protected override string IdColumn => "HSCodeID";
        protected override string NameColumn => "HSCode";
        protected override string AliasColumn => "HSCodeDescription";
        public override string AliasLabel => "Description";

        public override string PageTitle => "HS Code Setup";
        public override string ItemLabel => "HS Code";
        public override string PagePath => "/HSCodeSetup";
    }
}
