using HRMS.Services;

namespace HRMS
{
    public partial class EmploymentStatusSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblEmploymentStatus";
        protected override string IdColumn => "EmploymentStatusID";
        protected override string NameColumn => "EmploymentStatusName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Employment Status Setup";
        public override string ItemLabel => "Employment Status";
        public override string PagePath => "/EmploymentStatusSetup";
    }
}
