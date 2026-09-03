using HRMS.Services;

namespace HRMS
{
    public partial class ItemGroupSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblItemGroup";
        protected override string IdColumn => "ItemGroupID";
        protected override string NameColumn => "ItemGroupName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Item Group Setup";
        public override string ItemLabel => "Item Group";
        public override string PagePath => "/ItemGroupSetup";
    }
}
