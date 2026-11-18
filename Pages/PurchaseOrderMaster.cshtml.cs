using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace HRMS.Pages;

public class PurchaseOrderListItem
{
    public int PurchaseOrderID { get; set; }
    public string PurchaseOrderCode { get; set; } = "";
    public DateTime PurchaseOrderDate { get; set; }
    public string VendorName { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public int LineCount { get; set; }
}

public class PurchaseOrderInput
{
    public int PurchaseOrderID { get; set; }
    public string PurchaseOrderCode { get; set; } = "";
    public string PurchaseOrderDate { get; set; } = "";
    public int VendorID { get; set; }
    public string VendorName { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string OrderStatus { get; set; } = "Draft";
    public string TotalQty { get; set; } = "0";
    public string TotalTax { get; set; } = "0";
    public string TotalDiscount { get; set; } = "0";
    public string GrandTotal { get; set; } = "0";
}

public class PurchaseOrderItemInput
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public string Qty { get; set; } = "";
    public string UnitPrice { get; set; } = "";
    public string TaxAmount { get; set; } = "";
    public string DiscountAmount { get; set; } = "";
}

public class VendorLookupItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class PurchaseOrderMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public static readonly string[] OrderStatusOptions =
        { "Draft", "Pending", "Approved", "Received", "Cancelled" };

    public PurchaseOrderMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public List<PurchaseOrderListItem> PurchaseOrders { get; set; } = new();
    public List<VendorLookupItem> Vendors { get; set; } = new();
    public List<ProductLookupItem> Products { get; set; } = new();
    public PurchaseOrderInput Input { get; set; } = new();
    public List<PurchaseOrderItemInput> ItemRecords { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newPO)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newPO == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
            else
            {
                Input.PurchaseOrderCode = GenerateNextPOCode();
                Input.PurchaseOrderDate = DateTime.Today.ToString("yyyy-MM-dd");
                Input.OrderStatus = "Draft";
                EnsureDefaultItemRow();
            }
        }
        else
        {
            LoadPurchaseOrders();
        }
    }

    public IActionResult OnGetVendorInfo(int vendorId)
    {
        if (vendorId <= 0)
            return new JsonResult(new { code = "", name = "" });

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT VendorCode, Name
            FROM tblVendor
            WHERE VendorID = @Id AND IsActive = 1;", conn);
        cmd.Parameters.AddWithValue("@Id", vendorId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read())
            return new JsonResult(new { code = "", name = "" });

        return new JsonResult(new
        {
            code = dr.IsDBNull(0) ? "" : dr.GetString(0),
            name = dr.IsDBNull(1) ? "" : dr.GetString(1)
        });
    }

    public IActionResult OnPost(
        int PurchaseOrderID,
        bool EditMode,
        string PurchaseOrderCode,
        string PurchaseOrderDate,
        int VendorID,
        string VendorName,
        string Remarks,
        string OrderStatus,
        string TotalQty,
        string TotalTax,
        string TotalDiscount,
        string GrandTotal,
        string ItemsJson)
    {
        this.EditMode = EditMode;
        Input = new PurchaseOrderInput
        {
            PurchaseOrderID = PurchaseOrderID,
            PurchaseOrderCode = PurchaseOrderCode?.Trim() ?? "",
            PurchaseOrderDate = PurchaseOrderDate ?? "",
            VendorID = VendorID,
            VendorName = VendorName?.Trim() ?? "",
            Remarks = Remarks?.Trim() ?? "",
            OrderStatus = string.IsNullOrWhiteSpace(OrderStatus) ? "Draft" : OrderStatus.Trim(),
            TotalQty = TotalQty ?? "0",
            TotalTax = TotalTax ?? "0",
            TotalDiscount = TotalDiscount ?? "0",
            GrandTotal = GrandTotal ?? "0"
        };
        ItemRecords = DeserializeList<PurchaseOrderItemInput>(ItemsJson);

        if (string.IsNullOrWhiteSpace(Input.PurchaseOrderDate))
        {
            AlertMessage = "Purchase order date is required.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (Input.VendorID <= 0)
        {
            AlertMessage = "Please select a vendor from Vendor Master.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        var activeItems = ItemRecords.Where(HasItemContent).ToList();
        if (activeItems.Count == 0)
        {
            AlertMessage = "Add at least one order line item.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var totalQty = activeItems.Sum(l => DecimalValue(l.Qty));
            var totalTax = activeItems.Sum(l => DecimalValue(l.TaxAmount));
            var totalDiscount = activeItems.Sum(l => DecimalValue(l.DiscountAmount));
            var grandTotal = activeItems.Sum(CalcLineNet);

            Input.TotalQty = totalQty.ToString("0.####");
            Input.TotalTax = totalTax.ToString("0.##");
            Input.TotalDiscount = totalDiscount.ToString("0.##");
            Input.GrandTotal = grandTotal.ToString("0.##");

            int poId = SavePOCore(conn, tx, Input, totalQty, totalTax, totalDiscount, grandTotal);
            ReplacePOItems(conn, tx, poId, activeItems);

            tx.Commit();

            TempData["Alert"] = EditMode ? "Purchase order updated successfully." : "Purchase order created successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = poId });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using (var delItems = new SqlCommand("DELETE FROM tblPurchaseOrderItem WHERE PurchaseOrderID = @Id;", conn))
            {
                delItems.Parameters.AddWithValue("@Id", deleteId);
                delItems.ExecuteNonQuery();
            }
            using (var delHeader = new SqlCommand("DELETE FROM tblPurchaseOrder WHERE PurchaseOrderID = @Id;", conn))
            {
                delHeader.Parameters.AddWithValue("@Id", deleteId);
                delHeader.ExecuteNonQuery();
            }

            TempData["Alert"] = "Purchase order deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting purchase order: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private int SavePOCore(SqlConnection conn, SqlTransaction tx, PurchaseOrderInput input,
        decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal)
    {
        var poDate = DateTime.Parse(input.PurchaseOrderDate);

        if (input.PurchaseOrderID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblPurchaseOrder
                SET PurchaseOrderDate = @PurchaseOrderDate,
                    VendorID = @VendorID,
                    VendorName = @VendorName,
                    Remarks = @Remarks,
                    OrderStatus = @OrderStatus,
                    TotalQty = @TotalQty,
                    TotalTax = @TotalTax,
                    TotalDiscount = @TotalDiscount,
                    GrandTotal = @GrandTotal,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE PurchaseOrderID = @PurchaseOrderID;", conn, tx);

            BindPOHeaderParams(cmd, input, poDate, totalQty, totalTax, totalDiscount, grandTotal);
            cmd.Parameters.AddWithValue("@PurchaseOrderID", input.PurchaseOrderID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return input.PurchaseOrderID;
        }

        var code = string.IsNullOrWhiteSpace(input.PurchaseOrderCode)
            ? GenerateNextPOCode(conn, tx)
            : input.PurchaseOrderCode;

        using var ins = new SqlCommand(@"
            INSERT INTO tblPurchaseOrder
                (PurchaseOrderCode, PurchaseOrderDate, VendorID, VendorName, Remarks, OrderStatus,
                 TotalQty, TotalTax, TotalDiscount, GrandTotal, CreatedOn, CreatedByUserID)
            VALUES
                (@PurchaseOrderCode, @PurchaseOrderDate, @VendorID, @VendorName, @Remarks, @OrderStatus,
                 @TotalQty, @TotalTax, @TotalDiscount, @GrandTotal, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);

        ins.Parameters.AddWithValue("@PurchaseOrderCode", code);
        BindPOHeaderParams(ins, input, poDate, totalQty, totalTax, totalDiscount, grandTotal);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        return (int)ins.ExecuteScalar()!;
    }

    private static void BindPOHeaderParams(SqlCommand cmd, PurchaseOrderInput input, DateTime poDate,
        decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal)
    {
        cmd.Parameters.AddWithValue("@PurchaseOrderDate", poDate);
        cmd.Parameters.AddWithValue("@VendorID", input.VendorID > 0 ? input.VendorID : DBNull.Value);
        cmd.Parameters.AddWithValue("@VendorName", input.VendorName);
        cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(input.Remarks) ? DBNull.Value : input.Remarks);
        cmd.Parameters.AddWithValue("@OrderStatus", input.OrderStatus);
        cmd.Parameters.AddWithValue("@TotalQty", totalQty);
        cmd.Parameters.AddWithValue("@TotalTax", totalTax);
        cmd.Parameters.AddWithValue("@TotalDiscount", totalDiscount);
        cmd.Parameters.AddWithValue("@GrandTotal", grandTotal);
    }

    private void ReplacePOItems(SqlConnection conn, SqlTransaction tx, int poId, List<PurchaseOrderItemInput> items)
    {
        using (var del = new SqlCommand("DELETE FROM tblPurchaseOrderItem WHERE PurchaseOrderID = @PurchaseOrderID;", conn, tx))
        {
            del.Parameters.AddWithValue("@PurchaseOrderID", poId);
            del.ExecuteNonQuery();
        }

        int sort = 0;
        foreach (var line in items)
        {
            var net = CalcLineNet(line);
            using var ins = new SqlCommand(@"
                INSERT INTO tblPurchaseOrderItem
                    (PurchaseOrderID, ProductID, ProductCode, ProductDescription, Qty, UnitPrice,
                     TaxAmount, DiscountAmount, NetAmount, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@PurchaseOrderID, @ProductID, @ProductCode, @ProductDescription, @Qty, @UnitPrice,
                     @TaxAmount, @DiscountAmount, @NetAmount, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            ins.Parameters.AddWithValue("@PurchaseOrderID", poId);
            ins.Parameters.AddWithValue("@ProductID", line.ProductID > 0 ? line.ProductID : DBNull.Value);
            ins.Parameters.AddWithValue("@ProductCode", line.ProductCode?.Trim() ?? "");
            ins.Parameters.AddWithValue("@ProductDescription", line.ProductDescription?.Trim() ?? "");
            ins.Parameters.AddWithValue("@Qty", DecimalValue(line.Qty));
            ins.Parameters.AddWithValue("@UnitPrice", DecimalValue(line.UnitPrice));
            ins.Parameters.AddWithValue("@TaxAmount", DecimalValue(line.TaxAmount));
            ins.Parameters.AddWithValue("@DiscountAmount", DecimalValue(line.DiscountAmount));
            ins.Parameters.AddWithValue("@NetAmount", net);
            ins.Parameters.AddWithValue("@SortOrder", sort);
            AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
            ins.ExecuteNonQuery();
            sort++;
        }
    }

    private static bool HasItemContent(PurchaseOrderItemInput line) =>
        line.ProductID > 0
        || !string.IsNullOrWhiteSpace(line.ProductDescription)
        || !string.IsNullOrWhiteSpace(line.ProductCode)
        || DecimalValue(line.Qty) > 0
        || DecimalValue(line.UnitPrice) > 0;

    private static decimal CalcLineNet(PurchaseOrderItemInput line)
    {
        var qty = DecimalValue(line.Qty);
        var unitPrice = DecimalValue(line.UnitPrice);
        var tax = DecimalValue(line.TaxAmount);
        var discount = DecimalValue(line.DiscountAmount);
        return qty * unitPrice + tax - discount;
    }

    private void LoadPurchaseOrders()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                po.PurchaseOrderID,
                po.PurchaseOrderCode,
                po.PurchaseOrderDate,
                ISNULL(po.VendorName, '') AS VendorName,
                ISNULL(po.OrderStatus, '') AS OrderStatus,
                ISNULL(po.GrandTotal, 0) AS GrandTotal,
                ISNULL(agg.LineCount, 0) AS LineCount
            FROM tblPurchaseOrder po
            LEFT JOIN (
                SELECT PurchaseOrderID, COUNT(*) AS LineCount
                FROM tblPurchaseOrderItem
                GROUP BY PurchaseOrderID
            ) agg ON agg.PurchaseOrderID = po.PurchaseOrderID
            ORDER BY po.PurchaseOrderID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            PurchaseOrders.Add(new PurchaseOrderListItem
            {
                PurchaseOrderID = dr.GetInt32(0),
                PurchaseOrderCode = dr.GetString(1),
                PurchaseOrderDate = dr.GetDateTime(2),
                VendorName = dr.GetString(3),
                OrderStatus = dr.GetString(4),
                GrandTotal = dr.GetDecimal(5),
                LineCount = dr.GetInt32(6)
            });
        }
    }

    private void LoadForEdit(int poId)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT PurchaseOrderID, PurchaseOrderCode, PurchaseOrderDate, VendorID, VendorName,
                   Remarks, OrderStatus, TotalQty, TotalTax, TotalDiscount, GrandTotal
            FROM tblPurchaseOrder WHERE PurchaseOrderID = @Id;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Input = new PurchaseOrderInput
                {
                    PurchaseOrderID = dr.GetInt32(0),
                    PurchaseOrderCode = dr.GetString(1),
                    PurchaseOrderDate = dr.GetDateTime(2).ToString("yyyy-MM-dd"),
                    VendorID = dr.IsDBNull(3) ? 0 : dr.GetInt32(3),
                    VendorName = dr.IsDBNull(4) ? "" : dr.GetString(4),
                    Remarks = dr.IsDBNull(5) ? "" : dr.GetString(5),
                    OrderStatus = dr.IsDBNull(6) ? "Draft" : dr.GetString(6),
                    TotalQty = dr.IsDBNull(7) ? "0" : dr.GetDecimal(7).ToString("0.####"),
                    TotalTax = dr.IsDBNull(8) ? "0" : dr.GetDecimal(8).ToString("0.##"),
                    TotalDiscount = dr.IsDBNull(9) ? "0" : dr.GetDecimal(9).ToString("0.##"),
                    GrandTotal = dr.IsDBNull(10) ? "0" : dr.GetDecimal(10).ToString("0.##")
                };
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT ProductID, ProductCode, ProductDescription, Qty, UnitPrice, TaxAmount, DiscountAmount
            FROM tblPurchaseOrderItem
            WHERE PurchaseOrderID = @Id
            ORDER BY SortOrder, PurchaseOrderItemID;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", poId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                ItemRecords.Add(new PurchaseOrderItemInput
                {
                    ProductID = dr.IsDBNull(0) ? 0 : dr.GetInt32(0),
                    ProductCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                    ProductDescription = dr.IsDBNull(2) ? "" : dr.GetString(2),
                    Qty = dr.IsDBNull(3) ? "" : dr.GetDecimal(3).ToString("0.####"),
                    UnitPrice = dr.IsDBNull(4) ? "" : dr.GetDecimal(4).ToString("0.####"),
                    TaxAmount = dr.IsDBNull(5) ? "" : dr.GetDecimal(5).ToString("0.##"),
                    DiscountAmount = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.##")
                });
            }
        }

        if (ItemRecords.Count == 0)
            EnsureDefaultItemRow();
    }

    private void LoadLookups()
    {
        Vendors = LoadVendorLookup();
        Products = LoadProductLookup();
    }

    private List<VendorLookupItem> LoadVendorLookup()
    {
        var items = new List<VendorLookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT VendorID, VendorCode, Name
            FROM tblVendor
            WHERE IsActive = 1
            ORDER BY VendorCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
            var name = dr.IsDBNull(2) ? "" : dr.GetString(2);
            items.Add(new VendorLookupItem
            {
                Id = dr.GetInt32(0),
                Code = code,
                Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
            });
        }
        return items;
    }

    private List<ProductLookupItem> LoadProductLookup()
    {
        var items = new List<ProductLookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ProductID, ProductCode, ProductName
            FROM tblProduct
            WHERE IsActive = 1
            ORDER BY ProductCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.GetString(1);
            var name = dr.GetString(2);
            items.Add(new ProductLookupItem
            {
                Id = dr.GetInt32(0),
                Code = code,
                Name = $"{code} – {name}",
                ProductName = name
            });
        }
        return items;
    }

    private string GenerateNextPOCode(SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        var ownsConnection = conn == null;
        conn ??= new SqlConnection(_conn);
        if (ownsConnection) conn.Open();

        try
        {
            using var cmd = tx == null
                ? new SqlCommand(@"
                    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PurchaseOrderCode, 3, 10) AS INT)), 0)
                    FROM tblPurchaseOrder
                    WHERE PurchaseOrderCode LIKE 'PO[0-9]%';", conn)
                : new SqlCommand(@"
                    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PurchaseOrderCode, 3, 10) AS INT)), 0)
                    FROM tblPurchaseOrder
                    WHERE PurchaseOrderCode LIKE 'PO[0-9]%';", conn, tx);
            var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"PO{next:D6}";
        }
        finally
        {
            if (ownsConnection) conn.Dispose();
        }
    }

    private void EnsureDefaultItemRow()
    {
        if (ItemRecords.Count == 0)
            ItemRecords.Add(new PurchaseOrderItemInput());
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg)
            AlertMessage = msg;
        if (TempData["AlertType"] is string type)
            AlertType = type;
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static decimal DecimalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        return decimal.TryParse(value, out var d) ? d : 0m;
    }
}
