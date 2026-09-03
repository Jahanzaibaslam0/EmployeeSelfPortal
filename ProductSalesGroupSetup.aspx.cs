using HRMS.Services;

namespace HRMS
{
    public partial class ProductSalesGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblProductSalesGroup";
        protected override string IdColumn => "ProductSalesGroupID";
        protected override string NameColumn => "ProductSalesGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Sales Group Setup";
        public override string ItemLabel => "Sales Group";
        public override string PagePath => "/ProductSalesGroupSetup";
    }
}
