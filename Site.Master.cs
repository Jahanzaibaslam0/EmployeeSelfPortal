using System;
using System.Web.UI;

namespace HRMS
{
    public partial class SiteMaster : MasterPage
    {
        public string PageTitleText
        {
            get { return litTitle != null ? litTitle.Text : ""; }
            set
            {
                if (litTitle != null) litTitle.Text = value ?? "";
                if (appHeader != null) appHeader.PageTitleText = value ?? "";
            }
        }

        protected global::System.Web.UI.WebControls.Literal litTitle;
        protected global::HRMS.Controls.AppHeader appHeader;
        protected global::HRMS.Controls.AppFooter appFooter;

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
