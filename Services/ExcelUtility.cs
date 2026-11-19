using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace HRMS.Services
{
    public class FileDownload
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public static class ExcelUtility
    {
        public const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static FileDownload ToFile(XLWorkbook workbook, string filePrefix)
        {
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return Download(stream.ToArray(), filePrefix);
            }
        }

        /// <summary>
        /// Writes a simple .xlsx without ClosedXML (avoids System.Memory 4.0.1.1 on IIS).
        /// </summary>
        public static FileDownload ToSimpleFile(
            string sheetName,
            IReadOnlyList<string> headers,
            IList<IReadOnlyList<string>> rows,
            string filePrefix)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    sheetData.AppendChild(CreateRow(1, headers));
                    if (rows != null)
                    {
                        for (var i = 0; i < rows.Count; i++)
                            sheetData.AppendChild(CreateRow((uint)(i + 2), rows[i]));
                    }
                    worksheetPart.Worksheet = new Worksheet(sheetData);
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = TruncateSheetName(sheetName)
                    });
                    workbookPart.Workbook.Save();
                }
                return Download(stream.ToArray(), filePrefix);
            }
        }

        private static FileDownload Download(byte[] content, string filePrefix)
        {
            return new FileDownload
            {
                Content = content,
                ContentType = ExcelMime,
                FileName = $"{filePrefix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };
        }

        private static string TruncateSheetName(string sheetName)
        {
            var name = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName.Trim();
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' })
                name = name.Replace(c, ' ');
            return name.Length <= 31 ? name : name.Substring(0, 31);
        }

        private static Row CreateRow(uint rowIndex, IReadOnlyList<string> values)
        {
            var row = new Row { RowIndex = rowIndex };
            if (values == null) return row;
            for (var i = 0; i < values.Count; i++)
            {
                var text = values[i] ?? "";
                row.AppendChild(new Cell
                {
                    CellReference = ColumnName(i + 1) + rowIndex,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(text)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    })
                });
            }
            return row;
        }

        private static string ColumnName(int index)
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

        public static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (var col = 0; col < headers.Count; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
                sheet.Cell(1, col + 1).Style.Font.Bold = true;
            }
        }

        public static string CellText(IXLRow row, int column) =>
            row.Cell(column).GetString().Trim();

        public static int TryInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, out var n) ? n : 0;
        }

        public static decimal? TryDecimal(string value) =>
            decimal.TryParse(value, out var d) ? d : (decimal?)null;

        public static int? TryNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value, out var n) ? n : (int?)null;
        }

        public static bool ReadBool(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            var v = value.Trim();
            return v.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || v.Equals("true", StringComparison.OrdinalIgnoreCase)
                || v.Equals("1", StringComparison.OrdinalIgnoreCase)
                || v.Equals("active", StringComparison.OrdinalIgnoreCase)
                || v.Equals("y", StringComparison.OrdinalIgnoreCase);
        }

        public static DateTime? ReadDate(IXLRow row, int column)
        {
            var cell = row.Cell(column);
            if (cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime().Date;
            if (DateTime.TryParse(cell.GetString(), out var parsed)) return parsed.Date;
            return null;
        }

        public static string FormatDate(DateTime? value) =>
            value?.ToString("yyyy-MM-dd") ?? "";

        public static string FormatBool(bool value, bool asActive = false) =>
            asActive ? (value ? "Active" : "Inactive") : (value ? "Yes" : "No");
    }
}
