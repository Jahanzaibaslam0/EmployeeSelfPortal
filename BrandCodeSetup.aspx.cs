using HRMS.Services;

namespace HRMS
{
    public partial class BrandCodeSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblBrandCode";
        protected override string IdColumn => "BrandCodeID";
        protected override string NameColumn => "BrandCode";
        protected override string AliasColumn => "BrandName";

        public override string PageTitle => "Brand Code Setup";
        public override string ItemLabel => "Brand Code";
        public override string PagePath => "/BrandCodeSetup";
        public override string AliasLabel => "Brand Name";
    }
}
