using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class PostalSetupPage : AppBasePage
    {
        public string PageTitle => "Postal Setup";
        public ContactPostalItem Input { get; set; } = new ContactPostalItem { IsActive = true };
        public List<ContactPostalItem> Records { get; set; } = new List<ContactPostalItem>();
        public List<ContactCityItem> Cities { get; set; } = new List<ContactCityItem>();
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
                    _svc.DeactivatePostal(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0, Auth.CurrentUserId);
                    SetAlert("Postal code deactivated.");
                    Response.Redirect("~/PostalSetup.aspx");
                    return;
                }
                Input = new ContactPostalItem
                {
                    PostalID = int.TryParse(FormString("postalID"), out var id) ? id : 0,
                    CityID = int.TryParse(FormString("cityID"), out var cid) ? cid : 0,
                    PostalCode = FormString("postalCode"),
                    AreaName = FormString("areaName"),
                    IsActive = FormBool("isActive")
                };
                var error = _svc.SavePostal(Input, Auth.CurrentUserId);
                if (!string.IsNullOrEmpty(error))
                {
                    AlertMessage = error; AlertType = "error";
                    Cities = _svc.ListCityRecords();
                    Records = _svc.ListPostalRecords();
                    return;
                }
                SetAlert("Postal code saved.");
                Response.Redirect("~/PostalSetup.aspx");
                return;
            }
            LoadAlert(out var msg, out var typ); AlertMessage = msg; AlertType = typ;
            Cities = _svc.ListCityRecords();
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
            {
                var found = _svc.GetPostal(editId.Value);
                if (found != null) Input = found;
            }
            Records = _svc.ListPostalRecords();
        }
    }
}
