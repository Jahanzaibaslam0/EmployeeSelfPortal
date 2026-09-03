using HRMS.Services;

namespace HRMS
{
    public partial class InventoryTypeSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblInventoryType";
        protected override string IdColumn => "InventoryTypeID";
        protected override string NameColumn => "InventoryTypeName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Inventory Type Setup";
        public override string ItemLabel => "Inventory Type";
        public override string PagePath => "/InventoryTypeSetup";
    }
}
