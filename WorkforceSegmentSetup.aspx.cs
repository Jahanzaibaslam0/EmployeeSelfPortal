using HRMS.Services;

namespace HRMS
{
    public partial class WorkforceSegmentSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblWorkforceSegment";
        protected override string IdColumn => "WorkforceSegmentID";
        protected override string NameColumn => "WorkforceSegmentName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Workforce Segment Setup";
        public override string ItemLabel => "Workforce Segment";
        public override string PagePath => "/WorkforceSegmentSetup";
    }
}
