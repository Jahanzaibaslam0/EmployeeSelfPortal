using HRMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class StockRow
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string InventoryType { get; set; } = "";
    public decimal QtyOnHand { get; set; }
    public decimal AvgUnitCost { get; set; }
    public decimal StockValue { get; set; }
    public DateTime? LastReceiptDate { get; set; }
    public DateTime? LastIssueDate { get; set; }
}

public class InventoryTransactionRow
{
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal? UnitCost { get; set; }
    public string ReferenceType { get; set; } = "";
    public string ReferenceInfo { get; set; } = "";
    public string CreatedByUsername { get; set; } = "";
}

public class InventoryMasterModel : PageModel
{
    private readonly string _conn;

    public InventoryMasterModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Inventory Management";
    public List<StockRow> Stock { get; set; } = new();
    public List<InventoryTransactionRow> Transactions { get; set; } = new();
    public decimal TotalStockValue { get; set; }
    public int ProductCount { get; set; }
    public string Search { get; set; } = "";

    public void OnGet(string? search)
    {
        Search = search?.Trim() ?? "";
        LoadStock(Search);
        LoadRecentTransactions();
    }

    private void LoadStock(string search)
    {
        using var conn = new SqlConnection(_conn);
        var sql = @"
            SELECT p.ProductID, p.ProductCode, p.ProductName, ISNULL(it.InventoryTypeName, ''),
                   ISNULL(s.QtyOnHand, 0), ISNULL(s.AvgUnitCost, 0),
                   ISNULL(s.QtyOnHand, 0) * ISNULL(s.AvgUnitCost, 0),
                   s.LastReceiptDate, s.LastIssueDate
            FROM tblProduct p
            LEFT JOIN tblProductStock s ON s.ProductID = p.ProductID
            LEFT JOIN tblInventoryType it ON it.InventoryTypeID = p.InventoryTypeID
            WHERE p.IsActive = 1";
        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND (p.ProductCode LIKE @S OR p.ProductName LIKE @S)";
        sql += " ORDER BY p.ProductCode;";

        using var cmd = new SqlCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(search))
            cmd.Parameters.AddWithValue("@S", "%" + search + "%");
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var row = new StockRow
            {
                ProductID = dr.GetInt32(0),
                ProductCode = dr.GetString(1),
                ProductName = dr.GetString(2),
                InventoryType = dr.GetString(3),
                QtyOnHand = dr.GetDecimal(4),
                AvgUnitCost = dr.GetDecimal(5),
                StockValue = dr.GetDecimal(6),
                LastReceiptDate = dr.IsDBNull(7) ? null : dr.GetDateTime(7),
                LastIssueDate = dr.IsDBNull(8) ? null : dr.GetDateTime(8)
            };
            Stock.Add(row);
            TotalStockValue += row.StockValue;
            if (row.QtyOnHand > 0) ProductCount++;
        }
    }

    private void LoadRecentTransactions()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 50 t.TransactionDate, t.TransactionType, p.ProductCode, p.ProductName,
                   t.Qty, t.UnitCost, ISNULL(t.ReferenceType, ''),
                   CASE
                       WHEN t.PurchaseOrderID IS NOT NULL THEN 'PO #' + CAST(t.PurchaseOrderID AS NVARCHAR(20))
                       WHEN t.SalesOrderID IS NOT NULL THEN 'SO #' + CAST(t.SalesOrderID AS NVARCHAR(20))
                       WHEN t.GoodsReceiptID IS NOT NULL THEN 'GR #' + CAST(t.GoodsReceiptID AS NVARCHAR(20))
                       WHEN t.GoodsIssueID IS NOT NULL THEN 'GI #' + CAST(t.GoodsIssueID AS NVARCHAR(20))
                       ELSE ''
                   END,
                   ISNULL(t.CreatedByUsername, '')
            FROM tblInventoryTransaction t
            INNER JOIN tblProduct p ON p.ProductID = t.ProductID
            ORDER BY t.TransactionDate DESC, t.InventoryTransactionID DESC;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Transactions.Add(new InventoryTransactionRow
            {
                TransactionDate = dr.GetDateTime(0),
                TransactionType = dr.GetString(1),
                ProductCode = dr.GetString(2),
                ProductName = dr.GetString(3),
                Qty = dr.GetDecimal(4),
                UnitCost = dr.IsDBNull(5) ? null : dr.GetDecimal(5),
                ReferenceType = dr.GetString(6),
                ReferenceInfo = dr.GetString(7),
                CreatedByUsername = dr.GetString(8)
            });
        }
    }
}
