using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public static class PositionLeaveReasons
    {
        public static readonly string[] All =
        {
            "Leave",
            "Promotion",
            "Transfer",
            "Resignation",
            "Retirement",
            "Contract Ended",
            "Other"
        };
    }

    public class PositionAssignmentItem
    {
        public int PositionWorkerAssignmentID { get; set; }
        public int PositionID { get; set; }
        public string PositionNo { get; set; } = "";
        public string PositionDescription { get; set; } = "";
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public DateTime? AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
        public string Reason { get; set; } = "";
        public bool IsActive => !AssignmentEndDate.HasValue;

        public string EmployeeLabel =>
            string.IsNullOrWhiteSpace(EmployeeCode)
                ? EmployeeName
                : EmployeeCode + " – " + EmployeeName;

        public string StartDateDisplay =>
            AssignmentStartDate.HasValue ? AssignmentStartDate.Value.ToString("yyyy-MM-dd") : "";

        public string EndDateDisplay =>
            AssignmentEndDate.HasValue ? AssignmentEndDate.Value.ToString("yyyy-MM-dd") : "Present";

        public string TenureDisplay
        {
            get
            {
                if (!AssignmentStartDate.HasValue) return "";
                var end = AssignmentEndDate ?? DateTime.Today;
                if (end < AssignmentStartDate.Value) return "0 days";
                var days = (end - AssignmentStartDate.Value).Days + 1;
                if (days < 30) return days + (days == 1 ? " day" : " days");
                var months = (end.Year - AssignmentStartDate.Value.Year) * 12
                             + end.Month - AssignmentStartDate.Value.Month;
                if (end.Day < AssignmentStartDate.Value.Day) months--;
                if (months < 1) return days + " days";
                if (months < 12) return months + (months == 1 ? " month" : " months");
                var years = months / 12;
                var remMonths = months % 12;
                if (remMonths == 0) return years + (years == 1 ? " year" : " years");
                return years + (years == 1 ? " year" : " years") + ", "
                       + remMonths + (remMonths == 1 ? " month" : " months");
            }
        }

        public string StatusDisplay => IsActive ? "Active" : "Closed";
    }

    public class PositionEmployeeLookup
    {
        public int EmployeeID { get; set; }
        public string Label { get; set; } = "";
    }

    /// <summary>
    /// One active assignee per position; assignment history is append-only (close then create new).
    /// </summary>
    public class PositionAssignmentService
    {
        private readonly string _conn;

        public PositionAssignmentService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString
                    ?? throw new InvalidOperationException("HRMSConnection is not configured.");
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblPositionWorkerAssignment', N'U') IS NULL
BEGIN
    CREATE TABLE tblPositionWorkerAssignment (
        PositionWorkerAssignmentID INT IDENTITY(1,1) PRIMARY KEY,
        PositionID                 INT NOT NULL,
        EmployeeID                 INT NOT NULL,
        AssignmentStartDate        DATE NULL,
        AssignmentEndDate          DATE NULL,
        Reason                     NVARCHAR(500) NULL,
        SortOrder                  INT NOT NULL CONSTRAINT DF_tblPosWA_Sort_Runtime DEFAULT (1),
        CreatedOn                  DATETIME NOT NULL CONSTRAINT DF_tblPosWA_Created_Runtime DEFAULT (GETDATE()),
        CreatedByUserID            INT NULL,
        ModifiedOn                 DATETIME NULL,
        ModifiedByUserID           INT NULL
    );
END

IF COL_LENGTH(N'tblPositionWorkerAssignment', N'ModifiedOn') IS NULL
    ALTER TABLE tblPositionWorkerAssignment ADD ModifiedOn DATETIME NULL;

IF COL_LENGTH(N'tblPositionWorkerAssignment', N'ModifiedByUserID') IS NULL
    ALTER TABLE tblPositionWorkerAssignment ADD ModifiedByUserID INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblPosWA_Position_Open' AND object_id = OBJECT_ID(N'tblPositionWorkerAssignment'))
    CREATE NONCLUSTERED INDEX IX_tblPosWA_Position_Open
        ON tblPositionWorkerAssignment (PositionID)
        INCLUDE (EmployeeID, AssignmentStartDate, AssignmentEndDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblPosWA_Employee_Open' AND object_id = OBJECT_ID(N'tblPositionWorkerAssignment'))
    CREATE NONCLUSTERED INDEX IX_tblPosWA_Employee_Open
        ON tblPositionWorkerAssignment (EmployeeID)
        INCLUDE (PositionID, AssignmentEndDate);
", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Position number (e.g. EPS-000001) for the employee's current assignment.
        /// Active means end date is blank (Present) or not before today.
        /// </summary>
        public string GetActivePositionNoForEmployee(int employeeId)
        {
            if (employeeId <= 0) return "";
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 p.PositionNo
FROM tblPositionWorkerAssignment a
INNER JOIN tblPosition p ON p.PositionID = a.PositionID
WHERE a.EmployeeID = @EmployeeID
  AND (a.AssignmentEndDate IS NULL OR a.AssignmentEndDate >= CAST(GETDATE() AS DATE))
ORDER BY CASE WHEN a.AssignmentEndDate IS NULL THEN 0 ELSE 1 END,
         ISNULL(a.AssignmentStartDate, '1900-01-01') DESC,
         a.PositionWorkerAssignmentID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? "" : result.ToString() ?? "";
            }
        }

        public PositionAssignmentItem GetActiveAssignment(int positionId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1
    a.PositionWorkerAssignmentID, a.PositionID, p.PositionNo, ISNULL(p.Description,''),
    a.EmployeeID, e.EmployeeCode, LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))),
    a.AssignmentStartDate, a.AssignmentEndDate, ISNULL(a.Reason,'')
FROM tblPositionWorkerAssignment a
INNER JOIN tblPosition p ON p.PositionID = a.PositionID
INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
WHERE a.PositionID = @PositionID AND a.AssignmentEndDate IS NULL
ORDER BY a.AssignmentStartDate DESC, a.PositionWorkerAssignmentID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@PositionID", positionId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return Map(dr);
                }
            }
        }

        public List<PositionAssignmentItem> GetHistory(int positionId)
        {
            EnsureSchema();
            var list = new List<PositionAssignmentItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT
    a.PositionWorkerAssignmentID, a.PositionID, p.PositionNo, ISNULL(p.Description,''),
    a.EmployeeID, e.EmployeeCode, LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))),
    a.AssignmentStartDate, a.AssignmentEndDate, ISNULL(a.Reason,'')
FROM tblPositionWorkerAssignment a
INNER JOIN tblPosition p ON p.PositionID = a.PositionID
INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
WHERE a.PositionID = @PositionID
ORDER BY CASE WHEN a.AssignmentEndDate IS NULL THEN 0 ELSE 1 END,
         ISNULL(a.AssignmentStartDate, '1900-01-01') DESC,
         a.PositionWorkerAssignmentID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@PositionID", positionId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(Map(dr));
                }
            }
            return list;
        }

        public Dictionary<int, string> GetActiveAssigneeLabels(IEnumerable<int> positionIds)
        {
            EnsureSchema();
            var map = new Dictionary<int, string>();
            var ids = new List<int>();
            foreach (var id in positionIds)
                if (id > 0 && !ids.Contains(id)) ids.Add(id);
            if (ids.Count == 0) return map;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
SELECT a.PositionID,
       e.EmployeeCode + N' – ' + LTRIM(RTRIM(ISNULL(e.FirstName,'')+N' '+ISNULL(e.LastName,'')))
FROM tblPositionWorkerAssignment a
INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
WHERE a.AssignmentEndDate IS NULL;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var pid = dr.GetInt32(0);
                        if (ids.Contains(pid))
                            map[pid] = dr.GetString(1);
                    }
                }
            }
            return map;
        }

        public List<PositionEmployeeLookup> GetEmployeeLookups(bool activeOnly = true)
        {
            var list = new List<PositionEmployeeLookup>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID,
       e.EmployeeCode + N' – ' + LTRIM(RTRIM(ISNULL(e.FirstName,'')+N' '+ISNULL(e.LastName,'')))
FROM tblEmployee e
WHERE (@ActiveOnly = 0 OR e.Status = N'Active')
ORDER BY e.EmployeeCode, e.FirstName, e.LastName;", conn))
            {
                cmd.Parameters.AddWithValue("@ActiveOnly", activeOnly ? 1 : 0);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(new PositionEmployeeLookup
                        {
                            EmployeeID = dr.GetInt32(0),
                            Label = dr.GetString(1)
                        });
                }
            }
            return list;
        }

        public const string OverlapErrorMessage =
            "This position is already assigned to another employee during the selected period. Please select a start date after the current assignment ends or close the existing assignment first.";

        /// <summary>
        /// Inclusive date ranges overlap when startA &lt;= endB and startB &lt;= endA.
        /// Null end date means open-ended (treated as ongoing).
        /// </summary>
        public static bool PeriodsOverlap(DateTime startA, DateTime? endA, DateTime startB, DateTime? endB)
        {
            var aStart = startA.Date;
            var bStart = startB.Date;
            var aEnd = (endA ?? DateTime.MaxValue).Date;
            var bEnd = (endB ?? DateTime.MaxValue).Date;
            return aStart <= bEnd && bStart <= aEnd;
        }

        /// <summary>Throws if the proposed period overlaps any existing assignment for this position.</summary>
        public void ValidateNoOverlap(int positionId, DateTime startDate, DateTime? endDate = null, int? excludeAssignmentId = null)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    AssertNoPeriodOverlap(conn, tx, positionId, startDate.Date, endDate?.Date, excludeAssignmentId);
                    tx.Commit();
                }
            }
        }

        /// <summary>Assign employee to position. Period must not overlap any existing assignment for this position.</summary>
        public int AssignEmployee(int positionId, int employeeId, DateTime startDate, int userId, DateTime? endDate = null)
        {
            if (positionId <= 0) throw new InvalidOperationException("Save the position before assigning an employee.");
            if (employeeId <= 0) throw new InvalidOperationException("Select an employee from Employee Master.");
            startDate = startDate.Date;
            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date;
                if (endDate.Value < startDate)
                    throw new InvalidOperationException("End date cannot be earlier than the assignment start date.");
            }

            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    AssertPositionExists(conn, tx, positionId);
                    AssertEmployeeExists(conn, tx, employeeId);

                    // Open-ended active assignee blocks any new assignment until closed.
                    var existingOpen = GetOpenAssignmentId(conn, tx, positionId, null);
                    if (existingOpen > 0)
                        throw new InvalidOperationException(OverlapErrorMessage);

                    AssertNoPeriodOverlap(conn, tx, positionId, startDate, endDate, null);

                    var empOpen = GetOpenAssignmentForEmployee(conn, tx, employeeId);
                    if (empOpen.AssignmentId > 0)
                        throw new InvalidOperationException(
                            "This employee is already assigned to position " + empOpen.PositionNo
                            + ". Close that assignment before assigning them here.");

                    int newId;
                    using (var cmd = new SqlCommand(@"
INSERT INTO tblPositionWorkerAssignment
 (PositionID, EmployeeID, AssignmentStartDate, AssignmentEndDate, Reason, SortOrder, CreatedOn, CreatedByUserID)
VALUES
 (@PositionID, @EmployeeID, @StartDate, @EndDate, NULL, 1, GETDATE(), @UserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@PositionID", positionId);
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        cmd.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserID", userId > 0 ? (object)userId : DBNull.Value);
                        newId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    tx.Commit();
                    return newId;
                }
            }
        }

        /// <summary>Close the open assignment for a position (or specific assignment id).</summary>
        public void CloseAssignment(int positionId, int assignmentId, DateTime endDate, string reason, int userId)
        {
            if (positionId <= 0) throw new InvalidOperationException("Invalid position.");
            endDate = endDate.Date;
            reason = (reason ?? "").Trim();
            if (string.IsNullOrEmpty(reason))
                throw new InvalidOperationException("Reason for leaving the position is required.");

            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    DateTime? startDate = null;
                    int targetId = assignmentId;

                    using (var cmd = new SqlCommand(@"
SELECT PositionWorkerAssignmentID, AssignmentStartDate, AssignmentEndDate
FROM tblPositionWorkerAssignment
WHERE PositionID = @PositionID
  AND (@AssignmentID = 0 OR PositionWorkerAssignmentID = @AssignmentID)
  AND AssignmentEndDate IS NULL
ORDER BY AssignmentStartDate DESC, PositionWorkerAssignmentID DESC;", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@PositionID", positionId);
                        cmd.Parameters.AddWithValue("@AssignmentID", assignmentId);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                                throw new InvalidOperationException("No active assignment found to close.");
                            targetId = Convert.ToInt32(dr[0]);
                            startDate = dr.IsDBNull(1) ? (DateTime?)null : Convert.ToDateTime(dr[1]).Date;
                        }
                    }

                    if (startDate.HasValue && endDate < startDate.Value)
                        throw new InvalidOperationException("End date cannot be earlier than the assignment start date.");

                    using (var cmd = new SqlCommand(@"
UPDATE tblPositionWorkerAssignment
SET AssignmentEndDate = @EndDate,
    Reason = @Reason,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @UserID
WHERE PositionWorkerAssignmentID = @Id AND AssignmentEndDate IS NULL;", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@EndDate", endDate);
                        cmd.Parameters.AddWithValue("@Reason", reason.Length > 500 ? reason.Substring(0, 500) : reason);
                        cmd.Parameters.AddWithValue("@UserID", userId > 0 ? (object)userId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id", targetId);
                        if (cmd.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException("Could not close the assignment.");
                    }

                    tx.Commit();
                }
            }
        }

        private static PositionAssignmentItem Map(SqlDataReader dr)
        {
            return new PositionAssignmentItem
            {
                PositionWorkerAssignmentID = Convert.ToInt32(dr[0]),
                PositionID = Convert.ToInt32(dr[1]),
                PositionNo = dr.IsDBNull(2) ? "" : dr.GetString(2),
                PositionDescription = dr.IsDBNull(3) ? "" : dr.GetString(3),
                EmployeeID = Convert.ToInt32(dr[4]),
                EmployeeCode = dr.IsDBNull(5) ? "" : dr.GetString(5),
                EmployeeName = dr.IsDBNull(6) ? "" : dr.GetString(6).Trim(),
                AssignmentStartDate = dr.IsDBNull(7) ? (DateTime?)null : Convert.ToDateTime(dr[7]),
                AssignmentEndDate = dr.IsDBNull(8) ? (DateTime?)null : Convert.ToDateTime(dr[8]),
                Reason = dr.IsDBNull(9) ? "" : dr.GetString(9)
            };
        }

        private static void AssertNoPeriodOverlap(
            SqlConnection conn,
            SqlTransaction tx,
            int positionId,
            DateTime newStart,
            DateTime? newEnd,
            int? excludeAssignmentId)
        {
            using (var cmd = new SqlCommand(@"
SELECT a.PositionWorkerAssignmentID,
       a.AssignmentStartDate,
       a.AssignmentEndDate
FROM tblPositionWorkerAssignment a
WHERE a.PositionID = @PositionID
  AND (@ExcludeId IS NULL OR a.PositionWorkerAssignmentID <> @ExcludeId);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@PositionID", positionId);
                cmd.Parameters.AddWithValue("@ExcludeId",
                    excludeAssignmentId.HasValue ? (object)excludeAssignmentId.Value : DBNull.Value);

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        if (dr.IsDBNull(1)) continue;
                        var existStart = Convert.ToDateTime(dr[1]).Date;
                        DateTime? existEnd = dr.IsDBNull(2) ? (DateTime?)null : Convert.ToDateTime(dr[2]).Date;

                        if (PeriodsOverlap(newStart, newEnd, existStart, existEnd))
                            throw new InvalidOperationException(OverlapErrorMessage);
                    }
                }
            }
        }

        private static void AssertPositionExists(SqlConnection conn, SqlTransaction tx, int positionId)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM tblPosition WHERE PositionID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", positionId);
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("Position was not found.");
            }
        }

        private static void AssertEmployeeExists(SqlConnection conn, SqlTransaction tx, int employeeId)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM tblEmployee WHERE EmployeeID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("Selected employee was not found in Employee Master.");
            }
        }

        private static int GetOpenAssignmentId(SqlConnection conn, SqlTransaction tx, int positionId, int? excludeAssignmentId)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 PositionWorkerAssignmentID
FROM tblPositionWorkerAssignment
WHERE PositionID = @PositionID AND AssignmentEndDate IS NULL
  AND (@ExcludeId IS NULL OR PositionWorkerAssignmentID <> @ExcludeId);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@PositionID", positionId);
                cmd.Parameters.AddWithValue("@ExcludeId", excludeAssignmentId.HasValue ? (object)excludeAssignmentId.Value : DBNull.Value);
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
        }

        private static (int AssignmentId, string PositionNo) GetOpenAssignmentForEmployee(SqlConnection conn, SqlTransaction tx, int employeeId)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 a.PositionWorkerAssignmentID, p.PositionNo
FROM tblPositionWorkerAssignment a
INNER JOIN tblPosition p ON p.PositionID = a.PositionID
WHERE a.EmployeeID = @EmployeeID AND a.AssignmentEndDate IS NULL;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return (0, "");
                    return (Convert.ToInt32(dr[0]), dr.IsDBNull(1) ? "" : dr.GetString(1));
                }
            }
        }
    }
}
