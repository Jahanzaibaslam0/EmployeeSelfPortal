using System;
using System.Configuration;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public class InventoryService
    {
        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();

        public InventoryService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureStockRow(SqlConnection conn, SqlTransaction tx, int productId)
        {
            using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblProductStock WHERE ProductID = @ProductID)
                INSERT INTO tblProductStock (ProductID, QtyOnHand, AvgUnitCost, CreatedOn, CreatedByUserID)
                VALUES (@ProductID, 0, 0, GETDATE(), @CreatedByUserID);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
            }
        }

        public decimal GetQtyOnHand(SqlConnection conn, SqlTransaction tx, int productId)
        {
            using (var cmd = tx == null
                ? new SqlCommand("SELECT ISNULL(QtyOnHand, 0) FROM tblProductStock WHERE ProductID = @ProductID;", conn)
                : new SqlCommand("SELECT ISNULL(QtyOnHand, 0) FROM tblProductStock WHERE ProductID = @ProductID;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        public void ReceiveStock(
            SqlConnection conn,
            SqlTransaction tx,
            int productId,
            decimal qty,
            decimal unitCost,
            string referenceType,
            int? purchaseOrderId,
            int? purchaseOrderItemId,
            int? goodsReceiptId,
            string remarks)
        {
            if (productId <= 0 || qty <= 0)
                throw new InvalidOperationException("Invalid product or quantity for stock receipt.");

            EnsureStockRow(conn, tx, productId);

            decimal oldQty, oldAvg;
            using (var cmd = new SqlCommand(@"
                SELECT QtyOnHand, AvgUnitCost FROM tblProductStock WHERE ProductID = @ProductID;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) throw new InvalidOperationException("Stock record not found.");
                    oldQty = dr.GetDecimal(0);
                    oldAvg = dr.GetDecimal(1);
                }
            }

            var newQty = oldQty + qty;
            var newAvg = newQty == 0 ? 0m : ((oldQty * oldAvg) + (qty * unitCost)) / newQty;

            using (var upd = new SqlCommand(@"
                UPDATE tblProductStock
                SET QtyOnHand = @QtyOnHand,
                    AvgUnitCost = @AvgUnitCost,
                    LastReceiptDate = GETDATE(),
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ProductID = @ProductID;", conn, tx))
            {
                upd.Parameters.AddWithValue("@ProductID", productId);
                upd.Parameters.AddWithValue("@QtyOnHand", newQty);
                upd.Parameters.AddWithValue("@AvgUnitCost", newAvg);
                AuditHelper.AddModifiedBy(upd, _auth.CurrentUserId);
                upd.ExecuteNonQuery();
            }

            LogTransaction(conn, tx, "Receipt", productId, qty, unitCost, referenceType,
                purchaseOrderId, purchaseOrderItemId, null, null, goodsReceiptId, null, remarks);
        }

        public void IssueStock(
            SqlConnection conn,
            SqlTransaction tx,
            int productId,
            decimal qty,
            string referenceType,
            int? salesOrderId,
            int? salesOrderItemId,
            int? goodsIssueId,
            string remarks)
        {
            if (productId <= 0 || qty <= 0)
                throw new InvalidOperationException("Invalid product or quantity for stock issue.");

            EnsureStockRow(conn, tx, productId);

            var onHand = GetQtyOnHand(conn, tx, productId);
            if (onHand < qty)
                throw new InvalidOperationException($"Insufficient stock. Available: {onHand:N4}, requested: {qty:N4}.");

            decimal avgCost;
            using (var cmd = new SqlCommand("SELECT AvgUnitCost FROM tblProductStock WHERE ProductID = @ProductID;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                avgCost = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            using (var upd = new SqlCommand(@"
                UPDATE tblProductStock
                SET QtyOnHand = QtyOnHand - @Qty,
                    LastIssueDate = GETDATE(),
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ProductID = @ProductID;", conn, tx))
            {
                upd.Parameters.AddWithValue("@ProductID", productId);
                upd.Parameters.AddWithValue("@Qty", qty);
                AuditHelper.AddModifiedBy(upd, _auth.CurrentUserId);
                upd.ExecuteNonQuery();
            }

            LogTransaction(conn, tx, "Issue", productId, -qty, avgCost, referenceType,
                null, null, salesOrderId, salesOrderItemId, null, goodsIssueId, remarks);
        }

        private void LogTransaction(
            SqlConnection conn,
            SqlTransaction tx,
            string transactionType,
            int productId,
            decimal qty,
            decimal? unitCost,
            string referenceType,
            int? purchaseOrderId,
            int? purchaseOrderItemId,
            int? salesOrderId,
            int? salesOrderItemId,
            int? goodsReceiptId,
            int? goodsIssueId,
            string remarks)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO tblInventoryTransaction
                    (TransactionDate, TransactionType, ProductID, Qty, UnitCost,
                     ReferenceType, PurchaseOrderID, PurchaseOrderItemID,
                     SalesOrderID, SalesOrderItemID, GoodsReceiptID, GoodsIssueID,
                     Remarks, CreatedOn, CreatedByUserID, CreatedByUsername)
                VALUES
                    (GETDATE(), @TransactionType, @ProductID, @Qty, @UnitCost,
                     @ReferenceType, @PurchaseOrderID, @PurchaseOrderItemID,
                     @SalesOrderID, @SalesOrderItemID, @GoodsReceiptID, @GoodsIssueID,
                     @Remarks, GETDATE(), @CreatedByUserID, @CreatedByUsername);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@TransactionType", transactionType);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@UnitCost", (object)unitCost ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                cmd.Parameters.AddWithValue("@PurchaseOrderID", (object)purchaseOrderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PurchaseOrderItemID", (object)purchaseOrderItemId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SalesOrderID", (object)salesOrderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SalesOrderItemID", (object)salesOrderItemId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GoodsReceiptID", (object)goodsReceiptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GoodsIssueID", (object)goodsIssueId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.Parameters.AddWithValue("@CreatedByUsername", (object)_auth.CurrentUsername ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public string GenerateNextCode(SqlConnection conn, SqlTransaction tx, string table, string codeCol, string prefix, int pad)
        {
            var start = prefix.Length + 1;
            using (var cmd = new SqlCommand($@"
                SELECT ISNULL(MAX(TRY_CAST(SUBSTRING({codeCol}, {start}, 10) AS INT)), 0)
                FROM {table}
                WHERE {codeCol} LIKE '{prefix}[0-9]%';", conn, tx))
            {
                var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                return $"{prefix}{next.ToString().PadLeft(pad, '0')}";
            }
        }
    }
}
