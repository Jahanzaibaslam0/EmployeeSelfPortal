using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class StateSetupPage : AppBasePage
    {
        public string PageTitle => "State Setup";
        public ContactStateItem Input { get; set; } = new ContactStateItem { IsActive = true };
        public List<ContactStateItem> Records { get; set; } = new List<ContactStateItem>();
        public List<GeoLookupItem> Countries { get; set; } = new List<GeoLookupItem>();
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
                    _svc.DeactivateState(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0, Auth.CurrentUserId);
                    SetAlert("State deactivated.");
                    Response.Redirect("~/StateSetup.aspx");
                    return;
                }
                Input = new ContactStateItem
                {
                    StateID = int.TryParse(FormString("stateID"), out var id) ? id : 0,
                    CountryID = int.TryParse(FormString("countryID"), out var cid) ? cid : 0,
                    StateName = FormString("stateName"),
                    IsActive = FormBool("isActive")
                };
                var error = _svc.SaveState(Input, Auth.CurrentUserId);
                if (!string.IsNullOrEmpty(error))
                {
                    AlertMessage = error; AlertType = "error";
                    Countries = _svc.ListCountries(true);
                    Records = _svc.ListStateRecords();
                    return;
                }
                SetAlert("State saved.");
                Response.Redirect("~/StateSetup.aspx");
                return;
            }
            LoadAlert(out var msg, out var typ); AlertMessage = msg; AlertType = typ;
            Countries = _svc.ListCountries(true);
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
            {
                var found = _svc.GetState(editId.Value);
                if (found != null) Input = found;
            }
            Records = _svc.ListStateRecords();
        }
    }
}
