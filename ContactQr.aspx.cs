using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ContactQrPage : AppBasePage
    {
        private readonly ContactHubService _svc = new ContactHubService();

        public string PageTitle => "Contact QR Code";
        public ContactHubRow Item { get; set; }
        public string AlertMessage { get; set; } = "";

        public string CompanyName => Item == null ? "" : First(Item.OrgName, Item.LinkedMaster);
        public string MobileNumber => Item == null ? "" : First(Item.Mobile, Item.Phone, Item.Whatsapp);
        public string AddressText => Item == null ? "" : First(Item.AddressLine, Item.Branch);
        public string ShareText => Item == null ? "" : ContactHubService.ToPlainText(Item);
        public string ShareTitleJson => ToJson(Item == null ? "Contact" : Item.Name);
        public string ShareTextJson => ToJson(ShareText);
        public string DownloadUrl => Item == null ? "#" : ContactHubService.QrDownloadUrl(Item.ContactID, ContactHubService.QrStamp(Item));

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!Auth.IsAdmin && !Perms.CanRead("ContactMaster"))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var id = QueryInt("id") ?? 0;
            var handler = (Request.QueryString["handler"] ?? "").Trim();
            var view = (Request.QueryString["view"] ?? "All").Trim();
            if (id <= 0)
            {
                AlertMessage = "Contact was not found.";
                if (IsImageHandler(handler))
                {
                    Response.StatusCode = 404;
                    Response.End();
                }
                return;
            }

            Item = _svc.GetById(id, view, Auth.CurrentUserId);
            if (Item == null)
            {
                AlertMessage = "Contact was not found.";
                if (IsImageHandler(handler))
                {
                    Response.StatusCode = 404;
                    Response.End();
                }
                return;
            }

            if (IsImageHandler(handler))
            {
                WritePng(handler);
            }
        }

        public string PngUrl(int scale)
            => ContactHubService.QrPngUrl(Item.ContactID, ContactHubService.QrStamp(Item), scale);

        private static bool IsImageHandler(string handler) =>
            string.Equals(handler, "Png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(handler, "Download", StringComparison.OrdinalIgnoreCase);

        private void WritePng(string handler)
        {
            var scale = 4;
            int.TryParse(Request.QueryString["scale"], out scale);
            if (scale < 2) scale = 2;
            if (scale > 12) scale = 12;
            var bytes = ContactHubService.RenderQrPng(Item, scale);
            Response.Clear();
            Response.ContentType = "image/png";
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (string.Equals(handler, "Download", StringComparison.OrdinalIgnoreCase))
            {
                var name = "ContactQR_" + (Item.ContactCode ?? Item.ContactID.ToString()) + ".png";
                Response.AddHeader("Content-Disposition", "attachment; filename=\"" + name + "\"");
            }
            Response.BinaryWrite(bytes);
            Response.End();
        }

        private static string ToJson(string value)
            => new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(value ?? "");

        private static string First(params string[] values)
        {
            if (values == null) return "";
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return "";
        }
    }
}
