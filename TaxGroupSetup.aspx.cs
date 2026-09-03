using HRMS.Services;

namespace HRMS
{
    public partial class TaxGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblTaxGroup";
        protected override string IdColumn => "TaxGroupID";
        protected override string NameColumn => "TaxGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Tax Group Setup";
        public override string ItemLabel => "Tax Group";
        public override string PagePath => "/TaxGroupSetup";
    }
}
