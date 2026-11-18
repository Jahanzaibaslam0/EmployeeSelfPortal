using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class EmployeeDirectoryRow
    {
        public string Extension { get; set; } = "";
        public string CellNo { get; set; } = "";
        public string Email { get; set; } = "";
        public string PowerID { get; set; } = "";
        public string UserID { get; set; } = "";
        public string Title { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string PositionCode { get; set; } = "";
        public string Position { get; set; } = "";
        public string TitleGrade { get; set; } = "";
        public string Department { get; set; } = "";
        public string Division { get; set; } = "";
        public string Region { get; set; } = "";
        public string Team { get; set; } = "";
        public string Location { get; set; } = "";
        public string SalesGroup { get; set; } = "";
        public string State { get; set; } = "";
        public string BasedCity { get; set; } = "";
    }

    public class EmployeeReportPage : AppBasePage
    {
        public static readonly string[] DirectoryHeaders =
        {
            "Extension", "Cell No", "Email", "Power BI ID", "User ID", "Title",
            "Employee Code", "Position Code", "Position", "Title Grade",
            "Department", "Division", "Region", "Team", "Location",
            "Sales Group", "State", "Based City"
        };

        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public string PageTitle => "Internal Employee Directory";
        public List<EmployeeDirectoryRow> Records { get; private set; } = new List<EmployeeDirectoryRow>();
        public string Search { get; private set; } = "";
        public bool CanExport => Auth.IsAdmin || Perms.CanExport("EmployeeReport") || Perms.CanRead("EmployeeReport");

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            Search = (Request.QueryString["search"] ?? "").Trim();

            var handler = Request.QueryString["handler"] ?? Request.Form["__handler"] ?? "";
            if (string.Equals(handler, "DownloadExcel", StringComparison.OrdinalIgnoreCase)
                || string.Equals(handler, "Export", StringComparison.OrdinalIgnoreCase))
            {
                ExportExcel();
                return;
            }

            Records = LoadRecords(Search);
        }

        private void ExportExcel()
        {
            if (!CanExport)
            {
                Response.Redirect(ResolveUrl("~/EmployeeReport.aspx"));
                return;
            }

            var records = LoadRecords(Search);
            var rows = new List<IReadOnlyList<string>>(records.Count);
            foreach (var r in records)
            {
                rows.Add(new[]
                {
                    r.Extension, r.CellNo, r.Email, r.PowerID, r.UserID, r.Title,
                    r.EmployeeCode, r.PositionCode, r.Position, r.TitleGrade,
                    r.Department, r.Division, r.Region, r.Team, r.Location,
                    r.SalesGroup, r.State, r.BasedCity
                });
            }

            var file = ExcelUtility.ToSimpleFile(
                "Internal Employee Directory", DirectoryHeaders, rows, "InternalEmployeeDirectory");
            Response.Clear();
            Response.ContentType = file.ContentType;
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + file.FileName + "\"");
            Response.BinaryWrite(file.Content);
            Context.ApplicationInstance.CompleteRequest();
        }

        private List<EmployeeDirectoryRow> LoadRecords(string search)
        {
            var records = new List<EmployeeDirectoryRow>();
            var searchParam = string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%";
            var scope = _dataScope.GetEmployeeFilter("e");

            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
SELECT
    ISNULL(NULLIF(LTRIM(RTRIM(ext.ExtensionName)), ''), '') AS Extension,
    COALESCE(
        NULLIF(LTRIM(RTRIM(cCell.ContactValue)), ''),
        NULLIF(LTRIM(RTRIM(e.OfficialMobile)), ''),
        NULLIF(LTRIM(RTRIM(e.PersonalMobile)), ''),
        NULLIF(LTRIM(RTRIM(e.Phone)), '')
    ) AS CellNo,
    COALESCE(
        NULLIF(LTRIM(RTRIM(cEmail.ContactValue)), ''),
        NULLIF(LTRIM(RTRIM(e.OfficialEmail)), ''),
        NULLIF(LTRIM(RTRIM(e.Email)), '')
    ) AS Email,
    ISNULL(cPower.ContactValue, '') AS PowerID,
    ISNULL(NULLIF(LTRIM(RTRIM(u.Username)), ''), '') AS UserID,
    COALESCE(
        NULLIF(LTRIM(RTRIM(e.DisplayName)), ''),
        LTRIM(RTRIM(ISNULL(e.FirstName, '') + N' ' + ISNULL(e.LastName, '')))
    ) AS Title,
    e.EmployeeCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(pos.PositionNo)), ''),
        NULLIF(LTRIM(RTRIM(j.JobCode)), '')
    ) AS PositionCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(e.Designation)), ''),
        NULLIF(LTRIM(RTRIM(j.JobTitle)), ''),
        NULLIF(LTRIM(RTRIM(pos.Description)), '')
    ) AS Position,
    ISNULL(g.GradeName, '') AS TitleGrade,
    ISNULL(d.DepartmentName, '') AS Department,
    ISNULL(div.DivisionName, '') AS Division,
    ISNULL(r.RegionName, '') AS Region,
    ISNULL(st.SalesTeamName, '') AS Team,
    ISNULL(loc.LocationName, '') AS Location,
    ISNULL(sg.SalesGroupName, '') AS SalesGroup,
    ISNULL(p.ProvinceName, '') AS State,
    ISNULL(city.CityName, '') AS BasedCity
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblDivision div ON div.DivisionID = e.DivisionID
LEFT JOIN tblRegion r ON r.RegionID = e.RegionID
LEFT JOIN tblSalesTeam st ON st.SalesTeamID = e.SalesTeamID
LEFT JOIN tblLocation loc ON loc.LocationID = e.LocationID
LEFT JOIN tblSalesGroup sg ON sg.SalesGroupID = e.SalesGroupID
LEFT JOIN tblProvince p ON p.ProvinceID = e.ProvinceID
LEFT JOIN tblCity city ON city.CityID = e.CityID
LEFT JOIN tblGrade g ON g.GradeID = e.GradeID
LEFT JOIN tblJob j ON j.JobID = e.JobID
LEFT JOIN tblUser u ON u.UserID = e.UserID
LEFT JOIN tblExtension ext ON ext.ExtensionID = e.ExtensionID
OUTER APPLY (
    SELECT TOP 1 pwa.PositionNo, pwa.Description
    FROM tblPositionWorkerAssignment a
    INNER JOIN tblPosition pwa ON pwa.PositionID = a.PositionID
    WHERE a.EmployeeID = e.EmployeeID AND a.AssignmentEndDate IS NULL
    ORDER BY a.AssignmentStartDate DESC, a.PositionWorkerAssignmentID DESC
) pos
OUTER APPLY (
    SELECT TOP 1 ContactValue
    FROM tblEmployeeContact
    WHERE EmployeeID = e.EmployeeID
      AND ContactType IN (N'OfficialMobile', N'PersonalMobile', N'WhatsApp')
      AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
    ORDER BY
        CASE
            WHEN ContactType = N'OfficialMobile' AND IsPrimary = 1 THEN 0
            WHEN ContactType = N'OfficialMobile' THEN 1
            WHEN IsPrimary = 1 THEN 2
            WHEN ContactType = N'PersonalMobile' THEN 3
            ELSE 4
        END,
        ContactID DESC
) cCell
OUTER APPLY (
    SELECT TOP 1 ContactValue
    FROM tblEmployeeContact
    WHERE EmployeeID = e.EmployeeID AND ContactType = N'OfficialEmail'
      AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
    ORDER BY IsPrimary DESC, ContactID DESC
) cEmail
OUTER APPLY (
    SELECT TOP 1 ContactValue
    FROM tblEmployeeContact
    WHERE EmployeeID = e.EmployeeID
      AND ContactType IN (N'PowerBI ID', N'PowerID', N'Power BI ID', N'Power BI')
      AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
    ORDER BY IsPrimary DESC, ContactID DESC
) cPower
WHERE (@Search IS NULL OR
       e.EmployeeCode LIKE @Search OR
       e.FirstName LIKE @Search OR
       e.LastName LIKE @Search OR
       ISNULL(e.DisplayName, '') LIKE @Search OR
       ISNULL(e.Designation, '') LIKE @Search OR
       ISNULL(d.DepartmentName, '') LIKE @Search OR
       ISNULL(div.DivisionName, '') LIKE @Search OR
       ISNULL(r.RegionName, '') LIKE @Search OR
       ISNULL(st.SalesTeamName, '') LIKE @Search OR
       ISNULL(loc.LocationName, '') LIKE @Search OR
       ISNULL(sg.SalesGroupName, '') LIKE @Search OR
       ISNULL(p.ProvinceName, '') LIKE @Search OR
       ISNULL(city.CityName, '') LIKE @Search OR
       ISNULL(g.GradeName, '') LIKE @Search OR
       ISNULL(j.JobCode, '') LIKE @Search OR
       ISNULL(j.JobTitle, '') LIKE @Search OR
       ISNULL(pos.PositionNo, '') LIKE @Search OR
       ISNULL(u.UserCode, '') LIKE @Search OR
       ISNULL(u.Username, '') LIKE @Search OR
       ISNULL(cCell.ContactValue, '') LIKE @Search OR
       ISNULL(cEmail.ContactValue, '') LIKE @Search OR
       ISNULL(cPower.ContactValue, '') LIKE @Search OR
       ISNULL(ext.ExtensionName, '') LIKE @Search OR
       ISNULL(ext.ExtensionCode, '') LIKE @Search OR
       ISNULL(ext.AliasName, '') LIKE @Search)
  {scope.Sql}
ORDER BY e.EmployeeCode;", conn))
            {
                cmd.Parameters.AddWithValue("@Search", (object)searchParam ?? DBNull.Value);
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        records.Add(new EmployeeDirectoryRow
                        {
                            Extension = Val(dr, "Extension"),
                            CellNo = Val(dr, "CellNo"),
                            Email = Val(dr, "Email"),
                            PowerID = Val(dr, "PowerID"),
                            UserID = Val(dr, "UserID"),
                            Title = Val(dr, "Title"),
                            EmployeeCode = Val(dr, "EmployeeCode"),
                            PositionCode = Val(dr, "PositionCode"),
                            Position = Val(dr, "Position"),
                            TitleGrade = Val(dr, "TitleGrade"),
                            Department = Val(dr, "Department"),
                            Division = Val(dr, "Division"),
                            Region = Val(dr, "Region"),
                            Team = Val(dr, "Team"),
                            Location = Val(dr, "Location"),
                            SalesGroup = Val(dr, "SalesGroup"),
                            State = Val(dr, "State"),
                            BasedCity = Val(dr, "BasedCity")
                        });
                    }
                }
            }

            return records;
        }

        private static string Val(SqlDataReader dr, string col)
        {
            var o = dr[col];
            return o == null || o == DBNull.Value ? "" : o.ToString().Trim();
        }
    }
}
