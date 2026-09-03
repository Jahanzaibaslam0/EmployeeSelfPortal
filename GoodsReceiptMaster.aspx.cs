using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class GoodsReceiptListItem
    {
        public int GoodsReceiptID { get; set; }
        public string GoodsReceiptCode { get; set; } = "";
        public DateTime ReceiptDate { get; set; }
        public string PurchaseOrderCode { get; set; } = "";
        public string VendorName { get; set; } = "";
        public decimal TotalQty { get; set; }
    }

    public class PoLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ReceiptLineInput
    {
        public int PurchaseOrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductDescription { get; set; } = "";
        public string OrderedQty { get; set; } = "";
        public string AlreadyReceived { get; set; } = "";
        public string ReceiveQty { get; set; } = "";
        public string UnitCost { get; set; } = "";
    }

    public class PoLineForReceipt
    {
        public int PurchaseOrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductDescription { get; set; } = "";
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public partial class GoodsReceiptMasterPage : AppBasePage
    {
        private readonly InventoryService _inventory = new InventoryService();

        public string PageTitle => "Goods Receipt";
        public List<GoodsReceiptListItem> Receipts { get; set; } = new List<GoodsReceiptListItem>();
        public List<PoLookupItem> OpenPurchaseOrders { get; set; } = new List<PoLookupItem>();
        public List<ReceiptLineInput> Lines { get; set; } = new List<ReceiptLineInput>();
        public bool ShowForm { get; set; }
        public int SelectedPoId { get; set; }
        public string ReceiptDate { get; set; } = "";
        public string VendorName { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public string LinesJsonInitial => WebFormsJson.Serialize(Lines);

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                OnPostSave();
                return;
            }

            var newReceipt = Request.QueryString["newReceipt"] == "1" || Request.QueryString["newReceipt"] == "true";
            OnGet(newReceipt, QueryInt("poId"));
        }

        private void OnGet(bool newReceipt, int? poId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = newReceipt;
            if (ShowForm)
            {
                ReceiptDate = DateTime.Today.ToString("yyyy-MM-dd");
                LoadOpenPurchaseOrders();
                if (poId.HasValue && poId > 0)
                {
                    SelectedPoId = poId.Value;
                    LoadPoLines(poId.Value);
                }
            }
            else
            {
                LoadReceipts();
            }
        }

        private void OnPostSave()
        {
            ShowForm = true;
            ReceiptDate = FormString("ReceiptDate");
            SelectedPoId = int.TryParse(Request.Form["PurchaseOrderID"], out var poId) ? poId : 0;
            VendorName = FormString("VendorName");
            Remarks = FormString("Remarks");
            Lines = WebFormsJson.DeserializeList<ReceiptLineInput>(Request.Form["LinesJson"]);
            LoadOpenPurchaseOrders();

            if (SelectedPoId <= 0)
            {
                AlertMessage = "Please select a purchase order.";
                AlertType = "error";
                return;
            }

            var activeLines = Lines.Where(l => DecimalValue(l.ReceiveQty) > 0).ToList();
            if (activeLines.Count == 0)
            {
                AlertMessage = "Enter receive quantity for at least one line.";
                AlertType = "error";
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var grCode = _inventory.GenerateNextCode(conn, tx, "tblGoodsReceipt", "GoodsReceiptCode", "GR", 6);
                        int grId;
                        using (var ins = new SqlCommand(@"
INSERT INTO tblGoodsReceipt
    (GoodsReceiptCode, ReceiptDate, PurchaseOrderID, VendorName, Remarks, ReceiptStatus, CreatedOn, CreatedByUserID)
VALUES
    (@Code, @Date, @POId, @Vendor, @Remarks, 'Posted', GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@Code", grCode);
                            ins.Parameters.AddWithValue("@Date", DateTime.Parse(ReceiptDate));
                            ins.Parameters.AddWithValue("@POId", SelectedPoId);
                            ins.Parameters.AddWithValue("@Vendor", VendorName);
                            ins.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(Remarks) ? (object)DBNull.Value : Remarks);
                            AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                            grId = (int)ins.ExecuteScalar();
                        }

                        int sort = 0;
                        foreach (var line in activeLines)
                        {
                            var recvQty = DecimalValue(line.ReceiveQty);
                            var unitCost = DecimalValue(line.UnitCost);
                            var poItemId = line.PurchaseOrderItemID;

                            ValidatePendingQty(conn, tx, poItemId, recvQty);

                            using (var insItem = new SqlCommand(@"
INSERT INTO tblGoodsReceiptItem
    (GoodsReceiptID, PurchaseOrderItemID, ProductID, ProductCode, ProductDescription,
     ReceivedQty, UnitCost, SortOrder, CreatedOn, CreatedByUserID)
VALUES
    (@GRId, @POItemId, @ProductID, @Code, @Desc, @Qty, @Cost, @Sort, GETDATE(), @CreatedByUserID);", conn, tx))
                            {
                                insItem.Parameters.AddWithValue("@GRId", grId);
                                insItem.Parameters.AddWithValue("@POItemId", poItemId);
                                insItem.Parameters.AddWithValue("@ProductID", line.ProductID);
                                insItem.Parameters.AddWithValue("@Code", line.ProductCode);
                                insItem.Parameters.AddWithValue("@Desc", line.ProductDescription);
                                insItem.Parameters.AddWithValue("@Qty", recvQty);
                                insItem.Parameters.AddWithValue("@Cost", unitCost);
                                insItem.Parameters.AddWithValue("@Sort", sort++);
                                AuditHelper.AddCreatedBy(insItem, Auth.CurrentUserId);
                                insItem.ExecuteNonQuery();
                            }

                            using (var updPo = new SqlCommand(@"
UPDATE tblPurchaseOrderItem SET ReceivedQty = ReceivedQty + @Qty WHERE PurchaseOrderItemID = @Id;", conn, tx))
                            {
                                updPo.Parameters.AddWithValue("@Qty", recvQty);
                                updPo.Parameters.AddWithValue("@Id", poItemId);
                                updPo.ExecuteNonQuery();
                            }

                            _inventory.ReceiveStock(conn, tx, line.ProductID, recvQty, unitCost, "GRN",
                                SelectedPoId, poItemId, grId, "GR " + grCode);
                        }

                        UpdatePoStatusIfComplete(conn, tx, SelectedPoId);
                        tx.Commit();

                        Audit.Log("Create", "Goods Receipt", grId, grCode, "GoodsReceiptMaster", "/GoodsReceiptMaster.aspx",
                            "Save", "PO #" + SelectedPoId + ", " + activeLines.Count + " line(s)");

                        SetAlert("Goods receipt " + grCode + " posted. Stock updated.");
                    }
                }
                Response.Redirect("~/GoodsReceiptMaster.aspx");
            }
            catch (Exception ex)
            {
                AlertMessage = "Error: " + ex.Message;
                AlertType = "error";
                if (SelectedPoId > 0) LoadPoLines(SelectedPoId);
            }
        }

        private void ValidatePendingQty(SqlConnection conn, SqlTransaction tx, int poItemId, decimal recvQty)
        {
            using (var cmd = new SqlCommand(@"
SELECT Qty, ISNULL(ReceivedQty, 0) FROM tblPurchaseOrderItem WHERE PurchaseOrderItemID = @Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", poItemId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) throw new InvalidOperationException("Purchase order line not found.");
                    var ordered = dr.GetDecimal(0);
                    var received = dr.GetDecimal(1);
                    if (recvQty > ordered - received)
                        throw new InvalidOperationException("Receive quantity exceeds pending amount for line " + poItemId + ".");
                }
            }
        }

        private void UpdatePoStatusIfComplete(SqlConnection conn, SqlTransaction tx, int poId)
        {
            using (var cmd = new SqlCommand(@"
SELECT COUNT(*) FROM tblPurchaseOrderItem
WHERE PurchaseOrderID = @Id AND Qty > ISNULL(ReceivedQty, 0);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", poId);
                var openLines = Convert.ToInt32(cmd.ExecuteScalar());
                if (openLines == 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblPurchaseOrder SET OrderStatus = 'Received', ModifiedOn = GETDATE(), ModifiedByUserID = @UserId
WHERE PurchaseOrderID = @Id;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@Id", poId);
                        upd.Parameters.AddWithValue("@UserId", (object)Auth.CurrentUserId ?? DBNull.Value);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblPurchaseOrder SET OrderStatus = CASE WHEN OrderStatus = 'Draft' THEN 'Approved' ELSE OrderStatus END,
    ModifiedOn = GETDATE(), ModifiedByUserID = @UserId
WHERE PurchaseOrderID = @Id AND OrderStatus NOT IN ('Received', 'Cancelled');", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@Id", poId);
                        upd.Parameters.AddWithValue("@UserId", (object)Auth.CurrentUserId ?? DBNull.Value);
                        upd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void LoadReceipts()
        {
            Receipts.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT gr.GoodsReceiptID, gr.GoodsReceiptCode, gr.ReceiptDate,
       ISNULL(po.PurchaseOrderCode, ''), ISNULL(gr.VendorName, ''),
       ISNULL(SUM(gri.ReceivedQty), 0)
FROM tblGoodsReceipt gr
LEFT JOIN tblPurchaseOrder po ON po.PurchaseOrderID = gr.PurchaseOrderID
LEFT JOIN tblGoodsReceiptItem gri ON gri.GoodsReceiptID = gr.GoodsReceiptID
GROUP BY gr.GoodsReceiptID, gr.GoodsReceiptCode, gr.ReceiptDate, po.PurchaseOrderCode, gr.VendorName
ORDER BY gr.GoodsReceiptID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Receipts.Add(new GoodsReceiptListItem
                        {
                            GoodsReceiptID = dr.GetInt32(0),
                            GoodsReceiptCode = dr.GetString(1),
                            ReceiptDate = dr.GetDateTime(2),
                            PurchaseOrderCode = dr.GetString(3),
                            VendorName = dr.GetString(4),
                            TotalQty = dr.GetDecimal(5)
                        });
                    }
                }
            }
        }

        private void LoadOpenPurchaseOrders()
        {
            OpenPurchaseOrders.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DISTINCT po.PurchaseOrderID, po.PurchaseOrderCode + N' – ' + ISNULL(po.VendorName, '')
FROM tblPurchaseOrder po
INNER JOIN tblPurchaseOrderItem i ON i.PurchaseOrderID = po.PurchaseOrderID
WHERE po.OrderStatus NOT IN ('Cancelled', 'Draft')
  AND i.Qty > ISNULL(i.ReceivedQty, 0)
ORDER BY po.PurchaseOrderID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        OpenPurchaseOrders.Add(new PoLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }

        private void LoadPoLines(int poId)
        {
            Lines.Clear();
            foreach (var line in FetchPoLines(poId))
            {
                Lines.Add(new ReceiptLineInput
                {
                    PurchaseOrderItemID = line.PurchaseOrderItemID,
                    ProductID = line.ProductID,
                    ProductCode = line.ProductCode,
                    ProductDescription = line.ProductDescription,
                    OrderedQty = line.OrderedQty.ToString("0.####"),
                    AlreadyReceived = line.ReceivedQty.ToString("0.####"),
                    ReceiveQty = line.PendingQty.ToString("0.####"),
                    UnitCost = line.UnitPrice.ToString("0.####")
                });
            }
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT VendorName FROM tblPurchaseOrder WHERE PurchaseOrderID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", poId);
                conn.Open();
                VendorName = cmd.ExecuteScalar()?.ToString() ?? "";
            }
        }

        private List<PoLineForReceipt> FetchPoLines(int poId)
        {
            var list = new List<PoLineForReceipt>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT PurchaseOrderItemID, ProductID, ProductCode, ProductDescription,
       Qty, ISNULL(ReceivedQty, 0), UnitPrice
FROM tblPurchaseOrderItem
WHERE PurchaseOrderID = @Id AND Qty > ISNULL(ReceivedQty, 0)
ORDER BY SortOrder, PurchaseOrderItemID;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", poId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var ordered = dr.GetDecimal(4);
                        var received = dr.GetDecimal(5);
                        list.Add(new PoLineForReceipt
                        {
                            PurchaseOrderItemID = dr.GetInt32(0),
                            ProductID = dr.IsDBNull(1) ? 0 : dr.GetInt32(1),
                            ProductCode = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            ProductDescription = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            OrderedQty = ordered,
                            ReceivedQty = received,
                            PendingQty = ordered - received,
                            UnitPrice = dr.GetDecimal(6)
                        });
                    }
                }
            }
            return list;
        }

        private static decimal DecimalValue(string v) =>
            decimal.TryParse(v, out var d) ? d : 0m;
    }
}
