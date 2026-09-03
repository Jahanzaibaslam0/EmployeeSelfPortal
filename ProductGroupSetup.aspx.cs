using HRMS.Services;

namespace HRMS
{
    public partial class ProductGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblProductGroup";
        protected override string IdColumn => "ProductGroupID";
        protected override string NameColumn => "ProductGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Product Group Setup";
        public override string ItemLabel => "Product Group";
        public override string PagePath => "/ProductGroupSetup";
    }
}
