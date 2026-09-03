using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public partial class PurchaseOrderMasterPage : AppBasePage
    {
        public static readonly string[] OrderStatusOptions =
            { "Draft", "Pending", "Approved", "Received", "Cancelled" };

        public string PageTitle => "Purchase Order";
        public List<PurchaseOrderListItem> PurchaseOrders { get; set; } = new List<PurchaseOrderListItem>();
        public List<PartyLookupItem> Vendors { get; set; } = new List<PartyLookupItem>();
        public List<ProductLookupItem> Products { get; set; } = new List<ProductLookupItem>();
        public PurchaseOrderInput Input { get; set; } = new PurchaseOrderInput();
        public List<PurchaseOrderItemInput> ItemRecords { get; set; } = new List<PurchaseOrderItemInput>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public string ProductsJson => WebFormsJson.Serialize(Products);
        public string ItemsJsonInitial => WebFormsJson.Serialize(ItemRecords);

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                OnPostSave();
                return;
            }

            var newPO = Request.QueryString["newPO"] == "1" || Request.QueryString["newPO"] == "true";
            OnGet(QueryInt("editId"), newPO);
        }

        private void OnGet(int? editId, bool newPO)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newPO;

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

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new PurchaseOrderInput
            {
                PurchaseOrderID = int.TryParse(Request.Form["PurchaseOrderID"], out var id) ? id : 0,
                PurchaseOrderCode = FormString("PurchaseOrderCode"),
                PurchaseOrderDate = FormString("PurchaseOrderDate"),
                VendorID = int.TryParse(Request.Form["VendorID"], out var vid) ? vid : 0,
                VendorName = FormString("VendorName"),
                Remarks = FormString("Remarks"),
                OrderStatus = string.IsNullOrWhiteSpace(FormString("OrderStatus")) ? "Draft" : FormString("OrderStatus"),
                TotalQty = FormString("TotalQty"),
                TotalTax = FormString("TotalTax"),
                TotalDiscount = FormString("TotalDiscount"),
                GrandTotal = FormString("GrandTotal")
            };
            if (string.IsNullOrWhiteSpace(Input.TotalQty)) Input.TotalQty = "0";
            if (string.IsNullOrWhiteSpace(Input.TotalTax)) Input.TotalTax = "0";
            if (string.IsNullOrWhiteSpace(Input.TotalDiscount)) Input.TotalDiscount = "0";
            if (string.IsNullOrWhiteSpace(Input.GrandTotal)) Input.GrandTotal = "0";

            ItemRecords = WebFormsJson.DeserializeList<PurchaseOrderItemInput>(Request.Form["ItemsJson"]);

            if (string.IsNullOrWhiteSpace(Input.PurchaseOrderDate))
            {
                SetFormError("Purchase order date is required.");
                return;
            }
            if (Input.VendorID <= 0)
            {
                SetFormError("Please select a vendor from Vendor Master.");
                return;
            }

            var activeItems = ItemRecords.Where(HasItemContent).ToList();
            if (activeItems.Count == 0)
            {
                SetFormError("Add at least one order line item.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
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
                        Input.PurchaseOrderID = poId;
                    }
                }
                SetAlert(EditMode ? "Purchase order updated successfully." : "Purchase order created successfully.");
                Response.Redirect("~/PurchaseOrderMaster.aspx?editId=" + Input.PurchaseOrderID);
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
            }
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
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
                }
                SetAlert("Purchase order deleted successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error deleting purchase order: " + ex.Message, "error");
            }
            Response.Redirect("~/PurchaseOrderMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            if (ItemRecords.Count == 0) EnsureDefaultItemRow();
        }

        private int SavePOCore(SqlConnection conn, SqlTransaction tx, PurchaseOrderInput input,
            decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal)
        {
            var poDate = DateTime.Parse(input.PurchaseOrderDate);

            if (input.PurchaseOrderID > 0)
            {
                using (var cmd = new SqlCommand(@"
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
WHERE PurchaseOrderID = @PurchaseOrderID;", conn, tx))
                {
                    BindPOHeaderParams(cmd, input, poDate, totalQty, totalTax, totalDiscount, grandTotal);
                    cmd.Parameters.AddWithValue("@PurchaseOrderID", input.PurchaseOrderID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                    return input.PurchaseOrderID;
                }
            }

            var code = string.IsNullOrWhiteSpace(input.PurchaseOrderCode)
                ? GenerateNextPOCode(conn, tx)
                : input.PurchaseOrderCode;

            using (var ins = new SqlCommand(@"
INSERT INTO tblPurchaseOrder
    (PurchaseOrderCode, PurchaseOrderDate, VendorID, VendorName, Remarks, OrderStatus,
     TotalQty, TotalTax, TotalDiscount, GrandTotal, CreatedOn, CreatedByUserID)
VALUES
    (@PurchaseOrderCode, @PurchaseOrderDate, @VendorID, @VendorName, @Remarks, @OrderStatus,
     @TotalQty, @TotalTax, @TotalDiscount, @GrandTotal, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
            {
                ins.Parameters.AddWithValue("@PurchaseOrderCode", code);
                BindPOHeaderParams(ins, input, poDate, totalQty, totalTax, totalDiscount, grandTotal);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.PurchaseOrderCode = code;
                return (int)ins.ExecuteScalar();
            }
        }

        private static void BindPOHeaderParams(SqlCommand cmd, PurchaseOrderInput input, DateTime poDate,
            decimal totalQty, decimal totalTax, decimal totalDiscount, decimal grandTotal)
        {
            cmd.Parameters.AddWithValue("@PurchaseOrderDate", poDate);
            cmd.Parameters.AddWithValue("@VendorID", input.VendorID > 0 ? (object)input.VendorID : DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorName", input.VendorName);
            cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(input.Remarks) ? (object)DBNull.Value : input.Remarks);
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
                using (var ins = new SqlCommand(@"
INSERT INTO tblPurchaseOrderItem
    (PurchaseOrderID, ProductID, ProductCode, ProductDescription, Qty, UnitPrice,
     TaxAmount, DiscountAmount, NetAmount, SortOrder, CreatedOn, CreatedByUserID)
VALUES
    (@PurchaseOrderID, @ProductID, @ProductCode, @ProductDescription, @Qty, @UnitPrice,
     @TaxAmount, @DiscountAmount, @NetAmount, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@PurchaseOrderID", poId);
                    ins.Parameters.AddWithValue("@ProductID", line.ProductID > 0 ? (object)line.ProductID : DBNull.Value);
                    ins.Parameters.AddWithValue("@ProductCode", line.ProductCode?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@ProductDescription", line.ProductDescription?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@Qty", DecimalValue(line.Qty));
                    ins.Parameters.AddWithValue("@UnitPrice", DecimalValue(line.UnitPrice));
                    ins.Parameters.AddWithValue("@TaxAmount", DecimalValue(line.TaxAmount));
                    ins.Parameters.AddWithValue("@DiscountAmount", DecimalValue(line.DiscountAmount));
                    ins.Parameters.AddWithValue("@NetAmount", net);
                    ins.Parameters.AddWithValue("@SortOrder", sort);
                    AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                    ins.ExecuteNonQuery();
                }
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
            PurchaseOrders.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
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
ORDER BY po.PurchaseOrderID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadForEdit(int poId)
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
SELECT PurchaseOrderID, PurchaseOrderCode, PurchaseOrderDate, VendorID, VendorName,
       Remarks, OrderStatus, TotalQty, TotalTax, TotalDiscount, GrandTotal
FROM tblPurchaseOrder WHERE PurchaseOrderID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    using (var dr = cmd.ExecuteReader())
                    {
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
                }

                ItemRecords.Clear();
                using (var cmd = new SqlCommand(@"
SELECT ProductID, ProductCode, ProductDescription, Qty, UnitPrice, TaxAmount, DiscountAmount
FROM tblPurchaseOrderItem
WHERE PurchaseOrderID = @Id
ORDER BY SortOrder, PurchaseOrderItemID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", poId);
                    using (var dr = cmd.ExecuteReader())
                    {
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

        private List<PartyLookupItem> LoadVendorLookup()
        {
            var items = new List<PartyLookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT VendorID, VendorCode, Name
FROM tblVendor
WHERE IsActive = 1
ORDER BY VendorCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        var name = dr.IsDBNull(2) ? "" : dr.GetString(2);
                        items.Add(new PartyLookupItem
                        {
                            Id = dr.GetInt32(0),
                            Code = code,
                            Name = string.IsNullOrEmpty(code) ? name : code + " – " + name
                        });
                    }
                }
            }
            return items;
        }

        private List<ProductLookupItem> LoadProductLookup()
        {
            var items = new List<ProductLookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ProductID, ProductCode, ProductName
FROM tblProduct
WHERE IsActive = 1
ORDER BY ProductCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.GetString(1);
                        var name = dr.GetString(2);
                        items.Add(new ProductLookupItem
                        {
                            Id = dr.GetInt32(0),
                            Code = code,
                            Name = code + " – " + name,
                            ProductName = name
                        });
                    }
                }
            }
            return items;
        }

        private string GenerateNextPOCode(SqlConnection conn = null, SqlTransaction tx = null)
        {
            var ownsConnection = conn == null;
            if (ownsConnection)
            {
                conn = new SqlConnection(Conn);
                conn.Open();
            }
            try
            {
                using (var cmd = tx == null
                    ? new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PurchaseOrderCode, 3, 10) AS INT)), 0)
FROM tblPurchaseOrder
WHERE PurchaseOrderCode LIKE 'PO[0-9]%';", conn)
                    : new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PurchaseOrderCode, 3, 10) AS INT)), 0)
FROM tblPurchaseOrder
WHERE PurchaseOrderCode LIKE 'PO[0-9]%';", conn, tx))
                {
                    var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    return "PO" + next.ToString("D6");
                }
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

        public static string StatusCss(string status)
        {
            if (string.IsNullOrEmpty(status)) return "status-draft";
            switch (status.ToLowerInvariant())
            {
                case "pending": return "status-pending";
                case "approved": return "status-approved";
                case "received": return "status-received";
                case "cancelled": return "status-cancelled";
                default: return "status-draft";
            }
        }

        private static decimal DecimalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            return decimal.TryParse(value, out var d) ? d : 0m;
        }
    }
}
