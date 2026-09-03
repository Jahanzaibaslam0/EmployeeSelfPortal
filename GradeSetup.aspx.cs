using HRMS.Services;

namespace HRMS
{
    public partial class GradeSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblGrade";
        protected override string IdColumn => "GradeID";
        protected override string NameColumn => "GradeName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Grade Setup";
        public override string ItemLabel => "Grade";
        public override string PagePath => "/GradeSetup";
    }
}
