using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.Script.Serialization;

namespace HRMS.Services
{
    public class GeoLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ParentId { get; set; }
    }

    public class ContactCountryItem
    {
        public int CountryID { get; set; }
        public string CountryName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ContactStateItem
    {
        public int StateID { get; set; }
        public int CountryID { get; set; }
        public string CountryName { get; set; } = "";
        public string StateName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ContactCityItem
    {
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string StateName { get; set; } = "";
        public string CountryName { get; set; } = "";
        public string CityName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ContactPostalItem
    {
        public int PostalID { get; set; }
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string CityName { get; set; } = "";
        public string StateName { get; set; } = "";
        public string CountryName { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string AreaName { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public string PostalDisplay =>
            string.IsNullOrWhiteSpace(AreaName) ? PostalCode : PostalCode + " - " + AreaName;
    }

    public class ContactStreetItem
    {
        public int StreetID { get; set; }
        public int PostalID { get; set; }
        public string PostalCode { get; set; } = "";
        public string CityName { get; set; } = "";
        public string StateName { get; set; } = "";
        public string StreetName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ContactAddressItem
    {
        public int AddressID { get; set; }
        public int ContactID { get; set; }
        public int CountryID { get; set; }
        public int StateID { get; set; }
        public int CityID { get; set; }
        public int PostalID { get; set; }
        public int StreetID { get; set; }
        public string AddressLine { get; set; } = "";
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsBilling { get; set; }
        public bool IsShipping { get; set; }
        public bool IsPrimary { get; set; }
        public string WorkflowStatus { get; set; } = "Approved";
        public string VerificationStatus { get; set; } = "Verified";
        public string CountryName { get; set; } = "";
        public string StateName { get; set; } = "";
        public string CityName { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string StreetName { get; set; } = "";

        [ScriptIgnore]
        public string LatitudeText =>
            Latitude.HasValue ? Latitude.Value.ToString("0.#######", CultureInfo.InvariantCulture) : "";
        [ScriptIgnore]
        public string LongitudeText =>
            Longitude.HasValue ? Longitude.Value.ToString("0.#######", CultureInfo.InvariantCulture) : "";
        [ScriptIgnore]
        public string Coordinate =>
            Latitude.HasValue && Longitude.HasValue
                ? LatitudeText + "," + LongitudeText
                : "";

        [ScriptIgnore]
        public string LocationLabel
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(StreetName)) parts.Add(StreetName);
                if (!string.IsNullOrWhiteSpace(PostalCode)) parts.Add(PostalCode);
                if (!string.IsNullOrWhiteSpace(CityName)) parts.Add(CityName);
                if (!string.IsNullOrWhiteSpace(StateName)) parts.Add(StateName);
                if (!string.IsNullOrWhiteSpace(CountryName)) parts.Add(CountryName);
                return string.Join(" / ", parts);
            }
        }

        [ScriptIgnore]
        public string TypeDisplay
        {
            get
            {
                var flags = new List<string>();
                if (IsPrimary) flags.Add("Primary");
                if (IsBilling) flags.Add("Billing");
                if (IsShipping) flags.Add("Shipping");
                return flags.Count == 0 ? "Other" : string.Join(", ", flags);
            }
        }
    }

    /// <summary>
    /// Country → State → City → Postal → Street masters, used as lookups on contact addresses.
    /// Separate from tblLocation (organization work location) and tblCity (organization city).
    /// </summary>
    public class ContactAddressService
    {
        private readonly string _conn;

        public ContactAddressService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString
                    ?? throw new InvalidOperationException("HRMSConnection is not configured.");
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                Exec(conn, CreateTablesSql);
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tblContactAddress', N'CountryID') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD CountryID INT NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'StateID') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD StateID INT NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'CityID') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD CityID INT NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'PostalID') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD PostalID INT NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'StreetID') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD StreetID INT NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'Latitude') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD Latitude DECIMAL(10,7) NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'Longitude') IS NULL
        ALTER TABLE dbo.tblContactAddress ADD Longitude DECIMAL(11,7) NULL;
    IF COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NULL
        EXEC(N'ALTER TABLE dbo.tblContactAddress ADD IsPrimary BIT NOT NULL CONSTRAINT DF_tblContactAddress_Primary DEFAULT (0)');
END");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NULL
    EXEC(N'ALTER TABLE dbo.tblContactAddress ADD IsPrimary BIT NOT NULL DEFAULT (0)');");
                ExecIgnore(conn, @"
IF COL_LENGTH(N'dbo.tblContactAddress', N'LocationSetupID') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblContactAddress') AND name = N'LocationSetupID' AND is_nullable = 0
)
    ALTER TABLE dbo.tblContactAddress ALTER COLUMN LocationSetupID INT NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactMaster', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactAddress_Contact', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Contact
        FOREIGN KEY (ContactID) REFERENCES dbo.tblContactMaster(ContactID) ON DELETE CASCADE;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Country', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Country
        FOREIGN KEY (CountryID) REFERENCES dbo.tblContactCountry(CountryID);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactAddress_State', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_State
        FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactAddress_City', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_City
        FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Postal', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Postal
        FOREIGN KEY (PostalID) REFERENCES dbo.tblContactPostal(PostalID);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactAddress_Street', N'F') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD CONSTRAINT FK_tblContactAddress_Street
        FOREIGN KEY (StreetID) REFERENCES dbo.tblContactStreet(StreetID);");
                ExecIgnore(conn, @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblContactAddress_Contact' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE NONCLUSTERED INDEX IX_tblContactAddress_Contact ON dbo.tblContactAddress (ContactID, SortOrder);");
                ExecIgnore(conn, @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Billing' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Billing ON dbo.tblContactAddress (ContactID) WHERE IsBilling = 1;");
                ExecIgnore(conn, @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Shipping' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Shipping ON dbo.tblContactAddress (ContactID) WHERE IsShipping = 1;");
                ExecIgnore(conn, @"
IF COL_LENGTH(N'dbo.tblContactAddress', N'IsPrimary') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactAddress_Primary' AND object_id = OBJECT_ID(N'dbo.tblContactAddress'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_tblContactAddress_Primary ON dbo.tblContactAddress (ContactID) WHERE IsPrimary = 1');");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactAddress', N'WorkflowStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContactAddr_Workflow DEFAULT N'Approved';");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactAddress', N'VerificationStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContactAddr_Verify DEFAULT N'Verified';");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NULL
    ALTER TABLE dbo.tblContactPostal ADD CityID INT NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.FK_tblContactPostal_City', N'F') IS NULL
AND COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NOT NULL
    ALTER TABLE dbo.tblContactPostal ADD CONSTRAINT FK_tblContactPostal_City
        FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID);");
                ExecIgnore(conn, @"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactPostal_Code' AND object_id = OBJECT_ID(N'dbo.tblContactPostal'))
    DROP INDEX UX_tblContactPostal_Code ON dbo.tblContactPostal;");
                ExecIgnore(conn, @"
IF COL_LENGTH(N'dbo.tblContactPostal', N'CityID') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tblContactPostal_CityCode' AND object_id = OBJECT_ID(N'dbo.tblContactPostal'))
    EXEC(N'CREATE UNIQUE INDEX UX_tblContactPostal_CityCode ON dbo.tblContactPostal (CityID, PostalCode) WHERE IsActive = 1 AND CityID IS NOT NULL');");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactPostal', N'AreaName') IS NULL
    ALTER TABLE dbo.tblContactPostal ADD AreaName NVARCHAR(150) NULL;");
            }
        }

        private const string CreateTablesSql = @"
IF OBJECT_ID(N'dbo.tblContactCountry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactCountry (
        CountryID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactCountry PRIMARY KEY,
        CountryName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactCountry_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactCountry_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
    CREATE UNIQUE INDEX UX_tblContactCountry_Name ON dbo.tblContactCountry (CountryName) WHERE IsActive = 1;
END

IF OBJECT_ID(N'dbo.tblContactState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactState (
        StateID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactState PRIMARY KEY,
        CountryID INT NOT NULL,
        StateName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactState_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactState_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactState_Country FOREIGN KEY (CountryID) REFERENCES dbo.tblContactCountry(CountryID)
    );
    CREATE UNIQUE INDEX UX_tblContactState_Name ON dbo.tblContactState (CountryID, StateName) WHERE IsActive = 1;
END

IF OBJECT_ID(N'dbo.tblContactCity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactCity (
        CityID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactCity PRIMARY KEY,
        StateID INT NOT NULL,
        CityName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactCity_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactCity_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactCity_State FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID)
    );
    CREATE UNIQUE INDEX UX_tblContactCity_Name ON dbo.tblContactCity (StateID, CityName) WHERE IsActive = 1;
END

IF OBJECT_ID(N'dbo.tblContactPostal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactPostal (
        PostalID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactPostal PRIMARY KEY,
        CityID INT NOT NULL,
        StateID INT NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        AreaName NVARCHAR(150) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactPostal_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactPostal_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactPostal_City FOREIGN KEY (CityID) REFERENCES dbo.tblContactCity(CityID),
        CONSTRAINT FK_tblContactPostal_State FOREIGN KEY (StateID) REFERENCES dbo.tblContactState(StateID)
    );
    CREATE UNIQUE INDEX UX_tblContactPostal_CityCode ON dbo.tblContactPostal (CityID, PostalCode) WHERE IsActive = 1;
END

IF OBJECT_ID(N'dbo.tblContactStreet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactStreet (
        StreetID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactStreet PRIMARY KEY,
        PostalID INT NOT NULL,
        StreetName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblContactStreet_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactStreet_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL,
        CONSTRAINT FK_tblContactStreet_Postal FOREIGN KEY (PostalID) REFERENCES dbo.tblContactPostal(PostalID)
    );
    CREATE UNIQUE INDEX UX_tblContactStreet_Name ON dbo.tblContactStreet (PostalID, StreetName) WHERE IsActive = 1;
END

IF OBJECT_ID(N'dbo.tblContactAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblContactAddress (
        AddressID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblContactAddress PRIMARY KEY,
        ContactID INT NOT NULL,
        CountryID INT NULL,
        StateID INT NULL,
        CityID INT NULL,
        PostalID INT NULL,
        StreetID INT NULL,
        AddressLine NVARCHAR(500) NULL,
        Latitude DECIMAL(10,7) NULL,
        Longitude DECIMAL(11,7) NULL,
        IsBilling BIT NOT NULL CONSTRAINT DF_tblContactAddress_Billing DEFAULT (0),
        IsShipping BIT NOT NULL CONSTRAINT DF_tblContactAddress_Shipping DEFAULT (0),
        IsPrimary BIT NOT NULL CONSTRAINT DF_tblContactAddress_Primary DEFAULT (0),
        SortOrder INT NOT NULL CONSTRAINT DF_tblContactAddress_Sort DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblContactAddress_Created DEFAULT (GETDATE()),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END
";

        private static void Exec(SqlConnection conn, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 60;
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecIgnore(SqlConnection conn, string sql)
        {
            try { Exec(conn, sql); }
            catch { }
        }

        public List<GeoLookupItem> ListCountries(bool activeOnly) =>
            LoadLookups("SELECT CountryID, CountryName, 0 FROM tblContactCountry WHERE (@A=0 OR IsActive=1) ORDER BY CountryName;", 0, activeOnly);

        public List<GeoLookupItem> ListStates(int countryId, bool activeOnly) =>
            LoadLookups("SELECT StateID, StateName, CountryID FROM tblContactState WHERE CountryID=@P AND (@A=0 OR IsActive=1) ORDER BY StateName;", countryId, activeOnly);

        public List<GeoLookupItem> ListCities(int stateId, bool activeOnly) =>
            LoadLookups("SELECT CityID, CityName, StateID FROM tblContactCity WHERE StateID=@P AND (@A=0 OR IsActive=1) ORDER BY CityName;", stateId, activeOnly);

        public List<GeoLookupItem> ListPostals(int cityId, bool activeOnly) =>
            LoadLookups(@"
SELECT PostalID,
       CASE WHEN ISNULL(AreaName,'') = '' THEN PostalCode ELSE PostalCode + N' - ' + AreaName END,
       CityID
FROM tblContactPostal
WHERE CityID=@P AND (@A=0 OR IsActive=1)
ORDER BY PostalCode;", cityId, activeOnly);

        public List<GeoLookupItem> ListStreets(int postalId, bool activeOnly) =>
            LoadLookups("SELECT StreetID, StreetName, PostalID FROM tblContactStreet WHERE PostalID=@P AND (@A=0 OR IsActive=1) ORDER BY StreetName;", postalId, activeOnly);

        public List<ContactCountryItem> ListCountryRecords(bool activeOnly)
        {
            EnsureSchema();
            var list = new List<ContactCountryItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT CountryID, CountryName, IsActive FROM tblContactCountry WHERE (@A=0 OR IsActive=1) ORDER BY IsActive DESC, CountryName;", conn))
            {
                cmd.Parameters.AddWithValue("@A", activeOnly ? 1 : 0);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactCountryItem { CountryID = dr.GetInt32(0), CountryName = dr.GetString(1), IsActive = dr.GetBoolean(2) });
            }
            return list;
        }

        public ContactCountryItem GetCountry(int id)
        {
            foreach (var row in ListCountryRecords(false))
                if (row.CountryID == id) return row;
            return null;
        }

        public string SaveCountry(ContactCountryItem item, int? userId)
        {
            if (string.IsNullOrWhiteSpace(item.CountryName)) return "Country name is required.";
            return ExecSave(item.CountryID,
                "UPDATE tblContactCountry SET CountryName=@Name, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE CountryID=@ID;",
                "INSERT INTO tblContactCountry (CountryName, IsActive, CreatedOn, CreatedByUserID) VALUES (@Name, @IsActive, GETDATE(), @CreatedByUserID); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cmd => { cmd.Parameters.AddWithValue("@Name", item.CountryName.Trim()); cmd.Parameters.AddWithValue("@IsActive", item.IsActive); },
                id => item.CountryID = id, userId, "This country already exists.");
        }

        public void DeactivateCountry(int id, int? userId) => Deactivate("tblContactCountry", "CountryID", id, userId);

        public List<ContactStateItem> ListStateRecords()
        {
            EnsureSchema();
            var list = new List<ContactStateItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT s.StateID, s.CountryID, c.CountryName, s.StateName, s.IsActive
FROM tblContactState s INNER JOIN tblContactCountry c ON c.CountryID=s.CountryID
ORDER BY s.IsActive DESC, c.CountryName, s.StateName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactStateItem
                        {
                            StateID = dr.GetInt32(0),
                            CountryID = dr.GetInt32(1),
                            CountryName = dr.GetString(2),
                            StateName = dr.GetString(3),
                            IsActive = dr.GetBoolean(4)
                        });
            }
            return list;
        }

        public ContactStateItem GetState(int id)
        {
            foreach (var row in ListStateRecords())
                if (row.StateID == id) return row;
            return null;
        }

        public string SaveState(ContactStateItem item, int? userId)
        {
            if (item.CountryID <= 0) return "Country is required.";
            if (string.IsNullOrWhiteSpace(item.StateName)) return "State name is required.";
            return ExecSave(item.StateID,
                "UPDATE tblContactState SET CountryID=@Parent, StateName=@Name, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE StateID=@ID;",
                "INSERT INTO tblContactState (CountryID, StateName, IsActive, CreatedOn, CreatedByUserID) VALUES (@Parent, @Name, @IsActive, GETDATE(), @CreatedByUserID); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Parent", item.CountryID);
                    cmd.Parameters.AddWithValue("@Name", item.StateName.Trim());
                    cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
                },
                id => item.StateID = id, userId, "This state already exists for the selected country.");
        }

        public void DeactivateState(int id, int? userId) => Deactivate("tblContactState", "StateID", id, userId);

        public List<ContactCityItem> ListCityRecords()
        {
            EnsureSchema();
            var list = new List<ContactCityItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT ci.CityID, ci.StateID, s.StateName, c.CountryName, ci.CityName, ci.IsActive
FROM tblContactCity ci
INNER JOIN tblContactState s ON s.StateID=ci.StateID
INNER JOIN tblContactCountry c ON c.CountryID=s.CountryID
ORDER BY ci.IsActive DESC, c.CountryName, s.StateName, ci.CityName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactCityItem
                        {
                            CityID = dr.GetInt32(0),
                            StateID = dr.GetInt32(1),
                            StateName = dr.GetString(2),
                            CountryName = dr.GetString(3),
                            CityName = dr.GetString(4),
                            IsActive = dr.GetBoolean(5)
                        });
            }
            return list;
        }

        public ContactCityItem GetCity(int id)
        {
            foreach (var row in ListCityRecords())
                if (row.CityID == id) return row;
            return null;
        }

        public string SaveCity(ContactCityItem item, int? userId)
        {
            if (item.StateID <= 0) return "State is required.";
            if (string.IsNullOrWhiteSpace(item.CityName)) return "City name is required.";
            return ExecSave(item.CityID,
                "UPDATE tblContactCity SET StateID=@Parent, CityName=@Name, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE CityID=@ID;",
                "INSERT INTO tblContactCity (StateID, CityName, IsActive, CreatedOn, CreatedByUserID) VALUES (@Parent, @Name, @IsActive, GETDATE(), @CreatedByUserID); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Parent", item.StateID);
                    cmd.Parameters.AddWithValue("@Name", item.CityName.Trim());
                    cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
                },
                id => item.CityID = id, userId, "This city already exists for the selected state.");
        }

        public void DeactivateCity(int id, int? userId) => Deactivate("tblContactCity", "CityID", id, userId);

        public List<ContactPostalItem> ListPostalRecords()
        {
            EnsureSchema();
            var list = new List<ContactPostalItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT p.PostalID, ISNULL(p.CityID,0), ISNULL(p.StateID,0), ISNULL(ci.CityName,''), ISNULL(s.StateName,''), ISNULL(c.CountryName,''), p.PostalCode, ISNULL(p.AreaName,''), p.IsActive
FROM tblContactPostal p
LEFT JOIN tblContactCity ci ON ci.CityID=p.CityID
LEFT JOIN tblContactState s ON s.StateID=ISNULL(ci.StateID, p.StateID)
LEFT JOIN tblContactCountry c ON c.CountryID=s.CountryID
ORDER BY p.IsActive DESC, c.CountryName, s.StateName, ci.CityName, p.PostalCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactPostalItem
                        {
                            PostalID = dr.GetInt32(0),
                            CityID = dr.GetInt32(1),
                            StateID = dr.GetInt32(2),
                            CityName = dr.GetString(3),
                            StateName = dr.GetString(4),
                            CountryName = dr.GetString(5),
                            PostalCode = dr.GetString(6),
                            AreaName = dr.GetString(7),
                            IsActive = dr.GetBoolean(8)
                        });
            }
            return list;
        }

        public ContactPostalItem GetPostal(int id)
        {
            foreach (var row in ListPostalRecords())
                if (row.PostalID == id) return row;
            return null;
        }

        public string SavePostal(ContactPostalItem item, int? userId)
        {
            if (item.CityID <= 0) return "City is required.";
            if (string.IsNullOrWhiteSpace(item.PostalCode)) return "Postal code is required.";
            var city = GetCity(item.CityID);
            if (city == null) return "Select a valid city from City Setup.";
            item.StateID = city.StateID;
            var area = string.IsNullOrWhiteSpace(item.AreaName) ? (object)DBNull.Value : item.AreaName.Trim();
            return ExecSave(item.PostalID,
                "UPDATE tblContactPostal SET CityID=@CityID, StateID=@StateID, PostalCode=@Name, AreaName=@AreaName, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE PostalID=@ID;",
                "INSERT INTO tblContactPostal (CityID, StateID, PostalCode, AreaName, IsActive, CreatedOn, CreatedByUserID) VALUES (@CityID, @StateID, @Name, @AreaName, @IsActive, GETDATE(), @CreatedByUserID); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@CityID", item.CityID);
                    cmd.Parameters.AddWithValue("@StateID", item.StateID);
                    cmd.Parameters.AddWithValue("@Name", item.PostalCode.Trim());
                    cmd.Parameters.AddWithValue("@AreaName", area);
                    cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
                },
                id => item.PostalID = id, userId, "This postal code already exists for the selected city.");
        }

        public void DeactivatePostal(int id, int? userId) => Deactivate("tblContactPostal", "PostalID", id, userId);

        public List<ContactStreetItem> ListStreetRecords()
        {
            EnsureSchema();
            var list = new List<ContactStreetItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT st.StreetID, st.PostalID, p.PostalCode, ISNULL(ci.CityName,''), ISNULL(s.StateName,''), st.StreetName, st.IsActive
FROM tblContactStreet st
INNER JOIN tblContactPostal p ON p.PostalID=st.PostalID
LEFT JOIN tblContactCity ci ON ci.CityID=p.CityID
LEFT JOIN tblContactState s ON s.StateID=ISNULL(ci.StateID, p.StateID)
ORDER BY st.IsActive DESC, s.StateName, ci.CityName, p.PostalCode, st.StreetName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactStreetItem
                        {
                            StreetID = dr.GetInt32(0),
                            PostalID = dr.GetInt32(1),
                            PostalCode = dr.GetString(2),
                            CityName = dr.GetString(3),
                            StateName = dr.GetString(4),
                            StreetName = dr.GetString(5),
                            IsActive = dr.GetBoolean(6)
                        });
            }
            return list;
        }

        public ContactStreetItem GetStreet(int id)
        {
            foreach (var row in ListStreetRecords())
                if (row.StreetID == id) return row;
            return null;
        }

        public string SaveStreet(ContactStreetItem item, int? userId)
        {
            if (item.PostalID <= 0) return "Postal code is required.";
            if (string.IsNullOrWhiteSpace(item.StreetName)) return "Street is required.";
            return ExecSave(item.StreetID,
                "UPDATE tblContactStreet SET PostalID=@Parent, StreetName=@Name, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE StreetID=@ID;",
                "INSERT INTO tblContactStreet (PostalID, StreetName, IsActive, CreatedOn, CreatedByUserID) VALUES (@Parent, @Name, @IsActive, GETDATE(), @CreatedByUserID); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Parent", item.PostalID);
                    cmd.Parameters.AddWithValue("@Name", item.StreetName.Trim());
                    cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
                },
                id => item.StreetID = id, userId, "This street already exists for the selected postal code.");
        }

        public void DeactivateStreet(int id, int? userId) => Deactivate("tblContactStreet", "StreetID", id, userId);

        public List<ContactAddressItem> ListAddresses(int contactId)
        {
            EnsureSchema();
            var list = new List<ContactAddressItem>();
            if (contactId <= 0) return list;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT a.AddressID, a.ContactID,
       ISNULL(a.CountryID,0), ISNULL(a.StateID,0), ISNULL(a.CityID,0), ISNULL(a.PostalID,0), ISNULL(a.StreetID,0),
       ISNULL(a.AddressLine,''), a.IsBilling, a.IsShipping, ISNULL(a.IsPrimary,0),
       a.Latitude, a.Longitude,
       ISNULL(c.CountryName,''), ISNULL(s.StateName,''), ISNULL(ci.CityName,''), ISNULL(p.PostalCode,''), ISNULL(st.StreetName,''),
       ISNULL(a.WorkflowStatus,N'Approved'), ISNULL(a.VerificationStatus,N'Verified')
FROM tblContactAddress a
LEFT JOIN tblContactCountry c ON c.CountryID=a.CountryID
LEFT JOIN tblContactState s ON s.StateID=a.StateID
LEFT JOIN tblContactCity ci ON ci.CityID=a.CityID
LEFT JOIN tblContactPostal p ON p.PostalID=a.PostalID
LEFT JOIN tblContactStreet st ON st.StreetID=a.StreetID
WHERE a.ContactID=@ContactID
ORDER BY a.SortOrder, a.AddressID;", conn))
            {
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(ReadAddress(dr));
            }
            return list;
        }

        public string SaveAddress(ContactAddressItem item, int? userId)
        {
            return "Contact addresses must be submitted through the approval workflow.";
        }

        public string ValidateAddress(ContactAddressItem item)
        {
            EnsureSchema();
            if (item == null || item.ContactID <= 0) return "Save the contact first, then add addresses.";
            if (item.CountryID <= 0) return "Country is required.";
            if (item.StateID <= 0) return "State is required.";
            if (item.CityID <= 0) return "City is required.";
            if (item.PostalID <= 0) return "Postal code is required.";
            if (item.StreetID <= 0) return "Street is required.";
            if ((item.Latitude.HasValue && !item.Longitude.HasValue) || (!item.Latitude.HasValue && item.Longitude.HasValue))
                return "Enter both latitude and longitude, or leave both blank.";
            if (item.Latitude.HasValue && (item.Latitude < -90 || item.Latitude > 90))
                return "Latitude must be between -90 and 90.";
            if (item.Longitude.HasValue && (item.Longitude < -180 || item.Longitude > 180))
                return "Longitude must be between -180 and 180.";
            if (!HierarchyMatches(item, out var hierarchyError)) return hierarchyError;
            return null;
        }

        public ContactAddressItem GetAddress(int addressId)
        {
            EnsureSchema();
            if (addressId <= 0) return null;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT a.AddressID, a.ContactID,
       ISNULL(a.CountryID,0), ISNULL(a.StateID,0), ISNULL(a.CityID,0), ISNULL(a.PostalID,0), ISNULL(a.StreetID,0),
       ISNULL(a.AddressLine,''), a.IsBilling, a.IsShipping, ISNULL(a.IsPrimary,0),
       a.Latitude, a.Longitude,
       ISNULL(c.CountryName,''), ISNULL(s.StateName,''), ISNULL(ci.CityName,''), ISNULL(p.PostalCode,''), ISNULL(st.StreetName,''),
       ISNULL(a.WorkflowStatus,N'Approved'), ISNULL(a.VerificationStatus,N'Verified')
FROM tblContactAddress a
LEFT JOIN tblContactCountry c ON c.CountryID=a.CountryID
LEFT JOIN tblContactState s ON s.StateID=a.StateID
LEFT JOIN tblContactCity ci ON ci.CityID=a.CityID
LEFT JOIN tblContactPostal p ON p.PostalID=a.PostalID
LEFT JOIN tblContactStreet st ON st.StreetID=a.StreetID
WHERE a.AddressID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", addressId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read()) return ReadAddress(dr);
            }
            return null;
        }

        public void FillLabels(ContactAddressItem item)
        {
            if (item == null) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                item.CountryName = LookupName(conn, "SELECT ISNULL(CountryName,'') FROM tblContactCountry WHERE CountryID=@Id", item.CountryID);
                item.StateName = LookupName(conn, "SELECT ISNULL(StateName,'') FROM tblContactState WHERE StateID=@Id", item.StateID);
                item.CityName = LookupName(conn, "SELECT ISNULL(CityName,'') FROM tblContactCity WHERE CityID=@Id", item.CityID);
                item.PostalCode = LookupName(conn, "SELECT ISNULL(PostalCode,'') FROM tblContactPostal WHERE PostalID=@Id", item.PostalID);
                item.StreetName = LookupName(conn, "SELECT ISNULL(StreetName,'') FROM tblContactStreet WHERE StreetID=@Id", item.StreetID);
            }
        }

        public void InsertAddress(SqlConnection conn, ContactAddressItem item, int? userId)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblContactAddress
 (ContactID, CountryID, StateID, CityID, PostalID, StreetID, AddressLine, Latitude, Longitude, IsBilling, IsShipping, IsPrimary,
  WorkflowStatus, VerificationStatus, SortOrder, CreatedOn, CreatedByUserID)
VALUES
 (@ContactID, @CountryID, @StateID, @CityID, @PostalID, @StreetID, @AddressLine, @Latitude, @Longitude, @IsBilling, @IsShipping, @IsPrimary,
  @WorkflowStatus, @VerificationStatus,
  (SELECT ISNULL(MAX(SortOrder),0)+1 FROM tblContactAddress WHERE ContactID=@ContactID),
  GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindAddress(cmd, item);
                AuditHelper.AddCreatedBy(cmd, userId);
                item.AddressID = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateAddressLive(SqlConnection conn, SqlTransaction tx, ContactAddressItem item, int? userId)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblContactAddress SET
  CountryID=@CountryID, StateID=@StateID, CityID=@CityID, PostalID=@PostalID, StreetID=@StreetID,
  AddressLine=@AddressLine, Latitude=@Latitude, Longitude=@Longitude,
  IsBilling=@IsBilling, IsShipping=@IsShipping, IsPrimary=@IsPrimary,
  WorkflowStatus=@WorkflowStatus, VerificationStatus=@VerificationStatus,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE AddressID=@AddressID AND ContactID=@ContactID;", conn, tx))
            {
                BindAddress(cmd, item);
                cmd.Parameters.AddWithValue("@AddressID", item.AddressID);
                AuditHelper.AddModifiedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void SetWorkflowState(SqlConnection conn, SqlTransaction tx, int addressId, string workflowStatus, string verificationStatus)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblContactAddress SET WorkflowStatus=@S, VerificationStatus=@V, ModifiedOn=GETDATE()
WHERE AddressID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@S", workflowStatus ?? ContactWorkflowStatuses.InProcess);
                cmd.Parameters.AddWithValue("@V", verificationStatus ?? ContactVerification.ToVerify);
                cmd.Parameters.AddWithValue("@Id", addressId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAddress(int addressId, int contactId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                DeleteAddress(conn, null, addressId, contactId);
            }
        }

        public void DeleteAddress(SqlConnection conn, SqlTransaction tx, int addressId, int contactId)
        {
            using (var cmd = new SqlCommand("DELETE FROM tblContactAddress WHERE AddressID=@AddressID AND ContactID=@ContactID;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@AddressID", addressId);
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string LookupName(SqlConnection conn, string sql, int id)
        {
            if (id <= 0) return "";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                var v = cmd.ExecuteScalar();
                return v == null || v == DBNull.Value ? "" : v.ToString();
            }
        }

        private bool HierarchyMatches(ContactAddressItem item, out string error)
        {
            error = null;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 1
FROM tblContactStreet st
INNER JOIN tblContactPostal p ON p.PostalID=st.PostalID
INNER JOIN tblContactCity ci ON ci.CityID=p.CityID
INNER JOIN tblContactState s ON s.StateID=ci.StateID
WHERE st.StreetID=@StreetID AND p.PostalID=@PostalID AND p.CityID=@CityID
  AND ci.CityID=@CityID AND s.StateID=@StateID AND s.CountryID=@CountryID;", conn))
            {
                cmd.Parameters.AddWithValue("@StreetID", item.StreetID);
                cmd.Parameters.AddWithValue("@PostalID", item.PostalID);
                cmd.Parameters.AddWithValue("@CityID", item.CityID);
                cmd.Parameters.AddWithValue("@StateID", item.StateID);
                cmd.Parameters.AddWithValue("@CountryID", item.CountryID);
                conn.Open();
                if (cmd.ExecuteScalar() == null)
                {
                    error = "Country, state, city, postal code, and street do not match. Please recorrect the lookups.";
                    return false;
                }
            }
            return true;
        }

        private List<GeoLookupItem> LoadLookups(string sql, int parentId, bool activeOnly)
        {
            EnsureSchema();
            var list = new List<GeoLookupItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (sql.Contains("@P")) cmd.Parameters.AddWithValue("@P", parentId);
                cmd.Parameters.AddWithValue("@A", activeOnly ? 1 : 0);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new GeoLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1), ParentId = dr.GetInt32(2) });
            }
            return list;
        }

        private string ExecSave(int id, string updateSql, string insertSql, Action<SqlCommand> bind, Action<int> setId, int? userId, string duplicateMessage)
        {
            EnsureSchema();
            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    if (id > 0)
                    {
                        using (var cmd = new SqlCommand(updateSql, conn))
                        {
                            bind(cmd);
                            cmd.Parameters.AddWithValue("@ID", id);
                            AuditHelper.AddModifiedBy(cmd, userId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(insertSql, conn))
                        {
                            bind(cmd);
                            AuditHelper.AddCreatedBy(cmd, userId);
                            setId((int)cmd.ExecuteScalar());
                        }
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                return duplicateMessage;
            }
            return null;
        }

        private void Deactivate(string table, string idColumn, int id, int? userId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand($@"
UPDATE {table} SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE {idColumn}=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                AuditHelper.AddModifiedBy(cmd, userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static ContactAddressItem ReadAddress(SqlDataReader dr)
        {
            return new ContactAddressItem
            {
                AddressID = dr.GetInt32(0),
                ContactID = dr.GetInt32(1),
                CountryID = dr.GetInt32(2),
                StateID = dr.GetInt32(3),
                CityID = dr.GetInt32(4),
                PostalID = dr.GetInt32(5),
                StreetID = dr.GetInt32(6),
                AddressLine = dr.GetString(7),
                IsBilling = dr.GetBoolean(8),
                IsShipping = dr.GetBoolean(9),
                IsPrimary = Convert.ToBoolean(dr.GetValue(10)),
                Latitude = dr.IsDBNull(11) ? (decimal?)null : dr.GetDecimal(11),
                Longitude = dr.IsDBNull(12) ? (decimal?)null : dr.GetDecimal(12),
                CountryName = dr.GetString(13),
                StateName = dr.GetString(14),
                CityName = dr.GetString(15),
                PostalCode = dr.GetString(16),
                StreetName = dr.GetString(17),
                WorkflowStatus = dr.FieldCount > 18 && !dr.IsDBNull(18) ? dr.GetString(18) : "Approved",
                VerificationStatus = dr.FieldCount > 19 && !dr.IsDBNull(19) ? dr.GetString(19) : "Verified"
            };
        }

        private static void BindAddress(SqlCommand cmd, ContactAddressItem item)
        {
            cmd.Parameters.AddWithValue("@ContactID", item.ContactID);
            cmd.Parameters.AddWithValue("@CountryID", item.CountryID);
            cmd.Parameters.AddWithValue("@StateID", item.StateID);
            cmd.Parameters.AddWithValue("@CityID", item.CityID);
            cmd.Parameters.AddWithValue("@PostalID", item.PostalID);
            cmd.Parameters.AddWithValue("@StreetID", item.StreetID);
            cmd.Parameters.AddWithValue("@AddressLine", string.IsNullOrWhiteSpace(item.AddressLine) ? (object)DBNull.Value : item.AddressLine.Trim());
            cmd.Parameters.AddWithValue("@Latitude", item.Latitude.HasValue ? (object)item.Latitude.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Longitude", item.Longitude.HasValue ? (object)item.Longitude.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@IsBilling", item.IsBilling);
            cmd.Parameters.AddWithValue("@IsShipping", item.IsShipping);
            cmd.Parameters.AddWithValue("@IsPrimary", item.IsPrimary);
            cmd.Parameters.AddWithValue("@WorkflowStatus", string.IsNullOrWhiteSpace(item.WorkflowStatus) ? ContactWorkflowStatuses.InProcess : item.WorkflowStatus);
            cmd.Parameters.AddWithValue("@VerificationStatus", string.IsNullOrWhiteSpace(item.VerificationStatus) ? ContactVerification.ToVerify : item.VerificationStatus);
        }
    }
}
