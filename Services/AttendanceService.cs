using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HRMS.Services
{
    public class AttendanceFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string EmpCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Location { get; set; } = "";
        public string Division { get; set; } = "";
        public string Designation { get; set; } = "";
        public string Status { get; set; } = "";
        public string View { get; set; } = "daily";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AttendanceRow
    {
        public string EmpCode { get; set; } = "";
        public string BiometricId { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Designation { get; set; } = "";
        public string Department { get; set; } = "";
        public string Division { get; set; } = "";
        public string Region { get; set; } = "";
        public string Location { get; set; } = "";
        public string AttDate { get; set; } = "";
        public DateTime? AttDateValue { get; set; }
        public string InTime { get; set; } = "";
        public string OutTime { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusCode { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public bool MatchedEmployee { get; set; }
    }

    public class AttendanceAggregateRow
    {
        public string Key { get; set; } = "";
        public string EmpCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Designation { get; set; } = "";
        public string Department { get; set; } = "";
        public string Division { get; set; } = "";
        public string Location { get; set; } = "";
        public string AttDate { get; set; } = "";
        public int Total { get; set; }
        public int P { get; set; }
        public int A { get; set; }
        public int LT { get; set; }
        public int Z { get; set; }
        public int H { get; set; }
        public int G { get; set; }
        public int K { get; set; }
        public int E { get; set; }
        public int ER { get; set; }
        public int B { get; set; }
        public int F { get; set; }
        public decimal AttPercent { get; set; }
    }

    public class AttendanceStatusCount
    {
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
        public string CssClass { get; set; } = "";
        public int Count { get; set; }
    }

    public class AttendanceQueryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string View { get; set; } = "daily";
        public int TotalCount { get; set; }
        public int DistinctEmployees { get; set; }
        public int MatchedRows { get; set; }
        public int DaySpan { get; set; }
        public decimal PresentPercent { get; set; }
        public string DateRangeLabel { get; set; } = "";
        public List<AttendanceRow> DetailRows { get; set; } = new List<AttendanceRow>();
        public List<AttendanceAggregateRow> AggregateRows { get; set; } = new List<AttendanceAggregateRow>();
        public List<AttendanceStatusCount> StatusBreakdown { get; set; } = new List<AttendanceStatusCount>();
        public List<string> DesignationOptions { get; set; } = new List<string>();
        public List<string> StatusOptions { get; set; } = new List<string>();
        public string MappedInTimeColumn { get; set; } = "";
        public string MappedOutTimeColumn { get; set; } = "";
        public string MappedStatusColumn { get; set; } = "";
    }

    /// <summary>
    /// Read-only access to external attendance DB, enriched with HRMS EmployeeMaster via BiometricID.
    /// </summary>
    public class AttendanceService
    {
        public const string FormKey = "EmployeeAttendance";

        private readonly string _attConn;
        private readonly string _hrmsConn;
        private readonly string _tableName;
        private readonly int _defaultPageSize;

        private static readonly string[] EmpCodeAliases =
        {
            "emp_code", "empcode", "employee_code", "employeecode", "personnel_number", "personnelnumber",
            "badge_no", "badgeno", "biometric_id", "biometricid", "bio_id", "bioid", "card_no", "cardno",
            "enroll_no", "enrollno", "userid", "user_id", "emp_id", "empid"
        };
        private static readonly string[] NameAliases =
        {
            "employee_name", "employeename", "emp_name", "empname", "name", "full_name", "fullname"
        };
        private static readonly string[] DesignationAliases =
        {
            "designation", "desig", "job_title", "jobtitle", "position"
        };
        private static readonly string[] DepartmentAliases =
        {
            "department", "dept", "dept_name", "department_name"
        };
        private static readonly string[] DivisionAliases =
        {
            "division", "div", "division_name"
        };
        private static readonly string[] LocationAliases =
        {
            "location", "loc", "office", "branch", "location_name", "work_location"
        };
        private static readonly string[] DateAliases =
        {
            "att_date", "attdate", "attendance_date", "attendancedate", "date", "punch_date", "punchdate",
            "work_date", "workdate", "duty_date", "dutydate", "shift_date", "shiftdate"
        };
        private static readonly string[] InTimeAliases =
        {
            "in_time", "intime", "time_in", "timein", "check_in", "checkin", "punch_in", "punchin",
            "first_in", "firstin", "first_punch", "firstpunch", "arrival", "arrival_time", "arrivaltime",
            "cin", "in1", "timein1", "start_time", "starttime", "entry_time", "entrytime", "checkintime",
            "in_punch", "inpunch", "clock_in", "clockin"
        };
        private static readonly string[] OutTimeAliases =
        {
            "out_time", "outtime", "time_out", "timeout", "check_out", "checkout", "punch_out", "punchout",
            "last_out", "lastout", "last_punch", "lastpunch", "departure", "departure_time", "departuretime",
            "cout", "out1", "timeout1", "end_time", "endtime", "exit_time", "exittime", "checkouttime",
            "out_punch", "outpunch", "clock_out", "clockout"
        };
        private static readonly string[] StatusAliases =
        {
            "status", "att_status", "attstatus", "attendance_status", "attendancestatus", "day_status",
            "daystatus", "present_status", "presentstatus", "status_code", "statuscode", "att_code",
            "attcode", "code", "result", "state", "remarks", "attendance", "day_type", "daytype"
        };

        public static readonly AttendanceStatusCount[] StatusCatalog =
        {
            new AttendanceStatusCount { Code = "P", Label = "Present", CssClass = "st-p" },
            new AttendanceStatusCount { Code = "A", Label = "Absent", CssClass = "st-a" },
            new AttendanceStatusCount { Code = "Z", Label = "Leave", CssClass = "st-z" },
            new AttendanceStatusCount { Code = "H", Label = "Holiday", CssClass = "st-h" },
            new AttendanceStatusCount { Code = "G", Label = "Gazetted Holiday", CssClass = "st-g" },
            new AttendanceStatusCount { Code = "LT", Label = "Late", CssClass = "st-lt" },
            new AttendanceStatusCount { Code = "K", Label = "Late Half Day", CssClass = "st-k" },
            new AttendanceStatusCount { Code = "E", Label = "Early Half Day", CssClass = "st-e" },
            new AttendanceStatusCount { Code = "B", Label = "Late Absent", CssClass = "st-b" },
            new AttendanceStatusCount { Code = "F", Label = "Early Absent", CssClass = "st-f" },
            new AttendanceStatusCount { Code = "ER", Label = "Early", CssClass = "st-er" }
        };

        public AttendanceService()
        {
            _attConn = ConfigurationManager.ConnectionStrings["AttendanceConnection"]?.ConnectionString ?? "";
            _hrmsConn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
            _tableName = ConfigurationManager.AppSettings["Attendance:TableName"] ?? "attendance_dashboard";
            if (!int.TryParse(ConfigurationManager.AppSettings["Attendance:PageSize"], out _defaultPageSize) || _defaultPageSize <= 0)
                _defaultPageSize = 50;
        }

        public AttendanceQueryResult Query(AttendanceFilter filter)
        {
            var result = new AttendanceQueryResult();
            filter = filter ?? new AttendanceFilter();
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = _defaultPageSize;
            result.View = string.IsNullOrWhiteSpace(filter.View) ? "daily" : filter.View.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(_attConn))
            {
                result.ErrorMessage = "Attendance database connection is not configured. Contact the system administrator.";
                return result;
            }

            try
            {
                List<AttendanceRow> raw;
                ColumnMap map;
                var biometricKeys = ResolveBiometricKeysForFilter(filter);
                using (var conn = new SqlConnection(_attConn))
                {
                    conn.Open();
                    map = ResolveColumnMap(conn);
                    if (map == null)
                    {
                        result.ErrorMessage = "Could not read table [" + _tableName + "] from the attendance database.";
                        return result;
                    }

                    result.MappedInTimeColumn = map.InTime ?? "(not found)";
                    result.MappedOutTimeColumn = map.OutTime ?? "(not found)";
                    result.MappedStatusColumn = map.Status ?? "(not found)";

                    raw = LoadAttendanceRows(conn, map, filter, biometricKeys, maxRows: 20000);
                }

                EnrichFromEmployeeMaster(raw);
                raw = ApplyEmployeeMasterFilters(raw, filter);

                result.DesignationOptions = raw
                    .Select(r => r.Designation)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                result.StatusOptions = StatusCatalog.Select(s => s.Code + " - " + s.Label).ToList();

                if (!string.IsNullOrWhiteSpace(filter.Status)
                    && !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var code = NormalizeStatusCode(filter.Status);
                    raw = raw.Where(r => r.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                BuildKpis(result, raw, filter);
                result.StatusBreakdown = BuildStatusBreakdown(raw);

                switch (result.View)
                {
                    case "monthly":
                        result.AggregateRows = AggregateByEmployee(raw);
                        break;
                    case "late":
                        result.DetailRows = raw.Where(r => IsLateStatus(r.StatusCode)).ToList();
                        break;
                    case "early":
                        result.DetailRows = raw.Where(r => IsEarlyStatus(r.StatusCode)).ToList();
                        break;
                    case "absent":
                        result.DetailRows = raw.Where(r => IsAbsentStatus(r.StatusCode)).ToList();
                        break;
                    case "leave":
                        result.DetailRows = raw.Where(r => r.StatusCode == "Z").ToList();
                        break;
                    case "detail":
                        result.DetailRows = raw;
                        break;
                    default: // daily
                        result.AggregateRows = AggregateByDate(raw);
                        break;
                }

                if (result.View == "daily" || result.View == "monthly")
                {
                    result.TotalCount = result.AggregateRows.Count;
                    result.AggregateRows = PageList(result.AggregateRows, filter.Page, filter.PageSize);
                }
                else
                {
                    result.TotalCount = result.DetailRows.Count;
                    result.DetailRows = PageList(result.DetailRows, filter.Page, filter.PageSize);
                }

                result.MatchedRows = raw.Count;
                result.Success = true;
            }
            catch (SqlException ex)
            {
                result.Success = false;
                result.ErrorMessage = FriendlySqlError(ex);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = "Unable to load attendance data: " + ex.Message;
            }

            return result;
        }

        public AttendanceQueryResult QueryForExport(AttendanceFilter filter)
        {
            filter = filter ?? new AttendanceFilter();
            filter.Page = 1;
            filter.PageSize = 20000;
            return Query(filter);
        }

        private List<string> ResolveBiometricKeysForFilter(AttendanceFilter filter)
        {
            var keys = new List<string>();
            if (string.IsNullOrWhiteSpace(filter.EmpCode) || string.IsNullOrWhiteSpace(_hrmsConn))
                return keys;

            var code = filter.EmpCode.Trim();
            try
            {
                using (var conn = new SqlConnection(_hrmsConn))
                {
                    conn.Open();
                    if (!ColumnExists(conn, "tblEmployee", "BiometricID")) return keys;
                    using (var cmd = new SqlCommand(@"
SELECT DISTINCT CAST(BiometricID AS NVARCHAR(50))
FROM tblEmployee
WHERE BiometricID IS NOT NULL
  AND (
        EmployeeCode LIKE @Q
     OR CAST(BiometricID AS NVARCHAR(50)) LIKE @Q
  );", conn))
                    {
                        cmd.Parameters.AddWithValue("@Q", "%" + code + "%");
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                if (!dr.IsDBNull(0))
                                    keys.Add(dr.GetString(0).Trim());
                            }
                        }
                    }
                }
            }
            catch { /* ignore */ }

            if (keys.Count == 0)
                keys.Add(code);
            return keys.Distinct(StringComparer.OrdinalIgnoreCase).Take(200).ToList();
        }

        private List<AttendanceRow> LoadAttendanceRows(SqlConnection conn, ColumnMap map, AttendanceFilter filter, List<string> biometricKeys, int maxRows)
        {
            var where = new StringBuilder(" WHERE 1=1 ");
            var cmd = new SqlCommand { Connection = conn };
            AppendAttendanceFilters(where, cmd, map, filter, biometricKeys);

            var sql = new StringBuilder();
            sql.Append("SELECT TOP (").Append(maxRows).Append(") ");
            sql.Append(SelectExpr(map.EmpCode, "EmpCode")).Append(", ");
            sql.Append(SelectExpr(map.EmployeeName, "EmployeeName")).Append(", ");
            sql.Append(SelectExpr(map.Designation, "Designation")).Append(", ");
            sql.Append(SelectExpr(map.Department, "Department")).Append(", ");
            sql.Append(SelectExpr(map.Division, "Division")).Append(", ");
            sql.Append(SelectExpr(map.Location, "Location")).Append(", ");
            sql.Append(SelectExpr(map.AttDate, "AttDate")).Append(", ");
            sql.Append(SelectExpr(map.InTime, "InTime")).Append(", ");
            sql.Append(SelectExpr(map.OutTime, "OutTime")).Append(", ");
            sql.Append(SelectExpr(map.Status, "Status"));
            sql.Append(" FROM [").Append(SanitizeIdent(_tableName)).Append("] a ");
            sql.Append(where);
            sql.Append(" ORDER BY ");
            if (!string.IsNullOrEmpty(map.AttDate))
                sql.Append("a.[").Append(SanitizeIdent(map.AttDate)).Append("] DESC, ");
            if (!string.IsNullOrEmpty(map.EmpCode))
                sql.Append("a.[").Append(SanitizeIdent(map.EmpCode)).Append("] ASC");
            else
                sql.Append("(SELECT NULL)");

            cmd.CommandText = sql.ToString();
            var list = new List<AttendanceRow>();
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var row = new AttendanceRow
                    {
                        EmpCode = Str(dr, "EmpCode"),
                        BiometricId = Str(dr, "EmpCode"),
                        EmployeeName = Str(dr, "EmployeeName"),
                        Designation = Str(dr, "Designation"),
                        Department = Str(dr, "Department"),
                        Division = Str(dr, "Division"),
                        Location = Str(dr, "Location"),
                        AttDate = FormatDate(dr["AttDate"]),
                        AttDateValue = ParseDateValue(dr["AttDate"]),
                        InTime = FormatTime(dr["InTime"]),
                        OutTime = FormatTime(dr["OutTime"]),
                        Status = Str(dr, "Status")
                    };
                    ApplyStatus(row);
                    list.Add(row);
                }
            }
            return list;
        }

        private void EnrichFromEmployeeMaster(List<AttendanceRow> rows)
        {
            if (rows == null || rows.Count == 0 || string.IsNullOrWhiteSpace(_hrmsConn))
                return;

            var bioKeys = rows
                .Select(r => (r.BiometricId ?? "").Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (bioKeys.Count == 0) return;

            var map = new Dictionary<string, HrmsEmployeeInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var conn = new SqlConnection(_hrmsConn))
                {
                    conn.Open();
                    var hasBio = ColumnExists(conn, "tblEmployee", "BiometricID");
                    if (!hasBio) return;

                    // Load active employees with biometric IDs (bounded).
                    using (var cmd = new SqlCommand(@"
SELECT TOP 20000
    e.EmployeeCode,
    CAST(e.BiometricID AS NVARCHAR(50)) AS BiometricID,
    LTRIM(RTRIM(ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,''))) AS EmployeeName,
    ISNULL(e.Designation,''),
    ISNULL(d.DepartmentName,''),
    ISNULL(dv.DivisionName,''),
    ISNULL(r.RegionName,''),
    ISNULL(l.LocationName,'')
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblDivision dv ON dv.DivisionID = e.DivisionID
LEFT JOIN tblRegion r ON r.RegionID = e.RegionID
LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
WHERE e.BiometricID IS NOT NULL;", conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var info = new HrmsEmployeeInfo
                            {
                                EmployeeCode = dr.IsDBNull(0) ? "" : dr.GetString(0),
                                BiometricId = dr.IsDBNull(1) ? "" : dr.GetString(1).Trim(),
                                EmployeeName = dr.IsDBNull(2) ? "" : dr.GetString(2).Trim(),
                                Designation = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                Department = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                Division = dr.IsDBNull(5) ? "" : dr.GetString(5),
                                Region = dr.IsDBNull(6) ? "" : dr.GetString(6),
                                Location = dr.IsDBNull(7) ? "" : dr.GetString(7)
                            };
                            if (string.IsNullOrWhiteSpace(info.BiometricId)) continue;
                            map[info.BiometricId] = info;
                            // Also index trimmed numeric forms (e.g. 02336 -> 2336)
                            if (int.TryParse(info.BiometricId, out var n))
                                map[n.ToString(CultureInfo.InvariantCulture)] = info;
                        }
                    }
                }
            }
            catch
            {
                // Graceful: attendance still shows without HRMS enrichment.
                return;
            }

            foreach (var row in rows)
            {
                var key = (row.BiometricId ?? "").Trim();
                if (string.IsNullOrEmpty(key)) continue;

                HrmsEmployeeInfo info = null;
                if (!map.TryGetValue(key, out info) && int.TryParse(key, out var n))
                    map.TryGetValue(n.ToString(CultureInfo.InvariantCulture), out info);

                if (info == null) continue;

                row.MatchedEmployee = true;
                row.EmpCode = string.IsNullOrWhiteSpace(info.EmployeeCode) ? row.EmpCode : info.EmployeeCode;
                row.BiometricId = info.BiometricId;
                if (!string.IsNullOrWhiteSpace(info.EmployeeName)) row.EmployeeName = info.EmployeeName;
                if (!string.IsNullOrWhiteSpace(info.Designation)) row.Designation = info.Designation;
                if (!string.IsNullOrWhiteSpace(info.Department)) row.Department = info.Department;
                if (!string.IsNullOrWhiteSpace(info.Division)) row.Division = info.Division;
                if (!string.IsNullOrWhiteSpace(info.Region)) row.Region = info.Region;
                if (!string.IsNullOrWhiteSpace(info.Location)) row.Location = info.Location;
            }
        }

        private static List<AttendanceRow> ApplyEmployeeMasterFilters(List<AttendanceRow> rows, AttendanceFilter filter)
        {
            IEnumerable<AttendanceRow> q = rows;
            if (!string.IsNullOrWhiteSpace(filter.EmpCode))
            {
                var code = filter.EmpCode.Trim();
                q = q.Where(r =>
                    (!string.IsNullOrEmpty(r.EmpCode) && r.EmpCode.IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(r.BiometricId) && r.BiometricId.IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            if (!string.IsNullOrWhiteSpace(filter.EmployeeName))
            {
                var name = filter.EmployeeName.Trim();
                q = q.Where(r => !string.IsNullOrEmpty(r.EmployeeName)
                                 && r.EmployeeName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                var v = filter.Department.Trim();
                q = q.Where(r => !string.IsNullOrEmpty(r.Department)
                                 && r.Department.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var v = filter.Location.Trim();
                q = q.Where(r => !string.IsNullOrEmpty(r.Location)
                                 && r.Location.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.Division))
            {
                var v = filter.Division.Trim();
                q = q.Where(r => !string.IsNullOrEmpty(r.Division)
                                 && r.Division.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.Designation)
                && !filter.Designation.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var v = filter.Designation.Trim();
                q = q.Where(r => string.Equals(r.Designation, v, StringComparison.OrdinalIgnoreCase));
            }
            return q.ToList();
        }

        private static void BuildKpis(AttendanceQueryResult result, List<AttendanceRow> rows, AttendanceFilter filter)
        {
            result.MatchedRows = rows.Count;
            result.DistinctEmployees = rows
                .Select(r => string.IsNullOrWhiteSpace(r.EmpCode) ? r.BiometricId : r.EmpCode)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var start = filter.StartDate ?? rows.Where(r => r.AttDateValue.HasValue).Select(r => r.AttDateValue.Value).DefaultIfEmpty(DateTime.Today).Min();
            var end = filter.EndDate ?? rows.Where(r => r.AttDateValue.HasValue).Select(r => r.AttDateValue.Value).DefaultIfEmpty(DateTime.Today).Max();
            if (end < start) end = start;
            result.DaySpan = (int)(end.Date - start.Date).TotalDays + 1;
            result.DateRangeLabel = start.ToString("dd MMM") + " — " + end.ToString("dd MMM yyyy");

            var presentish = rows.Count(r => r.StatusCode == "P" || r.StatusCode == "LT" || r.StatusCode == "K"
                                             || r.StatusCode == "E" || r.StatusCode == "ER");
            result.PresentPercent = rows.Count == 0 ? 0m
                : Math.Round((decimal)rows.Count(r => r.StatusCode == "P") * 100m / rows.Count, 1);
        }

        private static List<AttendanceStatusCount> BuildStatusBreakdown(List<AttendanceRow> rows)
        {
            var counts = rows.GroupBy(r => r.StatusCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            return StatusCatalog.Select(s => new AttendanceStatusCount
            {
                Code = s.Code,
                Label = s.Label,
                CssClass = s.CssClass,
                Count = counts.TryGetValue(s.Code, out var c) ? c : 0
            }).ToList();
        }

        private static List<AttendanceAggregateRow> AggregateByDate(List<AttendanceRow> rows)
        {
            return rows
                .GroupBy(r => r.AttDate)
                .OrderBy(g => g.Key)
                .Select(g => BuildAggregate(g.Key, g.ToList(), byEmployee: false))
                .ToList();
        }

        private static List<AttendanceAggregateRow> AggregateByEmployee(List<AttendanceRow> rows)
        {
            return rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.EmpCode) ? r.BiometricId : r.EmpCode, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key)
                .Select(g => BuildAggregate(g.Key, g.ToList(), byEmployee: true))
                .ToList();
        }

        private static AttendanceAggregateRow BuildAggregate(string key, List<AttendanceRow> rows, bool byEmployee)
        {
            var first = rows.FirstOrDefault() ?? new AttendanceRow();
            var agg = new AttendanceAggregateRow
            {
                Key = key,
                EmpCode = first.EmpCode,
                EmployeeName = first.EmployeeName,
                Designation = first.Designation,
                Department = first.Department,
                Division = first.Division,
                Location = first.Location,
                AttDate = byEmployee ? "" : key,
                Total = rows.Count,
                P = rows.Count(r => r.StatusCode == "P"),
                A = rows.Count(r => r.StatusCode == "A"),
                LT = rows.Count(r => r.StatusCode == "LT"),
                Z = rows.Count(r => r.StatusCode == "Z"),
                H = rows.Count(r => r.StatusCode == "H"),
                G = rows.Count(r => r.StatusCode == "G"),
                K = rows.Count(r => r.StatusCode == "K"),
                E = rows.Count(r => r.StatusCode == "E"),
                ER = rows.Count(r => r.StatusCode == "ER"),
                B = rows.Count(r => r.StatusCode == "B"),
                F = rows.Count(r => r.StatusCode == "F")
            };
            agg.AttPercent = agg.Total == 0 ? 0m : Math.Round((decimal)agg.P * 100m / agg.Total, 1);
            return agg;
        }

        private static void ApplyStatus(AttendanceRow row)
        {
            var code = NormalizeStatusCode(row.Status);
            if (string.IsNullOrEmpty(code))
            {
                var hasIn = !string.IsNullOrWhiteSpace(row.InTime) && row.InTime != "—";
                code = hasIn ? "P" : "A";
            }
            row.StatusCode = code;
            row.StatusLabel = StatusCatalog.FirstOrDefault(s => s.Code == code)?.Label ?? code;
            row.Status = string.IsNullOrWhiteSpace(row.Status) ? code : row.Status.Trim();
        }

        public static string NormalizeStatusCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var s = raw.Trim().ToUpperInvariant();
            // "P - Present" style
            var dash = s.IndexOf('-');
            if (dash > 0 && dash <= 3) s = s.Substring(0, dash).Trim();

            if (s == "P" || s == "PR" || (s.Contains("PRESENT") && !s.Contains("ABSENT"))) return "P";
            if (s == "A" || s == "AB" || (s.Contains("ABSENT") && !s.Contains("LATE") && !s.Contains("EARLY"))) return "A";
            if (s == "Z" || s == "L" || s == "LV" || s.Contains("LEAVE")) return "Z";
            if (s == "H" || s == "HO" || (s.Contains("HOLIDAY") && !s.Contains("GAZETT"))) return "H";
            if (s == "G" || s.Contains("GAZETT")) return "G";
            if (s == "LT" || s == "LATE" || (s.Contains("LATE") && !s.Contains("HALF") && !s.Contains("ABSENT"))) return "LT";
            if (s == "K" || s.Contains("LATE HALF")) return "K";
            if (s == "E" || s.Contains("EARLY HALF")) return "E";
            if (s == "B" || s.Contains("LATE ABSENT")) return "B";
            if (s == "F" || s.Contains("EARLY ABSENT")) return "F";
            if (s == "ER" || (s.Contains("EARLY") && !s.Contains("HALF") && !s.Contains("ABSENT"))) return "ER";

            // Exact short codes
            foreach (var c in StatusCatalog)
                if (s == c.Code) return c.Code;

            return s.Length <= 3 ? s : "";
        }

        private static bool IsLateStatus(string code)
            => code == "LT" || code == "K" || code == "B";

        private static bool IsEarlyStatus(string code)
            => code == "ER" || code == "E" || code == "F";

        private static bool IsAbsentStatus(string code)
            => code == "A" || code == "B" || code == "F";

        private static List<T> PageList<T>(List<T> source, int page, int pageSize)
        {
            if (source == null || source.Count == 0) return new List<T>();
            var skip = (page - 1) * pageSize;
            if (skip >= source.Count) skip = Math.Max(0, ((source.Count - 1) / pageSize) * pageSize);
            return source.Skip(skip).Take(pageSize).ToList();
        }

        private void AppendAttendanceFilters(StringBuilder where, SqlCommand cmd, ColumnMap map, AttendanceFilter filter, List<string> biometricKeys)
        {
            if (filter.StartDate.HasValue && map.AttDate != null)
            {
                where.Append(" AND CAST(a.[").Append(SanitizeIdent(map.AttDate)).Append("] AS DATE) >= @StartDate");
                cmd.Parameters.AddWithValue("@StartDate", filter.StartDate.Value.Date);
            }
            if (filter.EndDate.HasValue && map.AttDate != null)
            {
                where.Append(" AND CAST(a.[").Append(SanitizeIdent(map.AttDate)).Append("] AS DATE) <= @EndDate");
                cmd.Parameters.AddWithValue("@EndDate", filter.EndDate.Value.Date);
            }
            if (map.EmpCode != null && biometricKeys != null && biometricKeys.Count > 0)
            {
                where.Append(" AND (");
                for (var i = 0; i < biometricKeys.Count; i++)
                {
                    if (i > 0) where.Append(" OR ");
                    var p = "@Bio" + i;
                    where.Append("CAST(a.[").Append(SanitizeIdent(map.EmpCode)).Append("] AS NVARCHAR(100)) = ").Append(p);
                    where.Append(" OR CAST(a.[").Append(SanitizeIdent(map.EmpCode)).Append("] AS NVARCHAR(100)) LIKE ").Append(p).Append("Like");
                    cmd.Parameters.AddWithValue(p, biometricKeys[i]);
                    cmd.Parameters.AddWithValue(p + "Like", "%" + biometricKeys[i] + "%");
                }
                where.Append(")");
            }
        }

        private class HrmsEmployeeInfo
        {
            public string EmployeeCode;
            public string BiometricId;
            public string EmployeeName;
            public string Designation;
            public string Department;
            public string Division;
            public string Region;
            public string Location;
        }

        private class ColumnMap
        {
            public string EmpCode;
            public string EmployeeName;
            public string Designation;
            public string Department;
            public string Division;
            public string Location;
            public string AttDate;
            public string InTime;
            public string OutTime;
            public string Status;
        }

        private ColumnMap ResolveColumnMap(SqlConnection conn)
        {
            var cols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(@"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = @Table
ORDER BY ORDINAL_POSITION;", conn))
            {
                cmd.Parameters.AddWithValue("@Table", _tableName);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var name = dr.GetString(0);
                        cols[name] = name;
                    }
                }
            }
            if (cols.Count == 0) return null;

            var map = new ColumnMap
            {
                EmpCode = FindCol(cols, EmpCodeAliases),
                EmployeeName = FindCol(cols, NameAliases),
                Designation = FindCol(cols, DesignationAliases),
                Department = FindCol(cols, DepartmentAliases),
                Division = FindCol(cols, DivisionAliases),
                Location = FindCol(cols, LocationAliases),
                AttDate = FindCol(cols, DateAliases),
                InTime = FindCol(cols, InTimeAliases),
                OutTime = FindCol(cols, OutTimeAliases),
                Status = FindCol(cols, StatusAliases)
            };

            // Heuristic fallback for time/status columns with uncommon names
            if (map.InTime == null)
                map.InTime = FindColContains(cols, new[] { "intime", "timein", "checkin", "punchin", "firstin", "arrival" }, map);
            if (map.OutTime == null)
                map.OutTime = FindColContains(cols, new[] { "outtime", "timeout", "checkout", "punchout", "lastout", "depart" }, map);
            if (map.Status == null)
                map.Status = FindColContains(cols, new[] { "status", "attcode", "daytype", "present" }, map);

            return map;
        }

        private static string FindCol(Dictionary<string, string> cols, string[] aliases)
        {
            foreach (var a in aliases)
            {
                if (cols.TryGetValue(a, out var exact)) return exact;
            }
            foreach (var kv in cols)
            {
                var key = kv.Key.Replace(" ", "").Replace("_", "").Replace("-", "");
                foreach (var a in aliases)
                {
                    var alias = a.Replace(" ", "").Replace("_", "").Replace("-", "");
                    if (key.Equals(alias, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }
            }
            return null;
        }

        private static string FindColContains(Dictionary<string, string> cols, string[] tokens, ColumnMap exclude)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (exclude.EmpCode != null) used.Add(exclude.EmpCode);
            if (exclude.EmployeeName != null) used.Add(exclude.EmployeeName);
            if (exclude.AttDate != null) used.Add(exclude.AttDate);
            if (exclude.InTime != null) used.Add(exclude.InTime);
            if (exclude.OutTime != null) used.Add(exclude.OutTime);
            if (exclude.Status != null) used.Add(exclude.Status);

            foreach (var kv in cols)
            {
                if (used.Contains(kv.Key)) continue;
                var key = kv.Key.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
                foreach (var t in tokens)
                {
                    if (key.Contains(t)) return kv.Value;
                }
            }
            return null;
        }

        private static bool ColumnExists(SqlConnection conn, string table, string column)
        {
            using (var cmd = new SqlCommand(@"
SELECT CASE WHEN COL_LENGTH(@Table, @Column) IS NULL THEN 0 ELSE 1 END;", conn))
            {
                cmd.Parameters.AddWithValue("@Table", table);
                cmd.Parameters.AddWithValue("@Column", column);
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        private static string SelectExpr(string physicalCol, string alias)
        {
            if (string.IsNullOrEmpty(physicalCol))
                return "CAST(NULL AS NVARCHAR(200)) AS [" + alias + "]";
            return "a.[" + SanitizeIdent(physicalCol) + "] AS [" + alias + "]";
        }

        private static string Str(SqlDataReader dr, string col)
        {
            try
            {
                var ord = dr.GetOrdinal(col);
                if (dr.IsDBNull(ord)) return "";
                return Convert.ToString(dr.GetValue(ord), CultureInfo.InvariantCulture)?.Trim() ?? "";
            }
            catch { return ""; }
        }

        private static DateTime? ParseDateValue(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt.Date;
            if (DateTime.TryParse(Convert.ToString(value), out var parsed)) return parsed.Date;
            return null;
        }

        private static string FormatDate(object value)
        {
            var d = ParseDateValue(value);
            return d.HasValue ? d.Value.ToString("yyyy-MM-dd") : "—";
        }

        private static string FormatTime(object value)
        {
            if (value == null || value == DBNull.Value) return "—";
            if (value is DateTime dt)
            {
                // Date-only midnight often means empty punch
                if (dt.TimeOfDay.TotalSeconds == 0 && dt.Date != DateTime.MinValue.Date)
                    return dt.ToString("HH:mm");
                return dt.ToString("HH:mm");
            }
            if (value is TimeSpan ts)
            {
                if (ts.TotalSeconds <= 0) return "—";
                return ts.ToString(@"hh\:mm");
            }
            var s = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrEmpty(s) || s == "0" || s == "00:00" || s == "00:00:00") return "—";
            if (DateTime.TryParse(s, out var parsed)) return parsed.ToString("HH:mm");
            if (TimeSpan.TryParse(s, out var tsp))
                return tsp.TotalSeconds <= 0 ? "—" : tsp.ToString(@"hh\:mm");
            return s;
        }

        private static string FriendlySqlError(SqlException ex)
        {
            if (ex.Number == 18456 || ex.Number == 18452)
                return "Attendance database login failed. Verify AttendanceConnection credentials.";
            if (ex.Number == 4060)
                return "Cannot open attendance database. Verify Initial Catalog in AttendanceConnection.";
            if (ex.Number == 53 || ex.Number == -2 || ex.Number == 10060)
                return "Cannot reach the attendance database server.";
            if (ex.Message != null && ex.Message.IndexOf("Invalid object name", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Attendance table was not found. Confirm Attendance:TableName (expected attendance_dashboard).";
            return "Attendance database error: " + ex.Message;
        }

        private static string SanitizeIdent(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "x";
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == ' ')
                    sb.Append(ch == ' ' ? '_' : ch);
            }
            return sb.Length == 0 ? "x" : sb.ToString();
        }
    }
}
