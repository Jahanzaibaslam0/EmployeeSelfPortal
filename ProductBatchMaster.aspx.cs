using System;
using System.Collections.Generic;
using System.Globalization;
using HRMS.Services;

namespace HRMS
{
    public partial class ProductBatchMasterPage : AppBasePage
    {
        private readonly ProductBatchService _svc = new ProductBatchService();

        public string PageTitle => "Batch Management";
        public List<ProductBatchItem> Batches { get; set; } = new List<ProductBatchItem>();
        public List<ProductBatchLookup> Products { get; set; } = new List<ProductBatchLookup>();
        public List<ProductBatchLookup> Vendors { get; set; } = new List<ProductBatchLookup>();
        public ProductBatchItem Input { get; set; } = new ProductBatchItem();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public int FilterProductID { get; set; }
        public string FilterStatus { get; set; } = "";
        public string FilterSearch { get; set; } = "";
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public bool CanWrite { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();
            CanWrite = Auth.IsAdmin || Perms.CanWrite(ProductBatchService.FormKey);

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(FormInt("deleteId"));
                    return;
                }
                OnPostSave();
                return;
            }

            FilterProductID = QueryInt("productId") ?? 0;
            FilterStatus = (Request.QueryString["status"] ?? "").Trim();
            FilterSearch = (Request.QueryString["q"] ?? "").Trim();
            OnGet(QueryInt("editId"), Request.QueryString["newBatch"] == "1");
        }

        private void OnGet(int? editId, bool newBatch)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newBatch;
            Products = _svc.ListProducts();
            Vendors = _svc.ListVendors();

            if (ShowForm)
            {
                if (editId.HasValue && editId > 0)
                {
                    Input = _svc.GetById(editId.Value) ?? new ProductBatchItem();
                    if (Input.ProductBatchID <= 0)
                    {
                        SetAlert("Batch was not found.", "error");
                        Response.Redirect("~/ProductBatchMaster.aspx");
                        return;
                    }
                    EditMode = true;
                }
                else
                {
                    Input.ProductID = FilterProductID;
                    Input.BatchStatus = ProductBatchStatuses.Registered;
                }
            }
            else
            {
                Batches = _svc.List(FilterProductID, FilterStatus, FilterSearch);
                ExpiredCount = Batches.FindAll(b => b.BatchStatus == ProductBatchStatuses.Expired).Count;
                ExpiringSoonCount = Batches.FindAll(b => b.IsExpiringSoon).Count;
            }
        }

        private void OnPostSave()
        {
            if (!CanWrite)
            {
                SetAlert("You do not have permission to save batches.", "error");
                Response.Redirect("~/ProductBatchMaster.aspx");
                return;
            }

            EditMode = FormBool("EditMode");
            Input = ReadForm();
            try
            {
                var id = _svc.Save(Input);
                Audit.Log(EditMode ? "Update" : "Create", "Product Batch", id, Input.BatchNumber,
                    ProductBatchService.FormKey, "/ProductBatchMaster.aspx", "Save",
                    "Product " + Input.ProductID + " / " + Input.BatchNumber);
                SetAlert(EditMode ? "Batch updated." : "Batch created.");
                Response.Redirect("~/ProductBatchMaster.aspx?productId=" + Input.ProductID);
            }
            catch (Exception ex)
            {
                AlertMessage = ex.Message;
                AlertType = "error";
                ShowForm = true;
                Products = _svc.ListProducts();
                Vendors = _svc.ListVendors();
            }
        }

        private void OnPostDelete(int id)
        {
            if (!CanWrite)
            {
                SetAlert("You do not have permission to remove batches.", "error");
                Response.Redirect("~/ProductBatchMaster.aspx");
                return;
            }
            try
            {
                var item = _svc.GetById(id);
                _svc.Deactivate(id);
                Audit.Log("Delete", "Product Batch", id, item != null ? item.BatchNumber : "",
                    ProductBatchService.FormKey, "/ProductBatchMaster.aspx", "Delete", "Deactivated");
                SetAlert("Batch removed from the active list.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing batch: " + ex.Message, "error");
            }
            Response.Redirect("~/ProductBatchMaster.aspx");
        }

        private ProductBatchItem ReadForm()
        {
            decimal? mrp = null, rate = null;
            if (decimal.TryParse(FormString("BatchMrp"), NumberStyles.Any, CultureInfo.InvariantCulture, out var m)) mrp = m;
            if (decimal.TryParse(FormString("AssessableRate"), NumberStyles.Any, CultureInfo.InvariantCulture, out var r)) rate = r;

            return new ProductBatchItem
            {
                ProductBatchID = FormInt("ProductBatchID"),
                ProductID = FormInt("ProductID"),
                BatchNumber = FormString("BatchNumber"),
                ManufacturingDate = ParseDate(FormString("ManufacturingDate")),
                ExpiryDate = ParseDate(FormString("ExpiryDate")),
                BestBeforeDate = ParseDate(FormString("BestBeforeDate")),
                Manufacturer = FormString("Manufacturer"),
                VendorID = FormInt("VendorID"),
                BatchStatus = FormString("BatchStatus"),
                BatchMrp = mrp,
                AssessableRate = rate,
                Remarks = FormString("Remarks")
            };
        }

        private static DateTime? ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (DateTime.TryParse(raw, out var d)) return d.Date;
            return null;
        }

        public string Iso(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "";
        public string DisplayDate(DateTime? value) => value.HasValue ? value.Value.ToString("dd-MMM-yyyy") : "&ndash;";
    }
}
