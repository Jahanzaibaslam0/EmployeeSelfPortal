using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace HRMS.Pages;

public class SalesOrderListItem
{
    public int SalesOrderID { get; set; }
    public string SalesOrderCode { get; set; } = "";
    public DateTime SalesOrderDate { get; set; }
    public string CustomerName { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public int LineCount { get; set; }
}

public class SalesOrderInput
{
    public int SalesOrderID { get; set; }
    public string SalesOrderCode { get; set; } = "";
    public string SalesOrderDate { get; set; } = "";
    public int CustomerID { get; set; }
    public string CustomerName { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string OrderStatus { get; set; } = "Draft";
    public string TotalQty { get; set; } = "0";
    public string TotalTax { get; set; } = "0";
    public string TotalDiscount { get; set; } = "0";
    public string GrandTotal { get; set; } = "0";
}

public class SalesOrderItemInput
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public string Qty { get; set; } = "";
    public string UnitPrice { get; set; } = "";
    public string TaxAmount { get; set; } = "";
    public string DiscountAmount { get; set; } = "";
}

public class CustomerLookupItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class SalesProductLookupItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string SellingPrice { get; set; } = "";
}

public class SalesOrderHistoryItem
{
    public DateTime ActionAt { get; set; }
    public string ActionType { get; set; } = "";
    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public string ActionByUsername { get; set; } = "";
    public string Remarks { get; set; } = "";
}

public class SalesOrderMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly AuditService _audit;

    public static readonly string[] OrderStatusOptions =
        { "Draft", "Submitted", "Approved", "Delivered", "Cancelled" };

    public SalesOrderMasterModel(IConfiguration config, AuthService auth, AuditService audit)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _audit = audit;
    }

    public List<SalesOrderListItem> SalesOrders { get; set; } = new();
    public List<CustomerLookupItem> Customers { get; set; } = new();
    public List<SalesProductLookupItem> Products { get; set; } = new();
    public List<SalesOrderHistoryItem> OrderHistory { get; set; } = new();
    public SalesOrderInput Input { get; set; } = new();
    public List<SalesOrderItemInput> ItemRecords { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public bool IsReadOnly { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newSO)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newSO == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
                IsReadOnly = IsStatusLocked(Input.OrderStatus);
                LoadOrderHistory(editId.Value);
            }
            else
            {
                Input.SalesOrderCode = GenerateNextSOCode();
                Input.SalesOrderDate = DateTime.Today.ToString("yyyy-MM-dd");
                Input.OrderStatus = "Draft";
                EnsureDefaultItemRow();
            }
        }
        else
        {
            LoadSalesOrders();
        }
    }

    public IActionResult OnGetCustomerInfo(int customerId)
    {
        if (customerId <= 0)
            return new JsonResult(new { code = "", name = "" });

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CustomerCode, Name
            FROM tblCustomer
            WHERE CustomerID = @Id AND IsActive = 1;", conn);
        cmd.Parameters.AddWithValue("@Id", customerId);
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
        int SalesOrderID,
        bool EditMode,
        string SalesOrderCode,
        string SalesOrderDate,
        int CustomerID,
        string CustomerName,
        string Remarks,
        string OrderStatus,
        string TotalQty,
        string TotalTax,
        string TotalDiscount,
        string GrandTotal,
        string ItemsJson)
        => ProcessSave(SalesOrderID, EditMode, SalesOrderCode, SalesOrderDate, CustomerID, CustomerName,
            Remarks, OrderStatus, TotalQty, TotalTax, TotalDiscount, GrandTotal, ItemsJson,
            submit: false);

    public IActionResult OnPostSubmit(
        int SalesOrderID,
        bool EditMode,
        string SalesOrderCode,
        string SalesOrderDate,
        int CustomerID,
        string CustomerName,
        string Remarks,
        string OrderStatus,
        string TotalQty,
        string TotalTax,
        string TotalDiscount,
        string GrandTotal,
        string ItemsJson)
        => ProcessSave(SalesOrderID, EditMode, SalesOrderCode, SalesOrderDate, CustomerID, CustomerName,
            Remarks, OrderStatus, TotalQty, TotalTax, TotalDiscount, GrandTotal, ItemsJson,
            submit: true);

    public IActionResult OnPostCancel(int cancelId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            string currentStatus;
            string soCode;
            using (var cmd = new SqlCommand(@"
                SELECT OrderStatus, SalesOrderCode FROM tblSalesOrder WHERE SalesOrderID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", cancelId);
                using var dr = cmd.ExecuteReader();
                if (!dr.Read())
                {
                    TempData["Alert"] = "Sales order not found.";
                    TempData["AlertType"] = "error";
                    return RedirectToPage();
                }
                currentStatus = dr.GetString(0);
                soCode = dr.GetString(1);
            }

            if (currentStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Alert"] = "Sales order is already cancelled.";
                TempData["AlertType"] = "error";
                return RedirectToPage(new { editId = cancelId });
            }

            if (currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Alert"] = "Approved sales orders cannot be cancelled.";
                TempData["AlertType"] = "error";
                return RedirectToPage(new { editId = cancelId });
            }

            using (var cmd = new SqlCommand(@"
                UPDATE tblSalesOrder
                SET OrderStatus = 'Cancelled',
                    CancelledOn = GETDATE(),
                    CancelledByUserID = @CancelledByUserID,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE SalesOrderID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", cancelId);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.Parameters.AddWithValue("@CancelledByUserID", (object?)_auth.CurrentUserId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            LogOrderHistory(conn, null, cancelId, "Cancel", currentStatus, "Cancelled", null);
            _audit.Log("Cancel", "Sales Order", cancelId, soCode, "SalesOrderMaster", "/SalesOrderMaster",
                "OnPostCancel", $"Status changed from {currentStatus} to Cancelled");

            TempData["Alert"] = "Sales order cancelled successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error cancelling sales order: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = cancelId });
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using (var delHist = new SqlCommand("DELETE FROM tblSalesOrderHistory WHERE SalesOrderID = @Id;", conn))
            {
                delHist.Parameters.AddWithValue("@Id", deleteId);
                delHist.ExecuteNonQuery();
            }
            using (var delItems = new SqlCommand("DELETE FROM tblSalesOrderItem WHERE SalesOrderID = @Id;", conn))
            {
                delItems.Parameters.AddWithValue("@Id", deleteId);
                delItems.ExecuteNonQuery();
            }
            using (var delHeader = new SqlCommand("DELETE FROM tblSalesOrder WHERE SalesOrderID = @Id;", conn))
            {
                delHeader.Parameters.AddWithValue("@Id", deleteId);
                delHeader.ExecuteNonQuery();
            }

            _audit.Log("Delete", "Sales Order", deleteId, null, "SalesOrderMaster", "/SalesOrderMaster", "OnPostDelete");
            TempData["Alert"] = "Sales order deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting sales order: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private IActionResult ProcessSave(
        int salesOrderId, bool editMode, string salesOrderCode, string salesOrderDate,
        int customerId, string customerName, string remarks, string orderStatus,
        string totalQty, string totalTax, string totalDiscount, string grandTotal,
        string itemsJson, bool submit)
    {
        EditMode = editMode;
        Input = new SalesOrderInput
        {
            SalesOrderID = salesOrderId,
            SalesOrderCode = salesOrderCode?.Trim() ?? "",
            SalesOrderDate = salesOrderDate ?? "",
            CustomerID = customerId,
            CustomerName = customerName?.Trim() ?? "",
            Remarks = remarks?.Trim() ?? "",
            OrderStatus = string.IsNullOrWhiteSpace(orderStatus) ? "Draft" : orderStatus.Trim(),
            TotalQty = totalQty ?? "0",
            TotalTax = totalTax ?? "0",
            TotalDiscount = totalDiscount ?? "0",
            GrandTotal = grandTotal ?? "0"
        };
        ItemRecords = DeserializeList<SalesOrderItemInput>(itemsJson);

        if (string.IsNullOrWhiteSpace(Input.SalesOrderDate))
        {
            return FormError("Sales order date is required.");
        }

        if (Input.CustomerID <= 0)
        {
            return FormError("Please select a customer from Customer Master.");
        }

        if (Input.SalesOrderID > 0)
        {
            using var connCheck = new SqlConnection(_conn);
            using var cmdCheck = new SqlCommand("SELECT OrderStatus FROM tblSalesOrder WHERE SalesOrderID = @Id;", connCheck);
            cmdCheck.Parameters.AddWithValue("@Id", Input.SalesOrderID);
            connCheck.Open();
            var dbStatus = cmdCheck.ExecuteScalar()?.ToString() ?? "";
            if (IsStatusLocked(dbStatus))
                return FormError("This sales order cannot be modified in its current status.");
            if (submit && !dbStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                return FormError("Only draft sales orders can be submitted.");
            Input.OrderStatus = dbStatus;
        }

        var activeItems = ItemRecords.Where(HasItemContent).ToList();
        if (activeItems.Count == 0)
        {
            return FormError("Add at least one order line item.");
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var calcQty = activeItems.Sum(l => DecimalValue(l.Qty));
            var calcTax = activeItems.Sum(l => DecimalValue(l.TaxAmount));
            var calcDiscount = activeItems.Sum(l => DecimalValue(l.DiscountAmount));
            var calcGrand = activeItems.Sum(CalcLineNet);

            Input.TotalQty = calcQty.ToString("0.####");
            Input.TotalTax = calcTax.ToString("0.##");
            Input.TotalDiscount = calcDiscount.ToString("0.##");
            Input.GrandTotal = calcGrand.ToString("0.##");

            string? fromStatus = null;
            if (Input.SalesOrderID > 0)
            {
                using var cmdStatus = new SqlCommand("SELECT OrderStatus FROM tblSalesOrder WHERE SalesOrderID = @Id;", conn, tx);
                cmdStatus.Parameters.AddWithValue("@Id", Input.SalesOrderID);
                fromStatus = cmdStatus.ExecuteScalar()?.ToString();
            }

            var newStatus = submit ? "Submitted" : (fromStatus ?? "Draft");
            if (!submit && !string.IsNullOrEmpty(fromStatus))
                newStatus = fromStatus;
            Input.OrderStatus = newStatus;

            int soId = SaveSOCore(conn, tx, Input, calcQty, calcTax, calcDiscount, calcGrand, submit);
            ReplaceSOItems(conn, tx, soId, activeItems);

            var actionType = submit ? "Submit" : (EditMode ? "Update" : "Create");
            LogOrderHistory(conn, tx, soId, actionType, fromStatus, newStatus, Input.Remarks);

            tx.Commit();

            _audit.Log(
                actionType: submit ? "Submit" : (EditMode ? "Update" : "Create"),
                entityType: "Sales Order",
                entityId: soId,
                entityName: Input.SalesOrderCode,
                formKey: "SalesOrderMaster",
                pagePath: "/SalesOrderMaster",
                handlerName: submit ? "OnPostSubmit" : "OnPost",
                details: $"Status: {newStatus}, Grand Total: {Input.GrandTotal}");

            TempData["Alert"] = submit
                ? "Sales order submitted successfully."
                : EditMode ? "Sales order updated successfully." : "Sales order saved successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = soId });
        }
        catch (Exception ex)
        {
            return FormError("Error: " + ex.Message);
        }
    }

    private IActionResult FormError(string message)
    {
        AlertMessage = message;
        AlertType = "error";
        LoadLookups();
        ShowForm = true;
        if (Input.SalesOrderID > 0)
            LoadOrderHistory(Input.SalesOrderID);
        IsReadOnly = IsStatusLocked(Input.OrderStatus);
        return Page();
    }

    private static bool IsStatusLocked(string status) =>
        status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Approved", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);

    private int SaveSOCore(SqlConnection conn, SqlTransaction tx, SalesOrderInput input,
        decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal, bool submit)
    {
        var soDate = DateTime.Parse(input.SalesOrderDate);

        if (input.SalesOrderID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblSalesOrder
                SET SalesOrderDate = @SalesOrderDate,
                    CustomerID = @CustomerID,
                    CustomerName = @CustomerName,
                    Remarks = @Remarks,
                    OrderStatus = @OrderStatus,
                    TotalQty = @TotalQty,
                    TotalTax = @TotalTax,
                    TotalDiscount = @TotalDiscount,
                    GrandTotal = @GrandTotal,
                    SubmittedOn = CASE WHEN @Submit = 1 THEN GETDATE() ELSE SubmittedOn END,
                    SubmittedByUserID = CASE WHEN @Submit = 1 THEN @SubmittedByUserID ELSE SubmittedByUserID END,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE SalesOrderID = @SalesOrderID;", conn, tx);

            BindSOHeaderParams(cmd, input, soDate, totalQty, totalTax, totalDiscount, grandTotal);
            cmd.Parameters.AddWithValue("@Submit", submit ? 1 : 0);
            cmd.Parameters.AddWithValue("@SubmittedByUserID", (object?)_auth.CurrentUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SalesOrderID", input.SalesOrderID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return input.SalesOrderID;
        }

        var code = string.IsNullOrWhiteSpace(input.SalesOrderCode)
            ? GenerateNextSOCode(conn, tx)
            : input.SalesOrderCode;

        using var ins = new SqlCommand(@"
            INSERT INTO tblSalesOrder
                (SalesOrderCode, SalesOrderDate, CustomerID, CustomerName, Remarks, OrderStatus,
                 TotalQty, TotalTax, TotalDiscount, GrandTotal,
                 SubmittedOn, SubmittedByUserID, CreatedOn, CreatedByUserID)
            VALUES
                (@SalesOrderCode, @SalesOrderDate, @CustomerID, @CustomerName, @Remarks, @OrderStatus,
                 @TotalQty, @TotalTax, @TotalDiscount, @GrandTotal,
                 CASE WHEN @Submit = 1 THEN GETDATE() ELSE NULL END,
                 CASE WHEN @Submit = 1 THEN @SubmittedByUserID ELSE NULL END,
                 GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);

        ins.Parameters.AddWithValue("@SalesOrderCode", code);
        BindSOHeaderParams(ins, input, soDate, totalQty, totalTax, totalDiscount, grandTotal);
        ins.Parameters.AddWithValue("@Submit", submit ? 1 : 0);
        ins.Parameters.AddWithValue("@SubmittedByUserID", (object?)_auth.CurrentUserId ?? DBNull.Value);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.SalesOrderCode = code;
        return (int)ins.ExecuteScalar()!;
    }

    private static void BindSOHeaderParams(SqlCommand cmd, SalesOrderInput input, DateTime soDate,
        decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal)
    {
        cmd.Parameters.AddWithValue("@SalesOrderDate", soDate);
        cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID > 0 ? input.CustomerID : DBNull.Value);
        cmd.Parameters.AddWithValue("@CustomerName", input.CustomerName);
        cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(input.Remarks) ? DBNull.Value : input.Remarks);
        cmd.Parameters.AddWithValue("@OrderStatus", input.OrderStatus);
        cmd.Parameters.AddWithValue("@TotalQty", totalQty);
        cmd.Parameters.AddWithValue("@TotalTax", totalTax);
        cmd.Parameters.AddWithValue("@TotalDiscount", totalDiscount);
        cmd.Parameters.AddWithValue("@GrandTotal", grandTotal);
    }

    private void ReplaceSOItems(SqlConnection conn, SqlTransaction tx, int soId, List<SalesOrderItemInput> items)
    {
        using (var del = new SqlCommand("DELETE FROM tblSalesOrderItem WHERE SalesOrderID = @SalesOrderID;", conn, tx))
        {
            del.Parameters.AddWithValue("@SalesOrderID", soId);
            del.ExecuteNonQuery();
        }

        int sort = 0;
        foreach (var line in items)
        {
            var net = CalcLineNet(line);
            using var ins = new SqlCommand(@"
                INSERT INTO tblSalesOrderItem
                    (SalesOrderID, ProductID, ProductCode, ProductDescription, Qty, UnitPrice,
                     TaxAmount, DiscountAmount, NetAmount, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@SalesOrderID, @ProductID, @ProductCode, @ProductDescription, @Qty, @UnitPrice,
                     @TaxAmount, @DiscountAmount, @NetAmount, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            ins.Parameters.AddWithValue("@SalesOrderID", soId);
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

    private void LogOrderHistory(SqlConnection conn, SqlTransaction? tx, int soId,
        string actionType, string? fromStatus, string toStatus, string? remarks)
    {
        using var cmd = tx == null
            ? new SqlCommand(@"
                INSERT INTO tblSalesOrderHistory
                    (SalesOrderID, ActionType, FromStatus, ToStatus, Remarks, ActionAt, ActionByUserID, ActionByUsername)
                VALUES
                    (@SalesOrderID, @ActionType, @FromStatus, @ToStatus, @Remarks, GETDATE(), @ActionByUserID, @ActionByUsername);", conn)
            : new SqlCommand(@"
                INSERT INTO tblSalesOrderHistory
                    (SalesOrderID, ActionType, FromStatus, ToStatus, Remarks, ActionAt, ActionByUserID, ActionByUsername)
                VALUES
                    (@SalesOrderID, @ActionType, @FromStatus, @ToStatus, @Remarks, GETDATE(), @ActionByUserID, @ActionByUsername);", conn, tx);

        cmd.Parameters.AddWithValue("@SalesOrderID", soId);
        cmd.Parameters.AddWithValue("@ActionType", actionType);
        cmd.Parameters.AddWithValue("@FromStatus", (object?)fromStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ToStatus", toStatus);
        cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActionByUserID", (object?)_auth.CurrentUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActionByUsername", (object?)_auth.CurrentUsername ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static bool HasItemContent(SalesOrderItemInput line) =>
        line.ProductID > 0
        || !string.IsNullOrWhiteSpace(line.ProductDescription)
        || !string.IsNullOrWhiteSpace(line.ProductCode)
        || DecimalValue(line.Qty) > 0
        || DecimalValue(line.UnitPrice) > 0;

    private static decimal CalcLineNet(SalesOrderItemInput line)
    {
        var qty = DecimalValue(line.Qty);
        var unitPrice = DecimalValue(line.UnitPrice);
        var tax = DecimalValue(line.TaxAmount);
        var discount = DecimalValue(line.DiscountAmount);
        return qty * unitPrice + tax - discount;
    }

    private void LoadSalesOrders()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                so.SalesOrderID,
                so.SalesOrderCode,
                so.SalesOrderDate,
                ISNULL(so.CustomerName, '') AS CustomerName,
                ISNULL(so.OrderStatus, '') AS OrderStatus,
                ISNULL(so.GrandTotal, 0) AS GrandTotal,
                ISNULL(agg.LineCount, 0) AS LineCount
            FROM tblSalesOrder so
            LEFT JOIN (
                SELECT SalesOrderID, COUNT(*) AS LineCount
                FROM tblSalesOrderItem
                GROUP BY SalesOrderID
            ) agg ON agg.SalesOrderID = so.SalesOrderID
            ORDER BY so.SalesOrderID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            SalesOrders.Add(new SalesOrderListItem
            {
                SalesOrderID = dr.GetInt32(0),
                SalesOrderCode = dr.GetString(1),
                SalesOrderDate = dr.GetDateTime(2),
                CustomerName = dr.GetString(3),
                OrderStatus = dr.GetString(4),
                GrandTotal = dr.GetDecimal(5),
                LineCount = dr.GetInt32(6)
            });
        }
    }

    private void LoadForEdit(int soId)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT SalesOrderID, SalesOrderCode, SalesOrderDate, CustomerID, CustomerName,
                   Remarks, OrderStatus, TotalQty, TotalTax, TotalDiscount, GrandTotal
            FROM tblSalesOrder WHERE SalesOrderID = @Id;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", soId);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Input = new SalesOrderInput
                {
                    SalesOrderID = dr.GetInt32(0),
                    SalesOrderCode = dr.GetString(1),
                    SalesOrderDate = dr.GetDateTime(2).ToString("yyyy-MM-dd"),
                    CustomerID = dr.IsDBNull(3) ? 0 : dr.GetInt32(3),
                    CustomerName = dr.IsDBNull(4) ? "" : dr.GetString(4),
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
            FROM tblSalesOrderItem
            WHERE SalesOrderID = @Id
            ORDER BY SortOrder, SalesOrderItemID;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", soId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                ItemRecords.Add(new SalesOrderItemInput
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

    private void LoadOrderHistory(int soId)
    {
        OrderHistory.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ActionAt, ActionType, ISNULL(FromStatus, ''), ISNULL(ToStatus, ''),
                   ISNULL(ActionByUsername, ''), ISNULL(Remarks, '')
            FROM tblSalesOrderHistory
            WHERE SalesOrderID = @Id
            ORDER BY ActionAt DESC, SalesOrderHistoryID DESC;", conn);
        cmd.Parameters.AddWithValue("@Id", soId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            OrderHistory.Add(new SalesOrderHistoryItem
            {
                ActionAt = dr.GetDateTime(0),
                ActionType = dr.GetString(1),
                FromStatus = dr.GetString(2),
                ToStatus = dr.GetString(3),
                ActionByUsername = dr.GetString(4),
                Remarks = dr.GetString(5)
            });
        }
    }

    private void LoadLookups()
    {
        Customers = LoadCustomerLookup();
        Products = LoadProductLookup();
    }

    private List<CustomerLookupItem> LoadCustomerLookup()
    {
        var items = new List<CustomerLookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CustomerID, CustomerCode, Name
            FROM tblCustomer
            WHERE IsActive = 1
            ORDER BY CustomerCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
            var name = dr.IsDBNull(2) ? "" : dr.GetString(2);
            items.Add(new CustomerLookupItem
            {
                Id = dr.GetInt32(0),
                Code = code,
                Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
            });
        }
        return items;
    }

    private List<SalesProductLookupItem> LoadProductLookup()
    {
        var items = new List<SalesProductLookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ProductID, ProductCode, ProductName, ISNULL(SellingPrice, 0)
            FROM tblProduct
            WHERE IsActive = 1
            ORDER BY ProductCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.GetString(1);
            var name = dr.GetString(2);
            var price = dr.GetDecimal(3);
            items.Add(new SalesProductLookupItem
            {
                Id = dr.GetInt32(0),
                Code = code,
                Name = $"{code} – {name}",
                ProductName = name,
                SellingPrice = price > 0 ? price.ToString("0.####") : ""
            });
        }
        return items;
    }

    private string GenerateNextSOCode(SqlConnection? conn = null, SqlTransaction? tx = null)
    {
        var ownsConnection = conn == null;
        conn ??= new SqlConnection(_conn);
        if (ownsConnection) conn.Open();

        try
        {
            using var cmd = tx == null
                ? new SqlCommand(@"
                    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(SalesOrderCode, 3, 10) AS INT)), 0)
                    FROM tblSalesOrder
                    WHERE SalesOrderCode LIKE 'SO[0-9]%';", conn)
                : new SqlCommand(@"
                    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(SalesOrderCode, 3, 10) AS INT)), 0)
                    FROM tblSalesOrder
                    WHERE SalesOrderCode LIKE 'SO[0-9]%';", conn, tx);
            var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"SO{next:D6}";
        }
        finally
        {
            if (ownsConnection) conn.Dispose();
        }
    }

    private void EnsureDefaultItemRow()
    {
        if (ItemRecords.Count == 0)
            ItemRecords.Add(new SalesOrderItemInput());
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
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
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        return decimal.TryParse(value, out var d) ? d : 0m;
    }
}
