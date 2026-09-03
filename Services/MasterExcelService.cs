using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using ClosedXML.Excel;

namespace HRMS.Services
{
public class MasterExcelService
{
    private readonly string _conn;

    public MasterExcelService()
    {
        _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
    }

    #region Customer

    private static readonly string[] CustomerHeaders =
    {
        "CustomerID", "CustomerCode", "Name", "SearchName", "DealForBranch", "City", "Province",
        "ModeOfDelivery", "CustomerGroup", "CustomerClass", "MethodOfPayment", "TermsOfPayment",
        "Currency", "BillPreference", "FBRStatus", "TaxGroup", "CNIC", "NTN",
        "IsCAP", "IsMandatoryCreditLimit", "IsInvoiceHold",
        "TotalBusinessPotential", "TargetBusinessSharePercent", "TargetBusinessAmount",
        "CreditLimit", "AHDCreditLimit", "PHDCreditLimit", "HHDCreditLimit", "Status"
    };

    public FileDownload ExportCustomers()
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT c.CustomerID, c.CustomerCode, c.Name, ISNULL(c.SearchName, ''),
                   ISNULL(l.LocationName, ''), ISNULL(ci.CityName, ''), ISNULL(p.ProvinceName, ''),
                   ISNULL(mod.ModeOfDeliveryName, ''), ISNULL(cg.CustomerGroupName, ''), ISNULL(cc.CustomerClassName, ''),
                   ISNULL(mop.MethodOfPaymentName, ''), ISNULL(top.TermsOfPaymentName, ''),
                   ISNULL(cur.CurrencyCode, ''), ISNULL(bp.BillPreferenceName, ''), ISNULL(fbr.FBRStatusName, ''),
                   ISNULL(tg.TaxGroupName, ''), ISNULL(c.CNIC, ''), ISNULL(c.NTN, ''),
                   c.IsCAP, c.IsMandatoryCreditLimit, c.IsInvoiceHold,
                   ISNULL(c.TotalBusinessPotential, 0), ISNULL(c.TargetBusinessSharePercent, 0), ISNULL(c.TargetBusinessAmount, 0),
                   ISNULL(c.CreditLimit, 0), ISNULL(c.AHDCreditLimit, 0), ISNULL(c.PHDCreditLimit, 0), ISNULL(c.HHDCreditLimit, 0),
                   c.IsActive
            FROM tblCustomer c
            LEFT JOIN tblLocation l ON l.LocationID = c.DealForBranchID
            LEFT JOIN tblCity ci ON ci.CityID = c.CityID
            LEFT JOIN tblProvince p ON p.ProvinceID = c.ProvinceID
            LEFT JOIN tblModeOfDelivery mod ON mod.ModeOfDeliveryID = c.ModeOfDeliveryID
            LEFT JOIN tblCustomerGroup cg ON cg.CustomerGroupID = c.CustomerGroupID
            LEFT JOIN tblCustomerClass cc ON cc.CustomerClassID = c.CustomerClassID
            LEFT JOIN tblMethodOfPayment mop ON mop.MethodOfPaymentID = c.MethodOfPaymentID
            LEFT JOIN tblTermsOfPayment top ON top.TermsOfPaymentID = c.TermsOfPaymentID
            LEFT JOIN tblCurrency cur ON cur.CurrencyID = c.CurrencyID
            LEFT JOIN tblBillPreference bp ON bp.BillPreferenceID = c.BillPreferenceID
            LEFT JOIN tblFBRStatus fbr ON fbr.FBRStatusID = c.FBRStatusID
            LEFT JOIN tblTaxGroup tg ON tg.TaxGroupID = c.TaxGroupID
            ORDER BY c.CustomerCode;", conn);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Customers");
        ExcelUtility.WriteHeaders(sheet, CustomerHeaders);

        using var dr = cmd.ExecuteReader();
        var row = 2;
        while (dr.Read())
        {
            var c = 1;
            sheet.Cell(row, c++).Value = dr.GetInt32(0);
            sheet.Cell(row, c++).Value = dr.GetString(1);
            sheet.Cell(row, c++).Value = dr.GetString(2);
            sheet.Cell(row, c++).Value = dr.GetString(3);
            sheet.Cell(row, c++).Value = dr.GetString(4);
            sheet.Cell(row, c++).Value = dr.GetString(5);
            sheet.Cell(row, c++).Value = dr.GetString(6);
            sheet.Cell(row, c++).Value = dr.GetString(7);
            sheet.Cell(row, c++).Value = dr.GetString(8);
            sheet.Cell(row, c++).Value = dr.GetString(9);
            sheet.Cell(row, c++).Value = dr.GetString(10);
            sheet.Cell(row, c++).Value = dr.GetString(11);
            sheet.Cell(row, c++).Value = dr.GetString(12);
            sheet.Cell(row, c++).Value = dr.GetString(13);
            sheet.Cell(row, c++).Value = dr.GetString(14);
            sheet.Cell(row, c++).Value = dr.GetString(15);
            sheet.Cell(row, c++).Value = dr.GetString(16);
            sheet.Cell(row, c++).Value = dr.GetString(17);
            sheet.Cell(row, c++).Value = ExcelUtility.FormatBool(dr.GetBoolean(18));
            sheet.Cell(row, c++).Value = ExcelUtility.FormatBool(dr.GetBoolean(19));
            sheet.Cell(row, c++).Value = ExcelUtility.FormatBool(dr.GetBoolean(20));
            sheet.Cell(row, c++).Value = dr.GetInt32(21);
            sheet.Cell(row, c++).Value = dr.GetDecimal(22);
            sheet.Cell(row, c++).Value = dr.GetInt32(23);
            sheet.Cell(row, c++).Value = dr.GetInt32(24);
            sheet.Cell(row, c++).Value = dr.GetInt32(25);
            sheet.Cell(row, c++).Value = dr.GetInt32(26);
            sheet.Cell(row, c++).Value = dr.GetInt32(27);
            sheet.Cell(row, c++).Value = ExcelUtility.FormatBool(dr.GetBoolean(28), asActive: true);
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ExcelUtility.ToFile(workbook, "CustomerMaster");
    }

    public ExcelImportResult ImportCustomers(HttpPostedFile file, int? userId)
    {
        using var workbook = new XLWorkbook(file.InputStream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RowsUsed().Skip(1).ToList();

        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var processed = 0;
        foreach (var row in rows)
        {
            var name = ExcelUtility.CellText(row, 3);
            var code = ExcelUtility.CellText(row, 2);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code)) continue;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Row {row.RowNumber()}: Name is required.");

            var customerId = ExcelUtility.TryInt(ExcelUtility.CellText(row, 1));
            if (customerId <= 0 && !string.IsNullOrWhiteSpace(code))
                customerId = FindIdByCode(conn, tx, "tblCustomer", "CustomerID", "CustomerCode", code);

            var input = new
            {
                Name = name,
                SearchName = ExcelUtility.CellText(row, 4),
                DealForBranchID = ResolveLookup(conn, tx, "tblLocation", "LocationID", "LocationName", ExcelUtility.CellText(row, 5)),
                CityID = ResolveLookup(conn, tx, "tblCity", "CityID", "CityName", ExcelUtility.CellText(row, 6)),
                ProvinceID = ResolveLookup(conn, tx, "tblProvince", "ProvinceID", "ProvinceName", ExcelUtility.CellText(row, 7)),
                ModeOfDeliveryID = ResolveLookup(conn, tx, "tblModeOfDelivery", "ModeOfDeliveryID", "ModeOfDeliveryName", ExcelUtility.CellText(row, 8)),
                CustomerGroupID = ResolveLookup(conn, tx, "tblCustomerGroup", "CustomerGroupID", "CustomerGroupName", ExcelUtility.CellText(row, 9)),
                CustomerClassID = ResolveLookup(conn, tx, "tblCustomerClass", "CustomerClassID", "CustomerClassName", ExcelUtility.CellText(row, 10)),
                MethodOfPaymentID = ResolveLookup(conn, tx, "tblMethodOfPayment", "MethodOfPaymentID", "MethodOfPaymentName", ExcelUtility.CellText(row, 11)),
                TermsOfPaymentID = ResolveLookup(conn, tx, "tblTermsOfPayment", "TermsOfPaymentID", "TermsOfPaymentName", ExcelUtility.CellText(row, 12)),
                CurrencyID = ResolveCurrency(conn, tx, ExcelUtility.CellText(row, 13)),
                BillPreferenceID = ResolveLookup(conn, tx, "tblBillPreference", "BillPreferenceID", "BillPreferenceName", ExcelUtility.CellText(row, 14)),
                FBRStatusID = ResolveLookup(conn, tx, "tblFBRStatus", "FBRStatusID", "FBRStatusName", ExcelUtility.CellText(row, 15)),
                TaxGroupID = ResolveLookup(conn, tx, "tblTaxGroup", "TaxGroupID", "TaxGroupName", ExcelUtility.CellText(row, 16)),
                CNIC = ExcelUtility.CellText(row, 17),
                NTN = ExcelUtility.CellText(row, 18),
                IsCAP = ExcelUtility.ReadBool(ExcelUtility.CellText(row, 19)),
                IsMandatoryCreditLimit = ExcelUtility.ReadBool(ExcelUtility.CellText(row, 20)),
                IsInvoiceHold = ExcelUtility.ReadBool(ExcelUtility.CellText(row, 21)),
                TotalBusinessPotential = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 22)),
                TargetBusinessSharePercent = ExcelUtility.TryDecimal(ExcelUtility.CellText(row, 23)),
                TargetBusinessAmount = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 24)),
                CreditLimit = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 25)),
                AHDCreditLimit = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 26)),
                PHDCreditLimit = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 27)),
                HHDCreditLimit = ExcelUtility.TryNullableInt(ExcelUtility.CellText(row, 28)),
                IsActive = ExcelUtility.ReadBool(ExcelUtility.CellText(row, 29), true)
            };

            if (customerId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblCustomer SET
                        Name = @Name, SearchName = @SearchName,
                        DealForBranchID = @DealForBranchID, CityID = @CityID, ProvinceID = @ProvinceID,
                        ModeOfDeliveryID = @ModeOfDeliveryID, CustomerGroupID = @CustomerGroupID, CustomerClassID = @CustomerClassID,
                        MethodOfPaymentID = @MethodOfPaymentID, TermsOfPaymentID = @TermsOfPaymentID,
                        CurrencyID = @CurrencyID, BillPreferenceID = @BillPreferenceID, FBRStatusID = @FBRStatusID, TaxGroupID = @TaxGroupID,
                        CNIC = @CNIC, NTN = @NTN, IsCAP = @IsCAP, IsMandatoryCreditLimit = @IsMandatoryCreditLimit, IsInvoiceHold = @IsInvoiceHold,
                        TotalBusinessPotential = @TotalBusinessPotential, TargetBusinessSharePercent = @TargetBusinessSharePercent,
                        TargetBusinessAmount = @TargetBusinessAmount, CreditLimit = @CreditLimit,
                        AHDCreditLimit = @AHDCreditLimit, PHDCreditLimit = @PHDCreditLimit, HHDCreditLimit = @HHDCreditLimit,
                        IsActive = @IsActive, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE CustomerID = @CustomerID;", conn, tx);
                BindCustomerParams(cmd, input, userId);
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                var newCode = string.IsNullOrWhiteSpace(code) ? GenerateNextCode(conn, tx, "tblCustomer", "CustomerCode", "CUS", 6) : code;
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblCustomer
                        (CustomerCode, Name, SearchName, DealForBranchID, CityID, ProvinceID,
                         ModeOfDeliveryID, CustomerGroupID, CustomerClassID, MethodOfPaymentID, TermsOfPaymentID,
                         CurrencyID, BillPreferenceID, FBRStatusID, TaxGroupID, CNIC, NTN,
                         IsCAP, IsMandatoryCreditLimit, IsInvoiceHold,
                         TotalBusinessPotential, TargetBusinessSharePercent, TargetBusinessAmount,
                         CreditLimit, AHDCreditLimit, PHDCreditLimit, HHDCreditLimit,
                         IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@CustomerCode, @Name, @SearchName, @DealForBranchID, @CityID, @ProvinceID,
                         @ModeOfDeliveryID, @CustomerGroupID, @CustomerClassID, @MethodOfPaymentID, @TermsOfPaymentID,
                         @CurrencyID, @BillPreferenceID, @FBRStatusID, @TaxGroupID, @CNIC, @NTN,
                         @IsCAP, @IsMandatoryCreditLimit, @IsInvoiceHold,
                         @TotalBusinessPotential, @TargetBusinessSharePercent, @TargetBusinessAmount,
                         @CreditLimit, @AHDCreditLimit, @PHDCreditLimit, @HHDCreditLimit,
                         @IsActive, GETDATE(), @CreatedByUserID);", conn, tx);
                BindCustomerParams(cmd, input, userId);
                cmd.Parameters.AddWithValue("@CustomerCode", newCode);
                AuditHelper.AddCreatedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }

            processed++;
        }

        tx.Commit();
        return new ExcelImportResult { Success = true, Processed = processed, Message = $"{processed} customer record(s) imported successfully." };
    }

    private static void BindCustomerParams<T>(SqlCommand cmd, T input, int? userId)
    {
        foreach (var prop in typeof(T).GetProperties())
            cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(input) ?? DBNull.Value);
        if (!cmd.Parameters.Contains("@ModifiedByUserID"))
            AuditHelper.AddModifiedBy(cmd, userId);
    }

    #endregion

    #region Contact

    private static readonly string[] ContactHeaders =
    {
        "ContactID", "ContactCode", "CustomerCode", "ContactFor", "ContactType", "ContactStatus",
        "Name", "SearchName", "Gender", "MaritalStatus", "ProfessionalTitle", "Department",
        "OfficeLocation", "AvailableFrom", "AvailableTo", "ReportToManagerName",
        "Phone", "Mobile", "Email", "Whatsapp", "URL", "Fax"
    };

    public FileDownload ExportContacts()
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT c.ContactID, c.ContactCode, ISNULL(cu.CustomerCode, ''), ISNULL(c.ContactFor, ''),
                   c.ContactType, c.ContactStatus, c.Name, ISNULL(c.SearchName, ''),
                   ISNULL(g.GenderName, ''), ISNULL(c.MaritalStatus, ''), ISNULL(c.ProfessionalTitle, ''),
                   ISNULL(c.Department, ''), ISNULL(c.OfficeLocation, ''),
                   c.AvailableFrom, c.AvailableTo, ISNULL(c.ReportToManagerName, ''),
                   ISNULL(c.Phone, ''), ISNULL(c.Mobile, ''), ISNULL(c.Email, ''),
                   ISNULL(c.Whatsapp, ''), ISNULL(c.URL, ''), ISNULL(c.Fax, '')
            FROM tblContactMaster c
            LEFT JOIN tblCustomer cu ON cu.CustomerID = c.CustomerID
            LEFT JOIN tblGender g ON g.GenderID = c.GenderID
            ORDER BY c.ContactCode;", conn);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Contacts");
        ExcelUtility.WriteHeaders(sheet, ContactHeaders);

        using var dr = cmd.ExecuteReader();
        var row = 2;
        while (dr.Read())
        {
            sheet.Cell(row, 1).Value = dr.GetInt32(0);
            sheet.Cell(row, 2).Value = dr.GetString(1);
            sheet.Cell(row, 3).Value = dr.GetString(2);
            sheet.Cell(row, 4).Value = dr.GetString(3);
            sheet.Cell(row, 5).Value = dr.GetString(4);
            sheet.Cell(row, 6).Value = dr.GetString(5);
            sheet.Cell(row, 7).Value = dr.GetString(6);
            sheet.Cell(row, 8).Value = dr.GetString(7);
            sheet.Cell(row, 9).Value = dr.GetString(8);
            sheet.Cell(row, 10).Value = dr.GetString(9);
            sheet.Cell(row, 11).Value = dr.GetString(10);
            sheet.Cell(row, 12).Value = dr.GetString(11);
            sheet.Cell(row, 13).Value = dr.GetString(12);
            sheet.Cell(row, 14).Value = dr.IsDBNull(13) ? "" : ((TimeSpan)dr.GetValue(13)).ToString(@"hh\:mm");
            sheet.Cell(row, 15).Value = dr.IsDBNull(14) ? "" : ((TimeSpan)dr.GetValue(14)).ToString(@"hh\:mm");
            sheet.Cell(row, 16).Value = dr.GetString(15);
            sheet.Cell(row, 17).Value = dr.GetString(16);
            sheet.Cell(row, 18).Value = dr.GetString(17);
            sheet.Cell(row, 19).Value = dr.GetString(18);
            sheet.Cell(row, 20).Value = dr.GetString(19);
            sheet.Cell(row, 21).Value = dr.GetString(20);
            sheet.Cell(row, 22).Value = dr.GetString(21);
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ExcelUtility.ToFile(workbook, "ContactMaster");
    }

    public ExcelImportResult ImportContacts(HttpPostedFile file, int? userId)
    {
        using var workbook = new XLWorkbook(file.InputStream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RowsUsed().Skip(1).ToList();

        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var processed = 0;
        foreach (var row in rows)
        {
            var name = ExcelUtility.CellText(row, 7);
            var code = ExcelUtility.CellText(row, 2);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code)) continue;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Row {row.RowNumber()}: Name is required.");

            var contactId = ExcelUtility.TryInt(ExcelUtility.CellText(row, 1));
            if (contactId <= 0 && !string.IsNullOrWhiteSpace(code))
                contactId = FindIdByCode(conn, tx, "tblContactMaster", "ContactID", "ContactCode", code);

            var customerCode = ExcelUtility.CellText(row, 3);
            int? customerId = null;
            if (!string.IsNullOrWhiteSpace(customerCode))
            {
                var id = FindIdByCode(conn, tx, "tblCustomer", "CustomerID", "CustomerCode", customerCode);
                if (id <= 0) throw new InvalidOperationException($"Row {row.RowNumber()}: Customer '{customerCode}' not found.");
                customerId = id;
            }

            var genderName = ExcelUtility.CellText(row, 9);
            int? genderId = string.IsNullOrWhiteSpace(genderName)
                ? null
                : ResolveLookup(conn, tx, "tblGender", "GenderID", "GenderName", genderName);

            var availableFrom = ParseTime(ExcelUtility.CellText(row, 14));
            var availableTo = ParseTime(ExcelUtility.CellText(row, 15));

            if (contactId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblContactMaster SET
                        CustomerID = @CustomerID, ContactFor = @ContactFor, ContactType = @ContactType, ContactStatus = @ContactStatus,
                        Name = @Name, SearchName = @SearchName, GenderID = @GenderID, MaritalStatus = @MaritalStatus,
                        ProfessionalTitle = @ProfessionalTitle, Department = @Department, OfficeLocation = @OfficeLocation,
                        AvailableFrom = @AvailableFrom, AvailableTo = @AvailableTo, ReportToManagerName = @ReportToManagerName,
                        Phone = @Phone, Mobile = @Mobile, Email = @Email, Whatsapp = @Whatsapp, URL = @URL, Fax = @Fax,
                        ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE ContactID = @ContactID;", conn, tx);
                AddContactParams(cmd, row, customerId, genderId, availableFrom, availableTo, userId, isUpdate: true);
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                var newCode = string.IsNullOrWhiteSpace(code) ? GenerateNextCode(conn, tx, "tblContactMaster", "ContactCode", "CNT", 6) : code;
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblContactMaster
                        (ContactCode, CustomerID, ContactFor, ContactType, ContactStatus, Name, SearchName,
                         GenderID, MaritalStatus, ProfessionalTitle, Department, OfficeLocation,
                         AvailableFrom, AvailableTo, ReportToManagerName, Phone, Mobile, Email, Whatsapp, URL, Fax,
                         CreatedOn, CreatedByUserID)
                    VALUES
                        (@ContactCode, @CustomerID, @ContactFor, @ContactType, @ContactStatus, @Name, @SearchName,
                         @GenderID, @MaritalStatus, @ProfessionalTitle, @Department, @OfficeLocation,
                         @AvailableFrom, @AvailableTo, @ReportToManagerName, @Phone, @Mobile, @Email, @Whatsapp, @URL, @Fax,
                         GETDATE(), @CreatedByUserID);", conn, tx);
                AddContactParams(cmd, row, customerId, genderId, availableFrom, availableTo, userId, isUpdate: false);
                cmd.Parameters.AddWithValue("@ContactCode", newCode);
                AuditHelper.AddCreatedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }

            processed++;
        }

        tx.Commit();
        return new ExcelImportResult { Success = true, Processed = processed, Message = $"{processed} contact record(s) imported successfully." };
    }

    private static void AddContactParams(SqlCommand cmd, IXLRow row, int? customerId, int? genderId, object availableFrom, object availableTo, int? userId, bool isUpdate)
    {
        cmd.Parameters.AddWithValue("@CustomerID", (object)customerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContactFor", NullStr(ExcelUtility.CellText(row, 4)));
        cmd.Parameters.AddWithValue("@ContactType", string.IsNullOrWhiteSpace(ExcelUtility.CellText(row, 5)) ? "Customer" : ExcelUtility.CellText(row, 5));
        cmd.Parameters.AddWithValue("@ContactStatus", string.IsNullOrWhiteSpace(ExcelUtility.CellText(row, 6)) ? "Active" : ExcelUtility.CellText(row, 6));
        cmd.Parameters.AddWithValue("@Name", ExcelUtility.CellText(row, 7));
        cmd.Parameters.AddWithValue("@SearchName", NullStr(ExcelUtility.CellText(row, 8)));
        cmd.Parameters.AddWithValue("@GenderID", (object)genderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MaritalStatus", NullStr(ExcelUtility.CellText(row, 10)));
        cmd.Parameters.AddWithValue("@ProfessionalTitle", NullStr(ExcelUtility.CellText(row, 11)));
        cmd.Parameters.AddWithValue("@Department", NullStr(ExcelUtility.CellText(row, 12)));
        cmd.Parameters.AddWithValue("@OfficeLocation", NullStr(ExcelUtility.CellText(row, 13)));
        cmd.Parameters.AddWithValue("@AvailableFrom", availableFrom ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AvailableTo", availableTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ReportToManagerName", NullStr(ExcelUtility.CellText(row, 16)));
        cmd.Parameters.AddWithValue("@Phone", NullStr(ExcelUtility.CellText(row, 17)));
        cmd.Parameters.AddWithValue("@Mobile", NullStr(ExcelUtility.CellText(row, 18)));
        cmd.Parameters.AddWithValue("@Email", NullStr(ExcelUtility.CellText(row, 19)));
        cmd.Parameters.AddWithValue("@Whatsapp", NullStr(ExcelUtility.CellText(row, 20)));
        cmd.Parameters.AddWithValue("@URL", NullStr(ExcelUtility.CellText(row, 21)));
        cmd.Parameters.AddWithValue("@Fax", NullStr(ExcelUtility.CellText(row, 22)));
        if (isUpdate) AuditHelper.AddModifiedBy(cmd, userId);
    }

    #endregion

    #region Position

    private static readonly string[] PositionHeaders =
    {
        "PositionID", "PositionNo", "Description", "JobTitle", "Department", "ReportsToPositionNo",
        "Title", "PositionType", "PositionDuration", "PositionStartDate", "PositionEndDate", "Status"
    };

    private static readonly string[] PositionWorkerHeaders =
    {
        "PositionNo", "EmployeeCode", "AssignmentStartDate", "AssignmentEndDate", "Reason"
    };

    public FileDownload ExportPositions()
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using var workbook = new XLWorkbook();
        var posSheet = workbook.Worksheets.Add("Positions");
        ExcelUtility.WriteHeaders(posSheet, PositionHeaders);

        using (var cmd = new SqlCommand(@"
            SELECT p.PositionID, p.PositionNo, ISNULL(p.Description, ''), ISNULL(j.JobTitle, ''), ISNULL(d.DepartmentName, ''),
                   ISNULL(rp.PositionNo, ''), ISNULL(t.TitleName, ''), ISNULL(et.EmploymentTypeName, ''),
                   ISNULL(p.PositionDuration, ''), p.PositionStartDate, p.PositionEndDate, p.IsActive
            FROM tblPosition p
            LEFT JOIN tblJob j ON j.JobID = p.JobID
            LEFT JOIN tblDepartment d ON d.DepartmentID = p.DepartmentID
            LEFT JOIN tblPosition rp ON rp.PositionID = p.ReportsToPositionID
            LEFT JOIN tblTitle t ON t.TitleID = p.TitleID
            LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = p.PositionTypeID
            ORDER BY p.PositionNo;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            var row = 2;
            while (dr.Read())
            {
                posSheet.Cell(row, 1).Value = dr.GetInt32(0);
                posSheet.Cell(row, 2).Value = dr.GetString(1);
                posSheet.Cell(row, 3).Value = dr.GetString(2);
                posSheet.Cell(row, 4).Value = dr.GetString(3);
                posSheet.Cell(row, 5).Value = dr.GetString(4);
                posSheet.Cell(row, 6).Value = dr.GetString(5);
                posSheet.Cell(row, 7).Value = dr.GetString(6);
                posSheet.Cell(row, 8).Value = dr.GetString(7);
                posSheet.Cell(row, 9).Value = dr.GetString(8);
                posSheet.Cell(row, 10).Value = dr.IsDBNull(9) ? "" : dr.GetDateTime(9).ToString("yyyy-MM-dd");
                posSheet.Cell(row, 11).Value = dr.IsDBNull(10) ? "" : dr.GetDateTime(10).ToString("yyyy-MM-dd");
                posSheet.Cell(row, 12).Value = ExcelUtility.FormatBool(dr.GetBoolean(11), asActive: true);
                row++;
            }
        }

        var workerSheet = workbook.Worksheets.Add("WorkerAssignments");
        ExcelUtility.WriteHeaders(workerSheet, PositionWorkerHeaders);
        using (var cmd = new SqlCommand(@"
            SELECT p.PositionNo, e.EmployeeCode, a.AssignmentStartDate, a.AssignmentEndDate, ISNULL(a.Reason, '')
            FROM tblPositionWorkerAssignment a
            INNER JOIN tblPosition p ON p.PositionID = a.PositionID
            INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
            ORDER BY p.PositionNo, a.SortOrder, a.PositionWorkerAssignmentID;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            var row = 2;
            while (dr.Read())
            {
                workerSheet.Cell(row, 1).Value = dr.GetString(0);
                workerSheet.Cell(row, 2).Value = dr.GetString(1);
                workerSheet.Cell(row, 3).Value = dr.IsDBNull(2) ? "" : dr.GetDateTime(2).ToString("yyyy-MM-dd");
                workerSheet.Cell(row, 4).Value = dr.IsDBNull(3) ? "" : dr.GetDateTime(3).ToString("yyyy-MM-dd");
                workerSheet.Cell(row, 5).Value = dr.GetString(4);
                row++;
            }
        }

        posSheet.Columns().AdjustToContents();
        workerSheet.Columns().AdjustToContents();
        return ExcelUtility.ToFile(workbook, "PositionMaster");
    }

    public ExcelImportResult ImportPositions(HttpPostedFile file, int? userId)
    {
        using var workbook = new XLWorkbook(file.InputStream);
        var posSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Positions", StringComparison.OrdinalIgnoreCase))
                       ?? workbook.Worksheets.First();
        var workerSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("WorkerAssignments", StringComparison.OrdinalIgnoreCase));

        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var processed = 0;
        var positionMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in posSheet.RowsUsed().Skip(1))
        {
            var positionNo = ExcelUtility.CellText(row, 2);
            if (string.IsNullOrWhiteSpace(positionNo)) continue;

            var positionId = ExcelUtility.TryInt(ExcelUtility.CellText(row, 1));
            if (positionId <= 0)
                positionId = FindIdByCode(conn, tx, "tblPosition", "PositionID", "PositionNo", positionNo);

            var reportsToNo = ExcelUtility.CellText(row, 6);
            int? reportsToId = string.IsNullOrWhiteSpace(reportsToNo)
                ? null
                : FindIdByCode(conn, tx, "tblPosition", "PositionID", "PositionNo", reportsToNo);

            var jobId = ResolveLookup(conn, tx, "tblJob", "JobID", "JobTitle", ExcelUtility.CellText(row, 4));
            var deptId = ResolveLookup(conn, tx, "tblDepartment", "DepartmentID", "DepartmentName", ExcelUtility.CellText(row, 5));
            var titleId = ResolveLookup(conn, tx, "tblTitle", "TitleID", "TitleName", ExcelUtility.CellText(row, 7));
            var typeId = ResolveLookup(conn, tx, "tblEmploymentType", "EmploymentTypeID", "EmploymentTypeName", ExcelUtility.CellText(row, 8));

            if (positionId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblPosition SET
                        PositionNo = @PositionNo, Description = @Description, JobID = @JobID, DepartmentID = @DepartmentID,
                        ReportsToPositionID = @ReportsToPositionID, TitleID = @TitleID, PositionTypeID = @PositionTypeID,
                        PositionDuration = @PositionDuration, PositionStartDate = @PositionStartDate, PositionEndDate = @PositionEndDate,
                        IsActive = @IsActive, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE PositionID = @PositionID;", conn, tx);
                cmd.Parameters.AddWithValue("@PositionNo", positionNo);
                cmd.Parameters.AddWithValue("@Description", NullStr(ExcelUtility.CellText(row, 3)));
                cmd.Parameters.AddWithValue("@JobID", (object)jobId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartmentID", (object)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReportsToPositionID", (object)reportsToId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TitleID", (object)titleId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionTypeID", (object)typeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionDuration", NullStr(ExcelUtility.CellText(row, 9)));
                cmd.Parameters.AddWithValue("@PositionStartDate", (object)ExcelUtility.ReadDate(row, 10) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionEndDate", (object)ExcelUtility.ReadDate(row, 11) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", ExcelUtility.ReadBool(ExcelUtility.CellText(row, 12), true));
                cmd.Parameters.AddWithValue("@PositionID", positionId);
                AuditHelper.AddModifiedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblPosition
                        (PositionNo, Description, JobID, DepartmentID, ReportsToPositionID, TitleID, PositionTypeID,
                         PositionDuration, PositionStartDate, PositionEndDate, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@PositionNo, @Description, @JobID, @DepartmentID, @ReportsToPositionID, @TitleID, @PositionTypeID,
                         @PositionDuration, @PositionStartDate, @PositionEndDate, @IsActive, GETDATE(), @CreatedByUserID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
                cmd.Parameters.AddWithValue("@PositionNo", positionNo);
                cmd.Parameters.AddWithValue("@Description", NullStr(ExcelUtility.CellText(row, 3)));
                cmd.Parameters.AddWithValue("@JobID", (object)jobId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartmentID", (object)deptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReportsToPositionID", (object)reportsToId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TitleID", (object)titleId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionTypeID", (object)typeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionDuration", NullStr(ExcelUtility.CellText(row, 9)));
                cmd.Parameters.AddWithValue("@PositionStartDate", (object)ExcelUtility.ReadDate(row, 10) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionEndDate", (object)ExcelUtility.ReadDate(row, 11) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", ExcelUtility.ReadBool(ExcelUtility.CellText(row, 12), true));
                AuditHelper.AddCreatedBy(cmd, userId);
                positionId = (int)cmd.ExecuteScalar();
            }

            positionMap[positionNo] = positionId;
            processed++;
        }

        if (workerSheet != null)
        {
            var workersByPosition = workerSheet.RowsUsed().Skip(1)
                .GroupBy(r => ExcelUtility.CellText(r, 1), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key));

            foreach (var group in workersByPosition)
            {
                if (!positionMap.TryGetValue(group.Key, out var positionId))
                    positionId = FindIdByCode(conn, tx, "tblPosition", "PositionID", "PositionNo", group.Key);
                if (positionId <= 0) continue;

                using (var del = new SqlCommand("DELETE FROM tblPositionWorkerAssignment WHERE PositionID = @PositionID;", conn, tx))
                {
                    del.Parameters.AddWithValue("@PositionID", positionId);
                    del.ExecuteNonQuery();
                }

                var sort = 0;
                foreach (var wRow in group)
                {
                    var empCode = ExcelUtility.CellText(wRow, 2);
                    if (string.IsNullOrWhiteSpace(empCode)) continue;
                    var empId = FindIdByCode(conn, tx, "tblEmployee", "EmployeeID", "EmployeeCode", empCode);
                    if (empId <= 0)
                        throw new InvalidOperationException($"WorkerAssignments row {wRow.RowNumber()}: Employee '{empCode}' not found.");

                    using var ins = new SqlCommand(@"
                        INSERT INTO tblPositionWorkerAssignment
                            (PositionID, EmployeeID, AssignmentStartDate, AssignmentEndDate, Reason, SortOrder, CreatedOn, CreatedByUserID)
                        VALUES
                            (@PositionID, @EmployeeID, @StartDate, @EndDate, @Reason, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);
                    ins.Parameters.AddWithValue("@PositionID", positionId);
                    ins.Parameters.AddWithValue("@EmployeeID", empId);
                    ins.Parameters.AddWithValue("@StartDate", (object)ExcelUtility.ReadDate(wRow, 3) ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@EndDate", (object)ExcelUtility.ReadDate(wRow, 4) ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Reason", NullStr(ExcelUtility.CellText(wRow, 5)));
                    ins.Parameters.AddWithValue("@SortOrder", ++sort);
                    AuditHelper.AddCreatedBy(ins, userId);
                    ins.ExecuteNonQuery();
                }
            }
        }

        tx.Commit();
        return new ExcelImportResult { Success = true, Processed = processed, Message = $"{processed} position record(s) imported successfully." };
    }

    #endregion

    #region Employee

    private static readonly string[] EmployeeHeaders =
    {
        "EmployeeID", "EmployeeCode", "FirstName", "LastName", "FathersHusbandsName", "NationalIDNumber",
        "Gender", "Department", "Division", "Designation", "EmploymentType", "EmploymentStatus",
        "LegalEntity", "Region", "Location", "DateOfJoining", "BasicSalary", "Status"
    };

    public FileDownload ExportEmployees()
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName, ISNULL(e.FathersHusbandsName, ''),
                   ISNULL(e.NationalIDNumber, ''), ISNULL(g.GenderName, ISNULL(e.Gender, '')),
                   ISNULL(d.DepartmentName, ''), ISNULL(dv.DivisionName, ''), ISNULL(e.Designation, ''),
                   ISNULL(et.EmploymentTypeName, ''), ISNULL(es.EmploymentStatusName, ''),
                   ISNULL(le.LegalEntityName, ''), ISNULL(r.RegionName, ''), ISNULL(l.LocationName, ''),
                   e.DateOfJoining, e.BasicSalary, e.Status
            FROM tblEmployee e
            LEFT JOIN tblGender g ON g.GenderID = e.GenderID
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblDivision dv ON dv.DivisionID = e.DivisionID
            LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = e.EmploymentTypeID
            LEFT JOIN tblEmploymentStatus es ON es.EmploymentStatusID = e.EmploymentStatusID
            LEFT JOIN tblLegalEntity le ON le.LegalEntityID = e.LegalEntityID
            LEFT JOIN tblRegion r ON r.RegionID = e.RegionID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            ORDER BY e.EmployeeCode;", conn);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");
        ExcelUtility.WriteHeaders(sheet, EmployeeHeaders);

        using var dr = cmd.ExecuteReader();
        var row = 2;
        while (dr.Read())
        {
            sheet.Cell(row, 1).Value = dr.GetInt32(0);
            sheet.Cell(row, 2).Value = dr.GetString(1);
            sheet.Cell(row, 3).Value = dr.GetString(2);
            sheet.Cell(row, 4).Value = dr.GetString(3);
            sheet.Cell(row, 5).Value = dr.GetString(4);
            sheet.Cell(row, 6).Value = dr.GetString(5);
            sheet.Cell(row, 7).Value = dr.GetString(6);
            sheet.Cell(row, 8).Value = dr.GetString(7);
            sheet.Cell(row, 9).Value = dr.GetString(8);
            sheet.Cell(row, 10).Value = dr.GetString(9);
            sheet.Cell(row, 11).Value = dr.GetString(10);
            sheet.Cell(row, 12).Value = dr.GetString(11);
            sheet.Cell(row, 13).Value = dr.GetString(12);
            sheet.Cell(row, 14).Value = dr.GetString(13);
            sheet.Cell(row, 15).Value = dr.GetString(14);
            sheet.Cell(row, 16).Value = dr.IsDBNull(15) ? "" : dr.GetDateTime(15).ToString("yyyy-MM-dd");
            sheet.Cell(row, 17).Value = dr.GetDecimal(16);
            sheet.Cell(row, 18).Value = dr.GetString(17);
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ExcelUtility.ToFile(workbook, "EmployeeMaster");
    }

    public ExcelImportResult ImportEmployees(HttpPostedFile file, int? userId)
    {
        using var workbook = new XLWorkbook(file.InputStream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RowsUsed().Skip(1).ToList();

        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var processed = 0;
        foreach (var row in rows)
        {
            var firstName = ExcelUtility.CellText(row, 3);
            var lastName = ExcelUtility.CellText(row, 4);
            var code = ExcelUtility.CellText(row, 2);
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(code)) continue;
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                throw new InvalidOperationException($"Row {row.RowNumber()}: FirstName and LastName are required.");

            var deptName = ExcelUtility.CellText(row, 8);
            var deptId = ResolveLookup(conn, tx, "tblDepartment", "DepartmentID", "DepartmentName", deptName);
            if (!deptId.HasValue)
                throw new InvalidOperationException($"Row {row.RowNumber()}: Department '{deptName}' not found.");

            var employeeId = ExcelUtility.TryInt(ExcelUtility.CellText(row, 1));
            if (employeeId <= 0 && !string.IsNullOrWhiteSpace(code))
                employeeId = FindIdByCode(conn, tx, "tblEmployee", "EmployeeID", "EmployeeCode", code);

            var genderName = ExcelUtility.CellText(row, 7);
            var genderId = ResolveLookup(conn, tx, "tblGender", "GenderID", "GenderName", genderName);
            var divisionId = ResolveLookup(conn, tx, "tblDivision", "DivisionID", "DivisionName", ExcelUtility.CellText(row, 9));
            var empTypeId = ResolveLookup(conn, tx, "tblEmploymentType", "EmploymentTypeID", "EmploymentTypeName", ExcelUtility.CellText(row, 11));
            var empStatusId = ResolveLookup(conn, tx, "tblEmploymentStatus", "EmploymentStatusID", "EmploymentStatusName", ExcelUtility.CellText(row, 12));
            var legalEntityId = ResolveLookup(conn, tx, "tblLegalEntity", "LegalEntityID", "LegalEntityName", ExcelUtility.CellText(row, 13));
            var regionId = ResolveLookup(conn, tx, "tblRegion", "RegionID", "RegionName", ExcelUtility.CellText(row, 14));
            var locationId = ResolveLookup(conn, tx, "tblLocation", "LocationID", "LocationName", ExcelUtility.CellText(row, 15));
            var status = string.IsNullOrWhiteSpace(ExcelUtility.CellText(row, 18)) ? "Active" : ExcelUtility.CellText(row, 18);
            var salary = ExcelUtility.TryDecimal(ExcelUtility.CellText(row, 17)) ?? 0m;
            var joinDate = ExcelUtility.ReadDate(row, 16);

            if (employeeId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblEmployee SET
                        EmployeeCode = @EmployeeCode, FirstName = @FirstName, LastName = @LastName,
                        FathersHusbandsName = @FathersHusbandsName, NationalIDNumber = @NationalIDNumber,
                        Gender = @Gender, GenderID = @GenderID, DepartmentID = @DepartmentID, DivisionID = @DivisionID,
                        Designation = @Designation, EmploymentTypeID = @EmploymentTypeID, EmploymentStatusID = @EmploymentStatusID,
                        LegalEntityID = @LegalEntityID, RegionID = @RegionID, LocationID = @LocationID,
                        DateOfJoining = @DateOfJoining, BasicSalary = @BasicSalary, Status = @Status,
                        ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE EmployeeID = @EmployeeID;", conn, tx);
                cmd.Parameters.AddWithValue("@EmployeeCode", string.IsNullOrWhiteSpace(code) ? DBNull.Value : code);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@FathersHusbandsName", NullStr(ExcelUtility.CellText(row, 5)));
                cmd.Parameters.AddWithValue("@NationalIDNumber", NullStr(ExcelUtility.CellText(row, 6)));
                cmd.Parameters.AddWithValue("@Gender", NullStr(genderName));
                cmd.Parameters.AddWithValue("@GenderID", (object)genderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartmentID", deptId.Value);
                cmd.Parameters.AddWithValue("@DivisionID", (object)divisionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Designation", NullStr(ExcelUtility.CellText(row, 10)));
                cmd.Parameters.AddWithValue("@EmploymentTypeID", (object)empTypeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmploymentStatusID", (object)empStatusId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LegalEntityID", (object)legalEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegionID", (object)regionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LocationID", (object)locationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateOfJoining", (object)joinDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BasicSalary", salary);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                AuditHelper.AddModifiedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                var newCode = string.IsNullOrWhiteSpace(code)
                    ? GenerateNextCode(conn, tx, "tblEmployee", "EmployeeCode", "EMP", 6)
                    : code;
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblEmployee
                        (EmployeeCode, FirstName, LastName, FathersHusbandsName, NationalIDNumber,
                         Gender, GenderID, DepartmentID, DivisionID, Designation,
                         EmploymentTypeID, EmploymentStatusID, LegalEntityID, RegionID, LocationID,
                         DateOfJoining, BasicSalary, Status, CreatedOn, CreatedByUserID)
                    VALUES
                        (@EmployeeCode, @FirstName, @LastName, @FathersHusbandsName, @NationalIDNumber,
                         @Gender, @GenderID, @DepartmentID, @DivisionID, @Designation,
                         @EmploymentTypeID, @EmploymentStatusID, @LegalEntityID, @RegionID, @LocationID,
                         @DateOfJoining, @BasicSalary, @Status, GETDATE(), @CreatedByUserID);", conn, tx);
                cmd.Parameters.AddWithValue("@EmployeeCode", newCode);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@FathersHusbandsName", NullStr(ExcelUtility.CellText(row, 5)));
                cmd.Parameters.AddWithValue("@NationalIDNumber", NullStr(ExcelUtility.CellText(row, 6)));
                cmd.Parameters.AddWithValue("@Gender", NullStr(genderName));
                cmd.Parameters.AddWithValue("@GenderID", (object)genderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartmentID", deptId.Value);
                cmd.Parameters.AddWithValue("@DivisionID", (object)divisionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Designation", NullStr(ExcelUtility.CellText(row, 10)));
                cmd.Parameters.AddWithValue("@EmploymentTypeID", (object)empTypeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmploymentStatusID", (object)empStatusId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LegalEntityID", (object)legalEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegionID", (object)regionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LocationID", (object)locationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateOfJoining", (object)joinDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BasicSalary", salary);
                cmd.Parameters.AddWithValue("@Status", status);
                AuditHelper.AddCreatedBy(cmd, userId);
                cmd.ExecuteNonQuery();
            }

            processed++;
        }

        tx.Commit();
        return new ExcelImportResult { Success = true, Processed = processed, Message = $"{processed} employee record(s) imported successfully." };
    }

    #endregion

    #region Helpers

    private static object NullStr(string value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object ParseTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeSpan.TryParse(value, out var t) ? t : null;
    }

    private static int FindIdByCode(SqlConnection conn, SqlTransaction tx, string table, string idCol, string codeCol, string code)
    {
        using var cmd = new SqlCommand($"SELECT TOP 1 {idCol} FROM {table} WHERE {codeCol} = @Code;", conn, tx);
        cmd.Parameters.AddWithValue("@Code", code.Trim());
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private static int? ResolveLookup(SqlConnection conn, SqlTransaction tx, string table, string idCol, string nameCol, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using var cmd = new SqlCommand($@"
            SELECT TOP 1 {idCol}
            FROM {table}
            WHERE {nameCol} = @Value
               OR (COL_LENGTH('{table}','AliasName') IS NOT NULL AND AliasName = @Value);", conn, tx);
        cmd.Parameters.AddWithValue("@Value", value.Trim());
        try
        {
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Lookup value '{value}' not found in {table}.");
            return Convert.ToInt32(result);
        }
        catch (SqlException)
        {
            using var fallback = new SqlCommand($"SELECT TOP 1 {idCol} FROM {table} WHERE {nameCol} = @Value;", conn, tx);
            fallback.Parameters.AddWithValue("@Value", value.Trim());
            var result = fallback.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Lookup value '{value}' not found in {table}.");
            return Convert.ToInt32(result);
        }
    }

    private static int? ResolveCurrency(SqlConnection conn, SqlTransaction tx, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 CurrencyID FROM tblCurrency
            WHERE CurrencyCode = @Value OR CurrencyName = @Value;", conn, tx);
        cmd.Parameters.AddWithValue("@Value", value.Trim());
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException($"Currency '{value}' not found.");
        return Convert.ToInt32(result);
    }

    private static string GenerateNextCode(SqlConnection conn, SqlTransaction tx, string table, string codeCol, string prefix, int digits)
    {
        using var cmd = new SqlCommand($@"
            SELECT ISNULL(MAX(TRY_CAST(SUBSTRING({codeCol}, {prefix.Length + 1}, 10) AS INT)), 0)
            FROM {table}
            WHERE {codeCol} LIKE @Pattern;", conn, tx);
        cmd.Parameters.AddWithValue("@Pattern", prefix + "[0-9]%");
        var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        return $"{prefix}{next.ToString().PadLeft(digits, '0')}";
    }

    #endregion
}
}
