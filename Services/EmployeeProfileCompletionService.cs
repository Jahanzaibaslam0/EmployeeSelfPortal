using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

using EmployeeInput = global::HRMS.EmployeeInput;
using EmployeeDocumentInput = global::HRMS.EmployeeDocumentInput;
using EmployeeContactInput = global::HRMS.EmployeeContactInput;
using EmployeeAddressInput = global::HRMS.EmployeeAddressInput;
using EmployeeIdentificationInput = global::HRMS.EmployeeIdentificationInput;
using EmployeeFamilyMemberInput = global::HRMS.EmployeeFamilyMemberInput;
using EmployeeBankInput = global::HRMS.EmployeeBankInput;
using EmployeeEducationInput = global::HRMS.EmployeeEducationInput;
using EmployeeCertificateInput = global::HRMS.EmployeeCertificateInput;

namespace HRMS.Services
{
    public class EmployeeProfileCompletionCategory
    {
        public string Name { get; set; } = "";
        public int Filled { get; set; }
        public int Total { get; set; }
        public decimal Score { get; set; }
        public string Detail => Total <= 0 ? "Complete" : Filled + " of " + Total;
    }

    public class EmployeeProfileCompletionScore
    {
        public decimal Personal { get; set; }
        public decimal Background { get; set; }
        public decimal Employment { get; set; }
        public decimal Documents { get; set; }
        public decimal Other { get; set; }
        public decimal Percent { get; set; }
        public List<EmployeeProfileCompletionCategory> Categories { get; set; } = new List<EmployeeProfileCompletionCategory>();

        public string PercentDisplay => Percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        public string BarWidth => Percent.ToString("0.##", CultureInfo.InvariantCulture);
        public string ToneClass =>
            Percent >= 100m ? "is-complete" : Percent >= 70m ? "is-good" : Percent >= 40m ? "is-mid" : "is-low";
    }

    public class EmployeeProfileCompletionService
    {
        public const decimal CategoryWeight = 20m;
        private readonly string _conn;

        public EmployeeProfileCompletionService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public EmployeeProfileCompletionScore Calculate(
            EmployeeInput employee,
            IList<EmployeeDocumentInput> documents,
            IList<EmployeeContactInput> contacts,
            IList<EmployeeAddressInput> addresses,
            IList<EmployeeIdentificationInput> identifications,
            IList<EmployeeFamilyMemberInput> family,
            IList<EmployeeBankInput> banks,
            IList<EmployeeEducationInput> education,
            IList<EmployeeCertificateInput> certificates,
            IList<int> requiredDocumentTypeIds)
        {
            var e = employee ?? new EmployeeInput();
            var personal = Category("Personal information",
                HasText(e.PhotoPath),
                HasText(e.EmployeeCode),
                HasText(e.BiometricID),
                HasText(e.Status),
                HasText(e.FirstName),
                HasText(e.LastName),
                HasText(e.FathersHusbandsName),
                HasText(e.NationalIDNumber),
                HasText(e.DateOfBirth),
                e.GenderID > 0 || HasText(e.Gender));

            var background = Category("Personal & background",
                e.NationalityID > 0,
                HasText(e.Domicile),
                e.ReligionID > 0,
                HasText(e.MaritalStatus),
                e.BloodGroupID > 0);

            var employment = Category("Employment & organization",
                e.LegalEntityID > 0,
                e.BusinessUnitID > 0,
                e.DivisionID > 0,
                e.DesignationID > 0 || HasText(e.Designation),
                e.DepartmentID > 0,
                e.LocationID > 0,
                e.RegionID > 0,
                e.EmploymentStatusID > 0,
                e.SalesTeamID > 0,
                e.ExtensionID > 0,
                HasMoney(e.BasicSalary),
                e.DealForBranchID > 0,
                HasText(e.PrivilegeTier));

            var required = (requiredDocumentTypeIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            var uploadedTypes = new HashSet<int>();
            if (documents != null)
            {
                foreach (var d in documents)
                {
                    if (d == null || d.DocumentTypeID <= 0) continue;
                    if (!HasText(d.DocumentPath) && !HasText(d.OriginalFileName)) continue;
                    uploadedTypes.Add(d.DocumentTypeID);
                }
            }
            bool[] docFlags;
            if (required.Count == 0)
                docFlags = new[] { true };
            else
                docFlags = required.Select(id => uploadedTypes.Contains(id)).ToArray();
            var docs = Category("Document uploads", docFlags);

            var other = Category("Other profile information",
                HasText(e.DisplayName),
                e.LanguageID > 0,
                e.WorkerCategoryID > 0,
                e.EmploymentTypeID > 0,
                e.WorkforceSegmentID > 0,
                e.WorkArrangementID > 0,
                e.AttendanceMethodID > 0,
                e.CostCenterID > 0,
                e.SecondaryLocationID > 0,
                e.JobID > 0,
                e.CityID > 0,
                e.ProvinceID > 0,
                e.GradeID > 0,
                e.BenefitEntitlementID > 0,
                e.UserID > 0,
                e.TemporaryResponsibleEmployeeID > 0,
                e.PermanentResponsibleEmployeeID > 0,
                HasText(e.DateOfJoining),
                HasText(e.EmploymentStartDate),
                HasText(e.ProbationPeriodDays),
                HasText(e.ProbationEndDate),
                HasText(e.ConfirmationDate),
                HasMoney(e.ConvenienceAllowance),
                HasFilledContact(contacts),
                HasFilledAddress(addresses),
                HasFilledIdentification(identifications),
                HasFilledFamily(family),
                HasFilledEducation(education),
                HasFilledCertificate(certificates),
                HasFilledBank(banks));

            var score = new EmployeeProfileCompletionScore
            {
                Personal = personal.Score,
                Background = background.Score,
                Employment = employment.Score,
                Documents = docs.Score,
                Other = other.Score
            };
            score.Categories.Add(personal);
            score.Categories.Add(background);
            score.Categories.Add(employment);
            score.Categories.Add(docs);
            score.Categories.Add(other);
            var total = personal.Score + background.Score + employment.Score + docs.Score + other.Score;
            if (total > 100m) total = 100m;
            if (total < 0m) total = 0m;
            score.Percent = decimal.Round(total, 1, MidpointRounding.AwayFromZero);
            if (score.Percent > 100m) score.Percent = 100m;
            return score;
        }

        public List<int> LoadRequiredDocumentTypeIds()
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(_conn)) return ids;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT DocumentTypeID
FROM tblDocumentType
WHERE ISNULL(IsActive,1)=1
  AND (
        COL_LENGTH(N'dbo.tblDocumentType', N'IsRequired') IS NULL
        OR ISNULL(IsRequired,1)=1
      )
ORDER BY DocumentTypeID;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            ids.Add(Convert.ToInt32(dr[0]));
                    }
                }
            }
            catch
            {
                try
                {
                    using (var conn = new SqlConnection(_conn))
                    using (var cmd = new SqlCommand("SELECT DocumentTypeID FROM tblDocumentType WHERE ISNULL(IsActive,1)=1;", conn))
                    {
                        conn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                                ids.Add(Convert.ToInt32(dr[0]));
                        }
                    }
                }
                catch { }
            }
            return ids;
        }

        public Dictionary<int, EmployeeProfileCompletionScore> ScoreEmployees(IList<int> employeeIds)
        {
            var map = new Dictionary<int, EmployeeProfileCompletionScore>();
            if (employeeIds == null || employeeIds.Count == 0) return map;
            var ids = employeeIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return map;
            try
            {
            var required = LoadRequiredDocumentTypeIds();

            var employees = new Dictionary<int, EmployeeInput>();
            var docs = new Dictionary<int, List<EmployeeDocumentInput>>();
            var contacts = new Dictionary<int, List<EmployeeContactInput>>();
            var addresses = new Dictionary<int, List<EmployeeAddressInput>>();
            var idents = new Dictionary<int, List<EmployeeIdentificationInput>>();
            var family = new Dictionary<int, List<EmployeeFamilyMemberInput>>();
            var banks = new Dictionary<int, List<EmployeeBankInput>>();
            var education = new Dictionary<int, List<EmployeeEducationInput>>();
            var certs = new Dictionary<int, List<EmployeeCertificateInput>>();

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                try { LoadEmployeeRows(conn, ids, employees); }
                catch { }
                LoadChildDocs(conn, ids, docs);
                LoadChildFlag(conn, ids, "SELECT EmployeeID, ContactValue FROM tblEmployeeContact WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(contacts, id, new EmployeeContactInput { ContactValue = val }));
                LoadChildFlag(conn, ids, "SELECT EmployeeID, AddressLine FROM tblEmployeeAddress WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(addresses, id, new EmployeeAddressInput { AddressLine = val }));
                LoadChildFlag(conn, ids, "SELECT EmployeeID, RegistrationNo FROM tblEmployeeIdentification WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(idents, id, new EmployeeIdentificationInput { RegistrationNo = val }));
                LoadChildFlag(conn, ids, "SELECT EmployeeID, MemberName FROM tblEmployeeFamilyMember WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(family, id, new EmployeeFamilyMemberInput { MemberName = val }));
                LoadChildFlag(conn, ids, "SELECT EmployeeID, HighestQualification FROM tblEmployeeEducation WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(education, id, new EmployeeEducationInput { HighestQualification = val }));
                LoadChildFlag(conn, ids, "SELECT EmployeeID, CertificationName FROM tblEmployeeCertificate WHERE EmployeeID IN (" + JoinIds(ids) + ")",
                    (id, val) => Add(certs, id, new EmployeeCertificateInput { CertificationName = val }));
                try
                {
                    using (var cmd = new SqlCommand("SELECT EmployeeID, BankID FROM tblEmployeeBank WHERE EmployeeID IN (" + JoinIds(ids) + ");", conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var id = Convert.ToInt32(dr[0]);
                            var bankId = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr[1]);
                            Add(banks, id, new EmployeeBankInput { BankID = bankId });
                        }
                    }
                }
                catch { }
            }

            foreach (var id in ids)
            {
                EmployeeInput emp;
                if (!employees.TryGetValue(id, out emp)) emp = new EmployeeInput { EmployeeID = id };
                map[id] = Calculate(emp,
                    Get(docs, id), Get(contacts, id), Get(addresses, id), Get(idents, id),
                    Get(family, id), Get(banks, id), Get(education, id), Get(certs, id), required);
            }
            }
            catch { }
            return map;
        }

        private static void LoadEmployeeRows(SqlConnection conn, List<int> ids, Dictionary<int, EmployeeInput> employees)
        {
            using (var cmd = new SqlCommand(@"
SELECT EmployeeID, EmployeeCode, FirstName, LastName, FathersHusbandsName, DisplayName, NationalIDNumber,
       BiometricID, Gender, GenderID, DateOfBirth, MaritalStatus, DepartmentID, DivisionID, NationalityID,
       ReligionID, LanguageID, WorkerCategoryID, EmploymentTypeID, EmploymentStatusID, WorkforceSegmentID,
       WorkArrangementID, AttendanceMethodID, LegalEntityID, BusinessUnitID, SalesTeamID, CostCenterID,
       RegionID, LocationID, DealForBranchID, SecondaryLocationID, JobID, CityID, ProvinceID, GradeID,
       DesignationID, PrivilegeTier, ExtensionID, Domicile, BloodGroupID, BenefitEntitlementID, UserID,
       TemporaryResponsibleEmployeeID, PermanentResponsibleEmployeeID,        Designation, DateOfJoining, EmploymentStartDate,
       ProbationPeriodDays, ProbationEndDate, ConfirmationDate, BasicSalary, ConvenienceAllowance, Status, PhotoPath
FROM tblEmployee WHERE EmployeeID IN (" + JoinIds(ids) + ");", conn))
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var id = Convert.ToInt32(dr["EmployeeID"]);
                    employees[id] = new EmployeeInput
                    {
                        EmployeeID = id,
                        EmployeeCode = Str(dr, "EmployeeCode"),
                        FirstName = Str(dr, "FirstName"),
                        LastName = Str(dr, "LastName"),
                        FathersHusbandsName = Str(dr, "FathersHusbandsName"),
                        DisplayName = Str(dr, "DisplayName"),
                        NationalIDNumber = Str(dr, "NationalIDNumber"),
                        BiometricID = dr["BiometricID"] == DBNull.Value ? "" : Convert.ToString(dr["BiometricID"]),
                        Gender = Str(dr, "Gender"),
                        GenderID = IntVal(dr, "GenderID"),
                        DateOfBirth = DateVal(dr, "DateOfBirth"),
                        MaritalStatus = Str(dr, "MaritalStatus"),
                        DepartmentID = IntVal(dr, "DepartmentID"),
                        DivisionID = IntVal(dr, "DivisionID"),
                        NationalityID = IntVal(dr, "NationalityID"),
                        ReligionID = IntVal(dr, "ReligionID"),
                        LanguageID = IntVal(dr, "LanguageID"),
                        WorkerCategoryID = IntVal(dr, "WorkerCategoryID"),
                        EmploymentTypeID = IntVal(dr, "EmploymentTypeID"),
                        EmploymentStatusID = IntVal(dr, "EmploymentStatusID"),
                        WorkforceSegmentID = IntVal(dr, "WorkforceSegmentID"),
                        WorkArrangementID = IntVal(dr, "WorkArrangementID"),
                        AttendanceMethodID = IntVal(dr, "AttendanceMethodID"),
                        LegalEntityID = IntVal(dr, "LegalEntityID"),
                        BusinessUnitID = IntVal(dr, "BusinessUnitID"),
                        SalesTeamID = IntVal(dr, "SalesTeamID"),
                        CostCenterID = IntVal(dr, "CostCenterID"),
                        RegionID = IntVal(dr, "RegionID"),
                        LocationID = IntVal(dr, "LocationID"),
                        DealForBranchID = IntVal(dr, "DealForBranchID"),
                        SecondaryLocationID = IntVal(dr, "SecondaryLocationID"),
                        JobID = IntVal(dr, "JobID"),
                        CityID = IntVal(dr, "CityID"),
                        ProvinceID = IntVal(dr, "ProvinceID"),
                        GradeID = IntVal(dr, "GradeID"),
                        DesignationID = IntVal(dr, "DesignationID"),
                        PrivilegeTier = Str(dr, "PrivilegeTier"),
                        ExtensionID = IntVal(dr, "ExtensionID"),
                        Domicile = Str(dr, "Domicile"),
                        BloodGroupID = IntVal(dr, "BloodGroupID"),
                        BenefitEntitlementID = IntVal(dr, "BenefitEntitlementID"),
                        UserID = IntVal(dr, "UserID"),
                        TemporaryResponsibleEmployeeID = IntVal(dr, "TemporaryResponsibleEmployeeID"),
                        PermanentResponsibleEmployeeID = IntVal(dr, "PermanentResponsibleEmployeeID"),
                        Designation = Str(dr, "Designation"),
                        DateOfJoining = DateVal(dr, "DateOfJoining"),
                        EmploymentStartDate = DateVal(dr, "EmploymentStartDate"),
                        ProbationPeriodDays = dr["ProbationPeriodDays"] == DBNull.Value ? "" : Convert.ToString(dr["ProbationPeriodDays"]),
                        ProbationEndDate = DateVal(dr, "ProbationEndDate"),
                        ConfirmationDate = DateVal(dr, "ConfirmationDate"),
                        BasicSalary = dr["BasicSalary"] == DBNull.Value ? "" : Convert.ToString(dr["BasicSalary"]),
                        ConvenienceAllowance = dr["ConvenienceAllowance"] == DBNull.Value ? "" : Convert.ToString(dr["ConvenienceAllowance"]),
                        Status = Str(dr, "Status"),
                        PhotoPath = Str(dr, "PhotoPath")
                    };
                }
            }
        }

        private static void LoadChildDocs(SqlConnection conn, List<int> ids, Dictionary<int, List<EmployeeDocumentInput>> docs)
        {
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT EmployeeID, DocumentTypeID, DocumentPath, OriginalFileName
FROM tblEmployeeDocument WHERE EmployeeID IN (" + JoinIds(ids) + ");", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var id = Convert.ToInt32(dr[0]);
                        Add(docs, id, new EmployeeDocumentInput
                        {
                            DocumentTypeID = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr[1]),
                            DocumentPath = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            OriginalFileName = dr.IsDBNull(3) ? "" : dr.GetString(3)
                        });
                    }
                }
            }
            catch { }
        }

        private static void LoadChildFlag(SqlConnection conn, List<int> ids, string sql, Action<int, string> add)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var id = Convert.ToInt32(dr[0]);
                        var val = dr.IsDBNull(1) ? "" : Convert.ToString(dr[1]);
                        add(id, val ?? "");
                    }
                }
            }
            catch { }
        }

        private static EmployeeProfileCompletionCategory Category(string name, params bool[] flags)
        {
            var total = flags == null || flags.Length == 0 ? 1 : flags.Length;
            var filled = 0;
            if (flags != null)
            {
                foreach (var f in flags)
                    if (f) filled++;
            }
            var score = total <= 0 ? CategoryWeight : decimal.Round(CategoryWeight * filled / total, 4, MidpointRounding.AwayFromZero);
            if (filled >= total) score = CategoryWeight;
            return new EmployeeProfileCompletionCategory
            {
                Name = name,
                Filled = filled,
                Total = total,
                Score = score
            };
        }

        private static bool HasText(string value) => !string.IsNullOrWhiteSpace(value);

        private static bool HasMoney(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            decimal n;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out n)
                || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out n);
        }

        private static bool HasFilledContact(IList<EmployeeContactInput> rows)
            => rows != null && rows.Any(r => r != null && HasText(r.ContactValue));

        private static bool HasFilledAddress(IList<EmployeeAddressInput> rows)
            => rows != null && rows.Any(r => r != null && HasText(r.AddressLine));

        private static bool HasFilledIdentification(IList<EmployeeIdentificationInput> rows)
            => rows != null && rows.Any(r => r != null && HasText(r.RegistrationNo));

        private static bool HasFilledFamily(IList<EmployeeFamilyMemberInput> rows)
            => rows != null && rows.Any(r => r != null && HasText(r.MemberName));

        private static bool HasFilledEducation(IList<EmployeeEducationInput> rows)
            => rows != null && rows.Any(r => r != null && (HasText(r.HighestQualification) || HasText(r.Institution)));

        private static bool HasFilledCertificate(IList<EmployeeCertificateInput> rows)
            => rows != null && rows.Any(r => r != null && (HasText(r.CertificationName) || HasText(r.CertificateNumber)));

        private static bool HasFilledBank(IList<EmployeeBankInput> rows)
            => rows != null && rows.Any(r => r != null && r.BankID > 0);

        private static string JoinIds(List<int> ids) => string.Join(",", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray());

        private static void Add<T>(Dictionary<int, List<T>> map, int id, T item)
        {
            List<T> list;
            if (!map.TryGetValue(id, out list))
            {
                list = new List<T>();
                map[id] = list;
            }
            list.Add(item);
        }

        private static List<T> Get<T>(Dictionary<int, List<T>> map, int id)
        {
            List<T> list;
            return map.TryGetValue(id, out list) ? list : new List<T>();
        }

        private static string Str(SqlDataReader dr, string col)
        {
            var i = dr.GetOrdinal(col);
            return dr.IsDBNull(i) ? "" : Convert.ToString(dr.GetValue(i)) ?? "";
        }

        private static int IntVal(SqlDataReader dr, string col)
        {
            var i = dr.GetOrdinal(col);
            if (dr.IsDBNull(i)) return 0;
            return Convert.ToInt32(dr.GetValue(i));
        }

        private static string DateVal(SqlDataReader dr, string col)
        {
            var i = dr.GetOrdinal(col);
            if (dr.IsDBNull(i)) return "";
            return Convert.ToDateTime(dr.GetValue(i)).ToString("yyyy-MM-dd");
        }
    }
}
