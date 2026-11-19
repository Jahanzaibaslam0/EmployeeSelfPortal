using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace HRMS.Services
{
    public static class ProductBatchStatuses
    {
        public const string Registered = "Registered";
        public const string Active = "Active";
        public const string Blocked = "Blocked";
        public const string Expired = "Expired";

        public static readonly string[] All =
        {
            Registered, Active, Blocked, Expired
        };

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case Active: return "badge badge-success";
                case Registered: return "badge badge-info";
                case Blocked: return "badge badge-warning";
                case Expired: return "badge badge-danger";
                default: return "badge";
            }
        }

        public static bool IsUsable(string status)
        {
            return string.Equals(status, Active, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, Registered, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ProductBatchItem
    {
        public int ProductBatchID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string BatchNumber { get; set; } = "";
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? BestBeforeDate { get; set; }
        public string Manufacturer { get; set; } = "";
        public int VendorID { get; set; }
        public string VendorName { get; set; } = "";
        public string BatchStatus { get; set; } = ProductBatchStatuses.Registered;
        public decimal? BatchMrp { get; set; }
        public decimal? AssessableRate { get; set; }
        public string Remarks { get; set; } = "";
        public decimal QtyOnHand { get; set; }
        public bool IsActive { get; set; } = true;
        public string StatusBadgeClass => ProductBatchStatuses.BadgeClass(BatchStatus);
        public bool IsExpiringSoon
        {
            get
            {
                if (!ExpiryDate.HasValue) return false;
                var days = (ExpiryDate.Value.Date - DateTime.Today).TotalDays;
                return days >= 0 && days <= 30 && ProductBatchStatuses.IsUsable(BatchStatus);
            }
        }
    }

    public class ProductBatchOption
    {
        public int ProductBatchID { get; set; }
        public string Label { get; set; } = "";
        public string BatchStatus { get; set; } = "";
        public bool Usable { get; set; }
        public decimal QtyOnHand { get; set; }
    }

    public class ProductBatchLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ProductBatchService
    {
        public const string FormKey = "ProductBatchMaster";
        private static readonly Regex BatchNumberPattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._\-]{0,49}$", RegexOptions.Compiled);

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();

        public ProductBatchService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblProductBatch', N'U') IS NULL
BEGIN
    CREATE TABLE tblProductBatch (
        ProductBatchID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductID INT NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        ManufacturingDate DATE NULL,
        ExpiryDate DATE NULL,
        BestBeforeDate DATE NULL,
        Manufacturer NVARCHAR(150) NULL,
        VendorID INT NULL,
        BatchStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblProductBatch_Status DEFAULT N'Registered',
        BatchMrp DECIMAL(18,4) NULL,
        AssessableRate DECIMAL(18,4) NULL,
        Remarks NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblProductBatch_Active DEFAULT 1,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductBatch_Created DEFAULT GETDATE(),
        CreatedByUserID INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END;

IF EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.name = N'UX_tblProductBatch_Product_Batch'
      AND i.object_id = OBJECT_ID(N'tblProductBatch')
      AND i.has_filter = 0)
    DROP INDEX UX_tblProductBatch_Product_Batch ON tblProductBatch;

IF OBJECT_ID(N'tblProductBatch', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.name = N'UX_tblProductBatch_Product_Batch'
      AND i.object_id = OBJECT_ID(N'tblProductBatch'))
    CREATE UNIQUE INDEX UX_tblProductBatch_Product_Batch
        ON tblProductBatch (ProductID, BatchNumber) WHERE IsActive = 1;

IF OBJECT_ID(N'tblProductBatchStock', N'U') IS NULL
BEGIN
    CREATE TABLE tblProductBatchStock (
        ProductBatchID INT NOT NULL PRIMARY KEY,
        QtyOnHand DECIMAL(18,4) NOT NULL CONSTRAINT DF_tblProductBatchStock_Qty DEFAULT 0,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblProductBatchStock_Created DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL,
        ModifiedByUserID INT NULL
    );
END;

IF COL_LENGTH(N'tblInventoryTransaction', N'ProductBatchID') IS NULL
    ALTER TABLE tblInventoryTransaction ADD ProductBatchID INT NULL;

UPDATE tblProductBatch
SET BatchStatus = N'Expired', ModifiedOn = GETDATE()
WHERE ExpiryDate IS NOT NULL
  AND ExpiryDate < CAST(GETDATE() AS DATE)
  AND BatchStatus NOT IN (N'Expired', N'Blocked');
", conn))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<ProductBatchLookup> ListProducts()
        {
            var list = new List<ProductBatchLookup>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT ProductID, LTRIM(RTRIM(ISNULL(ProductCode,'') + N' - ' + ISNULL(ProductName,'')))
FROM tblProduct
WHERE ISNULL(IsActive,1)=1
ORDER BY ProductCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(new ProductBatchLookup { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
            return list;
        }

        public List<ProductBatchLookup> ListVendors()
        {
            var list = new List<ProductBatchLookup>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT VendorID, LTRIM(RTRIM(ISNULL(VendorCode,'') + N' - ' + ISNULL(Name,'')))
FROM tblVendor
WHERE ISNULL(IsActive,1)=1
ORDER BY Name;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(new ProductBatchLookup { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
            return list;
        }

        public List<ProductBatchItem> List(int productId = 0, string status = null, string search = null)
        {
            EnsureSchema();
            var list = new List<ProductBatchItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT b.ProductBatchID, b.ProductID, ISNULL(p.ProductCode,''), ISNULL(p.ProductName,''),
       b.BatchNumber, b.ManufacturingDate, b.ExpiryDate, b.BestBeforeDate,
       ISNULL(b.Manufacturer,''), ISNULL(b.VendorID,0),
       LTRIM(RTRIM(ISNULL(v.VendorCode,'') + N' ' + ISNULL(v.Name,''))),
       b.BatchStatus, b.BatchMrp, b.AssessableRate, ISNULL(b.Remarks,''),
       ISNULL(s.QtyOnHand,0), b.IsActive
FROM tblProductBatch b
INNER JOIN tblProduct p ON p.ProductID = b.ProductID
LEFT JOIN tblVendor v ON v.VendorID = b.VendorID
LEFT JOIN tblProductBatchStock s ON s.ProductBatchID = b.ProductBatchID
WHERE b.IsActive = 1
  AND (@ProductID = 0 OR b.ProductID = @ProductID)
  AND (@Status = N'' OR b.BatchStatus = @Status)
  AND (@Search = N'' OR b.BatchNumber LIKE @SearchLike OR p.ProductCode LIKE @SearchLike OR p.ProductName LIKE @SearchLike
       OR ISNULL(b.Manufacturer,'') LIKE @SearchLike)
ORDER BY p.ProductCode, b.ExpiryDate, b.BatchNumber;", conn))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.Parameters.AddWithValue("@Status", status ?? "");
                var q = (search ?? "").Trim();
                cmd.Parameters.AddWithValue("@Search", q);
                cmd.Parameters.AddWithValue("@SearchLike", "%" + q + "%");
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(ReadItem(dr));
                }
            }
            return list;
        }

        public ProductBatchItem GetById(int id)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT b.ProductBatchID, b.ProductID, ISNULL(p.ProductCode,''), ISNULL(p.ProductName,''),
       b.BatchNumber, b.ManufacturingDate, b.ExpiryDate, b.BestBeforeDate,
       ISNULL(b.Manufacturer,''), ISNULL(b.VendorID,0),
       LTRIM(RTRIM(ISNULL(v.VendorCode,'') + N' ' + ISNULL(v.Name,''))),
       b.BatchStatus, b.BatchMrp, b.AssessableRate, ISNULL(b.Remarks,''),
       ISNULL(s.QtyOnHand,0), b.IsActive
FROM tblProductBatch b
INNER JOIN tblProduct p ON p.ProductID = b.ProductID
LEFT JOIN tblVendor v ON v.VendorID = b.VendorID
LEFT JOIN tblProductBatchStock s ON s.ProductBatchID = b.ProductBatchID
WHERE b.ProductBatchID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return ReadItem(dr);
                }
            }
        }

        public bool ProductHasBatches(int productId)
        {
            if (productId <= 0) return false;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM tblProductBatch WHERE ProductID=@ProductID AND IsActive=1
) THEN 1 ELSE 0 END;", conn))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        public List<ProductBatchOption> ListOptionsForProduct(int productId, bool issue)
        {
            var list = new List<ProductBatchOption>();
            if (productId <= 0) return list;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT b.ProductBatchID, b.BatchNumber, b.BatchStatus, b.ExpiryDate, ISNULL(s.QtyOnHand,0)
FROM tblProductBatch b
LEFT JOIN tblProductBatchStock s ON s.ProductBatchID = b.ProductBatchID
WHERE b.ProductID=@ProductID AND b.IsActive=1
ORDER BY CASE WHEN b.ExpiryDate IS NULL THEN 1 ELSE 0 END, b.ExpiryDate, b.BatchNumber;", conn))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var status = dr.GetString(2);
                        var expiry = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3);
                        var qty = dr.GetDecimal(4);
                        var expired = expiry.HasValue && expiry.Value.Date < DateTime.Today;
                        var usable = ProductBatchStatuses.IsUsable(status) && !expired;
                        if (issue && qty <= 0) usable = false;
                        var expLabel = expiry.HasValue ? expiry.Value.ToString("dd-MMM-yyyy") : "no expiry";
                        list.Add(new ProductBatchOption
                        {
                            ProductBatchID = dr.GetInt32(0),
                            BatchStatus = expired ? ProductBatchStatuses.Expired : status,
                            QtyOnHand = qty,
                            Usable = usable,
                            Label = dr.GetString(1) + " (" + (expired ? ProductBatchStatuses.Expired : status)
                                + ", exp " + expLabel + ", qty " + qty.ToString("0.####") + ")"
                        });
                    }
                }
            }
            return list;
        }

        public int Save(ProductBatchItem input)
        {
            EnsureSchema();
            Validate(input);

            if (IsDuplicate(input.ProductID, input.BatchNumber, input.ProductBatchID))
                throw new InvalidOperationException("This batch number already exists for the selected product. Product and batch number must be unique together.");

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (input.ProductBatchID > 0)
                {
                    using (var cmd = new SqlCommand(@"
UPDATE tblProductBatch
SET ProductID=@ProductID, BatchNumber=@BatchNumber, ManufacturingDate=@Mfg, ExpiryDate=@Exp,
    BestBeforeDate=@Best, Manufacturer=@Manufacturer, VendorID=@VendorID, BatchStatus=@Status,
    BatchMrp=@Mrp, AssessableRate=@Rate, Remarks=@Remarks, IsActive=1,
    ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ProductBatchID=@Id;", conn))
                    {
                        Bind(cmd, input);
                        cmd.Parameters.AddWithValue("@Id", input.ProductBatchID);
                        AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                    return input.ProductBatchID;
                }

                using (var cmd = new SqlCommand(@"
INSERT INTO tblProductBatch
    (ProductID, BatchNumber, ManufacturingDate, ExpiryDate, BestBeforeDate, Manufacturer, VendorID,
     BatchStatus, BatchMrp, AssessableRate, Remarks, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@ProductID, @BatchNumber, @Mfg, @Exp, @Best, @Manufacturer, @VendorID,
     @Status, @Mrp, @Rate, @Remarks, 1, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    Bind(cmd, input);
                    AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Deactivate(int id)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblProductBatch
SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ProductBatchID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void AssertUsable(SqlConnection conn, SqlTransaction tx, int productId, int productBatchId, bool issue, decimal qty)
        {
            if (productBatchId <= 0)
            {
                if (ProductHasBatches(productId))
                    throw new InvalidOperationException("Select a batch. This product is tracked by batch.");
                return;
            }

            using (var cmd = new SqlCommand(@"
SELECT b.ProductID, b.BatchNumber, b.BatchStatus, b.ExpiryDate, ISNULL(s.QtyOnHand,0), b.IsActive
FROM tblProductBatch b
LEFT JOIN tblProductBatchStock s ON s.ProductBatchID = b.ProductBatchID
WHERE b.ProductBatchID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", productBatchId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new InvalidOperationException("The selected batch was not found.");
                    var batchProduct = dr.GetInt32(0);
                    var number = dr.GetString(1);
                    var status = dr.GetString(2);
                    var expiry = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3);
                    var onHand = dr.GetDecimal(4);
                    var active = dr.GetBoolean(5);
                    dr.Close();

                    if (batchProduct != productId)
                        throw new InvalidOperationException("Batch " + number + " does not belong to the selected product.");
                    if (!active)
                        throw new InvalidOperationException("Batch " + number + " is inactive.");
                    if (string.Equals(status, ProductBatchStatuses.Blocked, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Batch " + number + " is blocked and cannot be used in inventory transactions.");
                    if (string.Equals(status, ProductBatchStatuses.Expired, StringComparison.OrdinalIgnoreCase)
                        || (expiry.HasValue && expiry.Value.Date < DateTime.Today))
                        throw new InvalidOperationException("Batch " + number + " is expired and cannot be used in inventory transactions.");
                    if (!ProductBatchStatuses.IsUsable(status))
                        throw new InvalidOperationException("Batch " + number + " status is " + status + " and cannot be used.");
                    if (issue && onHand < qty)
                        throw new InvalidOperationException("Insufficient batch stock for " + number + ". Available: " + onHand.ToString("N4") + ".");
                }
            }
        }

        public void ApplyStockDelta(SqlConnection conn, SqlTransaction tx, int productBatchId, decimal qtyDelta)
        {
            if (productBatchId <= 0 || qtyDelta == 0) return;
            using (var ensure = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM tblProductBatchStock WHERE ProductBatchID=@Id)
    INSERT INTO tblProductBatchStock (ProductBatchID, QtyOnHand, CreatedOn)
    VALUES (@Id, 0, GETDATE());", conn, tx))
            {
                ensure.Parameters.AddWithValue("@Id", productBatchId);
                ensure.ExecuteNonQuery();
            }

            using (var upd = new SqlCommand(@"
UPDATE tblProductBatchStock
SET QtyOnHand = QtyOnHand + @Qty, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE ProductBatchID=@Id;", conn, tx))
            {
                upd.Parameters.AddWithValue("@Id", productBatchId);
                upd.Parameters.AddWithValue("@Qty", qtyDelta);
                AuditHelper.AddModifiedBy(upd, _auth.CurrentUserId);
                upd.ExecuteNonQuery();
            }
        }

        private void Validate(ProductBatchItem input)
        {
            if (input == null) throw new InvalidOperationException("Batch details are required.");
            if (input.ProductID <= 0)
                throw new InvalidOperationException("Select a product from Product Master.");
            input.BatchNumber = (input.BatchNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(input.BatchNumber))
                throw new InvalidOperationException("Batch number is required.");
            if (!BatchNumberPattern.IsMatch(input.BatchNumber))
                throw new InvalidOperationException("Batch number must be alphanumeric (letters, numbers, hyphen, underscore or dot).");
            if (input.ManufacturingDate.HasValue && input.ExpiryDate.HasValue
                && input.ExpiryDate.Value.Date < input.ManufacturingDate.Value.Date)
                throw new InvalidOperationException("Expiry date cannot be earlier than the manufacturing date.");
            if (input.ManufacturingDate.HasValue && input.BestBeforeDate.HasValue
                && input.BestBeforeDate.Value.Date < input.ManufacturingDate.Value.Date)
                throw new InvalidOperationException("Shelf life / best before date cannot be earlier than the manufacturing date.");
            if (string.IsNullOrWhiteSpace(input.BatchStatus)
                || Array.IndexOf(ProductBatchStatuses.All, input.BatchStatus) < 0)
                input.BatchStatus = ProductBatchStatuses.Registered;
            if (input.ExpiryDate.HasValue && input.ExpiryDate.Value.Date < DateTime.Today
                && ProductBatchStatuses.IsUsable(input.BatchStatus))
                input.BatchStatus = ProductBatchStatuses.Expired;
        }

        private bool IsDuplicate(int productId, string batchNumber, int excludeId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT COUNT(*) FROM tblProductBatch
WHERE ProductID=@ProductID AND UPPER(BatchNumber)=UPPER(@Batch) AND ProductBatchID<>@Id AND IsActive=1;", conn))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.Parameters.AddWithValue("@Batch", batchNumber);
                cmd.Parameters.AddWithValue("@Id", excludeId);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void Bind(SqlCommand cmd, ProductBatchItem input)
        {
            cmd.Parameters.AddWithValue("@ProductID", input.ProductID);
            cmd.Parameters.AddWithValue("@BatchNumber", input.BatchNumber);
            cmd.Parameters.AddWithValue("@Mfg", (object)input.ManufacturingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Exp", (object)input.ExpiryDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Best", (object)input.BestBeforeDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Manufacturer", string.IsNullOrWhiteSpace(input.Manufacturer) ? (object)DBNull.Value : input.Manufacturer.Trim());
            cmd.Parameters.AddWithValue("@VendorID", input.VendorID > 0 ? (object)input.VendorID : DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", input.BatchStatus);
            cmd.Parameters.AddWithValue("@Mrp", (object)input.BatchMrp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Rate", (object)input.AssessableRate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(input.Remarks) ? (object)DBNull.Value : input.Remarks.Trim());
        }

        private static ProductBatchItem ReadItem(SqlDataReader dr)
        {
            return new ProductBatchItem
            {
                ProductBatchID = dr.GetInt32(0),
                ProductID = dr.GetInt32(1),
                ProductCode = dr.IsDBNull(2) ? "" : dr.GetString(2),
                ProductName = dr.IsDBNull(3) ? "" : dr.GetString(3),
                BatchNumber = dr.GetString(4),
                ManufacturingDate = dr.IsDBNull(5) ? (DateTime?)null : dr.GetDateTime(5),
                ExpiryDate = dr.IsDBNull(6) ? (DateTime?)null : dr.GetDateTime(6),
                BestBeforeDate = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7),
                Manufacturer = dr.IsDBNull(8) ? "" : dr.GetString(8),
                VendorID = dr.IsDBNull(9) ? 0 : Convert.ToInt32(dr.GetValue(9)),
                VendorName = dr.IsDBNull(10) ? "" : dr.GetString(10).Trim(),
                BatchStatus = dr.IsDBNull(11) ? ProductBatchStatuses.Registered : dr.GetString(11),
                BatchMrp = dr.IsDBNull(12) ? (decimal?)null : dr.GetDecimal(12),
                AssessableRate = dr.IsDBNull(13) ? (decimal?)null : dr.GetDecimal(13),
                Remarks = dr.IsDBNull(14) ? "" : dr.GetString(14),
                QtyOnHand = dr.IsDBNull(15) ? 0 : dr.GetDecimal(15),
                IsActive = !dr.IsDBNull(16) && dr.GetBoolean(16)
            };
        }
    }
}
