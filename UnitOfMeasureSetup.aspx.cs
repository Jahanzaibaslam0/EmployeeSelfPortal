using HRMS.Services;

namespace HRMS
{
    public partial class UnitOfMeasureSetupPage : LookupSetupBasePage
    {
        protected override string TableName => "tblUnitOfMeasure";
        protected override string IdColumn => "UnitOfMeasureID";
        protected override string NameColumn => "UnitOfMeasureName";
        protected override string AliasColumn => "AliasName";

        public override string PageTitle => "Unit of Measure Setup";
        public override string ItemLabel => "Unit of Measure";
        public override string PagePath => "/UnitOfMeasureSetup";
    }
}
