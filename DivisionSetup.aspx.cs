using HRMS.Services;

namespace HRMS
{
    public partial class DivisionSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblDivision";
        protected override string IdColumn => "DivisionID";
        protected override string NameColumn => "DivisionName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Division Setup";
        public override string ItemLabel => "Division";
        public override string PagePath => "/DivisionSetup";
    }
}
