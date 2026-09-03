using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class GoodsIssueListItem
    {
        public int GoodsIssueID { get; set; }
        public string GoodsIssueCode { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public string SalesOrderCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal TotalQty { get; set; }
    }

    public class SoLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class IssueLineInput
    {
        public int SalesOrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductDescription { get; set; } = "";
        public string OrderedQty { get; set; } = "";
        public string AlreadyIssued { get; set; } = "";
        public string StockOnHand { get; set; } = "";
        public string IssueQty { get; set; } = "";
    }

    public class SoLineForIssue
    {
        public int SalesOrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductDescription { get; set; } = "";
        public decimal OrderedQty { get; set; }
        public decimal IssuedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal StockOnHand { get; set; }
    }

    public partial class GoodsIssueMasterPage : AppBasePage
    {
        private readonly InventoryService _inventory = new InventoryService();

        public string PageTitle => "Goods Issue";
        public List<GoodsIssueListItem> Issues { get; set; } = new List<GoodsIssueListItem>();
        public List<SoLookupItem> OpenSalesOrders { get; set; } = new List<SoLookupItem>();
        public List<IssueLineInput> Lines { get; set; } = new List<IssueLineInput>();
        public bool ShowForm { get; set; }
        public int SelectedSoId { get; set; }
        public string IssueDate { get; set; } = "";
        public string CustomerName { get; set; } = "";
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

            var newIssue = Request.QueryString["newIssue"] == "1" || Request.QueryString["newIssue"] == "true";
            OnGet(newIssue, QueryInt("soId"));
        }

        private void OnGet(bool newIssue, int? soId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = newIssue;
            if (ShowForm)
            {
                IssueDate = DateTime.Today.ToString("yyyy-MM-dd");
                LoadOpenSalesOrders();
                if (soId.HasValue && soId > 0)
                {
                    SelectedSoId = soId.Value;
                    LoadSoLines(soId.Value);
                }
            }
            else
            {
                LoadIssues();
            }
        }

        private void OnPostSave()
        {
            ShowForm = true;
            IssueDate = FormString("IssueDate");
            SelectedSoId = int.TryParse(Request.Form["SalesOrderID"], out var soId) ? soId : 0;
            CustomerName = FormString("CustomerName");
            Remarks = FormString("Remarks");
            Lines = WebFormsJson.DeserializeList<IssueLineInput>(Request.Form["LinesJson"]);
            LoadOpenSalesOrders();

            if (SelectedSoId <= 0)
            {
                AlertMessage = "Please select a sales order.";
                AlertType = "error";
                return;
            }

            var activeLines = Lines.Where(l => DecimalValue(l.IssueQty) > 0).ToList();
            if (activeLines.Count == 0)
            {
                AlertMessage = "Enter issue quantity for at least one line.";
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
                        var giCode = _inventory.GenerateNextCode(conn, tx, "tblGoodsIssue", "GoodsIssueCode", "GI", 6);
                        int giId;
                        using (var ins = new SqlCommand(@"
INSERT INTO tblGoodsIssue
    (GoodsIssueCode, IssueDate, SalesOrderID, CustomerName, Remarks, IssueStatus, CreatedOn, CreatedByUserID)
VALUES
    (@Code, @Date, @SOId, @Customer, @Remarks, 'Posted', GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@Code", giCode);
                            ins.Parameters.AddWithValue("@Date", DateTime.Parse(IssueDate));
                            ins.Parameters.AddWithValue("@SOId", SelectedSoId);
                            ins.Parameters.AddWithValue("@Customer", CustomerName);
                            ins.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(Remarks) ? (object)DBNull.Value : Remarks);
                            AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                            giId = (int)ins.ExecuteScalar();
                        }

                        int sort = 0;
                        foreach (var line in activeLines)
                        {
                            var issueQty = DecimalValue(line.IssueQty);
                            var soItemId = line.SalesOrderItemID;

                            ValidatePendingIssue(conn, tx, soItemId, issueQty);

                            decimal unitCost = 0;
                            using (var costCmd = new SqlCommand("SELECT ISNULL(AvgUnitCost, 0) FROM tblProductStock WHERE ProductID = @Id;", conn, tx))
                            {
                                costCmd.Parameters.AddWithValue("@Id", line.ProductID);
                                var c = costCmd.ExecuteScalar();
                                unitCost = c == null || c == DBNull.Value ? 0m : Convert.ToDecimal(c);
                            }

                            using (var insItem = new SqlCommand(@"
INSERT INTO tblGoodsIssueItem
    (GoodsIssueID, SalesOrderItemID, ProductID, ProductCode, ProductDescription,
     IssuedQty, UnitCost, SortOrder, CreatedOn, CreatedByUserID)
VALUES
    (@GIId, @SOItemId, @ProductID, @Code, @Desc, @Qty, @Cost, @Sort, GETDATE(), @CreatedByUserID);", conn, tx))
                            {
                                insItem.Parameters.AddWithValue("@GIId", giId);
                                insItem.Parameters.AddWithValue("@SOItemId", soItemId);
                                insItem.Parameters.AddWithValue("@ProductID", line.ProductID);
                                insItem.Parameters.AddWithValue("@Code", line.ProductCode);
                                insItem.Parameters.AddWithValue("@Desc", line.ProductDescription);
                                insItem.Parameters.AddWithValue("@Qty", issueQty);
                                insItem.Parameters.AddWithValue("@Cost", unitCost);
                                insItem.Parameters.AddWithValue("@Sort", sort++);
                                AuditHelper.AddCreatedBy(insItem, Auth.CurrentUserId);
                                insItem.ExecuteNonQuery();
                            }

                            using (var updSo = new SqlCommand(@"
UPDATE tblSalesOrderItem SET IssuedQty = IssuedQty + @Qty WHERE SalesOrderItemID = @Id;", conn, tx))
                            {
                                updSo.Parameters.AddWithValue("@Qty", issueQty);
                                updSo.Parameters.AddWithValue("@Id", soItemId);
                                updSo.ExecuteNonQuery();
                            }

                            _inventory.IssueStock(conn, tx, line.ProductID, issueQty, "GIN",
                                SelectedSoId, soItemId, giId, "GI " + giCode);
                        }

                        UpdateSoStatusIfComplete(conn, tx, SelectedSoId);
                        tx.Commit();

                        Audit.Log("Create", "Goods Issue", giId, giCode, "GoodsIssueMaster", "/GoodsIssueMaster.aspx",
                            "Save", "SO #" + SelectedSoId + ", " + activeLines.Count + " line(s)");

                        SetAlert("Goods issue " + giCode + " posted. Stock updated.");
                    }
                }
                Response.Redirect("~/GoodsIssueMaster.aspx");
            }
            catch (Exception ex)
            {
                AlertMessage = "Error: " + ex.Message;
                AlertType = "error";
                if (SelectedSoId > 0) LoadSoLines(SelectedSoId);
            }
        }

        private void ValidatePendingIssue(SqlConnection conn, SqlTransaction tx, int soItemId, decimal issueQty)
        {
            using (var cmd = new SqlCommand(@"
SELECT Qty, ISNULL(IssuedQty, 0) FROM tblSalesOrderItem WHERE SalesOrderItemID = @Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", soItemId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) throw new InvalidOperationException("Sales order line not found.");
                    var ordered = dr.GetDecimal(0);
                    var issued = dr.GetDecimal(1);
                    if (issueQty > ordered - issued)
                        throw new InvalidOperationException("Issue quantity exceeds pending amount for line " + soItemId + ".");
                }
            }
        }

        private void UpdateSoStatusIfComplete(SqlConnection conn, SqlTransaction tx, int soId)
        {
            using (var cmd = new SqlCommand(@"
SELECT COUNT(*) FROM tblSalesOrderItem
WHERE SalesOrderID = @Id AND Qty > ISNULL(IssuedQty, 0);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", soId);
                var openLines = Convert.ToInt32(cmd.ExecuteScalar());
                var newStatus = openLines == 0 ? "Delivered" : "Approved";
                using (var upd = new SqlCommand(@"
UPDATE tblSalesOrder SET OrderStatus = @Status, ModifiedOn = GETDATE(), ModifiedByUserID = @UserId
WHERE SalesOrderID = @Id AND OrderStatus NOT IN ('Cancelled', 'Draft');", conn, tx))
                {
                    upd.Parameters.AddWithValue("@Status", newStatus);
                    upd.Parameters.AddWithValue("@Id", soId);
                    upd.Parameters.AddWithValue("@UserId", (object)Auth.CurrentUserId ?? DBNull.Value);
                    upd.ExecuteNonQuery();
                }

                if (openLines == 0)
                {
                    using (var hist = new SqlCommand(@"
INSERT INTO tblSalesOrderHistory (SalesOrderID, ActionType, FromStatus, ToStatus, ActionAt, ActionByUserID, ActionByUsername)
VALUES (@SOId, 'Deliver', 'Submitted', 'Delivered', GETDATE(), @UserId, @User);", conn, tx))
                    {
                        hist.Parameters.AddWithValue("@SOId", soId);
                        hist.Parameters.AddWithValue("@UserId", (object)Auth.CurrentUserId ?? DBNull.Value);
                        hist.Parameters.AddWithValue("@User", (object)Auth.CurrentUsername ?? DBNull.Value);
                        hist.ExecuteNonQuery();
                    }
                }
            }
        }

        private string GetCustomerName(int soId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT CustomerName FROM tblSalesOrder WHERE SalesOrderID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", soId);
                conn.Open();
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        private void LoadIssues()
        {
            Issues.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT gi.GoodsIssueID, gi.GoodsIssueCode, gi.IssueDate,
       ISNULL(so.SalesOrderCode, ''), ISNULL(gi.CustomerName, ''),
       ISNULL(SUM(gii.IssuedQty), 0)
FROM tblGoodsIssue gi
LEFT JOIN tblSalesOrder so ON so.SalesOrderID = gi.SalesOrderID
LEFT JOIN tblGoodsIssueItem gii ON gii.GoodsIssueID = gi.GoodsIssueID
GROUP BY gi.GoodsIssueID, gi.GoodsIssueCode, gi.IssueDate, so.SalesOrderCode, gi.CustomerName
ORDER BY gi.GoodsIssueID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Issues.Add(new GoodsIssueListItem
                        {
                            GoodsIssueID = dr.GetInt32(0),
                            GoodsIssueCode = dr.GetString(1),
                            IssueDate = dr.GetDateTime(2),
                            SalesOrderCode = dr.GetString(3),
                            CustomerName = dr.GetString(4),
                            TotalQty = dr.GetDecimal(5)
                        });
                    }
                }
            }
        }

        private void LoadOpenSalesOrders()
        {
            OpenSalesOrders.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DISTINCT so.SalesOrderID, so.SalesOrderCode + N' – ' + ISNULL(so.CustomerName, '')
FROM tblSalesOrder so
INNER JOIN tblSalesOrderItem i ON i.SalesOrderID = so.SalesOrderID
WHERE so.OrderStatus IN ('Submitted', 'Approved')
  AND i.Qty > ISNULL(i.IssuedQty, 0)
ORDER BY so.SalesOrderID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        OpenSalesOrders.Add(new SoLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }

        private void LoadSoLines(int soId)
        {
            Lines.Clear();
            foreach (var line in FetchSoLines(soId))
            {
                Lines.Add(new IssueLineInput
                {
                    SalesOrderItemID = line.SalesOrderItemID,
                    ProductID = line.ProductID,
                    ProductCode = line.ProductCode,
                    ProductDescription = line.ProductDescription,
                    OrderedQty = line.OrderedQty.ToString("0.####"),
                    AlreadyIssued = line.IssuedQty.ToString("0.####"),
                    StockOnHand = line.StockOnHand.ToString("0.####"),
                    IssueQty = line.PendingQty.ToString("0.####")
                });
            }
            CustomerName = GetCustomerName(soId) ?? "";
        }

        private List<SoLineForIssue> FetchSoLines(int soId)
        {
            var list = new List<SoLineForIssue>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT i.SalesOrderItemID, i.ProductID, i.ProductCode, i.ProductDescription,
       i.Qty, ISNULL(i.IssuedQty, 0), ISNULL(s.QtyOnHand, 0)
FROM tblSalesOrderItem i
LEFT JOIN tblProductStock s ON s.ProductID = i.ProductID
WHERE i.SalesOrderID = @Id AND i.Qty > ISNULL(i.IssuedQty, 0)
ORDER BY i.SortOrder, i.SalesOrderItemID;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", soId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var ordered = dr.GetDecimal(4);
                        var issued = dr.GetDecimal(5);
                        list.Add(new SoLineForIssue
                        {
                            SalesOrderItemID = dr.GetInt32(0),
                            ProductID = dr.IsDBNull(1) ? 0 : dr.GetInt32(1),
                            ProductCode = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            ProductDescription = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            OrderedQty = ordered,
                            IssuedQty = issued,
                            PendingQty = ordered - issued,
                            StockOnHand = dr.GetDecimal(6)
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
