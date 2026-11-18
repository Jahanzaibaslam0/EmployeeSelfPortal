using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace HRMS.Pages;

public class CustomerListItem
{
    public int CustomerID { get; set; }
    public string CustomerCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string SearchName { get; set; } = "";
    public string CityName { get; set; } = "";
    public string CustomerGroupName { get; set; } = "";
    public string NTN { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class CustomerInput
{
    public int CustomerID { get; set; }
    public string CustomerCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string SearchName { get; set; } = "";
    public int DealForBranchID { get; set; }
    public int CityID { get; set; }
    public int ProvinceID { get; set; }
    public int ModeOfDeliveryID { get; set; }
    public int CustomerGroupID { get; set; }
    public int CustomerClassID { get; set; }
    public int MethodOfPaymentID { get; set; }
    public int TermsOfPaymentID { get; set; }
    public int CurrencyID { get; set; }
    public int BillPreferenceID { get; set; }
    public int FBRStatusID { get; set; }
    public int TaxGroupID { get; set; }
    public string CNIC { get; set; } = "";
    public string NTN { get; set; } = "";
    public bool IsCAP { get; set; }
    public bool IsMandatoryCreditLimit { get; set; }
    public bool IsInvoiceHold { get; set; }
    public string TotalBusinessPotential { get; set; } = "";
    public string TargetBusinessSharePercent { get; set; } = "";
    public string TargetBusinessAmount { get; set; } = "";
    public string CreditLimit { get; set; } = "";
    public string AHDCreditLimit { get; set; } = "";
    public string PHDCreditLimit { get; set; } = "";
    public string HHDCreditLimit { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class CustomerMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    private static readonly Regex AlphanumericNameRegex = new(@"^[a-zA-Z0-9\s\-_.&]+$", RegexOptions.Compiled);
    private static readonly Regex AlphanumericNtnRegex = new(@"^[a-zA-Z0-9\-]+$", RegexOptions.Compiled);

    public CustomerMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public List<CustomerListItem> Customers { get; set; } = new();
    public CustomerInput Input { get; set; } = new();
    public List<LookupItem> Locations { get; set; } = new();
    public List<LookupItem> Cities { get; set; } = new();
    public List<LookupItem> Provinces { get; set; } = new();
    public List<LookupItem> ModeOfDeliveries { get; set; } = new();
    public List<LookupItem> CustomerGroups { get; set; } = new();
    public List<LookupItem> CustomerClasses { get; set; } = new();
    public List<LookupItem> MethodOfPayments { get; set; } = new();
    public List<LookupItem> TermsOfPayments { get; set; } = new();
    public List<LookupItem> Currencies { get; set; } = new();
    public List<LookupItem> BillPreferences { get; set; } = new();
    public List<LookupItem> FBRStatuses { get; set; } = new();
    public List<LookupItem> TaxGroups { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newCustomer)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newCustomer == true;

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
                Input.CustomerCode = GenerateNextCustomerCode();
            }
        }
        else
        {
            LoadCustomers();
        }
    }

    public IActionResult OnPost(
        int CustomerID,
        bool EditMode,
        string Name,
        string SearchName,
        int DealForBranchID,
        int CityID,
        int ProvinceID,
        int ModeOfDeliveryID,
        int CustomerGroupID,
        int CustomerClassID,
        int MethodOfPaymentID,
        int TermsOfPaymentID,
        int CurrencyID,
        int BillPreferenceID,
        int FBRStatusID,
        int TaxGroupID,
        string CNIC,
        string NTN,
        bool IsCAP,
        bool IsMandatoryCreditLimit,
        bool IsInvoiceHold,
        string TotalBusinessPotential,
        string TargetBusinessSharePercent,
        string TargetBusinessAmount,
        string CreditLimit,
        string AHDCreditLimit,
        string PHDCreditLimit,
        string HHDCreditLimit,
        bool IsActive)
    {
        this.EditMode = EditMode;
        Input = new CustomerInput
        {
            CustomerID = CustomerID,
            Name = Name?.Trim() ?? "",
            SearchName = SearchName?.Trim() ?? "",
            DealForBranchID = DealForBranchID,
            CityID = CityID,
            ProvinceID = ProvinceID,
            ModeOfDeliveryID = ModeOfDeliveryID,
            CustomerGroupID = CustomerGroupID,
            CustomerClassID = CustomerClassID,
            MethodOfPaymentID = MethodOfPaymentID,
            TermsOfPaymentID = TermsOfPaymentID,
            CurrencyID = CurrencyID,
            BillPreferenceID = BillPreferenceID,
            FBRStatusID = FBRStatusID,
            TaxGroupID = TaxGroupID,
            CNIC = CNIC?.Trim() ?? "",
            NTN = NTN?.Trim() ?? "",
            IsCAP = IsCAP,
            IsMandatoryCreditLimit = IsMandatoryCreditLimit,
            IsInvoiceHold = IsInvoiceHold,
            TotalBusinessPotential = TotalBusinessPotential?.Trim() ?? "",
            TargetBusinessSharePercent = TargetBusinessSharePercent?.Trim() ?? "",
            TargetBusinessAmount = TargetBusinessAmount?.Trim() ?? "",
            CreditLimit = CreditLimit?.Trim() ?? "",
            AHDCreditLimit = AHDCreditLimit?.Trim() ?? "",
            PHDCreditLimit = PHDCreditLimit?.Trim() ?? "",
            HHDCreditLimit = HHDCreditLimit?.Trim() ?? "",
            IsActive = IsActive
        };

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            SetFormError("Customer name is required.");
            return Page();
        }

        if (!AlphanumericNameRegex.IsMatch(Input.Name))
        {
            SetFormError("Customer name must be alphanumeric (letters, numbers, spaces, - _ . & allowed).");
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.NTN) && !AlphanumericNtnRegex.IsMatch(Input.NTN))
        {
            SetFormError("NTN must be alphanumeric.");
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input, EditMode);
            TempData["Alert"] = EditMode ? "Customer updated successfully." : "Customer created successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.CustomerID });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            SetFormError("A customer with this ID or duplicate value already exists.");
            return Page();
        }
        catch (Exception ex)
        {
            SetFormError("Error: " + ex.Message);
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblCustomer
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE CustomerID = @CustomerID;", conn);
            cmd.Parameters.AddWithValue("@CustomerID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Customer removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing customer: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SetFormError(string message)
    {
        AlertMessage = message;
        AlertType = "error";
        LoadLookups();
        ShowForm = true;
        if (!EditMode)
            Input.CustomerCode = GenerateNextCustomerCode();
    }

    private void SaveRecord(SqlConnection conn, CustomerInput input, bool editMode)
    {
        if (editMode && input.CustomerID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblCustomer
                SET Name = @Name,
                    SearchName = @SearchName,
                    DealForBranchID = @DealForBranchID,
                    CityID = @CityID,
                    ProvinceID = @ProvinceID,
                    ModeOfDeliveryID = @ModeOfDeliveryID,
                    CustomerGroupID = @CustomerGroupID,
                    CustomerClassID = @CustomerClassID,
                    MethodOfPaymentID = @MethodOfPaymentID,
                    TermsOfPaymentID = @TermsOfPaymentID,
                    CurrencyID = @CurrencyID,
                    BillPreferenceID = @BillPreferenceID,
                    FBRStatusID = @FBRStatusID,
                    TaxGroupID = @TaxGroupID,
                    CNIC = @CNIC,
                    NTN = @NTN,
                    IsCAP = @IsCAP,
                    IsMandatoryCreditLimit = @IsMandatoryCreditLimit,
                    IsInvoiceHold = @IsInvoiceHold,
                    TotalBusinessPotential = @TotalBusinessPotential,
                    TargetBusinessSharePercent = @TargetBusinessSharePercent,
                    TargetBusinessAmount = @TargetBusinessAmount,
                    CreditLimit = @CreditLimit,
                    AHDCreditLimit = @AHDCreditLimit,
                    PHDCreditLimit = @PHDCreditLimit,
                    HHDCreditLimit = @HHDCreditLimit,
                    IsActive = @IsActive,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE CustomerID = @CustomerID;", conn);
            BindParams(cmd, input);
            cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        input.CustomerCode = GenerateNextCustomerCode(conn);
        using var ins = new SqlCommand(@"
            INSERT INTO tblCustomer
                (CustomerCode, Name, SearchName, DealForBranchID, CityID, ProvinceID,
                 ModeOfDeliveryID, CustomerGroupID, CustomerClassID, MethodOfPaymentID, TermsOfPaymentID,
                 CurrencyID, BillPreferenceID, FBRStatusID, TaxGroupID,
                 CNIC, NTN, IsCAP, IsMandatoryCreditLimit, IsInvoiceHold,
                 TotalBusinessPotential, TargetBusinessSharePercent, TargetBusinessAmount,
                 CreditLimit, AHDCreditLimit, PHDCreditLimit, HHDCreditLimit,
                 IsActive, CreatedOn, CreatedByUserID)
            VALUES
                (@CustomerCode, @Name, @SearchName, @DealForBranchID, @CityID, @ProvinceID,
                 @ModeOfDeliveryID, @CustomerGroupID, @CustomerClassID, @MethodOfPaymentID, @TermsOfPaymentID,
                 @CurrencyID, @BillPreferenceID, @FBRStatusID, @TaxGroupID,
                 @CNIC, @NTN, @IsCAP, @IsMandatoryCreditLimit, @IsInvoiceHold,
                 @TotalBusinessPotential, @TargetBusinessSharePercent, @TargetBusinessAmount,
                 @CreditLimit, @AHDCreditLimit, @PHDCreditLimit, @HHDCreditLimit,
                 @IsActive, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        BindParams(ins, input);
        ins.Parameters.AddWithValue("@CustomerCode", input.CustomerCode);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.CustomerID = (int)ins.ExecuteScalar()!;
    }

    private static void BindParams(SqlCommand cmd, CustomerInput input)
    {
        cmd.Parameters.AddWithValue("@Name", input.Name);
        cmd.Parameters.AddWithValue("@SearchName", string.IsNullOrWhiteSpace(input.SearchName) ? DBNull.Value : input.SearchName);
        cmd.Parameters.AddWithValue("@DealForBranchID", Fk(input.DealForBranchID));
        cmd.Parameters.AddWithValue("@CityID", Fk(input.CityID));
        cmd.Parameters.AddWithValue("@ProvinceID", Fk(input.ProvinceID));
        cmd.Parameters.AddWithValue("@ModeOfDeliveryID", Fk(input.ModeOfDeliveryID));
        cmd.Parameters.AddWithValue("@CustomerGroupID", Fk(input.CustomerGroupID));
        cmd.Parameters.AddWithValue("@CustomerClassID", Fk(input.CustomerClassID));
        cmd.Parameters.AddWithValue("@MethodOfPaymentID", Fk(input.MethodOfPaymentID));
        cmd.Parameters.AddWithValue("@TermsOfPaymentID", Fk(input.TermsOfPaymentID));
        cmd.Parameters.AddWithValue("@CurrencyID", Fk(input.CurrencyID));
        cmd.Parameters.AddWithValue("@BillPreferenceID", Fk(input.BillPreferenceID));
        cmd.Parameters.AddWithValue("@FBRStatusID", Fk(input.FBRStatusID));
        cmd.Parameters.AddWithValue("@TaxGroupID", Fk(input.TaxGroupID));
        cmd.Parameters.AddWithValue("@CNIC", string.IsNullOrWhiteSpace(input.CNIC) ? DBNull.Value : input.CNIC);
        cmd.Parameters.AddWithValue("@NTN", string.IsNullOrWhiteSpace(input.NTN) ? DBNull.Value : input.NTN);
        cmd.Parameters.AddWithValue("@IsCAP", input.IsCAP);
        cmd.Parameters.AddWithValue("@IsMandatoryCreditLimit", input.IsMandatoryCreditLimit);
        cmd.Parameters.AddWithValue("@IsInvoiceHold", input.IsInvoiceHold);
        cmd.Parameters.AddWithValue("@TotalBusinessPotential", IntParam(input.TotalBusinessPotential));
        cmd.Parameters.AddWithValue("@TargetBusinessSharePercent", DecimalParam(input.TargetBusinessSharePercent));
        cmd.Parameters.AddWithValue("@TargetBusinessAmount", IntParam(input.TargetBusinessAmount));
        cmd.Parameters.AddWithValue("@CreditLimit", IntParam(input.CreditLimit));
        cmd.Parameters.AddWithValue("@AHDCreditLimit", IntParam(input.AHDCreditLimit));
        cmd.Parameters.AddWithValue("@PHDCreditLimit", IntParam(input.PHDCreditLimit));
        cmd.Parameters.AddWithValue("@HHDCreditLimit", IntParam(input.HHDCreditLimit));
        cmd.Parameters.AddWithValue("@IsActive", input.IsActive);
    }

    private static object Fk(int id) => id > 0 ? id : DBNull.Value;

    private static object IntParam(string value) =>
        int.TryParse(value, out var n) ? n : DBNull.Value;

    private static object DecimalParam(string value) =>
        decimal.TryParse(value, out var d) ? d : DBNull.Value;

    private string GenerateNextCustomerCode(SqlConnection? conn = null)
    {
        var ownsConnection = conn == null;
        conn ??= new SqlConnection(_conn);
        if (ownsConnection) conn.Open();

        try
        {
            using var cmd = new SqlCommand(@"
                SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(CustomerCode, 4, 10) AS INT)), 0)
                FROM tblCustomer
                WHERE CustomerCode LIKE 'CUS[0-9]%';", conn);
            var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"CUS{next:D6}";
        }
        finally
        {
            if (ownsConnection) conn.Dispose();
        }
    }

    private void LoadCustomers()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT c.CustomerID, c.CustomerCode, c.Name, ISNULL(c.SearchName, ''),
                   ISNULL(ci.CityName, ''), ISNULL(g.CustomerGroupName, ''), ISNULL(c.NTN, ''), c.IsActive
            FROM tblCustomer c
            LEFT JOIN tblCity ci ON ci.CityID = c.CityID
            LEFT JOIN tblCustomerGroup g ON g.CustomerGroupID = c.CustomerGroupID
            ORDER BY c.IsActive DESC, c.CustomerCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Customers.Add(new CustomerListItem
            {
                CustomerID = dr.GetInt32(0),
                CustomerCode = dr.GetString(1),
                Name = dr.GetString(2),
                SearchName = dr.GetString(3),
                CityName = dr.GetString(4),
                CustomerGroupName = dr.GetString(5),
                NTN = dr.GetString(6),
                IsActive = dr.GetBoolean(7)
            });
        }
    }

    private void LoadForEdit(int customerId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CustomerID, CustomerCode, Name, SearchName,
                   DealForBranchID, CityID, ProvinceID, ModeOfDeliveryID, CustomerGroupID, CustomerClassID,
                   MethodOfPaymentID, TermsOfPaymentID, CurrencyID, BillPreferenceID, FBRStatusID, TaxGroupID,
                   CNIC, NTN, IsCAP, IsMandatoryCreditLimit, IsInvoiceHold,
                   TotalBusinessPotential, TargetBusinessSharePercent, TargetBusinessAmount,
                   CreditLimit, AHDCreditLimit, PHDCreditLimit, HHDCreditLimit, IsActive
            FROM tblCustomer
            WHERE CustomerID = @CustomerID;", conn);
        cmd.Parameters.AddWithValue("@CustomerID", customerId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return;

        Input = new CustomerInput
        {
            CustomerID = dr.GetInt32(0),
            CustomerCode = dr.GetString(1),
            Name = dr.GetString(2),
            SearchName = dr.IsDBNull(3) ? "" : dr.GetString(3),
            DealForBranchID = dr.IsDBNull(4) ? 0 : dr.GetInt32(4),
            CityID = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
            ProvinceID = dr.IsDBNull(6) ? 0 : dr.GetInt32(6),
            ModeOfDeliveryID = dr.IsDBNull(7) ? 0 : dr.GetInt32(7),
            CustomerGroupID = dr.IsDBNull(8) ? 0 : dr.GetInt32(8),
            CustomerClassID = dr.IsDBNull(9) ? 0 : dr.GetInt32(9),
            MethodOfPaymentID = dr.IsDBNull(10) ? 0 : dr.GetInt32(10),
            TermsOfPaymentID = dr.IsDBNull(11) ? 0 : dr.GetInt32(11),
            CurrencyID = dr.IsDBNull(12) ? 0 : dr.GetInt32(12),
            BillPreferenceID = dr.IsDBNull(13) ? 0 : dr.GetInt32(13),
            FBRStatusID = dr.IsDBNull(14) ? 0 : dr.GetInt32(14),
            TaxGroupID = dr.IsDBNull(15) ? 0 : dr.GetInt32(15),
            CNIC = dr.IsDBNull(16) ? "" : dr.GetString(16),
            NTN = dr.IsDBNull(17) ? "" : dr.GetString(17),
            IsCAP = !dr.IsDBNull(18) && dr.GetBoolean(18),
            IsMandatoryCreditLimit = !dr.IsDBNull(19) && dr.GetBoolean(19),
            IsInvoiceHold = !dr.IsDBNull(20) && dr.GetBoolean(20),
            TotalBusinessPotential = dr.IsDBNull(21) ? "" : dr.GetInt32(21).ToString(),
            TargetBusinessSharePercent = dr.IsDBNull(22) ? "" : dr.GetDecimal(22).ToString("0.##"),
            TargetBusinessAmount = dr.IsDBNull(23) ? "" : dr.GetInt32(23).ToString(),
            CreditLimit = dr.IsDBNull(24) ? "" : dr.GetInt32(24).ToString(),
            AHDCreditLimit = dr.IsDBNull(25) ? "" : dr.GetInt32(25).ToString(),
            PHDCreditLimit = dr.IsDBNull(26) ? "" : dr.GetInt32(26).ToString(),
            HHDCreditLimit = dr.IsDBNull(27) ? "" : dr.GetInt32(27).ToString(),
            IsActive = !dr.IsDBNull(28) && dr.GetBoolean(28)
        };
    }

    private void LoadLookups()
    {
        Locations = LoadLookup("tblLocation", "LocationID", "LocationName");
        Cities = LoadLookup("tblCity", "CityID", "CityName");
        Provinces = LoadLookup("tblProvince", "ProvinceID", "ProvinceName");
        ModeOfDeliveries = LoadLookup("tblModeOfDelivery", "ModeOfDeliveryID", "ModeOfDeliveryName");
        CustomerGroups = LoadLookup("tblCustomerGroup", "CustomerGroupID", "CustomerGroupName");
        CustomerClasses = LoadLookup("tblCustomerClass", "CustomerClassID", "CustomerClassName");
        MethodOfPayments = LoadLookup("tblMethodOfPayment", "MethodOfPaymentID", "MethodOfPaymentName");
        TermsOfPayments = LoadLookup("tblTermsOfPayment", "TermsOfPaymentID", "TermsOfPaymentName");
        Currencies = LoadCurrencyLookup();
        BillPreferences = LoadLookup("tblBillPreference", "BillPreferenceID", "BillPreferenceName");
        FBRStatuses = LoadLookup("tblFBRStatus", "FBRStatusID", "FBRStatusName");
        TaxGroups = LoadLookup("tblTaxGroup", "TaxGroupID", "TaxGroupName");
    }

    private List<LookupItem> LoadLookup(string table, string idCol, string nameCol)
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idCol}, {nameCol}
            FROM {table}
            WHERE IsActive = 1
            ORDER BY {nameCol};", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        return items;
    }

    private List<LookupItem> LoadCurrencyLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CurrencyID, CurrencyCode, CurrencyName
            FROM tblCurrency
            WHERE IsActive = 1
            ORDER BY CurrencyCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.GetString(1);
            var name = dr.GetString(2);
            items.Add(new LookupItem { Id = dr.GetInt32(0), Name = $"{code} – {name}" });
        }
        return items;
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }
}
