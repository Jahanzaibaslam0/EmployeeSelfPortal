using HRMS.Services;

namespace HRMS
{
    public partial class ProductNatureSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblProductNature";
        protected override string IdColumn => "ProductNatureID";
        protected override string NameColumn => "ProductNatureName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Product Nature Setup";
        public override string ItemLabel => "Product Nature";
        public override string PagePath => "/ProductNatureSetup";
    }
}
