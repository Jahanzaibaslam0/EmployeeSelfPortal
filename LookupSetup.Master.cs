using System.Web.UI;

namespace HRMS
{
    public partial class LookupSetupMaster : MasterPage
    {
        protected LookupSetupBasePage SetupPage => Page as LookupSetupBasePage;

        protected string PageTitle => SetupPage?.PageTitle ?? "";
        protected string ItemLabel => SetupPage?.ItemLabel ?? "";
        protected string PagePath => SetupPage?.PagePath?.TrimStart('/') ?? "";
        protected string AliasLabel => SetupPage?.AliasLabel ?? "Alias";
        protected int AliasMaxLength => SetupPage?.AliasMaxLength ?? 50;
        protected bool ShowAlias => SetupPage?.ShowAlias ?? false;
        protected string AlertMessage => SetupPage?.AlertMessage ?? "";
        protected string AlertType => SetupPage?.AlertType ?? "success";
        protected LookupRecord Input => SetupPage?.Input ?? new LookupRecord();
        protected System.Collections.Generic.List<LookupRecord> Records => SetupPage?.Records ?? new System.Collections.Generic.List<LookupRecord>();
    }
}
