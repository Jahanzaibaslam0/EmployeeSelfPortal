using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ContactCitySetupPage : AppBasePage
    {
        public string PageTitle => "City Setup";
        public ContactCityItem Input { get; set; } = new ContactCityItem { IsActive = true };
        public List<ContactCityItem> Records { get; set; } = new List<ContactCityItem>();
        public List<ContactStateItem> States { get; set; } = new List<ContactStateItem>();
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
                    _svc.DeactivateCity(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0, Auth.CurrentUserId);
                    SetAlert("City deactivated.");
                    Response.Redirect("~/ContactCitySetup.aspx");
                    return;
                }
                Input = new ContactCityItem
                {
                    CityID = int.TryParse(FormString("cityID"), out var id) ? id : 0,
                    StateID = int.TryParse(FormString("stateID"), out var sid) ? sid : 0,
                    CityName = FormString("cityName"),
                    IsActive = FormBool("isActive")
                };
                var error = _svc.SaveCity(Input, Auth.CurrentUserId);
                if (!string.IsNullOrEmpty(error))
                {
                    AlertMessage = error; AlertType = "error";
                    States = _svc.ListStateRecords();
                    Records = _svc.ListCityRecords();
                    return;
                }
                SetAlert("City saved.");
                Response.Redirect("~/ContactCitySetup.aspx");
                return;
            }
            LoadAlert(out var msg, out var typ); AlertMessage = msg; AlertType = typ;
            States = _svc.ListStateRecords();
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
            {
                var found = _svc.GetCity(editId.Value);
                if (found != null) Input = found;
            }
            Records = _svc.ListCityRecords();
        }
    }
}
