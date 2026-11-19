using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ProductSerialBarcodePage : AppBasePage
    {
        private readonly ProductSerializationService _svc = new ProductSerializationService();

        public string PageTitle => "GS1 DataMatrix";
        public ProductSerialItem Item { get; set; }
        public string AlertMessage { get; set; } = "";
        public bool PrintMode { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!(Auth.IsAdmin || Perms.CanRead(ProductSerializationService.FormKey)))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var id = QueryInt("id") ?? 0;
            var handler = (Request.QueryString["handler"] ?? "").Trim();
            PrintMode = Request.QueryString["print"] == "1";

            Item = _svc.GetSerialById(id);
            if (Item == null)
            {
                AlertMessage = "Serialization record was not found.";
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
                return;
            }
        }

        private static bool IsImageHandler(string handler) =>
            string.Equals(handler, "Png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(handler, "Download", StringComparison.OrdinalIgnoreCase);

        private void WritePng(string handler)
        {
            var withHri = !string.Equals(Request.QueryString["plain"], "1", StringComparison.OrdinalIgnoreCase);
            var module = 8;
            int.TryParse(Request.QueryString["scale"], out module);
            if (module < 4) module = 4;
            if (module > 16) module = 16;
            var bytes = _svc.RenderDataMatrixPng(Item, module, withHri);
            Response.Clear();
            Response.ContentType = "image/png";
            if (string.Equals(handler, "Download", StringComparison.OrdinalIgnoreCase))
            {
                var name = "GS1_DataMatrix_" + Item.Hexacode + ".png";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + name);
            }
            else
            {
                Response.Cache.SetMaxAge(TimeSpan.FromMinutes(5));
            }
            Response.BinaryWrite(bytes);
            Response.End();
        }

        public string HriHtml()
        {
            if (Item == null) return "";
            return Server.HtmlEncode(Item.Gs1HumanReadable ?? "").Replace("\n", "<br />");
        }
    }
}
