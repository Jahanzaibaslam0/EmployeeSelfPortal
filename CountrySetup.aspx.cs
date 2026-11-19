using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class CountrySetupPage : AppBasePage
    {
        public string PageTitle => "Country Setup";
        public ContactCountryItem Input { get; set; } = new ContactCountryItem { IsActive = true };
        public List<ContactCountryItem> Records { get; set; } = new List<ContactCountryItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        private readonly ContactAddressService _svc = new ContactAddressService();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();
            if (IsPostBack)
            {
                if (string.Equals(Request.Form["__handler"], "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    _svc.DeactivateCountry(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0, Auth.CurrentUserId);
                    SetAlert("Country deactivated.");
                    Response.Redirect("~/CountrySetup.aspx");
                    return;
                }
                Input = new ContactCountryItem
                {
                    CountryID = int.TryParse(FormString("countryID"), out var id) ? id : 0,
                    CountryName = FormString("countryName"),
                    IsActive = FormBool("isActive")
                };
                var error = _svc.SaveCountry(Input, Auth.CurrentUserId);
                if (!string.IsNullOrEmpty(error))
                {
                    AlertMessage = error; AlertType = "error";
                    Records = _svc.ListCountryRecords(false);
                    return;
                }
                SetAlert(Input.CountryID > 0 ? "Country saved." : "Country created.");
                Response.Redirect("~/CountrySetup.aspx");
                return;
            }
            LoadAlert(out var msg, out var typ); AlertMessage = msg; AlertType = typ;
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
            {
                var found = _svc.GetCountry(editId.Value);
                if (found != null) Input = found;
            }
            Records = _svc.ListCountryRecords(false);
        }
    }
}
