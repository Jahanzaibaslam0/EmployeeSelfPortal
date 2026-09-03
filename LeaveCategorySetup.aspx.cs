using HRMS.Services;

namespace HRMS
{
    public partial class LeaveCategorySetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblLeaveCategory";
        protected override string IdColumn => "LeaveCategoryID";
        protected override string NameColumn => "LeaveCategoryName";

        public override string PageTitle => "Leave Category Setup";
        public override string ItemLabel => "Leave Category";
        public override string PagePath => "/LeaveCategorySetup";
    }
}
