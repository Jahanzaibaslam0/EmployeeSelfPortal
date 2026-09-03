using HRMS.Services;

namespace HRMS
{
    public partial class BusinessSegmentSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblBusinessSegment";
        protected override string IdColumn => "BusinessSegmentID";
        protected override string NameColumn => "BusinessSegmentName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Business Segment Setup";
        public override string ItemLabel => "Business Segment";
        public override string PagePath => "/BusinessSegmentSetup";
    }
}
