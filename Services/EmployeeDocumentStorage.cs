using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace HRMS.Services
{
    /// <summary>
    /// Stores employee documents on the ERP share:
    /// \\AIBG.com\erpdocs\Employee Document\{EmployeeCode}\CNIC.pdf
    /// Folder name is the Employee Master code (EMP000001), not the numeric EmployeeID.
    /// Creates the root and employee-code folder when they do not exist.
    /// </summary>
    public class EmployeeDocumentStorage
    {
        public const string BasePath = @"\\AIBG.com\erpdocs\Employee Document";

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
        };

        public string EnsureEmployeeFolder(string employeeCode)
        {
            var folderName = SanitizeFolderName(employeeCode);
            if (string.IsNullOrWhiteSpace(folderName))
                throw new InvalidOperationException("Employee code from Employee Master is required to create the document folder.");

            try
            {
                if (!Directory.Exists(BasePath))
                    Directory.CreateDirectory(BasePath);

                var folder = Path.Combine(BasePath, folderName);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return folder;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Employee document folder is not available at " + BasePath + "\\" + folderName
                    + ". Create access is required on the share, then try again. " + ex.Message, ex);
            }
        }

        public string SaveUploadedFile(HttpPostedFile file, string employeeCode, string documentTypeName)
        {
            if (file == null || file.ContentLength <= 0)
                return null;

            var folder = EnsureEmployeeFolder(employeeCode);
            var ext = NormalizeExtension(file.FileName);
            var stem = SanitizeFileStem(documentTypeName);
            if (string.IsNullOrWhiteSpace(stem))
                stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(file.FileName));
            if (string.IsNullOrWhiteSpace(stem))
                stem = "Document";

            var fullPath = Path.Combine(folder, stem + ext);
            try
            {
                file.SaveAs(fullPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not save the document to " + fullPath + ". " + ex.Message, ex);
            }

            return fullPath;
        }

        public string SaveUploadedFile(Stream content, string originalFileName, string employeeCode, string documentTypeName)
        {
            if (content == null || content.Length <= 0)
                return null;

            var folder = EnsureEmployeeFolder(employeeCode);
            var ext = NormalizeExtension(originalFileName);
            var stem = SanitizeFileStem(documentTypeName);
            if (string.IsNullOrWhiteSpace(stem))
                stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(originalFileName ?? ""));
            if (string.IsNullOrWhiteSpace(stem))
                stem = "Document";

            var fullPath = Path.Combine(folder, stem + ext);
            try
            {
                if (content.CanSeek)
                    content.Position = 0;
                using (var fs = File.Create(fullPath))
                    content.CopyTo(fs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not save the document to " + fullPath + ". " + ex.Message, ex);
            }

            return fullPath;
        }

        public bool IsUnderEmployeeRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                var full = Path.GetFullPath(path);
                var root = Path.GetFullPath(BasePath);
                if (!root.EndsWith(Path.DirectorySeparatorChar.ToString())
                    && !root.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                    root += Path.DirectorySeparatorChar;
                return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string ViewUrl(int employeeDocumentId)
            => employeeDocumentId > 0 ? "/EmployeeDocumentFile.aspx?id=" + employeeDocumentId : "";

        public static string ContentTypeFor(string path)
        {
            var ext = Path.GetExtension(path ?? "").ToLowerInvariant();
            switch (ext)
            {
                case ".pdf": return "application/pdf";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                default: return "application/octet-stream";
            }
        }

        private static string NormalizeExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            foreach (var allowed in AllowedExtensions)
            {
                if (ext == allowed)
                    return ext;
            }
            return ".pdf";
        }

        private static string SanitizeFolderName(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return "";
            return Regex.Replace(employeeCode.Trim(), @"[^\w\-]", "");
        }

        private static string SanitizeFileStem(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var cleaned = Regex.Replace(name.Trim(), @"[^\w\- ]+", "");
            cleaned = Regex.Replace(cleaned, @"\s+", "_");
            return cleaned.Trim('_');
        }
    }
}
