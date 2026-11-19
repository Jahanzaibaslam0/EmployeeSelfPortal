using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using HRMS.Services;

namespace HRMS
{
    public partial class EmployeeDocumentFilePage : AppBasePage
    {
        private readonly EmployeeProfileAccessService _profileAccess = new EmployeeProfileAccessService();
        private readonly EmployeeDocumentStorage _storage = new EmployeeDocumentStorage();

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = 0;
            int.TryParse(Request.QueryString["id"], out id);
            if (id <= 0)
            {
                Response.StatusCode = 400;
                Response.Write("Document is required.");
                Response.End();
                return;
            }

            string storedPath;
            string originalName;
            int employeeId;
            if (!TryLoadDocument(id, out employeeId, out storedPath, out originalName))
            {
                Response.StatusCode = 404;
                Response.Write("Document was not found.");
                Response.End();
                return;
            }

            if (!_profileAccess.CanViewEmployee(employeeId))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var physical = ResolvePhysicalPath(storedPath);
            if (string.IsNullOrWhiteSpace(physical) || !File.Exists(physical))
            {
                Response.StatusCode = 404;
                Response.Write("The document file is not available on the server.");
                Response.End();
                return;
            }

            var downloadName = string.IsNullOrWhiteSpace(originalName)
                ? Path.GetFileName(physical)
                : Path.GetFileName(originalName);
            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = EmployeeDocumentStorage.ContentTypeFor(physical);
            Response.AddHeader("Content-Disposition", "inline; filename=\"" + downloadName.Replace("\"", "") + "\"");
            Response.TransmitFile(physical);
            Response.End();
        }

        private bool TryLoadDocument(int documentId, out int employeeId, out string path, out string originalName)
        {
            employeeId = 0;
            path = "";
            originalName = "";
            var connStr = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr)) return false;

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(@"
SELECT EmployeeID, ISNULL(DocumentPath,''), ISNULL(OriginalFileName,'')
FROM tblEmployeeDocument
WHERE EmployeeDocumentID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", documentId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return false;
                    employeeId = dr.GetInt32(0);
                    path = dr.GetString(1);
                    originalName = dr.GetString(2);
                    return true;
                }
            }
        }

        private string ResolvePhysicalPath(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;

            if (stored.StartsWith("~/uploads/", StringComparison.OrdinalIgnoreCase)
                || stored.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var virtualPath = stored.StartsWith("~", StringComparison.Ordinal) ? stored : "~" + stored;
                return Server.MapPath(virtualPath);
            }

            if (_storage.IsUnderEmployeeRoot(stored))
                return Path.GetFullPath(stored);

            return null;
        }
    }
}
