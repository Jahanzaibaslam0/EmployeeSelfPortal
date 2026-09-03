using HRMS.Services;

namespace HRMS
{
    public partial class CostCenterSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblCostCenter";
        protected override string IdColumn => "CostCenterID";
        protected override string NameColumn => "CostCenterName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Cost Center Setup";
        public override string ItemLabel => "Cost Center";
        public override string PagePath => "/CostCenterSetup";
    }
}
