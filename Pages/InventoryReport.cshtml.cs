using ClosedXML.Excel;
using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class InventoryReportRow
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string InventoryType { get; set; } = "";
    public decimal QtyOnHand { get; set; }
    public decimal AvgUnitCost { get; set; }
    public decimal StockValue { get; set; }
}

public class InventoryReportModel : PageModel
{
    private readonly string _conn;

    public InventoryReportModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Inventory Report";
    public List<InventoryReportRow> Records { get; set; } = new();
    public decimal TotalValue { get; set; }
    public string Search { get; set; } = "";
    public string ReportType { get; set; } = "stock";

    public void OnGet(string? search, string? reportType)
    {
        Search = search?.Trim() ?? "";
        ReportType = string.IsNullOrWhiteSpace(reportType) ? "stock" : reportType.Trim().ToLowerInvariant();
        Records = LoadRecords(Search);
        TotalValue = Records.Sum(r => r.StockValue);
    }

    public IActionResult OnGetDownloadExcel(string? search)
    {
        var records = LoadRecords(search?.Trim() ?? "");
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inventory Stock");
        var headers = new[] { "Product Code", "Product Name", "Inventory Type", "Qty On Hand", "Avg Unit Cost", "Stock Value" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var r in records)
        {
            sheet.Cell(row, 1).Value = r.ProductCode;
            sheet.Cell(row, 2).Value = r.ProductName;
            sheet.Cell(row, 3).Value = r.InventoryType;
            sheet.Cell(row, 4).Value = r.QtyOnHand;
            sheet.Cell(row, 5).Value = r.AvgUnitCost;
            sheet.Cell(row, 6).Value = r.StockValue;
            row++;
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryReport.xlsx");
    }

    private List<InventoryReportRow> LoadRecords(string search)
    {
        var list = new List<InventoryReportRow>();
        using var conn = new SqlConnection(_conn);
        var sql = @"
            SELECT p.ProductCode, p.ProductName, ISNULL(it.InventoryTypeName, ''),
                   ISNULL(s.QtyOnHand, 0), ISNULL(s.AvgUnitCost, 0),
                   ISNULL(s.QtyOnHand, 0) * ISNULL(s.AvgUnitCost, 0)
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
            list.Add(new InventoryReportRow
            {
                ProductCode = dr.GetString(0),
                ProductName = dr.GetString(1),
                InventoryType = dr.GetString(2),
                QtyOnHand = dr.GetDecimal(3),
                AvgUnitCost = dr.GetDecimal(4),
                StockValue = dr.GetDecimal(5)
            });
        }
        return list;
    }
}
