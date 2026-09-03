using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.UI;
using ClosedXML.Excel;
using HRMS.Services;

namespace HRMS
{
    public class LanguageRecord
    {
        public int LanguageID { get; set; }
        public string LanguageCode { get; set; } = "";
        public string LanguageName { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string Region { get; set; } = "";
        public string Source { get; set; } = "";
        public bool IsPriority { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LanguageSetupPage : AppBasePage
    {
        public string PageTitle => "Language Setup";
        public LanguageRecord Input { get; private set; } = new LanguageRecord { IsActive = true };
        public List<LanguageRecord> Languages { get; private set; } = new List<LanguageRecord>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            Form.Enctype = "multipart/form-data";

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete();
                    return;
                }
                if (string.Equals(handler, "UploadExcel", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostUploadExcel();
                    return;
                }
                if (string.Equals(handler, "DownloadExcel", StringComparison.OrdinalIgnoreCase))
                {
                    OnGetDownloadExcel();
                    return;
                }
                OnPostSave();
                return;
            }

            if (string.Equals(Request.QueryString["handler"], "DownloadExcel", StringComparison.OrdinalIgnoreCase))
            {
                OnGetDownloadExcel();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);

            Languages = LoadLanguageRecords();
        }

        private void OnPostSave()
        {
            var languageID = 0;
            int.TryParse(FormString("languageID"), out languageID);
            var languageCode = FormString("languageCode");
            var languageName = FormString("languageName");
            var nativeName = FormString("nativeName");
            var region = FormString("region");
            var source = FormString("source");
            var isPriority = FormBool("isPriority");
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                SetAlert("Language Code is required.", "error");
                Response.Redirect("~/LanguageSetup.aspx");
                return;
            }
            if (string.IsNullOrWhiteSpace(languageName))
            {
                SetAlert("Language Name is required.", "error");
                Response.Redirect("~/LanguageSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (languageID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblLanguage
                    SET LanguageCode = @LanguageCode,
                        LanguageName = @LanguageName,
                        NativeName = @NativeName,
                        Region = @Region,
                        Source = @Source,
                        IsPriority = @IsPriority,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE LanguageID = @LanguageID;", conn))
                        {
                            AddSaveParameters(cmd, languageID, languageCode, languageName, nativeName, region, source, isPriority, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Language record updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblLanguage
                        (LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@LanguageCode, @LanguageName, @NativeName, @Region, @Source, @IsPriority, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddSaveParameters(cmd, languageID, languageCode, languageName, nativeName, region, source, isPriority, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Language record added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    SetAlert("This language code already exists.", "error");
                else
                    SetAlert("Error: " + ex.Message, "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/LanguageSetup.aspx");
        }

        private void OnPostDelete()
        {
            var deleteId = 0;
            int.TryParse(FormString("deleteId"), out deleteId);
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
                UPDATE tblLanguage
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE LanguageID = @LanguageID;", conn))
                {
                    cmd.Parameters.AddWithValue("@LanguageID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Language record removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/LanguageSetup.aspx");
        }

        private void OnGetDownloadExcel()
        {
            var records = LoadLanguageRecords();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Languages");
                var headers = new[]
                {
                    "LanguageID", "Code", "Name", "Native Name", "Region", "Source", "Priority Flag", "Status"
                };
                for (var col = 0; col < headers.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = headers[col];
                    worksheet.Cell(1, col + 1).Style.Font.Bold = true;
                }
                for (var row = 0; row < records.Count; row++)
                {
                    var language = records[row];
                    var excelRow = row + 2;
                    worksheet.Cell(excelRow, 1).Value = language.LanguageID;
                    worksheet.Cell(excelRow, 2).Value = language.LanguageCode;
                    worksheet.Cell(excelRow, 3).Value = language.LanguageName;
                    worksheet.Cell(excelRow, 4).Value = language.NativeName;
                    worksheet.Cell(excelRow, 5).Value = language.Region;
                    worksheet.Cell(excelRow, 6).Value = language.Source;
                    worksheet.Cell(excelRow, 7).Value = language.IsPriority ? "Yes" : "No";
                    worksheet.Cell(excelRow, 8).Value = language.IsActive ? "Active" : "Inactive";
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    Response.Clear();
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Disposition",
                        "attachment; filename=LanguageSetup_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");
                    Response.BinaryWrite(stream.ToArray());
                    Response.End();
                }
            }
        }

        private void OnPostUploadExcel()
        {
            var languageFile = Request.Files["languageFile"];
            if (languageFile == null || languageFile.ContentLength == 0)
            {
                SetAlert("Please select an Excel file to upload.", "error");
                Response.Redirect("~/LanguageSetup.aspx");
                return;
            }

            try
            {
                using (var workbook = new XLWorkbook(languageFile.InputStream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var rows = worksheet.RowsUsed().Skip(1).ToList();

                    using (var conn = new SqlConnection(Conn))
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        {
                            var processed = 0;
                            foreach (var row in rows)
                            {
                                var code = row.Cell(2).GetString().Trim();
                                var name = row.Cell(3).GetString().Trim();
                                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                                    continue;

                                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                                    throw new InvalidOperationException("Row " + row.RowNumber() + " must have both Code and Name.");

                                var record = new LanguageRecord
                                {
                                    LanguageID = TryReadInt(row.Cell(1).GetString()),
                                    LanguageCode = code,
                                    LanguageName = name,
                                    NativeName = row.Cell(4).GetString().Trim(),
                                    Region = row.Cell(5).GetString().Trim(),
                                    Source = row.Cell(6).GetString().Trim(),
                                    IsPriority = ReadBool(row.Cell(7).GetString(), false),
                                    IsActive = ReadBool(row.Cell(8).GetString(), true)
                                };

                                UpsertLanguage(conn, tx, record);
                                processed++;
                            }
                            tx.Commit();
                            SetAlert(processed + " language record(s) uploaded successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetAlert("Error uploading language data: " + ex.Message, "error");
            }

            Response.Redirect("~/LanguageSetup.aspx");
        }

        private static void AddSaveParameters(
            SqlCommand cmd,
            int languageID,
            string languageCode,
            string languageName,
            string nativeName,
            string region,
            string source,
            bool isPriority,
            bool isActive)
        {
            cmd.Parameters.AddWithValue("@LanguageID", languageID);
            cmd.Parameters.AddWithValue("@LanguageCode", languageCode.Trim());
            cmd.Parameters.AddWithValue("@LanguageName", languageName.Trim());
            cmd.Parameters.AddWithValue("@NativeName", string.IsNullOrWhiteSpace(nativeName) ? (object)DBNull.Value : nativeName.Trim());
            cmd.Parameters.AddWithValue("@Region", string.IsNullOrWhiteSpace(region) ? (object)DBNull.Value : region.Trim());
            cmd.Parameters.AddWithValue("@Source", string.IsNullOrWhiteSpace(source) ? (object)DBNull.Value : source.Trim());
            cmd.Parameters.AddWithValue("@IsPriority", isPriority);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadForEdit(int languageID)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT LanguageID, LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive
            FROM tblLanguage
            WHERE LanguageID = @LanguageID;", conn))
            {
                cmd.Parameters.AddWithValue("@LanguageID", languageID);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        Input = ReadLanguage(dr);
                }
            }
        }

        private List<LanguageRecord> LoadLanguageRecords()
        {
            var languages = new List<LanguageRecord>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT LanguageID, LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive
            FROM tblLanguage
            ORDER BY IsActive DESC, IsPriority DESC, LanguageName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        languages.Add(ReadLanguage(dr));
                }
            }
            return languages;
        }

        private static int TryReadInt(string value)
        {
            int number;
            return int.TryParse(value, out number) ? number : 0;
        }

        private static bool ReadBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            var normalized = value.Trim().ToLowerInvariant();
            return normalized == "true" || normalized == "yes" || normalized == "y"
                || normalized == "1" || normalized == "active" || normalized == "priority";
        }

        private void UpsertLanguage(SqlConnection conn, SqlTransaction tx, LanguageRecord record)
        {
            var existingId = record.LanguageID > 0
                ? GetLanguageIdById(conn, tx, record.LanguageID)
                : GetLanguageIdByCode(conn, tx, record.LanguageCode);

            if (existingId > 0)
            {
                using (var updateCmd = new SqlCommand(@"
                UPDATE tblLanguage
                SET LanguageCode = @LanguageCode,
                    LanguageName = @LanguageName,
                    NativeName = @NativeName,
                    Region = @Region,
                    Source = @Source,
                    IsPriority = @IsPriority,
                    IsActive = @IsActive,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE LanguageID = @LanguageID;", conn, tx))
                {
                    AddSaveParameters(updateCmd, existingId, record.LanguageCode, record.LanguageName, record.NativeName, record.Region, record.Source, record.IsPriority, record.IsActive);
                    AuditHelper.AddModifiedBy(updateCmd, Auth.CurrentUserId);
                    updateCmd.ExecuteNonQuery();
                }
                return;
            }

            using (var insertCmd = new SqlCommand(@"
            INSERT INTO tblLanguage
                (LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive, CreatedOn, CreatedByUserID)
            VALUES
                (@LanguageCode, @LanguageName, @NativeName, @Region, @Source, @IsPriority, @IsActive, GETDATE(), @CreatedByUserID);", conn, tx))
            {
                AddSaveParameters(insertCmd, 0, record.LanguageCode, record.LanguageName, record.NativeName, record.Region, record.Source, record.IsPriority, record.IsActive);
                AuditHelper.AddCreatedBy(insertCmd, Auth.CurrentUserId);
                insertCmd.ExecuteNonQuery();
            }
        }

        private static int GetLanguageIdById(SqlConnection conn, SqlTransaction tx, int languageID)
        {
            using (var cmd = new SqlCommand("SELECT TOP 1 LanguageID FROM tblLanguage WHERE LanguageID = @LanguageID;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@LanguageID", languageID);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private static int GetLanguageIdByCode(SqlConnection conn, SqlTransaction tx, string languageCode)
        {
            using (var cmd = new SqlCommand("SELECT TOP 1 LanguageID FROM tblLanguage WHERE LanguageCode = @LanguageCode;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@LanguageCode", languageCode.Trim());
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private static LanguageRecord ReadLanguage(SqlDataReader dr)
        {
            return new LanguageRecord
            {
                LanguageID = Convert.ToInt32(dr["LanguageID"]),
                LanguageCode = dr["LanguageCode"].ToString() ?? "",
                LanguageName = dr["LanguageName"].ToString() ?? "",
                NativeName = dr["NativeName"] == DBNull.Value ? "" : dr["NativeName"].ToString() ?? "",
                Region = dr["Region"] == DBNull.Value ? "" : dr["Region"].ToString() ?? "",
                Source = dr["Source"] == DBNull.Value ? "" : dr["Source"].ToString() ?? "",
                IsPriority = Convert.ToBoolean(dr["IsPriority"]),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }
}
