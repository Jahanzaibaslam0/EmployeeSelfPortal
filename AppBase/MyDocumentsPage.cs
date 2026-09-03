using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class MyDocumentRow
    {
        public int EmployeeDocumentID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string DocumentTypeName { get; set; } = "";
        public string DocumentNumber { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
    }

    public class MyDocumentEmployeeOption
    {
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
    }

    /// <summary>
    /// My Documents logic (partial). Code-behind stub lives in MyDocuments.aspx.cs.
    /// Non-admin users see only their own documents; system administrators see all.
    /// </summary>
    public partial class MyDocumentsPage : AppBasePage
    {
        private readonly EmployeeProfileAccessService _profileAccess = new EmployeeProfileAccessService();

        public string PageTitle => "My Documents";
        public List<MyDocumentRow> Documents { get; private set; } = new List<MyDocumentRow>();
        public List<MyDocumentEmployeeOption> EmployeeOptions { get; private set; } = new List<MyDocumentEmployeeOption>();
        public bool IsAdminView { get; private set; }
        public string ScopeNote { get; private set; } = "";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "info";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;

            if (!_profileAccess.CanAccessMyDocuments())
            {
                SetAlert(EmployeeProfileAccessService.NotSynchronizedMessage, "warning");
                Response.Redirect("~/UserProfile.aspx");
                return;
            }

            // System administrators only: all documents. Everyone else: own linked employee only.
            IsAdminView = _profileAccess.CanViewAllEmployeeDocuments();
            ScopeNote = IsAdminView
                ? "Showing documents for all employees. Use filters to narrow the list."
                : "Showing only documents linked to your employee profile.";

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();

                    if (IsAdminView)
                    {
                        // Optional admin-only scope via query string; ignored for non-admins.
                        var filterEmployeeId = QueryInt("employeeId");
                        if (filterEmployeeId.HasValue && filterEmployeeId.Value > 0)
                            LoadEmployeeDocuments(conn, filterEmployeeId.Value);
                        else
                            LoadAllDocuments(conn);
                    }
                    else
                    {
                        // Never honor employeeId / editId from the URL for non-admin users.
                        var ownId = _profileAccess.GetLinkedEmployeeId();
                        if (!ownId.HasValue || ownId.Value <= 0)
                        {
                            SetAlert(EmployeeProfileAccessService.NotSynchronizedMessage, "warning");
                            Response.Redirect("~/UserProfile.aspx");
                            return;
                        }
                        LoadEmployeeDocuments(conn, ownId.Value);
                    }
                }

                // Defense in depth: drop any rows the caller should not see.
                EnforceDocumentVisibility();

                if (IsAdminView)
                {
                    EmployeeOptions = Documents
                        .Select(d => new MyDocumentEmployeeOption
                        {
                            EmployeeCode = d.EmployeeCode,
                            EmployeeName = d.EmployeeName
                        })
                        .GroupBy(e => e.EmployeeCode)
                        .Select(g => g.First())
                        .OrderBy(e => e.EmployeeCode)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                AlertMessage = "Error loading documents: " + ex.Message;
                AlertType = "error";
            }
        }

        private void EnforceDocumentVisibility()
        {
            if (IsAdminView) return;

            var ownId = _profileAccess.GetLinkedEmployeeId();
            if (!ownId.HasValue || ownId.Value <= 0)
            {
                Documents.Clear();
                return;
            }

            Documents = Documents
                .Where(d => d.EmployeeID == ownId.Value && _profileAccess.OwnsEmployee(d.EmployeeID))
                .ToList();
        }

        private void LoadAllDocuments(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(@"
SELECT d.EmployeeDocumentID, d.EmployeeID, e.EmployeeCode,
       ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
       ISNULL(dt.DocumentTypeName,'') AS DocumentTypeName,
       d.DocumentNumber, d.IssueDate, d.ExpiryDate,
       d.Remarks, d.DocumentPath, d.OriginalFileName, d.VerificationStatus
FROM tblEmployeeDocument d
INNER JOIN tblEmployee e ON e.EmployeeID = d.EmployeeID
LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
ORDER BY e.EmployeeCode, d.SortOrder, d.EmployeeDocumentID;", conn))
            {
                ReadDocuments(cmd);
            }
        }

        private void LoadEmployeeDocuments(SqlConnection conn, int employeeId)
        {
            using (var cmd = new SqlCommand(@"
SELECT d.EmployeeDocumentID, d.EmployeeID, e.EmployeeCode,
       ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
       ISNULL(dt.DocumentTypeName,'') AS DocumentTypeName,
       d.DocumentNumber, d.IssueDate, d.ExpiryDate,
       d.Remarks, d.DocumentPath, d.OriginalFileName, d.VerificationStatus
FROM tblEmployeeDocument d
INNER JOIN tblEmployee e ON e.EmployeeID = d.EmployeeID
LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
WHERE d.EmployeeID = @EmployeeID
ORDER BY d.SortOrder, d.EmployeeDocumentID;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                ReadDocuments(cmd);
            }
        }

        private void ReadDocuments(SqlCommand cmd)
        {
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Documents.Add(new MyDocumentRow
                    {
                        EmployeeDocumentID = Convert.ToInt32(dr["EmployeeDocumentID"]),
                        EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                        EmployeeCode = dr["EmployeeCode"]?.ToString() ?? "",
                        EmployeeName = (dr["EmployeeName"]?.ToString() ?? "").Trim(),
                        DocumentTypeName = dr["DocumentTypeName"]?.ToString() ?? "",
                        DocumentNumber = dr["DocumentNumber"] == DBNull.Value ? "" : dr["DocumentNumber"].ToString() ?? "",
                        IssueDate = dr["IssueDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
                        ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                        Remarks = dr["Remarks"] == DBNull.Value ? "" : dr["Remarks"].ToString() ?? "",
                        DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                        OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? "",
                        VerificationStatus = dr["VerificationStatus"] == DBNull.Value ? "Pending" : dr["VerificationStatus"].ToString() ?? "Pending"
                    });
                }
            }
        }

        public string DocumentHref(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("/", StringComparison.Ordinal))
                return ResolveUrl(path.StartsWith("~", StringComparison.Ordinal) ? path : "~" + path);
            return path;
        }

        public static string StatusBadgeClass(string status)
        {
            if (string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase))
                return "badge-verified";
            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
                return "badge-rejected";
            return "badge-pending";
        }
    }
}
