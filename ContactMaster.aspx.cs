using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class ContactListItem
    {
        public int ContactID { get; set; }
        public string ContactCode { get; set; } = "";
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Name { get; set; } = "";
        public string ContactType { get; set; } = "";
        public string ContactStatus { get; set; } = "Active";
        public string Mobile { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class ContactInput
    {
        public int ContactID { get; set; }
        public string ContactCode { get; set; } = "";
        public int CustomerID { get; set; }
        public string ContactFor { get; set; } = "";
        public string ContactType { get; set; } = "Customer";
        public string ContactStatus { get; set; } = "Active";
        public string Name { get; set; } = "";
        public string SearchName { get; set; } = "";
        public int GenderID { get; set; }
        public string MaritalStatus { get; set; } = "";
        public string ProfessionalTitle { get; set; } = "";
        public string Department { get; set; } = "";
        public string OfficeLocation { get; set; } = "";
        public string AvailableFrom { get; set; } = "";
        public string AvailableTo { get; set; } = "";
        public string ReportToManagerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Email { get; set; } = "";
        public string Whatsapp { get; set; } = "";
        public string URL { get; set; } = "";
        public string Fax { get; set; } = "";
    }

    public partial class ContactMasterPage : AppBasePage
    {
        private static readonly Regex NameRegex = new Regex(@"^[a-zA-Z0-9\s\-_.&]+$", RegexOptions.Compiled);

        public static readonly string[] ContactTypes = { "Customer", "Vendor", "Prospect", "Consultant", "Guest", "Others" };
        public static readonly string[] ContactStatuses = { "Active", "Inactive" };
        public static readonly string[] MaritalStatuses = { "Single", "Married", "Divorced", "Widowed", "Other" };

        public string PageTitle => "Contact Master";
        public List<ContactListItem> Contacts { get; set; } = new List<ContactListItem>();
        public ContactInput Input { get; set; } = new ContactInput();
        public List<LookupItem> Customers { get; set; } = new List<LookupItem>();
        public List<LookupItem> Genders { get; set; } = new List<LookupItem>();
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
                    SoftDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                OnPostSave();
                return;
            }

            var newContact = Request.QueryString["newContact"] == "1" || Request.QueryString["newContact"] == "true";
            OnGet(QueryInt("editId"), newContact);
        }

        private void OnGet(int? editId, bool newContact)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newContact;
            if (ShowForm)
            {
                LoadLookups();
                if (editId.HasValue && editId > 0)
                {
                    LoadForEdit(editId.Value);
                    EditMode = true;
                }
                else Input.ContactCode = GenerateNextContactCode();
            }
            else LoadContacts();
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new ContactInput
            {
                ContactID = int.TryParse(Request.Form["ContactID"], out var cid) ? cid : 0,
                CustomerID = int.TryParse(Request.Form["CustomerID"], out var cuid) ? cuid : 0,
                ContactFor = FormString("ContactFor"),
                ContactType = string.IsNullOrWhiteSpace(FormString("ContactType")) ? "Customer" : FormString("ContactType"),
                ContactStatus = string.IsNullOrWhiteSpace(FormString("ContactStatus")) ? "Active" : FormString("ContactStatus"),
                Name = FormString("Name"),
                SearchName = FormString("SearchName"),
                GenderID = int.TryParse(Request.Form["GenderID"], out var gid) ? gid : 0,
                MaritalStatus = FormString("MaritalStatus"),
                ProfessionalTitle = FormString("ProfessionalTitle"),
                Department = FormString("Department"),
                OfficeLocation = FormString("OfficeLocation"),
                AvailableFrom = FormString("AvailableFrom"),
                AvailableTo = FormString("AvailableTo"),
                ReportToManagerName = FormString("ReportToManagerName"),
                Phone = FormString("Phone"),
                Mobile = FormString("Mobile"),
                Email = FormString("Email"),
                Whatsapp = FormString("Whatsapp"),
                URL = FormString("URL"),
                Fax = FormString("Fax")
            };

            if (string.IsNullOrWhiteSpace(Input.Name)) { SetFormError("Contact name is required."); return; }
            if (!NameRegex.IsMatch(Input.Name)) { SetFormError("Contact name must be alphanumeric."); return; }
            if (!ContactTypes.Contains(Input.ContactType, StringComparer.OrdinalIgnoreCase)) { SetFormError("Invalid contact type."); return; }
            if (!ContactStatuses.Contains(Input.ContactStatus, StringComparer.OrdinalIgnoreCase)) { SetFormError("Invalid contact status."); return; }
            if (!InputValidators.TryValidateContactMasterFields(Input.Email, Input.Phone, Input.Mobile, Input.Whatsapp, Input.Fax, out var fieldError))
            {
                SetFormError(fieldError);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input, EditMode);
                }
                SetAlert(EditMode ? "Contact updated successfully." : "Contact created successfully.");
                Response.Redirect("~/ContactMaster.aspx?editId=" + Input.ContactID);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetFormError("A contact with this ID already exists.");
            }
            catch (Exception ex) { SetFormError("Error: " + ex.Message); }
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET ContactStatus='Inactive', ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ContactID=@ContactID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ContactID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Contact deactivated successfully.");
            }
            catch (Exception ex) { SetAlert("Error removing contact: " + ex.Message, "error"); }
            Response.Redirect("~/ContactMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message; AlertType = "error";
            LoadLookups(); ShowForm = true;
            if (!EditMode) Input.ContactCode = GenerateNextContactCode();
        }

        private void SaveRecord(SqlConnection conn, ContactInput input, bool editMode)
        {
            if (editMode && input.ContactID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET CustomerID=@CustomerID, ContactFor=@ContactFor, ContactType=@ContactType,
  ContactStatus=@ContactStatus, Name=@Name, SearchName=@SearchName, GenderID=@GenderID,
  MaritalStatus=@MaritalStatus, ProfessionalTitle=@ProfessionalTitle, Department=@Department,
  OfficeLocation=@OfficeLocation, AvailableFrom=@AvailableFrom, AvailableTo=@AvailableTo,
  ReportToManagerName=@ReportToManagerName, Phone=@Phone, Mobile=@Mobile, Email=@Email,
  Whatsapp=@Whatsapp, URL=@URL, Fax=@Fax, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ContactID=@ContactID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@ContactID", input.ContactID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            input.ContactCode = GenerateNextContactCode(conn);
            using (var ins = new SqlCommand(@"
INSERT INTO tblContactMaster
 (ContactCode, CustomerID, ContactFor, ContactType, ContactStatus, Name, SearchName,
  GenderID, MaritalStatus, ProfessionalTitle, Department, OfficeLocation,
  AvailableFrom, AvailableTo, ReportToManagerName, Phone, Mobile, Email, Whatsapp, URL, Fax,
  CreatedOn, CreatedByUserID)
VALUES
 (@ContactCode, @CustomerID, @ContactFor, @ContactType, @ContactStatus, @Name, @SearchName,
  @GenderID, @MaritalStatus, @ProfessionalTitle, @Department, @OfficeLocation,
  @AvailableFrom, @AvailableTo, @ReportToManagerName, @Phone, @Mobile, @Email, @Whatsapp, @URL, @Fax,
  GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                ins.Parameters.AddWithValue("@ContactCode", input.ContactCode);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.ContactID = (int)ins.ExecuteScalar();
            }
        }

        private static void BindParams(SqlCommand cmd, ContactInput input)
        {
            cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID > 0 ? (object)input.CustomerID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactFor", string.IsNullOrWhiteSpace(input.ContactFor) ? (object)DBNull.Value : input.ContactFor);
            cmd.Parameters.AddWithValue("@ContactType", input.ContactType);
            cmd.Parameters.AddWithValue("@ContactStatus", input.ContactStatus);
            cmd.Parameters.AddWithValue("@Name", input.Name);
            cmd.Parameters.AddWithValue("@SearchName", string.IsNullOrWhiteSpace(input.SearchName) ? (object)DBNull.Value : input.SearchName);
            cmd.Parameters.AddWithValue("@GenderID", input.GenderID > 0 ? (object)input.GenderID : DBNull.Value);
            cmd.Parameters.AddWithValue("@MaritalStatus", string.IsNullOrWhiteSpace(input.MaritalStatus) ? (object)DBNull.Value : input.MaritalStatus);
            cmd.Parameters.AddWithValue("@ProfessionalTitle", string.IsNullOrWhiteSpace(input.ProfessionalTitle) ? (object)DBNull.Value : input.ProfessionalTitle);
            cmd.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(input.Department) ? (object)DBNull.Value : input.Department);
            cmd.Parameters.AddWithValue("@OfficeLocation", string.IsNullOrWhiteSpace(input.OfficeLocation) ? (object)DBNull.Value : input.OfficeLocation);
            cmd.Parameters.AddWithValue("@AvailableFrom", TimeParam(input.AvailableFrom));
            cmd.Parameters.AddWithValue("@AvailableTo", TimeParam(input.AvailableTo));
            cmd.Parameters.AddWithValue("@ReportToManagerName", string.IsNullOrWhiteSpace(input.ReportToManagerName) ? (object)DBNull.Value : input.ReportToManagerName);
            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(input.Phone) ? (object)DBNull.Value : input.Phone);
            cmd.Parameters.AddWithValue("@Mobile", string.IsNullOrWhiteSpace(input.Mobile) ? (object)DBNull.Value : input.Mobile);
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(input.Email) ? (object)DBNull.Value : input.Email);
            cmd.Parameters.AddWithValue("@Whatsapp", string.IsNullOrWhiteSpace(input.Whatsapp) ? (object)DBNull.Value : input.Whatsapp);
            cmd.Parameters.AddWithValue("@URL", string.IsNullOrWhiteSpace(input.URL) ? (object)DBNull.Value : input.URL);
            cmd.Parameters.AddWithValue("@Fax", string.IsNullOrWhiteSpace(input.Fax) ? (object)DBNull.Value : input.Fax);
        }

        private static object TimeParam(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            return TimeSpan.TryParse(value, out var t) ? (object)t : DBNull.Value;
        }

        private static string FormatTime(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
            return value.ToString() ?? "";
        }

        private string GenerateNextContactCode(SqlConnection conn = null)
        {
            var owns = conn == null;
            if (owns) { conn = new SqlConnection(Conn); conn.Open(); }
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(ContactCode,4,10) AS INT)),0)
FROM tblContactMaster WHERE ContactCode LIKE 'CNT[0-9]%';", conn))
                {
                    return "CNT" + (Convert.ToInt32(cmd.ExecuteScalar()) + 1).ToString("D6");
                }
            }
            finally { if (owns) conn.Dispose(); }
        }

        private void LoadContacts()
        {
            Contacts.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT c.ContactID, c.ContactCode, ISNULL(cu.CustomerCode,''), ISNULL(cu.Name,''),
       c.Name, c.ContactType, c.ContactStatus, ISNULL(c.Mobile,''), ISNULL(c.Email,'')
FROM tblContactMaster c
LEFT JOIN tblCustomer cu ON cu.CustomerID=c.CustomerID
ORDER BY CASE WHEN c.ContactStatus='Active' THEN 0 ELSE 1 END, c.ContactCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Contacts.Add(new ContactListItem
                        {
                            ContactID = dr.GetInt32(0),
                            ContactCode = dr.GetString(1),
                            CustomerCode = dr.GetString(2),
                            CustomerName = dr.GetString(3),
                            Name = dr.GetString(4),
                            ContactType = dr.GetString(5),
                            ContactStatus = dr.GetString(6),
                            Mobile = dr.GetString(7),
                            Email = dr.GetString(8)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int contactId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ContactID, ContactCode, CustomerID, ContactFor, ContactType, ContactStatus,
       Name, SearchName, GenderID, MaritalStatus, ProfessionalTitle, Department,
       OfficeLocation, AvailableFrom, AvailableTo, ReportToManagerName,
       Phone, Mobile, Email, Whatsapp, URL, Fax
FROM tblContactMaster WHERE ContactID=@ContactID;", conn))
            {
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new ContactInput
                    {
                        ContactID = dr.GetInt32(0),
                        ContactCode = dr.GetString(1),
                        CustomerID = dr.IsDBNull(2) ? 0 : dr.GetInt32(2),
                        ContactFor = dr.IsDBNull(3) ? "" : dr.GetString(3),
                        ContactType = dr.GetString(4),
                        ContactStatus = dr.GetString(5),
                        Name = dr.GetString(6),
                        SearchName = dr.IsDBNull(7) ? "" : dr.GetString(7),
                        GenderID = dr.IsDBNull(8) ? 0 : dr.GetInt32(8),
                        MaritalStatus = dr.IsDBNull(9) ? "" : dr.GetString(9),
                        ProfessionalTitle = dr.IsDBNull(10) ? "" : dr.GetString(10),
                        Department = dr.IsDBNull(11) ? "" : dr.GetString(11),
                        OfficeLocation = dr.IsDBNull(12) ? "" : dr.GetString(12),
                        AvailableFrom = FormatTime(dr.IsDBNull(13) ? null : dr.GetValue(13)),
                        AvailableTo = FormatTime(dr.IsDBNull(14) ? null : dr.GetValue(14)),
                        ReportToManagerName = dr.IsDBNull(15) ? "" : dr.GetString(15),
                        Phone = dr.IsDBNull(16) ? "" : dr.GetString(16),
                        Mobile = dr.IsDBNull(17) ? "" : dr.GetString(17),
                        Email = dr.IsDBNull(18) ? "" : dr.GetString(18),
                        Whatsapp = dr.IsDBNull(19) ? "" : dr.GetString(19),
                        URL = dr.IsDBNull(20) ? "" : dr.GetString(20),
                        Fax = dr.IsDBNull(21) ? "" : dr.GetString(21)
                    };
                }
            }
        }

        private void LoadLookups()
        {
            Customers = new List<LookupItem>();
            Genders = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT CustomerID, CustomerCode, Name FROM tblCustomer WHERE IsActive=1 ORDER BY CustomerCode;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Customers.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) + " – " + dr.GetString(2) });
                }
                using (var cmd = new SqlCommand("SELECT GenderID, GenderName FROM tblGender WHERE IsActive=1 ORDER BY GenderName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Genders.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }
    }
}
