using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HRMS.Services;

namespace HRMS
{
    public partial class ProductSerializationPage : AppBasePage
    {
        private readonly ProductSerializationService _svc = new ProductSerializationService();

        public string PageTitle => "Product Serialization";
        public List<SerializationProductLookup> Products { get; set; } = new List<SerializationProductLookup>();
        public List<ProductBatchItem> Batches { get; set; } = new List<ProductBatchItem>();
        public List<ProductSerialItem> Serials { get; set; } = new List<ProductSerialItem>();
        public List<ProductSerializationRunItem> Runs { get; set; } = new List<ProductSerializationRunItem>();
        public ProductSerializationRunItem OpenedRun { get; set; }
        public ProductSerializationRunFilter RunFilter { get; set; } = new ProductSerializationRunFilter();
        public int RunTotalCount { get; set; }
        public List<ProductSerialStatusHistoryItem> History { get; set; } = new List<ProductSerialStatusHistoryItem>();
        public int SelectedProductID { get; set; }
        public int SelectedBatchID { get; set; }
        public string GtinNumber { get; set; } = "";
        public string ManufacturingDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string VendorName { get; set; } = "";
        public int GenerateQty { get; set; } = 1;
        public int CodeLength { get; set; } = 20;
        public ProductSerialFilter Filter { get; set; } = new ProductSerialFilter();
        public int TotalCount { get; set; }
        public int HistorySerialID { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public bool CanWrite { get; set; }
        public bool CanApprove { get; set; }
        public bool CanExport { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();
            CanWrite = Auth.IsAdmin || Perms.CanWrite(ProductSerializationService.FormKey);
            CanApprove = Auth.IsAdmin || Perms.CanApprove(ProductSerializationService.FormKey);
            CanExport = Auth.IsAdmin || Perms.CanExport(ProductSerializationService.FormKey) || Perms.CanRead(ProductSerializationService.FormKey);

            ReadSelection();

            if (!IsPostBack && string.Equals(Request.QueryString["handler"], "ExportExcel", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanExport)
                {
                    SetAlert("You do not have permission to export serialization records.", "error");
                    Response.Redirect("~/ProductSerialization.aspx");
                    return;
                }
                ExportExcel();
                return;
            }

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "";
                if (string.Equals(handler, "Generate", StringComparison.OrdinalIgnoreCase))
                {
                    OnGenerate();
                    return;
                }
                if (string.Equals(handler, "UpdateStatus", StringComparison.OrdinalIgnoreCase))
                {
                    OnUpdateStatus();
                    return;
                }
            }

            LoadPage();
        }

        private void ReadSelection()
        {
            SelectedProductID = QueryInt("productId") ?? FormInt("ProductID");
            SelectedBatchID = QueryInt("batchId") ?? FormInt("ProductBatchID");
            GenerateQty = FormInt("GenerateQty");
            if (GenerateQty <= 0) int.TryParse(Request.QueryString["qty"], out var q);
            CodeLength = FormInt("CodeLength");
            if (CodeLength <= 0 && !int.TryParse(Request.QueryString["len"], out var len)) CodeLength = 20;
            else if (CodeLength <= 0) CodeLength = 20;

            Filter.ProductID = QueryInt("fltProduct") ?? 0;
            Filter.Gtin = (Request.QueryString["fltGtin"] ?? "").Trim();
            Filter.BatchNumber = (Request.QueryString["fltBatch"] ?? "").Trim();
            Filter.Status = (Request.QueryString["fltStatus"] ?? "").Trim();
            Filter.Search = (Request.QueryString["q"] ?? "").Trim();
            Filter.GenerationKey = (Request.QueryString["run"] ?? "").Trim();
            Filter.Page = QueryInt("page") ?? 1;
            HistorySerialID = QueryInt("historyId") ?? 0;

            RunFilter.GenerationKey = (Request.QueryString["histGid"] ?? "").Trim();
            RunFilter.ProductName = (Request.QueryString["histProduct"] ?? "").Trim();
            RunFilter.Gtin = (Request.QueryString["histGtin"] ?? "").Trim();
            RunFilter.BatchNumber = (Request.QueryString["histBatch"] ?? "").Trim();
            RunFilter.DateFrom = ParseIsoDate(Request.QueryString["histFrom"]);
            RunFilter.DateTo = ParseIsoDate(Request.QueryString["histTo"]);
            RunFilter.Page = QueryInt("histPage") ?? 1;
        }

        private static DateTime? ParseIsoDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (DateTime.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
                return d.Date;
            if (DateTime.TryParse(raw, out var parsed)) return parsed.Date;
            return null;
        }

        private void LoadPage()
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            Products = _svc.ListProducts();

            var product = _svc.GetProduct(SelectedProductID);
            if (product != null)
            {
                GtinNumber = product.Gtin;
                Batches = _svc.ListBatches(SelectedProductID);
            }
            else
            {
                SelectedProductID = 0;
                SelectedBatchID = 0;
            }

            var batch = _svc.GetBatchForProduct(SelectedProductID, SelectedBatchID);
            if (batch != null)
            {
                ManufacturingDate = batch.ManufacturingDate.HasValue ? batch.ManufacturingDate.Value.ToString("dd-MMM-yyyy") : "";
                ExpiryDate = batch.ExpiryDate.HasValue ? batch.ExpiryDate.Value.ToString("dd-MMM-yyyy") : "";
                Manufacturer = batch.Manufacturer;
                VendorName = batch.VendorName;
            }
            else
            {
                SelectedBatchID = 0;
            }

            var runs = _svc.ListRuns(RunFilter);
            Runs = runs.Items;
            RunTotalCount = runs.TotalCount;

            if (string.IsNullOrEmpty(Filter.GenerationKey) && !string.IsNullOrEmpty(RunFilter.GenerationKey))
            {
                var exact = _svc.GetRunByKey(RunFilter.GenerationKey);
                if (exact != null)
                    Filter.GenerationKey = exact.GenerationKey;
            }

            if (!string.IsNullOrEmpty(Filter.GenerationKey))
            {
                OpenedRun = _svc.GetRunByKey(Filter.GenerationKey);
                Filter.PageSize = 200;
                var list = _svc.List(Filter);
                Serials = list.Items;
                TotalCount = list.TotalCount;
            }

            if (HistorySerialID > 0)
                History = _svc.ListHistory(HistorySerialID);
        }

        private void OnGenerate()
        {
            if (!CanWrite)
            {
                SetAlert("You do not have permission to generate serialization codes.", "error");
                Response.Redirect("~/ProductSerialization.aspx");
                return;
            }

            SelectedProductID = FormInt("ProductID");
            SelectedBatchID = FormInt("ProductBatchID");
            GenerateQty = FormInt("GenerateQty");
            CodeLength = FormInt("CodeLength");
            if (CodeLength <= 0) CodeLength = 20;

            try
            {
                var key = _svc.GenerateRun(SelectedProductID, SelectedBatchID, GenerateQty, CodeLength);
                Audit.Log("Create", "Product Serial", null, key, ProductSerializationService.FormKey,
                    "/ProductSerialization.aspx", "Generate",
                    "Product " + SelectedProductID + ", batch " + SelectedBatchID + ", qty " + GenerateQty + ", length " + CodeLength);
                SetAlert("Generated " + GenerateQty + " unique hexacode(s) as a new series. Generation ID " + key + ".");
                Response.Redirect("~/ProductSerialization.aspx?productId=" + SelectedProductID
                    + "&batchId=" + SelectedBatchID + "&run=" + Uri.EscapeDataString(key));
            }
            catch (Exception ex)
            {
                AlertMessage = ex.Message;
                AlertType = "error";
                LoadPage();
            }
        }

        private void OnUpdateStatus()
        {
            var id = FormInt("serialId");
            var status = FormString("serialStatus");
            var remarks = FormString("serialRemarks");
            try
            {
                _svc.UpdateStatus(id, status, remarks, CanApprove, CanWrite);
                Audit.Log("Update", "Product Serial", id, status, ProductSerializationService.FormKey,
                    "/ProductSerialization.aspx", "UpdateStatus", "Status " + status);
                SetAlert("Serialization status updated.");
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "error");
            }
            Response.Redirect(CurrentListUrl());
        }

        private void ExportExcel()
        {
            var rows = _svc.ListAll(Filter);
            var run = !string.IsNullOrEmpty(Filter.GenerationKey)
                ? _svc.GetRunByKey(Filter.GenerationKey)
                : null;
            var headers = new[]
            {
                "Sr. No.", "Generation ID", "Product Code", "Product Name", "GTIN Number",
                "Batch Number", "Hexacode (GS1 Serialization Number)", "Code Length",
                "Generation Date", "Status", "Remarks", "Created By", "GS1 Hint"
            };

            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    uint excelRow = 1;
                    if (run != null)
                    {
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Hexacode Generation Export", "", "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Generation ID", run.GenerationKey, "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Product", run.ProductName, "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "GTIN Number", run.GtinNumber, "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Batch Number", run.BatchNumber, "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Requested Quantity", run.RequestedQuantity.ToString(CultureInfo.InvariantCulture), "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Generated Quantity", run.GeneratedQuantity.ToString(CultureInfo.InvariantCulture), "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Generation Date / Time", DisplayDateTime(run.GeneratedOn), "", "", "", "", "", "", "", "", "", "", "" }));
                        sheetData.AppendChild(CreateExcelRow(excelRow++, new[] { "Generated By", run.GeneratedBy, "", "", "", "", "", "", "", "", "", "", "" }));
                        excelRow++;
                    }
                    sheetData.AppendChild(CreateExcelRow(excelRow, headers));
                    for (var i = 0; i < rows.Count; i++)
                    {
                        var r = rows[i];
                        sheetData.AppendChild(CreateExcelRow(excelRow + 1 + (uint)i, new[]
                        {
                            (i + 1).ToString(CultureInfo.InvariantCulture),
                            r.GenerationKey,
                            r.ProductCode,
                            r.ProductName,
                            r.GtinNumber,
                            r.BatchNumber,
                            r.Hexacode,
                            r.CodeLength.ToString(CultureInfo.InvariantCulture),
                            r.GeneratedOn.ToString("dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                            r.Status,
                            r.Remarks,
                            r.CreatedBy,
                            r.Gs1Hint
                        }));
                    }
                    worksheetPart.Worksheet = new Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Serialization"
                    });
                    workbookPart.Workbook.Save();
                }

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("Content-Disposition",
                    "attachment; filename=ProductSerialization_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");
                Response.BinaryWrite(stream.ToArray());
                Response.End();
            }
        }

        private static Row CreateExcelRow(uint rowIndex, IList<string> values)
        {
            var row = new Row { RowIndex = rowIndex };
            for (var i = 0; i < values.Count; i++)
            {
                var text = values[i] ?? "";
                row.AppendChild(new Cell
                {
                    CellReference = ExcelColumnName(i + 1) + rowIndex,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(text)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    })
                });
            }
            return row;
        }

        private static string ExcelColumnName(int index)
        {
            var name = "";
            while (index > 0)
            {
                var rem = (index - 1) % 26;
                name = (char)('A' + rem) + name;
                index = (index - 1) / 26;
            }
            return name;
        }

        public string CurrentListUrl()
        {
            var parts = new List<string>();
            if (SelectedProductID > 0) parts.Add("productId=" + SelectedProductID);
            if (SelectedBatchID > 0) parts.Add("batchId=" + SelectedBatchID);
            if (Filter.ProductID > 0) parts.Add("fltProduct=" + Filter.ProductID);
            if (!string.IsNullOrEmpty(Filter.Gtin)) parts.Add("fltGtin=" + Uri.EscapeDataString(Filter.Gtin));
            if (!string.IsNullOrEmpty(Filter.BatchNumber)) parts.Add("fltBatch=" + Uri.EscapeDataString(Filter.BatchNumber));
            if (!string.IsNullOrEmpty(Filter.Status)) parts.Add("fltStatus=" + Uri.EscapeDataString(Filter.Status));
            if (!string.IsNullOrEmpty(Filter.Search)) parts.Add("q=" + Uri.EscapeDataString(Filter.Search));
            if (!string.IsNullOrEmpty(Filter.GenerationKey)) parts.Add("run=" + Uri.EscapeDataString(Filter.GenerationKey));
            if (Filter.Page > 1) parts.Add("page=" + Filter.Page);
            if (!string.IsNullOrEmpty(RunFilter.GenerationKey)) parts.Add("histGid=" + Uri.EscapeDataString(RunFilter.GenerationKey));
            if (!string.IsNullOrEmpty(RunFilter.ProductName)) parts.Add("histProduct=" + Uri.EscapeDataString(RunFilter.ProductName));
            if (!string.IsNullOrEmpty(RunFilter.Gtin)) parts.Add("histGtin=" + Uri.EscapeDataString(RunFilter.Gtin));
            if (!string.IsNullOrEmpty(RunFilter.BatchNumber)) parts.Add("histBatch=" + Uri.EscapeDataString(RunFilter.BatchNumber));
            if (RunFilter.DateFrom.HasValue) parts.Add("histFrom=" + RunFilter.DateFrom.Value.ToString("yyyy-MM-dd"));
            if (RunFilter.DateTo.HasValue) parts.Add("histTo=" + RunFilter.DateTo.Value.ToString("yyyy-MM-dd"));
            if (RunFilter.Page > 1) parts.Add("histPage=" + RunFilter.Page);
            return "/ProductSerialization.aspx" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        }

        public string OpenRunUrl(string generationKey)
        {
            var parts = new List<string>();
            if (SelectedProductID > 0) parts.Add("productId=" + SelectedProductID);
            if (SelectedBatchID > 0) parts.Add("batchId=" + SelectedBatchID);
            parts.Add("run=" + Uri.EscapeDataString(generationKey ?? ""));
            if (!string.IsNullOrEmpty(RunFilter.GenerationKey)) parts.Add("histGid=" + Uri.EscapeDataString(RunFilter.GenerationKey));
            if (!string.IsNullOrEmpty(RunFilter.ProductName)) parts.Add("histProduct=" + Uri.EscapeDataString(RunFilter.ProductName));
            if (!string.IsNullOrEmpty(RunFilter.Gtin)) parts.Add("histGtin=" + Uri.EscapeDataString(RunFilter.Gtin));
            if (!string.IsNullOrEmpty(RunFilter.BatchNumber)) parts.Add("histBatch=" + Uri.EscapeDataString(RunFilter.BatchNumber));
            if (RunFilter.DateFrom.HasValue) parts.Add("histFrom=" + RunFilter.DateFrom.Value.ToString("yyyy-MM-dd"));
            if (RunFilter.DateTo.HasValue) parts.Add("histTo=" + RunFilter.DateTo.Value.ToString("yyyy-MM-dd"));
            return "/ProductSerialization.aspx?" + string.Join("&", parts);
        }

        public string HistPageUrl(int page)
        {
            var saved = RunFilter.Page;
            var savedRun = Filter.GenerationKey;
            Filter.GenerationKey = "";
            RunFilter.Page = page;
            var url = CurrentListUrl();
            RunFilter.Page = saved;
            Filter.GenerationKey = savedRun;
            return url;
        }

        public int RunPageCount
        {
            get
            {
                var size = RunFilter.PageSize <= 0 ? 20 : RunFilter.PageSize;
                return RunTotalCount <= 0 ? 1 : (int)Math.Ceiling(RunTotalCount / (double)size);
            }
        }

        public string Iso(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "";
        public string DisplayDate(DateTime? value) => value.HasValue ? value.Value.ToString("dd-MMM-yyyy") : "-";
        public string DisplayDateTime(DateTime value) => value.ToString("dd-MMM-yyyy HH:mm:ss");

        public string PageUrl(int page)
        {
            var saved = Filter.Page;
            Filter.Page = page;
            var url = CurrentListUrl();
            Filter.Page = saved;
            return url;
        }

        public int PageCount
        {
            get
            {
                var size = Filter.PageSize <= 0 ? 50 : Filter.PageSize;
                return TotalCount <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)size);
            }
        }

        public string StatusOption(string current, string option)
        {
            var selected = string.Equals(current, option, StringComparison.OrdinalIgnoreCase) ? " selected" : "";
            var needsApprove = string.Equals(option, ProductSerialStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(option, ProductSerialStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(current, ProductSerialStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(current, ProductSerialStatuses.Rejected, StringComparison.OrdinalIgnoreCase);
            var disabled = needsApprove && !CanApprove && !string.Equals(current, option, StringComparison.OrdinalIgnoreCase)
                ? " disabled" : "";
            return "<option value=\"" + option + "\"" + selected + disabled + ">" + option + "</option>";
        }
    }
}
