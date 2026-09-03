using HRMS.Services;

namespace HRMS
{
    public partial class UnitSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblUnit";
        protected override string IdColumn => "UnitID";
        protected override string NameColumn => "UnitName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Unit Setup";
        public override string ItemLabel => "Unit";
        public override string PagePath => "/UnitSetup";
    }
}
