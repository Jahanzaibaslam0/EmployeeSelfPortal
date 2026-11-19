using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class StreetSetupPage : AppBasePage
    {
        public string PageTitle => "Street Setup";
        public ContactStreetItem Input { get; set; } = new ContactStreetItem { IsActive = true };
        public List<ContactStreetItem> Records { get; set; } = new List<ContactStreetItem>();
        public List<ContactPostalItem> Postals { get; set; } = new List<ContactPostalItem>();
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
                    _svc.DeactivateStreet(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0, Auth.CurrentUserId);
                    SetAlert("Street deactivated.");
                    Response.Redirect("~/StreetSetup.aspx");
                    return;
                }
                Input = new ContactStreetItem
                {
                    StreetID = int.TryParse(FormString("streetID"), out var id) ? id : 0,
                    PostalID = int.TryParse(FormString("postalID"), out var pid) ? pid : 0,
                    StreetName = FormString("streetName"),
                    IsActive = FormBool("isActive")
                };
                var error = _svc.SaveStreet(Input, Auth.CurrentUserId);
                if (!string.IsNullOrEmpty(error))
                {
                    AlertMessage = error; AlertType = "error";
                    Postals = _svc.ListPostalRecords();
                    Records = _svc.ListStreetRecords();
                    return;
                }
                SetAlert("Street saved.");
                Response.Redirect("~/StreetSetup.aspx");
                return;
            }
            LoadAlert(out var msg, out var typ); AlertMessage = msg; AlertType = typ;
            Postals = _svc.ListPostalRecords();
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
            {
                var found = _svc.GetStreet(editId.Value);
                if (found != null) Input = found;
            }
            Records = _svc.ListStreetRecords();
        }
    }
}
