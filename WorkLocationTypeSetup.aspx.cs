namespace HRMS
{
    public partial class WorkLocationTypeSetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblWorkLocationType";
        protected override string IdColumn => "WorkLocationTypeID";
        protected override string CodeColumn => "WorkLocationTypeCode";
        protected override string NameColumn => "WorkLocationTypeName";
        protected override string CodePrefix => "WLT-";
        public override string PageTitle => "Work Location Type Setup";
        public override string ItemLabel => "Work Location Type";
        public override string PagePath => "WorkLocationTypeSetup";
    }
}
