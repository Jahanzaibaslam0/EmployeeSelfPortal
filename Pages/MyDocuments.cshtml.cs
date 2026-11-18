using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using HRMS.Services;

namespace HRMS.Pages;

public class MyDocumentRow
{
    public int EmployeeDocumentID { get; set; }
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

public class MyDocumentsModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly EmployeeProfileAccessService _profileAccess;

    public MyDocumentsModel(IConfiguration config, AuthService auth, EmployeeProfileAccessService profileAccess)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _profileAccess = profileAccess;
    }

    public string PageTitle => "My Documents";
    public List<MyDocumentRow> Documents { get; set; } = new();
    public bool IsAdminView { get; set; }

    public void OnGet()
    {
        IsAdminView = _profileAccess.HasFullEmployeeMasterAccess();

        if (!_profileAccess.CanAccessMyDocuments())
        {
            Documents.Clear();
            return;
        }

        using var conn = new SqlConnection(_conn);
        conn.Open();

        if (IsAdminView)
        {
            LoadAllDocuments(conn);
        }
        else
        {
            var empId = _profileAccess.GetLinkedEmployeeId();
            if (empId is > 0)
                LoadEmployeeDocuments(conn, empId.Value);
        }
    }

    private void LoadAllDocuments(SqlConnection conn)
    {
        using var cmd = new SqlCommand(@"
            SELECT d.EmployeeDocumentID, e.EmployeeCode, 
                   ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
                   ISNULL(dt.DocumentTypeName,'') AS DocumentTypeName,
                   d.DocumentNumber, d.IssueDate, d.ExpiryDate,
                   d.Remarks, d.DocumentPath, d.OriginalFileName, d.VerificationStatus
            FROM tblEmployeeDocument d
            INNER JOIN tblEmployee e ON e.EmployeeID = d.EmployeeID
            LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
            ORDER BY e.EmployeeCode, d.SortOrder, d.EmployeeDocumentID;", conn);

        ReadDocuments(cmd);
    }

    private void LoadEmployeeDocuments(SqlConnection conn, int employeeId)
    {
        using var cmd = new SqlCommand(@"
            SELECT d.EmployeeDocumentID, e.EmployeeCode,
                   ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
                   ISNULL(dt.DocumentTypeName,'') AS DocumentTypeName,
                   d.DocumentNumber, d.IssueDate, d.ExpiryDate,
                   d.Remarks, d.DocumentPath, d.OriginalFileName, d.VerificationStatus
            FROM tblEmployeeDocument d
            INNER JOIN tblEmployee e ON e.EmployeeID = d.EmployeeID
            LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
            WHERE d.EmployeeID = @EmployeeID
            ORDER BY d.SortOrder, d.EmployeeDocumentID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

        ReadDocuments(cmd);
    }

    private void ReadDocuments(SqlCommand cmd)
    {
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Documents.Add(new MyDocumentRow
            {
                EmployeeDocumentID = Convert.ToInt32(dr["EmployeeDocumentID"]),
                EmployeeCode = dr["EmployeeCode"]?.ToString() ?? "",
                EmployeeName = (dr["EmployeeName"]?.ToString() ?? "").Trim(),
                DocumentTypeName = dr["DocumentTypeName"]?.ToString() ?? "",
                DocumentNumber = dr["DocumentNumber"] == DBNull.Value ? "" : dr["DocumentNumber"].ToString()!,
                IssueDate = dr["IssueDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
                ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                Remarks = dr["Remarks"] == DBNull.Value ? "" : dr["Remarks"].ToString()!,
                DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString()!,
                OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString()!,
                VerificationStatus = dr["VerificationStatus"] == DBNull.Value ? "Pending" : dr["VerificationStatus"].ToString()!
            });
        }
    }
}
