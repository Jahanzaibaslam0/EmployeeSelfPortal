using HRMS.Services;

namespace HRMS
{
    public partial class TitleSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblTitle";
        protected override string IdColumn => "TitleID";
        protected override string NameColumn => "TitleName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Title Setup";
        public override string ItemLabel => "Title";
        public override string PagePath => "/TitleSetup";
    }
}
