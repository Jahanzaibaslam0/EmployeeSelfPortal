namespace HRMS
{
    public partial class WorkArrangementSetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblWorkArrangement";
        protected override string IdColumn => "WorkArrangementID";
        protected override string CodeColumn => "WorkArrangementCode";
        protected override string NameColumn => "WorkArrangementName";
        protected override string CodePrefix => "WA-";
        public override string PageTitle => "Work Arrangement Setup";
        public override string ItemLabel => "Work Arrangement";
        public override string PagePath => "WorkArrangementSetup";
    }
}
