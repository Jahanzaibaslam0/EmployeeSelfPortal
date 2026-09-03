using HRMS.Services;

namespace HRMS
{
    public partial class FBRStatusSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblFBRStatus";
        protected override string IdColumn => "FBRStatusID";
        protected override string NameColumn => "FBRStatusName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "FBR Status Setup";
        public override string ItemLabel => "FBR Status";
        public override string PagePath => "/FBRStatusSetup";
    }
}
