using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ContactLocationSetupPage : AppBasePage
    {
        public string PageTitle => "Contact Address Lookups";
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            try { new ContactAddressService().EnsureSchema(); } catch { }
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
        }
    }
}
