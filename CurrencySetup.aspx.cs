namespace HRMS
{
    public partial class CurrencySetupPage : SimpleCodeNameSetupPage
    {
        protected override string TableName => "tblCurrency";
        protected override string IdColumn => "CurrencyID";
        protected override string CodeColumn => "CurrencyCode";
        protected override string NameColumn => "CurrencyName";
        public override string PageTitle => "Currency Setup";
        public override string ItemLabel => "Currency";
        public override string PagePath => "CurrencySetup";
    }
}
