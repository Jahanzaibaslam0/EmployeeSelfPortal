using System;
using System.Text;
using HRMS.Services;

namespace HRMS
{
    public partial class ContactExportPage : AppBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Auth.IsAdmin && !Perms.CanRead("ContactMaster"))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var id = QueryInt("id").GetValueOrDefault();
            var view = (Request.QueryString["view"] ?? "All").Trim();
            if (id <= 0)
            {
                Response.StatusCode = 400;
                Response.End();
                return;
            }

            var row = new ContactHubService().GetById(id, view, Auth.CurrentUserId);
            if (row == null)
            {
                Response.StatusCode = 404;
                Response.End();
                return;
            }

            var vcf = ContactHubService.ToVCard(row);
            var file = (row.ContactCode ?? "contact") + ".vcf";
            Response.Clear();
            Response.ContentType = "text/vcard";
            Response.Charset = "utf-8";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + file + "\"");
            var bytes = Encoding.UTF8.GetPreamble();
            Response.BinaryWrite(bytes);
            Response.Write(vcf);
            Response.End();
        }
    }
}
