using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Web;
using HRMS.Services;

namespace HRMS
{
    public partial class GalleryImagePage : AppBasePage
    {
        public static string UrlFor(int galleryImageId)
        {
            return galleryImageId > 0 ? "/GalleryImage.aspx?id=" + galleryImageId : "";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(Request.QueryString["id"], out id) || id <= 0)
            {
                WriteNotFound();
                return;
            }

            string storedPath;
            string originalName;
            if (!TryLoad(id, out storedPath, out originalName))
            {
                WriteNotFound();
                return;
            }

            byte[] bytes;
            string contentType;
            if (!TryReadImage(storedPath, originalName, id, out bytes, out contentType))
            {
                WriteNotFound();
                return;
            }

            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = contentType;
            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetMaxAge(TimeSpan.FromMinutes(10));
            Response.BinaryWrite(bytes);
            Response.End();
        }

        private static bool TryLoad(int id, out string path, out string originalName)
        {
            path = "";
            originalName = "";
            var connStr = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr)) return false;
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(ImagePath,''), ISNULL(OriginalFileName,'')
FROM tblGalleryImage WHERE GalleryImageID = @Id AND IsActive = 1;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return false;
                    path = dr.GetString(0);
                    originalName = dr.GetString(1);
                    return !string.IsNullOrWhiteSpace(path);
                }
            }
        }

        private bool TryReadImage(string stored, string originalName, int id, out byte[] bytes, out string contentType)
        {
            bytes = null;
            contentType = "image/jpeg";
            if (string.IsNullOrWhiteSpace(stored)) return false;

            stored = stored.Trim();
            var extHint = Path.GetExtension(string.IsNullOrWhiteSpace(originalName) ? stored : originalName);

            var physical = ResolvePhysicalPath(stored);
            if (!string.IsNullOrWhiteSpace(physical) && File.Exists(physical))
            {
                bytes = File.ReadAllBytes(physical);
                contentType = ContentTypeFor(physical);
                return bytes.Length > 0;
            }

            Uri uri;
            if (Uri.TryCreate(stored, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var cached = CachePathFor(id, extHint);
                if (File.Exists(cached))
                {
                    bytes = File.ReadAllBytes(cached);
                    contentType = ContentTypeFor(cached);
                    return bytes.Length > 0;
                }

                try
                {
                    using (var client = new WebClient())
                    {
                        client.UseDefaultCredentials = true;
                        bytes = client.DownloadData(uri);
                        var headerType = client.ResponseHeaders != null ? client.ResponseHeaders["Content-Type"] : null;
                        if (!string.IsNullOrWhiteSpace(headerType) && headerType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                            contentType = headerType.Split(';')[0].Trim();
                        else
                            contentType = ContentTypeFor(extHint);
                    }
                    if (bytes != null && bytes.Length > 0)
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(cached));
                            File.WriteAllBytes(cached, bytes);
                        }
                        catch { }
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        private string ResolvePhysicalPath(string stored)
        {
            if (stored.StartsWith("~/uploads/", StringComparison.OrdinalIgnoreCase)
                || stored.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var virtualPath = stored.StartsWith("~", StringComparison.Ordinal) ? stored : "~" + stored;
                try { return Server.MapPath(virtualPath); }
                catch { return null; }
            }

            if (stored.StartsWith("\\\\", StringComparison.Ordinal) || (stored.Length >= 3 && stored[1] == ':' && stored[2] == '\\'))
            {
                try { return Path.GetFullPath(stored); }
                catch { return null; }
            }

            var fileName = Path.GetFileName(stored.Replace('/', Path.DirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
            {
                var local = Path.Combine(Server.MapPath("~/uploads/gallery"), fileName);
                if (File.Exists(local)) return local;
            }

            return null;
        }

        private string CachePathFor(int id, string ext)
        {
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".img" : ext.ToLowerInvariant();
            if (safeExt != ".png" && safeExt != ".jpg" && safeExt != ".jpeg" && safeExt != ".webp" && safeExt != ".gif")
                safeExt = ".img";
            return Path.Combine(Server.MapPath("~/uploads/gallery"), "remote_" + id + safeExt);
        }

        private static string ContentTypeFor(string pathOrExt)
        {
            var ext = Path.GetExtension(pathOrExt ?? "").ToLowerInvariant();
            switch (ext)
            {
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".bmp": return "image/bmp";
                default: return "image/jpeg";
            }
        }

        private void WriteNotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Response.ContentType = "text/plain";
            Response.Write("Gallery image was not found.");
            Response.End();
        }
    }
}
