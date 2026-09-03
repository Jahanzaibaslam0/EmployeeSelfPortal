namespace HRMS
{
    public partial class WorkerCategorySetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblWorkerCategory";
        protected override string IdColumn => "WorkerCategoryID";
        protected override string CodeColumn => "WorkerCategoryCode";
        protected override string NameColumn => "WorkerCategoryName";
        protected override string ExtraColumn => "Description";
        public override string PageTitle => "Worker Category Setup";
        public override string ItemLabel => "Worker Category";
        public override string PagePath => "WorkerCategorySetup";
    }
}
