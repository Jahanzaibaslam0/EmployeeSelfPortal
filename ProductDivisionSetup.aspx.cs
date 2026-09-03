using HRMS.Services;

namespace HRMS
{
    public partial class ProductDivisionSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblProductDivision";
        protected override string IdColumn => "ProductDivisionID";
        protected override string NameColumn => "ProductDivisionName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Division Setup";
        public override string ItemLabel => "Division";
        public override string PagePath => "/ProductDivisionSetup";
    }
}
