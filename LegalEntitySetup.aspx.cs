namespace HRMS
{
    public partial class LegalEntitySetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblLegalEntity";
        protected override string IdColumn => "LegalEntityID";
        protected override string CodeColumn => "LegalEntityCode";
        protected override string NameColumn => "LegalEntityName";
        protected override string ExtraColumn => "Description";
        public override string PageTitle => "Legal Entity Setup";
        public override string ItemLabel => "Legal Entity";
        public override string PagePath => "LegalEntitySetup";
    }
}
