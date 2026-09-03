using HRMS.Services;

namespace HRMS
{
    public partial class ProductTeamSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblProductTeam";
        protected override string IdColumn => "ProductTeamID";
        protected override string NameColumn => "ProductTeamName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Team Setup";
        public override string ItemLabel => "Team";
        public override string PagePath => "/ProductTeamSetup";
    }
}
