using HRMS.Services;

namespace HRMS
{
    public partial class ItemRegisteredSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblItemRegistered";
        protected override string IdColumn => "ItemRegisteredID";
        protected override string NameColumn => "ItemRegisteredName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Item Registered Setup";
        public override string ItemLabel => "Item Registered";
        public override string PagePath => "/ItemRegisteredSetup";
    }
}
