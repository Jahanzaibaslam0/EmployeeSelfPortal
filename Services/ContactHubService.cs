using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Web;

namespace HRMS.Services
{
    public class ContactHubFilter
    {
        public string View { get; set; } = "All";
        public string ContactType { get; set; } = "All";
        public string Verification { get; set; } = "All";
        public string AccessStatus { get; set; } = "All";
        public string RecordType { get; set; } = "All";
        public string Search { get; set; } = "";
        public string Position { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Sort { get; set; } = "code";
        public string Dir { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ContactHubRow
    {
        public int ContactID { get; set; }
        public string ContactCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string SearchName { get; set; } = "";
        public string ContactType { get; set; } = "";
        public string ContactStatus { get; set; } = "";
        public string LinkedMaster { get; set; } = "";
        public string AccountCode { get; set; } = "";
        public string EmployeeNumber { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Email { get; set; } = "";
        public string Whatsapp { get; set; } = "";
        public string Fax { get; set; } = "";
        public string URL { get; set; } = "";
        public string Position { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Department { get; set; } = "";
        public string RecordType { get; set; } = "People";
        public string VerificationStatus { get; set; } = "Verified";
        public string AccessStatus { get; set; } = "Open";
        public int CreatedByUserID { get; set; }
        public string OrgName { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string WorkflowStatus { get; set; } = "Approved";
    }

    public class ContactHubResult
    {
        public List<ContactHubRow> Items { get; set; } = new List<ContactHubRow>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public List<string> Positions { get; set; } = new List<string>();
        public List<string> Branches { get; set; } = new List<string>();
        public ContactHubFacets Facets { get; set; } = new ContactHubFacets();
    }

    public class ContactHubFacets
    {
        public int All { get; set; }
        public int Customers { get; set; }
        public int Vendors { get; set; }
        public int Employees { get; set; }
        public int ContactPersons { get; set; }
        public int NoBusinessLink { get; set; }
        public int ToVerify { get; set; }
        public int Restricted { get; set; }
        public int Quarantined { get; set; }
    }

    public class ContactHubService
    {
        public const string ViewAll = "All";
        public const string ViewMine = "Mine";
        public const string ViewCustomer = "Customer";
        public const string ViewVendor = "Vendor";
        public const string ViewEmployee = "Employee";
        public const string ViewNoBusinessLink = "NoBusinessLink";

        private readonly string _conn;

        public ContactHubService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            if (string.IsNullOrWhiteSpace(_conn)) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'CreatedByUserID') IS NULL ALTER TABLE dbo.tblContactMaster ADD CreatedByUserID INT NULL;");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'RecordType') IS NULL ALTER TABLE dbo.tblContactMaster ADD RecordType NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContact_RecordType DEFAULT N'People';");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'VerificationStatus') IS NULL ALTER TABLE dbo.tblContactMaster ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContact_Verify DEFAULT N'Verified';");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'AccessStatus') IS NULL ALTER TABLE dbo.tblContactMaster ADD AccessStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContact_Access DEFAULT N'Open';");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'WorkflowStatus') IS NULL ALTER TABLE dbo.tblContactMaster ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContact_Workflow DEFAULT N'Approved';");
            }
        }

        public ContactHubFilter FromRequest(HttpRequest request, string view)
        {
            var f = new ContactHubFilter { View = view ?? ViewAll };
            if (request == null) return f;
            f.ContactType = (request["ctype"] ?? "All").Trim();
            f.Verification = (request["verify"] ?? "All").Trim();
            f.AccessStatus = (request["access"] ?? "All").Trim();
            f.RecordType = (request["rtype"] ?? "All").Trim();
            f.Search = (request["q"] ?? "").Trim();
            f.Position = (request["position"] ?? "").Trim();
            f.Branch = (request["branch"] ?? "").Trim();
            f.Sort = (request["sort"] ?? "code").Trim().ToLowerInvariant();
            f.Dir = string.Equals(request["dir"], "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            int.TryParse(request["page"], out var page);
            f.Page = page < 1 ? 1 : page;
            return f;
        }

        public ContactHubResult Search(ContactHubFilter filter, int? currentUserId)
        {
            EnsureSchema();
            var result = new ContactHubResult { PageSize = filter.PageSize };
            if (string.IsNullOrWhiteSpace(_conn)) return result;

            var where = new StringBuilder(" WHERE 1=1 ");
            var args = new List<SqlParameter>();

            if (string.Equals(filter.View, ViewMine, StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND ISNULL(c.CreatedByUserID,0)=@MineUser ");
                args.Add(new SqlParameter("@MineUser", currentUserId ?? 0));
            }
            else if (string.Equals(filter.View, ViewCustomer, StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND c.ContactType=N'Customer' ");
            }
            else if (string.Equals(filter.View, ViewVendor, StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND c.ContactType=N'Vendor' ");
            }
            else if (string.Equals(filter.View, ViewEmployee, StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND c.ContactType=N'Employee' ");
            }
            else if (string.Equals(filter.View, ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND " + NoBusinessLinkSql("c") + " ");
            }

            if (!string.IsNullOrEmpty(filter.Position))
            {
                where.Append(" AND ISNULL(c.ProfessionalTitle,'')=@Position ");
                args.Add(new SqlParameter("@Position", filter.Position));
            }
            if (!string.IsNullOrEmpty(filter.Branch))
            {
                where.Append(" AND ISNULL(c.OfficeLocation,'')=@Branch ");
                args.Add(new SqlParameter("@Branch", filter.Branch));
            }
            if (!string.IsNullOrEmpty(filter.Search))
            {
                where.Append(@" AND (
                    c.Name LIKE @Q OR ISNULL(c.SearchName,'') LIKE @Q OR c.ContactCode LIKE @Q
                    OR ISNULL(c.Phone,'') LIKE @Q OR ISNULL(c.Mobile,'') LIKE @Q
                    OR ISNULL(c.Email,'') LIKE @Q OR ISNULL(c.Whatsapp,'') LIKE @Q OR ISNULL(c.Fax,'') LIKE @Q
                    OR ISNULL(cu.CustomerCode,'') LIKE @Q OR ISNULL(cu.Name,'') LIKE @Q
                    OR ISNULL(v.VendorCode,'') LIKE @Q OR ISNULL(v.Name,'') LIKE @Q
                    OR ISNULL(e.EmployeeCode,'') LIKE @Q
                    OR ISNULL(e.FirstName,'') LIKE @Q OR ISNULL(e.LastName,'') LIKE @Q
                ) ");
                args.Add(new SqlParameter("@Q", "%" + filter.Search + "%"));
            }

            var scopeWhere = where.ToString();

            if (!string.IsNullOrEmpty(filter.ContactType) && !filter.ContactType.Equals("All", StringComparison.OrdinalIgnoreCase)
                && !IsTypeLockedView(filter.View))
            {
                if (IsNoBusinessLink(filter.ContactType))
                    where.Append(" AND " + NoBusinessLinkSql("c") + " ");
                else if (filter.ContactType.Equals("ContactPersons", StringComparison.OrdinalIgnoreCase))
                    where.Append(" AND ISNULL(c.RecordType,N'People')=N'People' ");
                else if (filter.ContactType.Equals("Customers", StringComparison.OrdinalIgnoreCase)
                         || filter.ContactType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                    where.Append(" AND c.ContactType=N'Customer' ");
                else if (filter.ContactType.Equals("Vendors", StringComparison.OrdinalIgnoreCase)
                         || filter.ContactType.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
                    where.Append(" AND c.ContactType=N'Vendor' ");
                else if (filter.ContactType.Equals("Employees", StringComparison.OrdinalIgnoreCase)
                         || filter.ContactType.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                    where.Append(" AND c.ContactType=N'Employee' ");
            }

            if (filter.Verification.Equals("ToVerify", StringComparison.OrdinalIgnoreCase)
                || filter.Verification.Equals("To Verify", StringComparison.OrdinalIgnoreCase)
                || filter.Verification.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                where.Append(" AND ISNULL(c.VerificationStatus,N'Verified')=N'ToVerify' ");
            }

            if (filter.AccessStatus.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
                where.Append(" AND ISNULL(c.AccessStatus,N'Open')=N'Restricted' ");
            else if (filter.AccessStatus.Equals("Quarantined", StringComparison.OrdinalIgnoreCase))
                where.Append(" AND ISNULL(c.AccessStatus,N'Open')=N'Quarantined' ");

            if (filter.RecordType.Equals("People", StringComparison.OrdinalIgnoreCase))
                where.Append(" AND ISNULL(c.RecordType,N'People')=N'People' ");
            else if (filter.RecordType.Equals("Organisation", StringComparison.OrdinalIgnoreCase)
                     || filter.RecordType.Equals("Organization", StringComparison.OrdinalIgnoreCase))
                where.Append(" AND ISNULL(c.RecordType,N'People')=N'Organisation' ");

            var order = OrderSql(filter.Sort, filter.Dir);
            var fromSql = @"
FROM tblContactMaster c
LEFT JOIN tblCustomer cu ON cu.CustomerID=c.CustomerID
LEFT JOIN tblVendor v ON v.VendorID=c.VendorID
LEFT JOIN tblEmployee e ON e.EmployeeID=c.EmployeeID ";

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var countCmd = new SqlCommand("SELECT COUNT(*) " + fromSql + where, conn))
                {
                    foreach (var p in args) countCmd.Parameters.Add(Clone(p));
                    result.Total = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
                }

                using (var facetCmd = new SqlCommand(@"
SELECT
  COUNT(*),
  SUM(CASE WHEN c.ContactType=N'Customer' THEN 1 ELSE 0 END),
  SUM(CASE WHEN c.ContactType=N'Vendor' THEN 1 ELSE 0 END),
  SUM(CASE WHEN c.ContactType=N'Employee' THEN 1 ELSE 0 END),
  SUM(CASE WHEN ISNULL(c.RecordType,N'People')=N'People' THEN 1 ELSE 0 END),
  SUM(CASE WHEN " + NoBusinessLinkSql("c") + @" THEN 1 ELSE 0 END),
  SUM(CASE WHEN ISNULL(c.VerificationStatus,N'Verified')=N'ToVerify' THEN 1 ELSE 0 END),
  SUM(CASE WHEN ISNULL(c.AccessStatus,N'Open')=N'Restricted' THEN 1 ELSE 0 END),
  SUM(CASE WHEN ISNULL(c.AccessStatus,N'Open')=N'Quarantined' THEN 1 ELSE 0 END)
" + fromSql + scopeWhere, conn))
                {
                    foreach (var p in args) facetCmd.Parameters.Add(Clone(p));
                    using (var dr = facetCmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            result.Facets.All = dr.IsDBNull(0) ? 0 : Convert.ToInt32(dr.GetValue(0));
                            result.Facets.Customers = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr.GetValue(1));
                            result.Facets.Vendors = dr.IsDBNull(2) ? 0 : Convert.ToInt32(dr.GetValue(2));
                            result.Facets.Employees = dr.IsDBNull(3) ? 0 : Convert.ToInt32(dr.GetValue(3));
                            result.Facets.ContactPersons = dr.IsDBNull(4) ? 0 : Convert.ToInt32(dr.GetValue(4));
                            result.Facets.NoBusinessLink = dr.IsDBNull(5) ? 0 : Convert.ToInt32(dr.GetValue(5));
                            result.Facets.ToVerify = dr.IsDBNull(6) ? 0 : Convert.ToInt32(dr.GetValue(6));
                            result.Facets.Restricted = dr.IsDBNull(7) ? 0 : Convert.ToInt32(dr.GetValue(7));
                            result.Facets.Quarantined = dr.IsDBNull(8) ? 0 : Convert.ToInt32(dr.GetValue(8));
                        }
                    }
                }

                result.PageCount = Math.Max(1, (int)Math.Ceiling(result.Total / (double)filter.PageSize));
                result.Page = filter.Page > result.PageCount ? result.PageCount : filter.Page;
                var offset = (result.Page - 1) * filter.PageSize;

                var sql = @"
SELECT c.ContactID, c.ContactCode, c.Name, ISNULL(c.SearchName,''), c.ContactType, c.ContactStatus,
  CASE
    WHEN c.ContactType=N'Customer' THEN LTRIM(RTRIM(ISNULL(cu.CustomerCode,'') + N' ' + ISNULL(cu.Name,'')))
    WHEN c.ContactType=N'Vendor' THEN LTRIM(RTRIM(ISNULL(v.VendorCode,'') + N' ' + ISNULL(v.Name,'')))
    WHEN c.ContactType=N'Employee' THEN LTRIM(RTRIM(ISNULL(e.EmployeeCode,'') + N' ' + ISNULL(e.FirstName,'') + N' ' + ISNULL(e.LastName,'')))
    ELSE ISNULL(c.GuestName, c.Name)
  END,
  CASE
    WHEN c.ContactType=N'Customer' THEN ISNULL(cu.CustomerCode,'')
    WHEN c.ContactType=N'Vendor' THEN ISNULL(v.VendorCode,'')
    ELSE N''
  END,
  ISNULL(e.EmployeeCode,''),
  ISNULL(c.Phone,''), ISNULL(c.Mobile,''), ISNULL(c.Email,''), ISNULL(c.Whatsapp,''), ISNULL(c.Fax,''), ISNULL(c.URL,''),
  ISNULL(c.ProfessionalTitle,''), ISNULL(c.OfficeLocation,''), ISNULL(c.Department,''),
  ISNULL(c.RecordType,N'People'), ISNULL(c.VerificationStatus,N'Verified'), ISNULL(c.AccessStatus,N'Open'),
  ISNULL(c.CreatedByUserID,0),
  CASE
    WHEN c.ContactType=N'Customer' THEN ISNULL(cu.Name,'')
    WHEN c.ContactType=N'Vendor' THEN ISNULL(v.Name,'')
    ELSE N''
  END,
  ISNULL(c.WorkflowStatus,N'Approved')
" + fromSql + where + order + " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    foreach (var p in args) cmd.Parameters.Add(Clone(p));
                    cmd.Parameters.Add(new SqlParameter("@Skip", offset));
                    cmd.Parameters.Add(new SqlParameter("@Take", filter.PageSize));
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result.Items.Add(new ContactHubRow
                            {
                                ContactID = dr.GetInt32(0),
                                ContactCode = dr.GetString(1),
                                Name = dr.GetString(2),
                                SearchName = dr.GetString(3),
                                ContactType = dr.GetString(4),
                                ContactStatus = dr.GetString(5),
                                LinkedMaster = dr.IsDBNull(6) ? "" : dr.GetString(6),
                                AccountCode = dr.GetString(7),
                                EmployeeNumber = dr.GetString(8),
                                Phone = dr.GetString(9),
                                Mobile = dr.GetString(10),
                                Email = dr.GetString(11),
                                Whatsapp = dr.GetString(12),
                                Fax = dr.GetString(13),
                                URL = dr.GetString(14),
                                Position = dr.GetString(15),
                                Branch = dr.GetString(16),
                                Department = dr.GetString(17),
                                RecordType = dr.GetString(18),
                                VerificationStatus = dr.GetString(19),
                                AccessStatus = dr.GetString(20),
                                CreatedByUserID = dr.GetInt32(21),
                                OrgName = dr.GetString(22),
                                WorkflowStatus = dr.FieldCount > 23 && !dr.IsDBNull(23) ? dr.GetString(23) : "Approved"
                            });
                        }
                    }
                }

                result.Positions = Distinct(conn, "ProfessionalTitle", filter, currentUserId);
                result.Branches = Distinct(conn, "OfficeLocation", filter, currentUserId);
                FillAddresses(conn, result.Items);
            }

            return result;
        }

        public ContactHubRow GetById(int contactId, string view, int? currentUserId)
        {
            var filter = new ContactHubFilter { View = view, Search = "", Page = 1, PageSize = 1 };
            // Load directly
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT c.ContactID, c.ContactCode, c.Name, ISNULL(c.SearchName,''), c.ContactType, c.ContactStatus,
  ISNULL(c.Phone,''), ISNULL(c.Mobile,''), ISNULL(c.Email,''), ISNULL(c.Whatsapp,''), ISNULL(c.Fax,''), ISNULL(c.URL,''),
  ISNULL(c.ProfessionalTitle,''), ISNULL(c.OfficeLocation,''), ISNULL(c.Department,''),
  ISNULL(c.CreatedByUserID,0),
  CASE WHEN c.ContactType=N'Customer' THEN ISNULL(cu.Name,'') WHEN c.ContactType=N'Vendor' THEN ISNULL(v.Name,'') ELSE N'' END
FROM tblContactMaster c
LEFT JOIN tblCustomer cu ON cu.CustomerID=c.CustomerID
LEFT JOIN tblVendor v ON v.VendorID=c.VendorID
WHERE c.ContactID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                conn.Open();
                ContactHubRow row = null;
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    row = new ContactHubRow
                    {
                        ContactID = dr.GetInt32(0),
                        ContactCode = dr.GetString(1),
                        Name = dr.GetString(2),
                        SearchName = dr.GetString(3),
                        ContactType = dr.GetString(4),
                        ContactStatus = dr.GetString(5),
                        Phone = dr.GetString(6),
                        Mobile = dr.GetString(7),
                        Email = dr.GetString(8),
                        Whatsapp = dr.GetString(9),
                        Fax = dr.GetString(10),
                        URL = dr.GetString(11),
                        Position = dr.GetString(12),
                        Branch = dr.GetString(13),
                        Department = dr.GetString(14),
                        CreatedByUserID = dr.GetInt32(15),
                        OrgName = dr.GetString(16)
                    };
                }
                if (string.Equals(view, ViewMine, StringComparison.OrdinalIgnoreCase)
                    && currentUserId.GetValueOrDefault() != row.CreatedByUserID)
                    return null;
                if (string.Equals(view, ViewCustomer, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(row.ContactType, "Customer", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (string.Equals(view, ViewVendor, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(row.ContactType, "Vendor", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (string.Equals(view, ViewEmployee, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(row.ContactType, "Employee", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (string.Equals(view, ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase)
                    && !IsNoBusinessLink(row.ContactType))
                    return null;
                FillAddresses(conn, new List<ContactHubRow> { row });
                return row;
            }
        }

        public static string ToVCard(ContactHubRow c) => ToQrVCard(c);

        public static string ToQrVCard(ContactHubRow c)
        {
            if (c == null) return "";
            var company = FirstNonEmpty(c.OrgName, c.LinkedMaster);
            var address = FirstNonEmpty(c.AddressLine, c.Branch);
            var mobile = FirstNonEmpty(c.Mobile, c.Phone, c.Whatsapp);
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCARD");
            sb.AppendLine("VERSION:3.0");
            sb.AppendLine("UID:urn:hrms:contact:" + c.ContactID);
            sb.AppendLine("FN:" + EscapeVcf(c.Name));
            sb.AppendLine("N:" + EscapeVcf(c.Name) + ";;;;");
            if (!string.IsNullOrEmpty(c.ContactCode))
                sb.AppendLine("NICKNAME:" + EscapeVcf(c.ContactCode));
            if (!string.IsNullOrEmpty(company)) sb.AppendLine("ORG:" + EscapeVcf(company));
            if (!string.IsNullOrEmpty(c.Position)) sb.AppendLine("TITLE:" + EscapeVcf(c.Position));
            if (!string.IsNullOrEmpty(mobile)) sb.AppendLine("TEL;TYPE=CELL:" + EscapeVcf(mobile));
            if (!string.IsNullOrEmpty(c.Email)) sb.AppendLine("EMAIL:" + EscapeVcf(c.Email));
            if (!string.IsNullOrEmpty(address)) sb.AppendLine("ADR;TYPE=WORK:;;" + EscapeVcf(address) + ";;;;");
            sb.AppendLine("NOTE:" + EscapeVcf("HRMS " + c.ContactCode));
            sb.AppendLine("END:VCARD");
            return sb.ToString().Replace("\r\n", "\n");
        }

        public static string QrStamp(ContactHubRow c)
        {
            var payload = ToQrVCard(c);
            unchecked
            {
                uint hash = 2166136261;
                foreach (var ch in payload)
                {
                    hash ^= ch;
                    hash *= 16777619;
                }
                return hash.ToString("x8") + payload.Length.ToString();
            }
        }

        public static string QrPngUrl(int contactId, string stamp, int scale, string view = null)
            => "/ContactQr.aspx?id=" + contactId + "&handler=Png&scale=" + scale + "&v=" + (stamp ?? "") + ViewQuery(view);

        public static string QrViewUrl(int contactId, string view = null)
            => "/ContactQr.aspx?id=" + contactId + ViewQuery(view, true);

        public static string QrDownloadUrl(int contactId, string stamp, string view = null)
            => "/ContactQr.aspx?id=" + contactId + "&handler=Download&v=" + (stamp ?? "") + ViewQuery(view);

        private static string ViewQuery(string view, bool first = false)
        {
            if (string.IsNullOrWhiteSpace(view) || view.Equals("All", StringComparison.OrdinalIgnoreCase))
                return "";
            return (first ? "?" : "&") + "view=" + HttpUtility.UrlEncode(view.Trim());
        }

        public static byte[] RenderQrPng(ContactHubRow c, int modulePx = 4)
            => QrCodeEncoder.RenderPng(ToQrVCard(c), modulePx);

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null) return "";
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return "";
        }

        public static string ToPlainText(ContactHubRow c)
        {
            if (c == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine(c.Name);
            sb.AppendLine("Code: " + c.ContactCode);
            sb.AppendLine("Type: " + c.ContactType);
            if (!string.IsNullOrEmpty(c.LinkedMaster)) sb.AppendLine("Account: " + c.LinkedMaster);
            if (!string.IsNullOrEmpty(c.EmployeeNumber)) sb.AppendLine("Employee No: " + c.EmployeeNumber);
            if (!string.IsNullOrEmpty(c.Position)) sb.AppendLine("Position: " + c.Position);
            if (!string.IsNullOrEmpty(c.Branch)) sb.AppendLine("Branch: " + c.Branch);
            if (!string.IsNullOrEmpty(c.Phone)) sb.AppendLine("Phone: " + c.Phone);
            if (!string.IsNullOrEmpty(c.Mobile)) sb.AppendLine("Mobile: " + c.Mobile);
            if (!string.IsNullOrEmpty(c.Email)) sb.AppendLine("Email: " + c.Email);
            if (!string.IsNullOrEmpty(c.Whatsapp)) sb.AppendLine("WhatsApp: " + c.Whatsapp);
            if (!string.IsNullOrEmpty(c.OrgName)) sb.AppendLine("Company: " + c.OrgName);
            if (!string.IsNullOrEmpty(c.AddressLine)) sb.AppendLine("Address: " + c.AddressLine);
            else if (!string.IsNullOrEmpty(c.Branch)) sb.AppendLine("Address: " + c.Branch);
            return sb.ToString().Trim();
        }

        public static string Query(ContactHubFilter f, int page, string sort, string dir)
        {
            var q = HttpUtility.ParseQueryString("");
            if (!string.IsNullOrEmpty(f.ContactType) && f.ContactType != "All") q["ctype"] = f.ContactType;
            if (!string.IsNullOrEmpty(f.Verification) && f.Verification != "All") q["verify"] = f.Verification;
            if (!string.IsNullOrEmpty(f.AccessStatus) && f.AccessStatus != "All") q["access"] = f.AccessStatus;
            if (!string.IsNullOrEmpty(f.RecordType) && f.RecordType != "All") q["rtype"] = f.RecordType;
            if (!string.IsNullOrEmpty(f.Search)) q["q"] = f.Search;
            if (!string.IsNullOrEmpty(f.Position)) q["position"] = f.Position;
            if (!string.IsNullOrEmpty(f.Branch)) q["branch"] = f.Branch;
            q["sort"] = string.IsNullOrEmpty(sort) ? f.Sort : sort;
            q["dir"] = string.IsNullOrEmpty(dir) ? f.Dir : dir;
            q["page"] = page.ToString();
            return q.ToString();
        }

        private static string OrderSql(string sort, string dir)
        {
            var d = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            string col;
            switch ((sort ?? "").ToLowerInvariant())
            {
                case "name": col = "c.Name"; break;
                case "type": col = "c.ContactType"; break;
                case "status": col = "c.ContactStatus"; break;
                case "account": col = @"CASE
    WHEN c.ContactType=N'Customer' THEN ISNULL(cu.CustomerCode,'')
    WHEN c.ContactType=N'Vendor' THEN ISNULL(v.VendorCode,'')
    WHEN c.ContactType=N'Employee' THEN ISNULL(e.EmployeeCode,'')
    ELSE N'' END"; break;
                case "phone": col = "ISNULL(NULLIF(c.Mobile,''), c.Phone)"; break;
                default: col = "c.ContactCode"; break;
            }
            return " ORDER BY CASE WHEN c.ContactStatus=N'Active' THEN 0 ELSE 1 END, " + col + " " + d + " ";
        }

        private List<string> Distinct(SqlConnection conn, string column, ContactHubFilter filter, int? userId)
        {
            var list = new List<string>();
            var extra = "";
            if (string.Equals(filter.View, ViewMine, StringComparison.OrdinalIgnoreCase))
                extra = " AND ISNULL(CreatedByUserID,0)=" + (userId ?? 0);
            else if (string.Equals(filter.View, ViewCustomer, StringComparison.OrdinalIgnoreCase))
                extra = " AND ContactType=N'Customer'";
            else if (string.Equals(filter.View, ViewVendor, StringComparison.OrdinalIgnoreCase))
                extra = " AND ContactType=N'Vendor'";
            else if (string.Equals(filter.View, ViewEmployee, StringComparison.OrdinalIgnoreCase))
                extra = " AND ContactType=N'Employee'";
            else if (string.Equals(filter.View, ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase))
                extra = " AND " + NoBusinessLinkSql("");
            using (var cmd = new SqlCommand(
                "SELECT DISTINCT LTRIM(RTRIM(" + column + ")) FROM tblContactMaster WHERE ISNULL(" + column + ",'')<>''" + extra + " ORDER BY 1;", conn))
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                    list.Add(dr.GetString(0));
            }
            return list;
        }

        public static bool IsTypeLockedView(string view)
        {
            return string.Equals(view, ViewCustomer, StringComparison.OrdinalIgnoreCase)
                || string.Equals(view, ViewVendor, StringComparison.OrdinalIgnoreCase)
                || string.Equals(view, ViewEmployee, StringComparison.OrdinalIgnoreCase)
                || string.Equals(view, ViewNoBusinessLink, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNoBusinessLink(string contactType)
        {
            if (string.IsNullOrWhiteSpace(contactType)) return false;
            return contactType.Equals("NoBusinessLink", StringComparison.OrdinalIgnoreCase)
                || contactType.Equals("Guest/Others", StringComparison.OrdinalIgnoreCase)
                || contactType.Equals("Guest", StringComparison.OrdinalIgnoreCase)
                || contactType.Equals("Others", StringComparison.OrdinalIgnoreCase)
                || contactType.Equals("Prospect", StringComparison.OrdinalIgnoreCase)
                || contactType.Equals("Consultant", StringComparison.OrdinalIgnoreCase);
        }

        public static string NoBusinessLinkSql(string alias)
        {
            var a = string.IsNullOrEmpty(alias) ? "" : alias + ".";
            return a + "ContactType IN (N'Guest/Others', N'Guest', N'Others', N'Prospect', N'Consultant')";
        }

        private static void FillAddresses(SqlConnection conn, List<ContactHubRow> items)
        {
            if (items == null || items.Count == 0) return;
            try
            {
                using (var exists = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NULL THEN 0 ELSE 1 END;", conn))
                {
                    if (Convert.ToInt32(exists.ExecuteScalar() ?? 0) == 0) return;
                }
                var ids = new StringBuilder();
                var map = new Dictionary<int, ContactHubRow>();
                foreach (var item in items)
                {
                    if (item == null || item.ContactID <= 0 || map.ContainsKey(item.ContactID)) continue;
                    if (ids.Length > 0) ids.Append(',');
                    ids.Append(item.ContactID);
                    map[item.ContactID] = item;
                }
                if (map.Count == 0) return;
                using (var cmd = new SqlCommand(@"
SELECT a.ContactID,
       ISNULL(a.AddressLine,''),
       ISNULL(st.StreetName,''),
       ISNULL(p.PostalCode,''),
       ISNULL(ci.CityName,''),
       ISNULL(s.StateName,''),
       ISNULL(co.CountryName,''),
       ISNULL(a.IsPrimary,0),
       ISNULL(a.SortOrder,1)
FROM dbo.tblContactAddress a
LEFT JOIN dbo.tblContactStreet st ON st.StreetID=a.StreetID
LEFT JOIN dbo.tblContactPostal p ON p.PostalID=a.PostalID
LEFT JOIN dbo.tblContactCity ci ON ci.CityID=a.CityID
LEFT JOIN dbo.tblContactState s ON s.StateID=a.StateID
LEFT JOIN dbo.tblContactCountry co ON co.CountryID=a.CountryID
WHERE a.ContactID IN (" + ids + @")
ORDER BY a.ContactID, ISNULL(a.IsPrimary,0) DESC, ISNULL(a.SortOrder,1), a.AddressID;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    var seen = new HashSet<int>();
                    while (dr.Read())
                    {
                        var id = dr.GetInt32(0);
                        if (seen.Contains(id)) continue;
                        seen.Add(id);
                        ContactHubRow row;
                        if (!map.TryGetValue(id, out row)) continue;
                        row.AddressLine = JoinAddress(
                            dr.GetString(1), dr.GetString(2), dr.GetString(3),
                            dr.GetString(4), dr.GetString(5), dr.GetString(6));
                    }
                }
            }
            catch
            {
            }
        }

        private static string JoinAddress(params string[] parts)
        {
            var list = new List<string>();
            if (parts == null) return "";
            foreach (var p in parts)
            {
                var t = (p ?? "").Trim().Trim(',');
                if (t.Length == 0) continue;
                if (!list.Contains(t)) list.Add(t);
            }
            return string.Join(", ", list.ToArray());
        }

        private static SqlParameter Clone(SqlParameter p)
            => new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value);

        private static string EscapeVcf(string value)
            => (value ?? "").Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");

        private static void ExecIgnore(SqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
