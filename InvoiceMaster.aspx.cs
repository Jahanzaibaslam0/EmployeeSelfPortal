using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class InvoiceListItem
    {
        public int InvoiceID { get; set; }
        public string InvoiceCode { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public string InvoiceType { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string InvoiceRefNo { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int LineCount { get; set; }
    }

    public class InvoiceInput
    {
        public int InvoiceID { get; set; }
        public string InvoiceCode { get; set; } = "";
        public string InvoiceDate { get; set; } = "";
        public string InvoiceType { get; set; } = "";
        public string BuyerName { get; set; } = "Ghazi Brothers";
        public string BuyerNTNCNIC { get; set; } = "";
        public string BuyerProvince { get; set; } = "";
        public string BuyerAddress { get; set; } = "";
        public string BuyerRegistrationType { get; set; } = "";
        public string InvoiceRefNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerNTNCNIC { get; set; } = "";
        public int CustomerID { get; set; }
        public string CustomerAddress { get; set; } = "";
        public string TotalAmount { get; set; } = "0";
    }

    public class InvoiceItemInput
    {
        public int ProductID { get; set; }
        public string ItemID { get; set; } = "";
        public string HSCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Qty { get; set; } = "";
        public string UnitOfMeasure { get; set; } = "";
        public string UnitPrice { get; set; } = "";
        public string TaxAmount { get; set; } = "";
        public string ExtraTax { get; set; } = "";
        public string FedPayable { get; set; } = "";
        public string SalesType { get; set; } = "";
        public string SroItemSerialNo { get; set; } = "";
        public string FurtherTax { get; set; } = "";
        public string Discount { get; set; } = "";
    }

    public partial class InvoiceMasterPage : AppBasePage
    {
        public static readonly string[] InvoiceTypeOptions =
            { "Sale Invoice", "Debit Note", "Credit Note", "Export Invoice", "Other" };
        public static readonly string[] BuyerRegistrationTypeOptions =
            { "Registered", "Unregistered", "Unregistered Distributor", "Consumer" };
        public static readonly string[] SalesTypeOptions =
            { "Local", "Export", "Exempt", "Zero Rated", "Other" };

        public string PageTitle => "Invoice Master";
        public List<InvoiceListItem> Invoices { get; set; } = new List<InvoiceListItem>();
        public List<LookupItem> Customers { get; set; } = new List<LookupItem>();
        public List<ProductLookupItem> Products { get; set; } = new List<ProductLookupItem>();
        public InvoiceInput Input { get; set; } = new InvoiceInput();
        public List<InvoiceItemInput> ItemRecords { get; set; } = new List<InvoiceItemInput>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public string ProductsJson => WebFormsJson.Serialize(Products);
        public string SalesTypesJson => WebFormsJson.Serialize(SalesTypeOptions);
        public string ItemsJsonInit => WebFormsJson.Serialize(ItemRecords);

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

            var newInvoice = Request.QueryString["newInvoice"] == "1" || Request.QueryString["newInvoice"] == "true";
            OnGet(QueryInt("editId"), newInvoice);
        }

        private void OnGet(int? editId, bool newInvoice)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newInvoice;

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
                    Input.InvoiceCode = GenerateNextInvoiceCode();
                    Input.InvoiceDate = DateTime.Today.ToString("yyyy-MM-dd");
                    Input.BuyerName = "Ghazi Brothers";
                    EnsureDefaultItemRow();
                }
            }
            else
            {
                LoadInvoices();
            }
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new InvoiceInput
            {
                InvoiceID = int.TryParse(Request.Form["InvoiceID"], out var iid) ? iid : 0,
                InvoiceCode = FormString("InvoiceCode"),
                InvoiceDate = FormString("InvoiceDate"),
                InvoiceType = FormString("InvoiceType"),
                BuyerName = FormString("BuyerName"),
                BuyerNTNCNIC = FormString("BuyerNTNCNIC"),
                BuyerProvince = FormString("BuyerProvince"),
                BuyerAddress = FormString("BuyerAddress"),
                BuyerRegistrationType = FormString("BuyerRegistrationType"),
                InvoiceRefNo = FormString("InvoiceRefNo"),
                CustomerName = FormString("CustomerName"),
                CustomerNTNCNIC = FormString("CustomerNTNCNIC"),
                CustomerID = int.TryParse(Request.Form["CustomerID"], out var cid) ? cid : 0,
                CustomerAddress = FormString("CustomerAddress"),
                TotalAmount = FormString("TotalAmount")
            };
            ItemRecords = WebFormsJson.DeserializeList<InvoiceItemInput>(Request.Form["ItemsJson"]);

            if (string.IsNullOrWhiteSpace(Input.InvoiceDate))
            {
                SetFormError("Invoice date is required.");
                return;
            }

            var activeItems = ItemRecords.Where(HasItemContent).ToList();
            if (activeItems.Count == 0)
            {
                SetFormError("Add at least one invoice line item.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var total = activeItems.Sum(CalcLineTotal);
                        Input.TotalAmount = total.ToString("0.##");
                        var invoiceId = SaveInvoiceCore(conn, tx, Input, total);
                        ReplaceInvoiceItems(conn, tx, invoiceId, activeItems);
                        tx.Commit();
                        Input.InvoiceID = invoiceId;
                    }
                }
                SetAlert(EditMode ? "Invoice updated successfully." : "Invoice added successfully.");
                Response.Redirect("~/InvoiceMaster.aspx?editId=" + Input.InvoiceID);
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
                    using (var delItems = new SqlCommand("DELETE FROM tblInvoiceItem WHERE InvoiceID = @Id;", conn))
                    {
                        delItems.Parameters.AddWithValue("@Id", deleteId);
                        delItems.ExecuteNonQuery();
                    }
                    using (var delHeader = new SqlCommand("DELETE FROM tblInvoice WHERE InvoiceID = @Id;", conn))
                    {
                        delHeader.Parameters.AddWithValue("@Id", deleteId);
                        delHeader.ExecuteNonQuery();
                    }
                }
                SetAlert("Invoice deleted successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error deleting invoice: " + ex.Message, "error");
            }
            Response.Redirect("~/InvoiceMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            if (ItemRecords.Count == 0) EnsureDefaultItemRow();
            if (!EditMode && string.IsNullOrEmpty(Input.InvoiceCode))
                Input.InvoiceCode = GenerateNextInvoiceCode();
        }

        private int SaveInvoiceCore(SqlConnection conn, SqlTransaction tx, InvoiceInput input, decimal totalAmount)
        {
            var invoiceDate = DateTime.Parse(input.InvoiceDate);
            if (input.InvoiceID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblInvoice SET InvoiceDate=@InvoiceDate, InvoiceType=@InvoiceType, BuyerName=@BuyerName,
  BuyerNTNCNIC=@BuyerNTNCNIC, BuyerProvince=@BuyerProvince, BuyerAddress=@BuyerAddress,
  BuyerRegistrationType=@BuyerRegistrationType, InvoiceRefNo=@InvoiceRefNo, CustomerName=@CustomerName,
  CustomerNTNCNIC=@CustomerNTNCNIC, CustomerID=@CustomerID, CustomerAddress=@CustomerAddress,
  TotalAmount=@TotalAmount, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE InvoiceID=@InvoiceID;", conn, tx))
                {
                    BindInvoiceHeaderParams(cmd, input, invoiceDate, totalAmount);
                    cmd.Parameters.AddWithValue("@InvoiceID", input.InvoiceID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                    return input.InvoiceID;
                }
            }

            var code = string.IsNullOrWhiteSpace(input.InvoiceCode)
                ? GenerateNextInvoiceCode(conn, tx)
                : input.InvoiceCode;
            using (var ins = new SqlCommand(@"
INSERT INTO tblInvoice
 (InvoiceCode, InvoiceDate, InvoiceType, BuyerName, BuyerNTNCNIC, BuyerProvince, BuyerAddress,
  BuyerRegistrationType, InvoiceRefNo, CustomerName, CustomerNTNCNIC, CustomerID, CustomerAddress,
  TotalAmount, CreatedOn, CreatedByUserID)
VALUES
 (@InvoiceCode, @InvoiceDate, @InvoiceType, @BuyerName, @BuyerNTNCNIC, @BuyerProvince, @BuyerAddress,
  @BuyerRegistrationType, @InvoiceRefNo, @CustomerName, @CustomerNTNCNIC, @CustomerID, @CustomerAddress,
  @TotalAmount, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
            {
                ins.Parameters.AddWithValue("@InvoiceCode", code);
                BindInvoiceHeaderParams(ins, input, invoiceDate, totalAmount);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                return (int)ins.ExecuteScalar();
            }
        }

        private static void BindInvoiceHeaderParams(SqlCommand cmd, InvoiceInput input, DateTime invoiceDate, decimal totalAmount)
        {
            cmd.Parameters.AddWithValue("@InvoiceDate", invoiceDate);
            cmd.Parameters.AddWithValue("@InvoiceType", input.InvoiceType);
            cmd.Parameters.AddWithValue("@BuyerName", input.BuyerName);
            cmd.Parameters.AddWithValue("@BuyerNTNCNIC", input.BuyerNTNCNIC);
            cmd.Parameters.AddWithValue("@BuyerProvince", input.BuyerProvince);
            cmd.Parameters.AddWithValue("@BuyerAddress", input.BuyerAddress);
            cmd.Parameters.AddWithValue("@BuyerRegistrationType", input.BuyerRegistrationType);
            cmd.Parameters.AddWithValue("@InvoiceRefNo", input.InvoiceRefNo);
            cmd.Parameters.AddWithValue("@CustomerName", input.CustomerName);
            cmd.Parameters.AddWithValue("@CustomerNTNCNIC", input.CustomerNTNCNIC);
            cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID > 0 ? (object)input.CustomerID : DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerAddress", input.CustomerAddress);
            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
        }

        private void ReplaceInvoiceItems(SqlConnection conn, SqlTransaction tx, int invoiceId, List<InvoiceItemInput> items)
        {
            using (var del = new SqlCommand("DELETE FROM tblInvoiceItem WHERE InvoiceID=@InvoiceID;", conn, tx))
            {
                del.Parameters.AddWithValue("@InvoiceID", invoiceId);
                del.ExecuteNonQuery();
            }
            int sort = 0;
            foreach (var line in items)
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblInvoiceItem
 (InvoiceID, ProductID, ItemID, HSCode, ProductName, Qty, UnitOfMeasure, UnitPrice,
  TaxAmount, ExtraTax, FedPayable, SalesType, SroItemSerialNo, FurtherTax, Discount,
  SortOrder, CreatedOn, CreatedByUserID)
VALUES
 (@InvoiceID, @ProductID, @ItemID, @HSCode, @ProductName, @Qty, @UnitOfMeasure, @UnitPrice,
  @TaxAmount, @ExtraTax, @FedPayable, @SalesType, @SroItemSerialNo, @FurtherTax, @Discount,
  @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@InvoiceID", invoiceId);
                    ins.Parameters.AddWithValue("@ProductID", line.ProductID > 0 ? (object)line.ProductID : DBNull.Value);
                    ins.Parameters.AddWithValue("@ItemID", line.ItemID?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@HSCode", line.HSCode?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@ProductName", line.ProductName?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@Qty", DecimalValue(line.Qty));
                    ins.Parameters.AddWithValue("@UnitOfMeasure", line.UnitOfMeasure?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@UnitPrice", DecimalValue(line.UnitPrice));
                    ins.Parameters.AddWithValue("@TaxAmount", DecimalValue(line.TaxAmount));
                    ins.Parameters.AddWithValue("@ExtraTax", DecimalValue(line.ExtraTax));
                    ins.Parameters.AddWithValue("@FedPayable", DecimalValue(line.FedPayable));
                    ins.Parameters.AddWithValue("@SalesType", line.SalesType?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@SroItemSerialNo", line.SroItemSerialNo?.Trim() ?? "");
                    ins.Parameters.AddWithValue("@FurtherTax", DecimalValue(line.FurtherTax));
                    ins.Parameters.AddWithValue("@Discount", DecimalValue(line.Discount));
                    ins.Parameters.AddWithValue("@SortOrder", sort);
                    AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                    ins.ExecuteNonQuery();
                    sort++;
                }
            }
        }

        private static bool HasItemContent(InvoiceItemInput line) =>
            line.ProductID > 0
            || !string.IsNullOrWhiteSpace(line.ProductName)
            || !string.IsNullOrWhiteSpace(line.ItemID)
            || DecimalValue(line.Qty) > 0
            || DecimalValue(line.UnitPrice) > 0;

        private static decimal CalcLineTotal(InvoiceItemInput line)
        {
            return DecimalValue(line.Qty) * DecimalValue(line.UnitPrice)
                + DecimalValue(line.TaxAmount) + DecimalValue(line.ExtraTax)
                + DecimalValue(line.FedPayable) + DecimalValue(line.FurtherTax)
                - DecimalValue(line.Discount);
        }

        private void LoadInvoices()
        {
            Invoices.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT i.InvoiceID, i.InvoiceCode, i.InvoiceDate, ISNULL(i.InvoiceType,''), ISNULL(i.CustomerName,''),
       ISNULL(i.InvoiceRefNo,''), ISNULL(i.TotalAmount,0), ISNULL(agg.LineCount,0)
FROM tblInvoice i
LEFT JOIN (SELECT InvoiceID, COUNT(*) AS LineCount FROM tblInvoiceItem GROUP BY InvoiceID) agg
  ON agg.InvoiceID = i.InvoiceID
ORDER BY i.InvoiceID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Invoices.Add(new InvoiceListItem
                        {
                            InvoiceID = dr.GetInt32(0),
                            InvoiceCode = dr.GetString(1),
                            InvoiceDate = dr.GetDateTime(2),
                            InvoiceType = dr.GetString(3),
                            CustomerName = dr.GetString(4),
                            InvoiceRefNo = dr.GetString(5),
                            TotalAmount = dr.GetDecimal(6),
                            LineCount = dr.GetInt32(7)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int invoiceId)
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
SELECT InvoiceID, InvoiceCode, InvoiceDate, InvoiceType, BuyerName, BuyerNTNCNIC, BuyerProvince,
       BuyerAddress, BuyerRegistrationType, InvoiceRefNo, CustomerName, CustomerNTNCNIC,
       CustomerID, CustomerAddress, TotalAmount
FROM tblInvoice WHERE InvoiceID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            Input = new InvoiceInput
                            {
                                InvoiceID = dr.GetInt32(0),
                                InvoiceCode = dr.GetString(1),
                                InvoiceDate = dr.GetDateTime(2).ToString("yyyy-MM-dd"),
                                InvoiceType = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                BuyerName = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                BuyerNTNCNIC = dr.IsDBNull(5) ? "" : dr.GetString(5),
                                BuyerProvince = dr.IsDBNull(6) ? "" : dr.GetString(6),
                                BuyerAddress = dr.IsDBNull(7) ? "" : dr.GetString(7),
                                BuyerRegistrationType = dr.IsDBNull(8) ? "" : dr.GetString(8),
                                InvoiceRefNo = dr.IsDBNull(9) ? "" : dr.GetString(9),
                                CustomerName = dr.IsDBNull(10) ? "" : dr.GetString(10),
                                CustomerNTNCNIC = dr.IsDBNull(11) ? "" : dr.GetString(11),
                                CustomerID = dr.IsDBNull(12) ? 0 : dr.GetInt32(12),
                                CustomerAddress = dr.IsDBNull(13) ? "" : dr.GetString(13),
                                TotalAmount = dr.IsDBNull(14) ? "0" : dr.GetDecimal(14).ToString("0.##")
                            };
                        }
                    }
                }
                using (var cmd = new SqlCommand(@"
SELECT ProductID, ItemID, HSCode, ProductName, Qty, UnitOfMeasure, UnitPrice, TaxAmount,
       ExtraTax, FedPayable, SalesType, SroItemSerialNo, FurtherTax, Discount
FROM tblInvoiceItem WHERE InvoiceID=@Id ORDER BY SortOrder, InvoiceItemID;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ItemRecords.Add(new InvoiceItemInput
                            {
                                ProductID = dr.IsDBNull(0) ? 0 : dr.GetInt32(0),
                                ItemID = dr.IsDBNull(1) ? "" : dr.GetString(1),
                                HSCode = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                ProductName = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                Qty = dr.IsDBNull(4) ? "" : dr.GetDecimal(4).ToString("0.####"),
                                UnitOfMeasure = dr.IsDBNull(5) ? "" : dr.GetString(5),
                                UnitPrice = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.####"),
                                TaxAmount = dr.IsDBNull(7) ? "" : dr.GetDecimal(7).ToString("0.##"),
                                ExtraTax = dr.IsDBNull(8) ? "" : dr.GetDecimal(8).ToString("0.##"),
                                FedPayable = dr.IsDBNull(9) ? "" : dr.GetDecimal(9).ToString("0.##"),
                                SalesType = dr.IsDBNull(10) ? "" : dr.GetString(10),
                                SroItemSerialNo = dr.IsDBNull(11) ? "" : dr.GetString(11),
                                FurtherTax = dr.IsDBNull(12) ? "" : dr.GetDecimal(12).ToString("0.##"),
                                Discount = dr.IsDBNull(13) ? "" : dr.GetDecimal(13).ToString("0.##")
                            });
                        }
                    }
                }
            }
            if (ItemRecords.Count == 0) EnsureDefaultItemRow();
        }

        private void LoadLookups()
        {
            Customers = new List<LookupItem>();
            Products = new List<ProductLookupItem>();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
SELECT CustomerID, CustomerCode, Name FROM tblCustomer WHERE IsActive=1 ORDER BY Name;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        var name = dr.IsDBNull(2) ? "" : dr.GetString(2);
                        Customers.Add(new LookupItem
                        {
                            Id = dr.GetInt32(0),
                            Name = string.IsNullOrEmpty(code) ? name : code + " – " + name
                        });
                    }
                }
                using (var cmd = new SqlCommand(@"
SELECT p.ProductID, p.ProductCode, p.ProductName, ISNULL(p.ItemID,''), ISNULL(hs.HSCode,''),
       ISNULL(uom.UnitOfMeasureName, ISNULL(uom.AliasName,''))
FROM tblProduct p
LEFT JOIN tblHSCode hs ON hs.HSCodeID=p.HSCodeID
LEFT JOIN tblUnitOfMeasure uom ON uom.UnitOfMeasureID=p.IUUnitOfMeasureID
WHERE p.IsActive=1 ORDER BY p.ProductCode;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.GetString(1);
                        var name = dr.GetString(2);
                        Products.Add(new ProductLookupItem
                        {
                            Id = dr.GetInt32(0),
                            Code = code,
                            Name = code + " – " + name,
                            ProductName = name,
                            ItemID = dr.GetString(3),
                            HSCode = dr.GetString(4),
                            UnitOfMeasure = dr.GetString(5)
                        });
                    }
                }
            }
        }

        private string GenerateNextInvoiceCode(SqlConnection conn = null, SqlTransaction tx = null)
        {
            var owns = conn == null;
            if (owns) { conn = new SqlConnection(Conn); conn.Open(); }
            try
            {
                using (var cmd = tx == null
                    ? new SqlCommand(@"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(InvoiceCode,4,10) AS INT)),0) FROM tblInvoice WHERE InvoiceCode LIKE 'INV[0-9]%';", conn)
                    : new SqlCommand(@"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(InvoiceCode,4,10) AS INT)),0) FROM tblInvoice WHERE InvoiceCode LIKE 'INV[0-9]%';", conn, tx))
                {
                    return "INV" + (Convert.ToInt32(cmd.ExecuteScalar()) + 1).ToString("D6");
                }
            }
            finally { if (owns) conn.Dispose(); }
        }

        private void EnsureDefaultItemRow()
        {
            if (ItemRecords.Count == 0) ItemRecords.Add(new InvoiceItemInput());
        }

        private static decimal DecimalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            return decimal.TryParse(value, out var d) ? d : 0m;
        }
    }
}
