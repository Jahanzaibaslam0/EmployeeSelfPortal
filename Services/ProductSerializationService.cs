using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HRMS.Services
{
    public static class ProductSerialStatuses
    {
        public const string Draft = "Draft";
        public const string InProcess = "In Process";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static readonly string[] All = { Draft, InProcess, Approved, Rejected };

        public static bool IsKnown(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            foreach (var s in All)
                if (string.Equals(s, status, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case Approved: return "badge badge-success";
                case InProcess: return "badge badge-info";
                case Rejected: return "badge badge-danger";
                default: return "badge";
            }
        }
    }

    public class SerializationProductLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Gtin { get; set; } = "";
    }

    public class ProductSerialItem
    {
        public int ProductSerialID { get; set; }
        public string GenerationKey { get; set; } = "";
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int ProductBatchID { get; set; }
        public string BatchNumber { get; set; } = "";
        public string GtinNumber { get; set; } = "";
        public string Hexacode { get; set; } = "";
        public int CodeLength { get; set; }
        public string Status { get; set; } = ProductSerialStatuses.Draft;
        public string Remarks { get; set; } = "";
        public DateTime GeneratedOn { get; set; }
        public string CreatedBy { get; set; } = "";
        public string Gs1Hint { get; set; } = "";
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Gs1Payload { get; set; } = "";
        public string Gs1HumanReadable { get; set; } = "";
        public string StatusBadgeClass => ProductSerialStatuses.BadgeClass(Status);
    }

    public class ProductSerialStatusHistoryItem
    {
        public DateTime ChangedOn { get; set; }
        public string OldStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public string ChangedBy { get; set; } = "";
        public string Remarks { get; set; } = "";
    }

    public class ProductSerialFilter
    {
        public int ProductID { get; set; }
        public int ProductBatchID { get; set; }
        public string Gtin { get; set; } = "";
        public string BatchNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string Search { get; set; } = "";
        public string GenerationKey { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ProductSerialListResult
    {
        public List<ProductSerialItem> Items { get; set; } = new List<ProductSerialItem>();
        public int TotalCount { get; set; }
    }

    public class ProductSerializationRunItem
    {
        public int SerializationRunID { get; set; }
        public string GenerationKey { get; set; } = "";
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string GtinNumber { get; set; } = "";
        public int ProductBatchID { get; set; }
        public string BatchNumber { get; set; } = "";
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int RequestedQuantity { get; set; }
        public int GeneratedQuantity { get; set; }
        public DateTime GeneratedOn { get; set; }
        public string GeneratedBy { get; set; } = "";
        public string GenerationStatus { get; set; } = "Completed";
        public string StatusBadgeClass =>
            string.Equals(GenerationStatus, "Completed", StringComparison.OrdinalIgnoreCase)
                ? "badge badge-success"
                : "badge badge-info";
    }

    public class ProductSerializationRunFilter
    {
        public string GenerationKey { get; set; } = "";
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Gtin { get; set; } = "";
        public string BatchNumber { get; set; } = "";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ProductSerializationRunListResult
    {
        public List<ProductSerializationRunItem> Items { get; set; } = new List<ProductSerializationRunItem>();
        public int TotalCount { get; set; }
    }

    public class ProductSerializationService
    {
        public const string FormKey = "ProductSerialization";
        public const int MinCodeLength = 14;
        public const int MaxCodeLength = 20;
        public const int MaxGenerateQty = 2000;
        private static readonly Regex GtinPattern = new Regex(@"^\d{8}$|^\d{12,14}$", RegexOptions.Compiled);
        private static readonly char[] HexAlphabet = "0123456789ABCDEF".ToCharArray();

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly ProductBatchService _batches = new ProductBatchService();

        public ProductSerializationService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            _batches.EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF COL_LENGTH(N'tblProduct', N'GTINNumber') IS NULL
    ALTER TABLE tblProduct ADD GTINNumber NVARCHAR(14) NULL;
IF COL_LENGTH(N'tblProduct', N'SellingPrice') IS NULL
    ALTER TABLE tblProduct ADD SellingPrice DECIMAL(18,4) NULL;

IF OBJECT_ID(N'tblProductSerializationRun', N'U') IS NULL
BEGIN
    CREATE TABLE tblProductSerializationRun (
        SerializationRunID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GenerationKey NVARCHAR(40) NOT NULL,
        ProductID INT NOT NULL,
        ProductBatchID INT NOT NULL,
        GTINNumber NVARCHAR(14) NOT NULL,
        Quantity INT NOT NULL,
        CodeLength INT NOT NULL,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerializationRun_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL
    );
    CREATE UNIQUE INDEX UX_tblProductSerializationRun_Key ON tblProductSerializationRun (GenerationKey);
END;

IF COL_LENGTH(N'tblProductSerializationRun', N'ProductName') IS NULL
    ALTER TABLE tblProductSerializationRun ADD ProductName NVARCHAR(200) NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'BatchNumber') IS NULL
    ALTER TABLE tblProductSerializationRun ADD BatchNumber NVARCHAR(50) NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'ManufacturingDate') IS NULL
    ALTER TABLE tblProductSerializationRun ADD ManufacturingDate DATE NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'ExpiryDate') IS NULL
    ALTER TABLE tblProductSerializationRun ADD ExpiryDate DATE NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'GeneratedQuantity') IS NULL
    ALTER TABLE tblProductSerializationRun ADD GeneratedQuantity INT NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'GenerationStatus') IS NULL
    ALTER TABLE tblProductSerializationRun ADD GenerationStatus NVARCHAR(20) NULL;
IF COL_LENGTH(N'tblProductSerializationRun', N'GeneratedByName') IS NULL
    ALTER TABLE tblProductSerializationRun ADD GeneratedByName NVARCHAR(150) NULL;

IF COL_LENGTH(N'tblProductSerial', N'Gs1Payload') IS NULL
    ALTER TABLE tblProductSerial ADD Gs1Payload NVARCHAR(400) NULL;
IF COL_LENGTH(N'tblProductSerial', N'Gs1HumanReadable') IS NULL
    ALTER TABLE tblProductSerial ADD Gs1HumanReadable NVARCHAR(400) NULL;

IF OBJECT_ID(N'tblProductSerial', N'U') IS NULL
BEGIN
    CREATE TABLE tblProductSerial (
        ProductSerialID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SerializationRunID INT NOT NULL,
        GenerationKey NVARCHAR(40) NOT NULL,
        ProductID INT NOT NULL,
        ProductBatchID INT NOT NULL,
        GTINNumber NVARCHAR(14) NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        Hexacode NVARCHAR(20) NOT NULL,
        CodeLength INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_tblProductSerial_Status DEFAULT N'Draft',
        Remarks NVARCHAR(500) NULL,
        Gs1ElementHint NVARCHAR(200) NULL,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerial_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL
    );
    CREATE UNIQUE INDEX UX_tblProductSerial_Hexacode ON tblProductSerial (Hexacode);
END;

IF OBJECT_ID(N'tblProductSerialStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE tblProductSerialStatusHistory (
        SerialStatusHistoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductSerialID INT NOT NULL,
        OldStatus NVARCHAR(20) NULL,
        NewStatus NVARCHAR(20) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        ChangedOn DATETIME NOT NULL CONSTRAINT DF_tblProductSerialHist_Changed DEFAULT GETDATE(),
        ChangedByUserID INT NULL
    );
    CREATE INDEX IX_tblProductSerialHist_Serial ON tblProductSerialStatusHistory (ProductSerialID, ChangedOn DESC);
END;
", conn))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<SerializationProductLookup> ListProducts()
        {
            EnsureSchema();
            var list = new List<SerializationProductLookup>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT ProductID,
       LTRIM(RTRIM(ISNULL(ProductCode,'') + N' - ' + ISNULL(ProductName,''))),
       ISNULL(GTINNumber,'')
FROM tblProduct
WHERE ISNULL(IsActive,1)=1
ORDER BY ProductCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(new SerializationProductLookup
                        {
                            Id = dr.GetInt32(0),
                            Name = dr.GetString(1),
                            Gtin = dr.IsDBNull(2) ? "" : dr.GetString(2).Trim()
                        });
                }
            }
            return list;
        }

        public SerializationProductLookup GetProduct(int productId)
        {
            if (productId <= 0) return null;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT ProductID,
       LTRIM(RTRIM(ISNULL(ProductCode,'') + N' - ' + ISNULL(ProductName,''))),
       ISNULL(GTINNumber,'')
FROM tblProduct WHERE ProductID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", productId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return new SerializationProductLookup
                    {
                        Id = dr.GetInt32(0),
                        Name = dr.GetString(1),
                        Gtin = dr.IsDBNull(2) ? "" : dr.GetString(2).Trim()
                    };
                }
            }
        }

        public ProductBatchItem GetBatchForProduct(int productId, int batchId)
        {
            if (productId <= 0 || batchId <= 0) return null;
            var batch = _batches.GetById(batchId);
            if (batch == null || batch.ProductID != productId || !batch.IsActive)
                return null;
            return batch;
        }

        public List<ProductBatchItem> ListBatches(int productId)
        {
            if (productId <= 0) return new List<ProductBatchItem>();
            return _batches.List(productId);
        }

        public string GenerateRun(int productId, int batchId, int quantity, int codeLength)
        {
            EnsureSchema();
            if (quantity <= 0 || quantity > MaxGenerateQty)
                throw new InvalidOperationException("Enter a quantity between 1 and " + MaxGenerateQty + ".");
            if (codeLength < MinCodeLength || codeLength > MaxCodeLength)
                throw new InvalidOperationException("Code length must be between " + MinCodeLength + " and " + MaxCodeLength + " characters.");

            var product = GetProduct(productId);
            if (product == null)
                throw new InvalidOperationException("Select a product from Product Master.");
            var gtin = NormalizeGtin(product.Gtin, required: true);
            var batch = GetBatchForProduct(productId, batchId);
            if (batch == null)
                throw new InvalidOperationException("Select a batch that belongs to the selected product.");
            if (!batch.ManufacturingDate.HasValue || !batch.ExpiryDate.HasValue)
                throw new InvalidOperationException("Set manufacturing date and expiry date on the batch before generating GS1 DataMatrix codes.");

            var userId = _auth.CurrentUserId;
            var generatedBy = (_auth.CurrentUsername ?? "").Trim();
            var productName = product.Name;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var generationKey = NextUniqueGenerationKey(conn, tx);
                    int runId;
                    using (var run = new SqlCommand(@"
INSERT INTO tblProductSerializationRun
    (GenerationKey, ProductID, ProductBatchID, GTINNumber, Quantity, CodeLength, CreatedOn, CreatedByUserID,
     ProductName, BatchNumber, ManufacturingDate, ExpiryDate, GeneratedQuantity, GenerationStatus, GeneratedByName)
VALUES
    (@Key, @ProductID, @BatchID, @Gtin, @Qty, @Len, GETDATE(), @CreatedByUserID,
     @ProductName, @Batch, @Mfg, @Exp, 0, N'In Process', @GeneratedBy);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                    {
                        run.Parameters.AddWithValue("@Key", generationKey);
                        run.Parameters.AddWithValue("@ProductID", productId);
                        run.Parameters.AddWithValue("@BatchID", batchId);
                        run.Parameters.AddWithValue("@Gtin", gtin);
                        run.Parameters.AddWithValue("@Qty", quantity);
                        run.Parameters.AddWithValue("@Len", codeLength);
                        run.Parameters.AddWithValue("@ProductName", (object)productName ?? DBNull.Value);
                        run.Parameters.AddWithValue("@Batch", batch.BatchNumber);
                        run.Parameters.AddWithValue("@Mfg", (object)batch.ManufacturingDate ?? DBNull.Value);
                        run.Parameters.AddWithValue("@Exp", (object)batch.ExpiryDate ?? DBNull.Value);
                        run.Parameters.AddWithValue("@GeneratedBy", string.IsNullOrEmpty(generatedBy) ? (object)DBNull.Value : generatedBy);
                        AuditHelper.AddCreatedBy(run, userId);
                        runId = Convert.ToInt32(run.ExecuteScalar());
                    }

                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var actual = 0;
                    for (var i = 0; i < quantity; i++)
                    {
                        var code = NextUniqueHexacode(conn, tx, used, codeLength);
                        used.Add(code);
                        var payload = Gs1DataMatrix.BuildElementString(gtin, batch.ManufacturingDate, batch.BatchNumber, batch.ExpiryDate, code);
                        var hri = Gs1DataMatrix.BuildHumanReadable(gtin, batch.ManufacturingDate, batch.BatchNumber, batch.ExpiryDate, code);
                        using (var ins = new SqlCommand(@"
INSERT INTO tblProductSerial
    (SerializationRunID, GenerationKey, ProductID, ProductBatchID, GTINNumber, BatchNumber,
     Hexacode, CodeLength, Status, Gs1ElementHint, Gs1Payload, Gs1HumanReadable, CreatedOn, CreatedByUserID)
VALUES
    (@RunID, @Key, @ProductID, @BatchID, @Gtin, @Batch,
     @Hex, @Len, N'Draft', @Hint, @Payload, @Hri, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@RunID", runId);
                            ins.Parameters.AddWithValue("@Key", generationKey);
                            ins.Parameters.AddWithValue("@ProductID", productId);
                            ins.Parameters.AddWithValue("@BatchID", batchId);
                            ins.Parameters.AddWithValue("@Gtin", gtin);
                            ins.Parameters.AddWithValue("@Batch", batch.BatchNumber);
                            ins.Parameters.AddWithValue("@Hex", code);
                            ins.Parameters.AddWithValue("@Len", code.Length);
                            ins.Parameters.AddWithValue("@Hint", hri.Replace("\n", " "));
                            ins.Parameters.AddWithValue("@Payload", payload);
                            ins.Parameters.AddWithValue("@Hri", hri);
                            AuditHelper.AddCreatedBy(ins, userId);
                            var serialId = Convert.ToInt32(ins.ExecuteScalar());
                            WriteHistory(conn, tx, serialId, null, ProductSerialStatuses.Draft, "Generated " + generationKey, userId);
                        }
                        actual++;
                    }

                    var status = actual == quantity ? "Completed" : "Partial";
                    using (var fin = new SqlCommand(@"
UPDATE tblProductSerializationRun
SET GeneratedQuantity=@Actual, GenerationStatus=@Status
WHERE SerializationRunID=@Id;", conn, tx))
                    {
                        fin.Parameters.AddWithValue("@Actual", actual);
                        fin.Parameters.AddWithValue("@Status", status);
                        fin.Parameters.AddWithValue("@Id", runId);
                        fin.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return generationKey;
                }
            }
        }

        public ProductSerializationRunListResult ListRuns(ProductSerializationRunFilter filter)
        {
            EnsureSchema();
            filter = filter ?? new ProductSerializationRunFilter();
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 200) filter.PageSize = 20;
            var result = new ProductSerializationRunListResult();
            var offset = (filter.Page - 1) * filter.PageSize;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var count = new SqlCommand(ListRunsSql(true), conn))
                {
                    BindRunFilter(count, filter);
                    result.TotalCount = Convert.ToInt32(count.ExecuteScalar());
                }
                using (var cmd = new SqlCommand(ListRunsSql(false), conn))
                {
                    BindRunFilter(cmd, filter);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@Take", filter.PageSize);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            result.Items.Add(ReadRun(dr));
                    }
                }
            }
            return result;
        }

        public ProductSerializationRunItem GetRunByKey(string generationKey)
        {
            if (string.IsNullOrWhiteSpace(generationKey)) return null;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(RunSelectSql() + " WHERE r.GenerationKey=@Key;", conn))
            {
                cmd.Parameters.AddWithValue("@Key", generationKey.Trim());
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return ReadRun(dr);
                }
            }
        }

        public void UpdateStatus(int serialId, string newStatus, string remarks, bool canApprove, bool canWrite)
        {
            EnsureSchema();
            if (serialId <= 0) throw new InvalidOperationException("Select a serialization record.");
            newStatus = (newStatus ?? "").Trim();
            if (!ProductSerialStatuses.IsKnown(newStatus))
                throw new InvalidOperationException("Select a valid status.");
            remarks = (remarks ?? "").Trim();
            if (remarks.Length > 500) remarks = remarks.Substring(0, 500);

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                string current;
                using (var get = new SqlCommand("SELECT Status FROM tblProductSerial WHERE ProductSerialID=@Id;", conn))
                {
                    get.Parameters.AddWithValue("@Id", serialId);
                    var raw = get.ExecuteScalar();
                    if (raw == null || raw == DBNull.Value)
                        throw new InvalidOperationException("Serialization record was not found.");
                    current = Convert.ToString(raw);
                }

                if (string.Equals(current, newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    using (var rem = new SqlCommand(@"
UPDATE tblProductSerial SET Remarks=@Remarks WHERE ProductSerialID=@Id;", conn))
                    {
                        rem.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                        rem.Parameters.AddWithValue("@Id", serialId);
                        rem.ExecuteNonQuery();
                    }
                    return;
                }

                AssertStatusChange(current, newStatus, canApprove, canWrite);

                using (var tx = conn.BeginTransaction())
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblProductSerial SET Status=@Status, Remarks=@Remarks WHERE ProductSerialID=@Id;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@Status", newStatus);
                        upd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                        upd.Parameters.AddWithValue("@Id", serialId);
                        upd.ExecuteNonQuery();
                    }
                    WriteHistory(conn, tx, serialId, current, newStatus, remarks, _auth.CurrentUserId);
                    tx.Commit();
                }
            }
        }

        public ProductSerialListResult List(ProductSerialFilter filter)
        {
            EnsureSchema();
            filter = filter ?? new ProductSerialFilter();
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1) filter.PageSize = 50;
            if (filter.PageSize > 10000) filter.PageSize = 10000;
            var result = new ProductSerialListResult();
            var offset = (filter.Page - 1) * filter.PageSize;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var count = new SqlCommand(ListSql(true), conn))
                {
                    BindFilter(count, filter);
                    result.TotalCount = Convert.ToInt32(count.ExecuteScalar());
                }
                using (var cmd = new SqlCommand(ListSql(false), conn))
                {
                    BindFilter(cmd, filter);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@Take", filter.PageSize);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            result.Items.Add(ReadItem(dr));
                    }
                }
            }
            return result;
        }

        public List<ProductSerialItem> ListAll(ProductSerialFilter filter)
        {
            EnsureSchema();
            filter = filter ?? new ProductSerialFilter();
            var list = new List<ProductSerialItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(ListSql(false, paged: false), conn))
            {
                BindFilter(cmd, filter);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(ReadItem(dr));
                }
            }
            return list;
        }

        public ProductSerialItem GetSerialById(int id)
        {
            if (id <= 0) return null;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT s.ProductSerialID, s.GenerationKey, s.ProductID,
       ISNULL(p.ProductCode,''), ISNULL(p.ProductName,''),
       s.ProductBatchID, s.BatchNumber, s.GTINNumber, s.Hexacode, s.CodeLength,
       s.Status, ISNULL(s.Remarks,''), s.CreatedOn,
       ISNULL(NULLIF(LTRIM(RTRIM(u.FullName)), ''), ISNULL(u.Username,'')),
       ISNULL(s.Gs1ElementHint,''),
       r.ManufacturingDate, r.ExpiryDate,
       ISNULL(s.Gs1Payload,''), ISNULL(s.Gs1HumanReadable,'')
FROM tblProductSerial s
INNER JOIN tblProduct p ON p.ProductID = s.ProductID
LEFT JOIN tblUser u ON u.UserID = s.CreatedByUserID
LEFT JOIN tblProductSerializationRun r ON r.SerializationRunID = s.SerializationRunID
WHERE s.ProductSerialID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    var item = ReadItem(dr);
                    EnsureGs1Fields(item);
                    return item;
                }
            }
        }

        public byte[] RenderDataMatrixPng(ProductSerialItem item, int modulePx = 6, bool withHri = true)
        {
            if (item == null) throw new InvalidOperationException("Serialization record was not found.");
            EnsureGs1Fields(item);
            return Gs1DataMatrix.RenderPng(item.Gs1Payload, modulePx, withHri, item.Gs1HumanReadable);
        }

        public void EnsureGs1Fields(ProductSerialItem item)
        {
            if (item == null) return;
            if (!string.IsNullOrEmpty(item.Gs1Payload) && !string.IsNullOrEmpty(item.Gs1HumanReadable))
                return;
            item.Gs1Payload = Gs1DataMatrix.BuildElementString(
                item.GtinNumber, item.ManufacturingDate, item.BatchNumber, item.ExpiryDate, item.Hexacode);
            item.Gs1HumanReadable = Gs1DataMatrix.BuildHumanReadable(
                item.GtinNumber, item.ManufacturingDate, item.BatchNumber, item.ExpiryDate, item.Hexacode);
        }

        public List<ProductSerialStatusHistoryItem> ListHistory(int serialId)
        {
            var list = new List<ProductSerialStatusHistoryItem>();
            if (serialId <= 0) return list;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT h.ChangedOn, ISNULL(h.OldStatus,''), h.NewStatus,
       ISNULL(NULLIF(LTRIM(RTRIM(u.FullName)), ''), ISNULL(u.Username,'')),
       ISNULL(h.Remarks,'')
FROM tblProductSerialStatusHistory h
LEFT JOIN tblUser u ON u.UserID = h.ChangedByUserID
WHERE h.ProductSerialID=@Id
ORDER BY h.ChangedOn DESC, h.SerialStatusHistoryID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", serialId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new ProductSerialStatusHistoryItem
                        {
                            ChangedOn = dr.GetDateTime(0),
                            OldStatus = dr.GetString(1),
                            NewStatus = dr.GetString(2),
                            ChangedBy = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            Remarks = dr.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        public static string NormalizeGtin(string raw, bool required)
        {
            var gtin = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(gtin))
            {
                if (required)
                    throw new InvalidOperationException("GTIN is missing on Product Master. Enter a GS1 GTIN (8, 12, 13 or 14 digits) on the product, then generate codes.");
                return "";
            }
            if (!GtinPattern.IsMatch(gtin))
                throw new InvalidOperationException("GTIN must be 8, 12, 13 or 14 digits as stored on Product Master.");
            return gtin;
        }

        private static string RunSelectSql()
        {
            return @"
SELECT r.SerializationRunID, r.GenerationKey, r.ProductID,
       ISNULL(NULLIF(LTRIM(RTRIM(r.ProductName)), N''),
              LTRIM(RTRIM(ISNULL(p.ProductCode,'') + N' - ' + ISNULL(p.ProductName,'')))),
       r.GTINNumber, r.ProductBatchID,
       ISNULL(NULLIF(r.BatchNumber, N''), ISNULL(b.BatchNumber, N'')),
       r.ManufacturingDate, r.ExpiryDate, r.Quantity, ISNULL(r.GeneratedQuantity, r.Quantity),
       r.CreatedOn,
       ISNULL(NULLIF(r.GeneratedByName, N''), ISNULL(NULLIF(LTRIM(RTRIM(u.FullName)), N''), ISNULL(u.Username, N''))),
       ISNULL(r.GenerationStatus, N'Completed')
FROM tblProductSerializationRun r
INNER JOIN tblProduct p ON p.ProductID = r.ProductID
LEFT JOIN tblProductBatch b ON b.ProductBatchID = r.ProductBatchID
LEFT JOIN tblUser u ON u.UserID = r.CreatedByUserID";
        }

        private static string ListRunsSql(bool countOnly)
        {
            var from = @"
FROM tblProductSerializationRun r
INNER JOIN tblProduct p ON p.ProductID = r.ProductID
LEFT JOIN tblProductBatch b ON b.ProductBatchID = r.ProductBatchID
LEFT JOIN tblUser u ON u.UserID = r.CreatedByUserID
WHERE (@Key = N'' OR r.GenerationKey LIKE @KeyLike)
  AND (@ProductID = 0 OR r.ProductID = @ProductID)
  AND (@ProductName = N'' OR ISNULL(r.ProductName, N'') LIKE @ProductNameLike
       OR p.ProductName LIKE @ProductNameLike OR p.ProductCode LIKE @ProductNameLike)
  AND (@Gtin = N'' OR r.GTINNumber LIKE @GtinLike)
  AND (@BatchNo = N'' OR ISNULL(r.BatchNumber, N'') LIKE @BatchLike OR ISNULL(b.BatchNumber, N'') LIKE @BatchLike)
  AND (@From IS NULL OR r.CreatedOn >= @From)
  AND (@To IS NULL OR r.CreatedOn < DATEADD(DAY, 1, @To))";
            if (countOnly) return "SELECT COUNT(*) " + from;
            return @"
SELECT r.SerializationRunID, r.GenerationKey, r.ProductID,
       ISNULL(NULLIF(LTRIM(RTRIM(r.ProductName)), N''),
              LTRIM(RTRIM(ISNULL(p.ProductCode,'') + N' - ' + ISNULL(p.ProductName,'')))),
       r.GTINNumber, r.ProductBatchID,
       ISNULL(NULLIF(r.BatchNumber, N''), ISNULL(b.BatchNumber, N'')),
       r.ManufacturingDate, r.ExpiryDate, r.Quantity, ISNULL(r.GeneratedQuantity, r.Quantity),
       r.CreatedOn,
       ISNULL(NULLIF(r.GeneratedByName, N''), ISNULL(NULLIF(LTRIM(RTRIM(u.FullName)), N''), ISNULL(u.Username, N''))),
       ISNULL(r.GenerationStatus, N'Completed')
" + from + @"
ORDER BY r.CreatedOn DESC, r.SerializationRunID DESC
OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY;";
        }

        private static void BindRunFilter(SqlCommand cmd, ProductSerializationRunFilter filter)
        {
            var key = (filter.GenerationKey ?? "").Trim();
            var name = (filter.ProductName ?? "").Trim();
            var gtin = (filter.Gtin ?? "").Trim();
            var batch = (filter.BatchNumber ?? "").Trim();
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@KeyLike", "%" + key + "%");
            cmd.Parameters.AddWithValue("@ProductID", filter.ProductID);
            cmd.Parameters.AddWithValue("@ProductName", name);
            cmd.Parameters.AddWithValue("@ProductNameLike", "%" + name + "%");
            cmd.Parameters.AddWithValue("@Gtin", gtin);
            cmd.Parameters.AddWithValue("@GtinLike", "%" + gtin + "%");
            cmd.Parameters.AddWithValue("@BatchNo", batch);
            cmd.Parameters.AddWithValue("@BatchLike", "%" + batch + "%");
            cmd.Parameters.AddWithValue("@From", (object)filter.DateFrom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@To", (object)filter.DateTo ?? DBNull.Value);
        }

        private static ProductSerializationRunItem ReadRun(SqlDataReader dr)
        {
            return new ProductSerializationRunItem
            {
                SerializationRunID = dr.GetInt32(0),
                GenerationKey = dr.GetString(1),
                ProductID = dr.GetInt32(2),
                ProductName = dr.IsDBNull(3) ? "" : dr.GetString(3),
                GtinNumber = dr.IsDBNull(4) ? "" : dr.GetString(4),
                ProductBatchID = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
                BatchNumber = dr.IsDBNull(6) ? "" : dr.GetString(6),
                ManufacturingDate = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7),
                ExpiryDate = dr.IsDBNull(8) ? (DateTime?)null : dr.GetDateTime(8),
                RequestedQuantity = dr.GetInt32(9),
                GeneratedQuantity = dr.IsDBNull(10) ? 0 : dr.GetInt32(10),
                GeneratedOn = dr.GetDateTime(11),
                GeneratedBy = dr.IsDBNull(12) ? "" : dr.GetString(12),
                GenerationStatus = dr.IsDBNull(13) ? "Completed" : dr.GetString(13)
            };
        }

        private static string ListSql(bool countOnly, bool paged = true)
        {
            var from = @"
FROM tblProductSerial s
INNER JOIN tblProduct p ON p.ProductID = s.ProductID
LEFT JOIN tblUser u ON u.UserID = s.CreatedByUserID
LEFT JOIN tblProductSerializationRun r ON r.SerializationRunID = s.SerializationRunID
WHERE (@ProductID = 0 OR s.ProductID = @ProductID)
  AND (@BatchID = 0 OR s.ProductBatchID = @BatchID)
  AND (@Gtin = N'' OR s.GTINNumber LIKE @GtinLike)
  AND (@BatchNo = N'' OR s.BatchNumber LIKE @BatchLike)
  AND (@Status = N'' OR s.Status = @Status)
  AND (@Key = N'' OR s.GenerationKey = @Key)
  AND (@Search = N'' OR s.Hexacode LIKE @SearchLike OR s.GenerationKey LIKE @SearchLike
       OR p.ProductCode LIKE @SearchLike OR p.ProductName LIKE @SearchLike)";
            if (countOnly) return "SELECT COUNT(*) " + from;
            var select = @"
SELECT s.ProductSerialID, s.GenerationKey, s.ProductID,
       ISNULL(p.ProductCode,''), ISNULL(p.ProductName,''),
       s.ProductBatchID, s.BatchNumber, s.GTINNumber, s.Hexacode, s.CodeLength,
       s.Status, ISNULL(s.Remarks,''), s.CreatedOn,
       ISNULL(NULLIF(LTRIM(RTRIM(u.FullName)), ''), ISNULL(u.Username,'')),
       ISNULL(s.Gs1ElementHint,''),
       r.ManufacturingDate, r.ExpiryDate,
       ISNULL(s.Gs1Payload,''), ISNULL(s.Gs1HumanReadable,'')
" + from;
            if (!paged)
                return select + @"
ORDER BY s.ProductSerialID ASC;";
            return select + @"
ORDER BY s.CreatedOn DESC, s.ProductSerialID DESC
OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY;";
        }

        private static void BindFilter(SqlCommand cmd, ProductSerialFilter filter)
        {
            var gtin = (filter.Gtin ?? "").Trim();
            var batch = (filter.BatchNumber ?? "").Trim();
            var search = (filter.Search ?? "").Trim();
            cmd.Parameters.AddWithValue("@ProductID", filter.ProductID);
            cmd.Parameters.AddWithValue("@BatchID", filter.ProductBatchID);
            cmd.Parameters.AddWithValue("@Gtin", gtin);
            cmd.Parameters.AddWithValue("@GtinLike", "%" + gtin + "%");
            cmd.Parameters.AddWithValue("@BatchNo", batch);
            cmd.Parameters.AddWithValue("@BatchLike", "%" + batch + "%");
            cmd.Parameters.AddWithValue("@Status", filter.Status ?? "");
            cmd.Parameters.AddWithValue("@Key", filter.GenerationKey ?? "");
            cmd.Parameters.AddWithValue("@Search", search);
            cmd.Parameters.AddWithValue("@SearchLike", "%" + search + "%");
        }

        private static ProductSerialItem ReadItem(SqlDataReader dr)
        {
            return new ProductSerialItem
            {
                ProductSerialID = dr.GetInt32(0),
                GenerationKey = dr.GetString(1),
                ProductID = dr.GetInt32(2),
                ProductCode = dr.GetString(3),
                ProductName = dr.GetString(4),
                ProductBatchID = dr.GetInt32(5),
                BatchNumber = dr.GetString(6),
                GtinNumber = dr.GetString(7),
                Hexacode = dr.GetString(8),
                CodeLength = dr.GetInt32(9),
                Status = dr.GetString(10),
                Remarks = dr.GetString(11),
                GeneratedOn = dr.GetDateTime(12),
                CreatedBy = dr.IsDBNull(13) ? "" : dr.GetString(13),
                Gs1Hint = dr.IsDBNull(14) ? "" : dr.GetString(14),
                ManufacturingDate = dr.FieldCount > 15 && !dr.IsDBNull(15) ? dr.GetDateTime(15) : (DateTime?)null,
                ExpiryDate = dr.FieldCount > 16 && !dr.IsDBNull(16) ? dr.GetDateTime(16) : (DateTime?)null,
                Gs1Payload = dr.FieldCount > 17 && !dr.IsDBNull(17) ? dr.GetString(17) : "",
                Gs1HumanReadable = dr.FieldCount > 18 && !dr.IsDBNull(18) ? dr.GetString(18) : ""
            };
        }

        private static void AssertStatusChange(string current, string next, bool canApprove, bool canWrite)
        {
            var toTerminal = string.Equals(next, ProductSerialStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(next, ProductSerialStatuses.Rejected, StringComparison.OrdinalIgnoreCase);
            var fromTerminal = string.Equals(current, ProductSerialStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(current, ProductSerialStatuses.Rejected, StringComparison.OrdinalIgnoreCase);

            if (toTerminal || fromTerminal)
            {
                if (!canApprove)
                    throw new InvalidOperationException("Approve permission is required to set Approved or Rejected, or to change a code that is already approved or rejected.");
                return;
            }

            if (!canWrite && !canApprove)
                throw new InvalidOperationException("You do not have permission to change serialization status.");
        }

        private static void WriteHistory(SqlConnection conn, SqlTransaction tx, int serialId, string oldStatus, string newStatus, string remarks, int? userId)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblProductSerialStatusHistory
    (ProductSerialID, OldStatus, NewStatus, Remarks, ChangedOn, ChangedByUserID)
VALUES
    (@Id, @Old, @New, @Remarks, GETDATE(), @UserID);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", serialId);
                cmd.Parameters.AddWithValue("@Old", (object)oldStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@New", newStatus);
                cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks);
                cmd.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private string NextUniqueHexacode(SqlConnection conn, SqlTransaction tx, HashSet<string> used, int length)
        {
            for (var attempt = 0; attempt < 40; attempt++)
            {
                var code = RandomHex(length);
                if (used.Contains(code)) continue;
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM tblProductSerial WHERE Hexacode=@Hex;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Hex", code);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        return code;
                }
            }
            throw new InvalidOperationException("Could not allocate a unique hexacode. Try generating a smaller quantity.");
        }

        private static string RandomHex(int length)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
                sb.Append(HexAlphabet[bytes[i] % HexAlphabet.Length]);
            return sb.ToString();
        }

        private static string NextUniqueGenerationKey(SqlConnection conn, SqlTransaction tx)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var key = NewGenerationKey();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM tblProductSerializationRun WHERE GenerationKey=@Key;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Key", key);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        return key;
                }
            }
            throw new InvalidOperationException("Could not allocate a unique Generation ID. Try again.");
        }

        private static string NewGenerationKey()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            var sb = new StringBuilder(16);
            foreach (var b in bytes)
                sb.Append(b.ToString("X2"));
            return "GID-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + sb;
        }

        private static string BuildGs1Hint(string gtin, string batch, DateTime? expiry, string serial)
        {
            var gtin14 = (gtin ?? "").Trim();
            if (gtin14.Length > 0 && gtin14.Length < 14 && Regex.IsMatch(gtin14, @"^\d+$"))
                gtin14 = gtin14.PadLeft(14, '0');
            if (gtin14.Length != 14 || !Regex.IsMatch(gtin14, @"^\d{14}$"))
                return "Serial (AI 21) only. GTIN is not a 14-digit value, so a GS1 element string was not composed.";

            var parts = new StringBuilder();
            parts.Append("(01)").Append(gtin14);
            if (expiry.HasValue)
                parts.Append("(17)").Append(expiry.Value.ToString("yyMMdd"));
            if (!string.IsNullOrWhiteSpace(batch))
                parts.Append("(10)").Append(batch.Trim());
            parts.Append("(21)").Append(serial);
            return parts.ToString() + " [hint only — not a DataMatrix payload]";
        }
    }
}
