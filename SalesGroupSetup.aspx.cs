namespace HRMS
{
    public partial class SalesGroupSetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblSalesGroup";
        protected override string IdColumn => "SalesGroupID";
        protected override string CodeColumn => "SalesGroupCode";
        protected override string NameColumn => "SalesGroupName";
        protected override string CodePrefix => "SGP-";
        public override string PageTitle => "Sales Group Setup";
        public override string ItemLabel => "Sales Group";
        public override string PagePath => "SalesGroupSetup";
    }
}
