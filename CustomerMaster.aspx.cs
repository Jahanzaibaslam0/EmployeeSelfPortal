using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public partial class CustomerMasterPage : AppBasePage
    {
        private static readonly Regex AlphanumericNameRegex = new Regex(@"^[a-zA-Z0-9\s\-_.&]+$", RegexOptions.Compiled);
        private static readonly Regex AlphanumericNtnRegex = new Regex(@"^[a-zA-Z0-9\-]+$", RegexOptions.Compiled);

        public string PageTitle => "Customer Master";
        public List<CustomerListItem> Customers { get; set; } = new List<CustomerListItem>();
        public CustomerInput Input { get; set; } = new CustomerInput();
        public List<LookupItem> Locations { get; set; } = new List<LookupItem>();
        public List<LookupItem> Cities { get; set; } = new List<LookupItem>();
        public List<LookupItem> Provinces { get; set; } = new List<LookupItem>();
        public List<LookupItem> ModeOfDeliveries { get; set; } = new List<LookupItem>();
        public List<LookupItem> CustomerGroups { get; set; } = new List<LookupItem>();
        public List<LookupItem> CustomerClasses { get; set; } = new List<LookupItem>();
        public List<LookupItem> MethodOfPayments { get; set; } = new List<LookupItem>();
        public List<LookupItem> TermsOfPayments { get; set; } = new List<LookupItem>();
        public List<LookupItem> Currencies { get; set; } = new List<LookupItem>();
        public List<LookupItem> BillPreferences { get; set; } = new List<LookupItem>();
        public List<LookupItem> FBRStatuses { get; set; } = new List<LookupItem>();
        public List<LookupItem> TaxGroups { get; set; } = new List<LookupItem>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

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

            var newCustomer = Request.QueryString["newCustomer"] == "1" || Request.QueryString["newCustomer"] == "true";
            OnGet(QueryInt("editId"), newCustomer);
        }

        private void OnGet(int? editId, bool newCustomer)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newCustomer;

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

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new CustomerInput
            {
                CustomerID = int.TryParse(Request.Form["CustomerID"], out var cid) ? cid : 0,
                Name = FormString("Name"),
                SearchName = FormString("SearchName"),
                DealForBranchID = int.TryParse(Request.Form["DealForBranchID"], out var a) ? a : 0,
                CityID = int.TryParse(Request.Form["CityID"], out var b) ? b : 0,
                ProvinceID = int.TryParse(Request.Form["ProvinceID"], out var c) ? c : 0,
                ModeOfDeliveryID = int.TryParse(Request.Form["ModeOfDeliveryID"], out var d) ? d : 0,
                CustomerGroupID = int.TryParse(Request.Form["CustomerGroupID"], out var e) ? e : 0,
                CustomerClassID = int.TryParse(Request.Form["CustomerClassID"], out var f) ? f : 0,
                MethodOfPaymentID = int.TryParse(Request.Form["MethodOfPaymentID"], out var g) ? g : 0,
                TermsOfPaymentID = int.TryParse(Request.Form["TermsOfPaymentID"], out var h) ? h : 0,
                CurrencyID = int.TryParse(Request.Form["CurrencyID"], out var i) ? i : 0,
                BillPreferenceID = int.TryParse(Request.Form["BillPreferenceID"], out var j) ? j : 0,
                FBRStatusID = int.TryParse(Request.Form["FBRStatusID"], out var k) ? k : 0,
                TaxGroupID = int.TryParse(Request.Form["TaxGroupID"], out var l) ? l : 0,
                CNIC = FormString("CNIC"),
                NTN = FormString("NTN"),
                IsCAP = FormBool("IsCAP"),
                IsMandatoryCreditLimit = FormBool("IsMandatoryCreditLimit"),
                IsInvoiceHold = FormBool("IsInvoiceHold"),
                TotalBusinessPotential = FormString("TotalBusinessPotential"),
                TargetBusinessSharePercent = FormString("TargetBusinessSharePercent"),
                TargetBusinessAmount = FormString("TargetBusinessAmount"),
                CreditLimit = FormString("CreditLimit"),
                AHDCreditLimit = FormString("AHDCreditLimit"),
                PHDCreditLimit = FormString("PHDCreditLimit"),
                HHDCreditLimit = FormString("HHDCreditLimit"),
                IsActive = FormBool("IsActive")
            };

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                SetFormError("Customer name is required.");
                return;
            }
            if (!AlphanumericNameRegex.IsMatch(Input.Name))
            {
                SetFormError("Customer name must be alphanumeric (letters, numbers, spaces, - _ . & allowed).");
                return;
            }
            if (!string.IsNullOrWhiteSpace(Input.NTN) && !AlphanumericNtnRegex.IsMatch(Input.NTN))
            {
                SetFormError("NTN must be alphanumeric.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input, EditMode);
                }
                SetAlert(EditMode ? "Customer updated successfully." : "Customer created successfully.");
                Response.Redirect("~/CustomerMaster.aspx?editId=" + Input.CustomerID);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetFormError("A customer with this ID or duplicate value already exists.");
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
                using (var cmd = new SqlCommand(@"
UPDATE tblCustomer SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE CustomerID = @CustomerID;", conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Customer removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing customer: " + ex.Message, "error");
            }
            Response.Redirect("~/CustomerMaster.aspx");
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
                using (var cmd = new SqlCommand(@"
UPDATE tblCustomer SET Name=@Name, SearchName=@SearchName, DealForBranchID=@DealForBranchID,
  CityID=@CityID, ProvinceID=@ProvinceID, ModeOfDeliveryID=@ModeOfDeliveryID, CustomerGroupID=@CustomerGroupID,
  CustomerClassID=@CustomerClassID, MethodOfPaymentID=@MethodOfPaymentID, TermsOfPaymentID=@TermsOfPaymentID,
  CurrencyID=@CurrencyID, BillPreferenceID=@BillPreferenceID, FBRStatusID=@FBRStatusID, TaxGroupID=@TaxGroupID,
  CNIC=@CNIC, NTN=@NTN, IsCAP=@IsCAP, IsMandatoryCreditLimit=@IsMandatoryCreditLimit, IsInvoiceHold=@IsInvoiceHold,
  TotalBusinessPotential=@TotalBusinessPotential, TargetBusinessSharePercent=@TargetBusinessSharePercent,
  TargetBusinessAmount=@TargetBusinessAmount, CreditLimit=@CreditLimit, AHDCreditLimit=@AHDCreditLimit,
  PHDCreditLimit=@PHDCreditLimit, HHDCreditLimit=@HHDCreditLimit, IsActive=@IsActive,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE CustomerID=@CustomerID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            input.CustomerCode = GenerateNextCustomerCode(conn);
            using (var ins = new SqlCommand(@"
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
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                ins.Parameters.AddWithValue("@CustomerCode", input.CustomerCode);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.CustomerID = (int)ins.ExecuteScalar();
            }
        }

        private static void BindParams(SqlCommand cmd, CustomerInput input)
        {
            cmd.Parameters.AddWithValue("@Name", input.Name);
            cmd.Parameters.AddWithValue("@SearchName", string.IsNullOrWhiteSpace(input.SearchName) ? (object)DBNull.Value : input.SearchName);
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
            cmd.Parameters.AddWithValue("@CNIC", string.IsNullOrWhiteSpace(input.CNIC) ? (object)DBNull.Value : input.CNIC);
            cmd.Parameters.AddWithValue("@NTN", string.IsNullOrWhiteSpace(input.NTN) ? (object)DBNull.Value : input.NTN);
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

        private static object Fk(int id) => id > 0 ? (object)id : DBNull.Value;
        private static object IntParam(string value) => int.TryParse(value, out var n) ? (object)n : DBNull.Value;
        private static object DecimalParam(string value) => decimal.TryParse(value, out var d) ? (object)d : DBNull.Value;

        private string GenerateNextCustomerCode(SqlConnection conn = null)
        {
            var owns = conn == null;
            if (owns) { conn = new SqlConnection(Conn); conn.Open(); }
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(CustomerCode, 4, 10) AS INT)), 0)
FROM tblCustomer WHERE CustomerCode LIKE 'CUS[0-9]%';", conn))
                {
                    var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    return "CUS" + next.ToString("D6");
                }
            }
            finally { if (owns) conn.Dispose(); }
        }

        private void LoadCustomers()
        {
            Customers.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT c.CustomerID, c.CustomerCode, c.Name, ISNULL(c.SearchName, ''),
       ISNULL(ci.CityName, ''), ISNULL(g.CustomerGroupName, ''), ISNULL(c.NTN, ''), c.IsActive
FROM tblCustomer c
LEFT JOIN tblCity ci ON ci.CityID = c.CityID
LEFT JOIN tblCustomerGroup g ON g.CustomerGroupID = c.CustomerGroupID
ORDER BY c.IsActive DESC, c.CustomerCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadForEdit(int customerId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT CustomerID, CustomerCode, Name, SearchName,
       DealForBranchID, CityID, ProvinceID, ModeOfDeliveryID, CustomerGroupID, CustomerClassID,
       MethodOfPaymentID, TermsOfPaymentID, CurrencyID, BillPreferenceID, FBRStatusID, TaxGroupID,
       CNIC, NTN, IsCAP, IsMandatoryCreditLimit, IsInvoiceHold,
       TotalBusinessPotential, TargetBusinessSharePercent, TargetBusinessAmount,
       CreditLimit, AHDCreditLimit, PHDCreditLimit, HHDCreditLimit, IsActive
FROM tblCustomer WHERE CustomerID = @CustomerID;", conn))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
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
            }
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
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT " + idCol + ", " + nameCol + " FROM " + table + " WHERE IsActive=1 ORDER BY " + nameCol + ";", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
            }
            return items;
        }

        private List<LookupItem> LoadCurrencyLookup()
        {
            var items = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT CurrencyID, CurrencyCode, CurrencyName FROM tblCurrency WHERE IsActive=1 ORDER BY CurrencyCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) + " – " + dr.GetString(2) });
            }
            return items;
        }
    }
}
