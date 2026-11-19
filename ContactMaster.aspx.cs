using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
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
        public string LinkedMaster { get; set; } = "";
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
        public int VendorID { get; set; }
        public int EmployeeID { get; set; }
        public string GuestName { get; set; } = "";
        public string ContactFor { get; set; } = "";
        public string ContactType { get; set; } = "";
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
        public string RecordType { get; set; } = "People";
        public string VerificationStatus { get; set; } = ContactVerification.ToVerify;
        public string AccessStatus { get; set; } = "Open";
        public string WorkflowStatus { get; set; } = "Approved";
    }

    public partial class ContactMasterPage : AppBasePage
    {
        private static readonly Regex NameRegex = new Regex(@"^[a-zA-Z0-9\s\-_.&]+$", RegexOptions.Compiled);

        public const string TypeCustomer = "Customer";
        public const string TypeVendor = "Vendor";
        public const string TypeEmployee = "Employee";
        public const string TypeGuest = "Guest/Others";
        public static readonly string[] ContactTypes = { TypeCustomer, TypeVendor, TypeEmployee, TypeGuest };
        public static readonly string[] ContactStatuses = { "Active", "Inactive" };
        public static readonly string[] MaritalStatuses = { "Single", "Married", "Divorced", "Widowed", "Other" };

        public string PageTitle => "Contact Master";
        public List<ContactListItem> Contacts { get; set; } = new List<ContactListItem>();
        public ContactInput Input { get; set; } = new ContactInput();
        public List<LookupItem> Customers { get; set; } = new List<LookupItem>();
        public List<LookupItem> Vendors { get; set; } = new List<LookupItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> Genders { get; set; } = new List<LookupItem>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";
        public string PopupMessage { get; set; } = "";
        public ContactChangeRequest PendingRequest { get; set; }
        public ContactChangeRequest PendingAddressRequest { get; set; }
        public List<ContactChangeHistoryItem> WorkflowHistory { get; set; } = new List<ContactChangeHistoryItem>();
        public List<ContactChangeRequest> WorkflowRequests { get; set; } = new List<ContactChangeRequest>();
        public List<AuditEntry> AuditTrail { get; set; } = new List<AuditEntry>();
        private readonly ContactChangeWorkflowService _workflow = new ContactChangeWorkflowService();
        public List<ContactAddressItem> Addresses { get; set; } = new List<ContactAddressItem>();
        public List<GeoLookupItem> AddressCountries { get; set; } = new List<GeoLookupItem>();
        public List<GeoLookupItem> AddressStates { get; set; } = new List<GeoLookupItem>();
        public List<GeoLookupItem> AddressCities { get; set; } = new List<GeoLookupItem>();
        public List<GeoLookupItem> AddressPostals { get; set; } = new List<GeoLookupItem>();
        public List<GeoLookupItem> AddressStreets { get; set; } = new List<GeoLookupItem>();
        public ContactAddressItem AddressInput { get; set; } = new ContactAddressItem();

        public string ContactQrPngUrl
        {
            get
            {
                if (Input == null || Input.ContactID <= 0) return "";
                var row = CurrentQrRow();
                return ContactHubService.QrPngUrl(Input.ContactID, ContactHubService.QrStamp(row), 6);
            }
        }

        public string ContactQrViewUrl => Input == null || Input.ContactID <= 0 ? "#" : ContactHubService.QrViewUrl(Input.ContactID);
        public string ContactQrDownloadUrl
        {
            get
            {
                if (Input == null || Input.ContactID <= 0) return "#";
                return ContactHubService.QrDownloadUrl(Input.ContactID, ContactHubService.QrStamp(CurrentQrRow()));
            }
        }

        private readonly ContactAddressService _addresses = new ContactAddressService();

        private ContactHubRow CurrentQrRow()
        {
            var addr = (ContactAddressItem)null;
            foreach (var a in Addresses)
            {
                if (a != null && a.IsPrimary) { addr = a; break; }
            }
            if (addr == null && Addresses.Count > 0) addr = Addresses[0];
            return new ContactHubRow
            {
                ContactID = Input.ContactID,
                ContactCode = Input.ContactCode,
                Name = Input.Name,
                ContactType = Input.ContactType,
                Mobile = Input.Mobile,
                Phone = Input.Phone,
                Email = Input.Email,
                Whatsapp = Input.Whatsapp,
                Position = Input.ProfessionalTitle,
                Branch = Input.OfficeLocation,
                Department = Input.Department,
                OrgName = LinkedCompanyName(),
                AddressLine = addr == null ? "" : addr.LocationLabel
            };
        }

        private string LinkedCompanyName()
        {
            if (Input.ContactType == TypeCustomer)
            {
                foreach (var c in Customers)
                    if (c.Id == Input.CustomerID) return c.Name;
            }
            if (Input.ContactType == TypeVendor)
            {
                foreach (var v in Vendors)
                    if (v.Id == Input.VendorID) return v.Name;
            }
            return Input.GuestName ?? "";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            try { _addresses.EnsureSchema(); EnsureContactLinkSchema(); new ContactHubService().EnsureSchema(); _workflow.EnsureSchema(); }
            catch (Exception ex)
            {
                AlertMessage = "Contact address setup could not update the database: " + ex.Message;
                AlertType = "error";
            }

            if (!string.IsNullOrEmpty(Request.QueryString["geo"]))
            {
                ServeGeoLookup();
                return;
            }

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    SoftDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                if (string.Equals(handler, "SaveAddress", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostSaveAddress();
                    return;
                }
                if (string.Equals(handler, "DeleteAddress", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDeleteAddress();
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
                    PendingRequest = _workflow.GetPending(editId.Value);
                    Input = _workflow.OverlayPendingUpdate(Input);
                    LoadAddresses(editId.Value);
                    WorkflowRequests = _workflow.ListRequestsForContact(editId.Value);
                    WorkflowHistory = _workflow.HistoryForContact(editId.Value);
                    var addressIds = new List<int>();
                    foreach (var a in Addresses) addressIds.Add(a.AddressID);
                    foreach (var r in WorkflowRequests)
                        if (r.AddressID > 0) addressIds.Add(r.AddressID);
                    AuditTrail = _workflow.AuditTrailForContact(editId.Value, addressIds);
                }
                else
                {
                    Input.ContactCode = GenerateNextContactCode();
                    Input.VerificationStatus = ContactVerification.ToVerify;
                    Input.WorkflowStatus = ContactWorkflowStatuses.InProcess;
                    var preset = NormalizeContactType(Request.QueryString["presetType"] ?? "");
                    if (!string.IsNullOrEmpty(preset) && ContactTypes.Contains(preset, StringComparer.OrdinalIgnoreCase))
                        Input.ContactType = preset;
                }
            }
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new ContactInput
            {
                ContactID = int.TryParse(Request.Form["ContactID"], out var cid) ? cid : 0,
                CustomerID = int.TryParse(Request.Form["CustomerID"], out var cuid) ? cuid : 0,
                VendorID = int.TryParse(Request.Form["VendorID"], out var vid) ? vid : 0,
                EmployeeID = int.TryParse(Request.Form["EmployeeID"], out var eid) ? eid : 0,
                GuestName = FormString("GuestName"),
                ContactFor = FormString("ContactFor"),
                ContactType = NormalizeContactType(FormString("ContactType")),
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
                Fax = FormString("Fax"),
                RecordType = string.IsNullOrWhiteSpace(FormString("RecordType")) ? "People" : FormString("RecordType"),
                AccessStatus = string.IsNullOrWhiteSpace(FormString("AccessStatus")) ? "Open" : FormString("AccessStatus")
            };

            ApplyContactTypeLinks(Input);
            if (string.Equals(Input.ContactType, TypeGuest, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(Input.Name)
                && !string.IsNullOrWhiteSpace(Input.GuestName))
            {
                Input.Name = Input.GuestName.Trim();
            }

            if (string.IsNullOrWhiteSpace(Input.ContactType)) { SetFormError("Contact type is required."); return; }
            if (!ContactTypes.Contains(Input.ContactType, StringComparer.OrdinalIgnoreCase)) { SetFormError("Contact type must be Customer, Vendor, Employee, or Guest/Others."); return; }
            if (string.Equals(Input.ContactType, TypeCustomer, StringComparison.OrdinalIgnoreCase) && Input.CustomerID <= 0)
            { SetFormError("Select a customer from Customer Master."); return; }
            if (string.Equals(Input.ContactType, TypeVendor, StringComparison.OrdinalIgnoreCase) && Input.VendorID <= 0)
            { SetFormError("Select a vendor from Vendor Master."); return; }
            if (string.Equals(Input.ContactType, TypeEmployee, StringComparison.OrdinalIgnoreCase) && Input.EmployeeID <= 0)
            { SetFormError("Select an employee from Employee Master."); return; }
            if (string.Equals(Input.ContactType, TypeGuest, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Input.GuestName))
            { SetFormError("Guest Name / Description is required."); return; }
            if (string.IsNullOrWhiteSpace(Input.Name)) { SetFormError("Contact name is required."); return; }
            if (!string.Equals(Input.ContactType, TypeGuest, StringComparison.OrdinalIgnoreCase)
                && !NameRegex.IsMatch(Input.Name)) { SetFormError("Contact name must be alphanumeric."); return; }
            if (!ContactStatuses.Contains(Input.ContactStatus, StringComparer.OrdinalIgnoreCase)) { SetFormError("Invalid contact status."); return; }
            if (!InputValidators.TryValidateContactMasterFields(Input.Email, Input.Phone, Input.Mobile, Input.Whatsapp, Input.Fax, out var fieldError))
            {
                SetFormError(fieldError, popup: true);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (!TryEnsureUniqueContactChannels(conn, Input, out var uniqueError))
                    {
                        SetFormError(uniqueError, popup: true);
                        return;
                    }
                    var wfError = _workflow.Submit(Input, EditMode, SaveRecord, out _);
                    if (!string.IsNullOrEmpty(wfError))
                    {
                        SetFormError(wfError);
                        return;
                    }
                }
                SetAlert(EditMode
                    ? "Contact changes were submitted for approval. Live values stay unchanged until approved."
                    : "Contact created and submitted for approval (In Process).");
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
                if (_workflow.HasPendingForContact(deleteId))
                {
                    SetAlert("This contact has an in-process approval request. Finish that workflow before deactivating.", "error");
                    Response.Redirect("~/ContactMaster.aspx");
                    return;
                }
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

        private void SetFormError(string message, bool popup = false)
        {
            AlertMessage = message; AlertType = "error";
            if (popup) PopupMessage = message;
            LoadLookups(); ShowForm = true;
            if (!EditMode) Input.ContactCode = GenerateNextContactCode();
            else if (Input.ContactID > 0)
            {
                var posted = Input;
                LoadForEdit(Input.ContactID);
                posted.WorkflowStatus = Input.WorkflowStatus;
                posted.VerificationStatus = Input.VerificationStatus;
                posted.ContactCode = Input.ContactCode;
                Input = posted;
                LoadAddresses(Input.ContactID);
                WorkflowRequests = _workflow.ListRequestsForContact(Input.ContactID);
                WorkflowHistory = _workflow.HistoryForContact(Input.ContactID);
                var addressIds = new List<int>();
                foreach (var a in Addresses) addressIds.Add(a.AddressID);
                AuditTrail = _workflow.AuditTrailForContact(Input.ContactID, addressIds);
            }
        }

        private void OnPostSaveAddress()
        {
            var contactId = int.TryParse(Request.Form["ContactID"], out var cid) ? cid : 0;
            if (contactId <= 0)
            {
                SetAlert("Save the contact first, then add addresses.", "error");
                Response.Redirect("~/ContactMaster.aspx?newContact=1");
                return;
            }
            var item = new ContactAddressItem
            {
                AddressID = int.TryParse(FormString("AddressID"), out var aid) ? aid : 0,
                ContactID = contactId,
                CountryID = int.TryParse(FormString("CountryID"), out var ctry) ? ctry : 0,
                StateID = int.TryParse(FormString("StateID"), out var st) ? st : 0,
                CityID = int.TryParse(FormString("CityID"), out var cty) ? cty : 0,
                PostalID = int.TryParse(FormString("PostalID"), out var pst) ? pst : 0,
                StreetID = int.TryParse(FormString("StreetID"), out var str) ? str : 0,
                AddressLine = FormString("AddressLine"),
                Latitude = ParseCoordinate(FormString("Latitude"), out var latOk),
                Longitude = ParseCoordinate(FormString("Longitude"), out var lngOk),
                IsBilling = FormBool("IsBilling"),
                IsShipping = FormBool("IsShipping"),
                IsPrimary = FormBool("IsPrimary")
            };

            var isNew = item.AddressID <= 0;
            string error = null;
            if (!latOk) error = "Latitude is not a valid number. Please recorrect the value.";
            else if (!lngOk) error = "Longitude is not a valid number. Please recorrect the value.";
            if (string.IsNullOrEmpty(error))
                error = _workflow.SubmitAddress(item, out _);
            if (!string.IsNullOrEmpty(error))
            {
                EditMode = true;
                Input.ContactID = contactId;
                LoadForEdit(contactId);
                LoadLookups();
                AddressInput = item;
                LoadAddresses(contactId);
                AddressInput = item;
                ShowForm = true;
                AlertMessage = error;
                AlertType = "error";
                PopupMessage = error;
                return;
            }

            SetAlert(isNew
                ? "Address created and submitted for approval (In Process)."
                : "Address changes were submitted for approval. Live values stay unchanged until approved.");
            Response.Redirect("~/ContactMaster.aspx?editId=" + contactId);
        }

        private void OnPostDeleteAddress()
        {
            var contactId = int.TryParse(Request.Form["ContactID"], out var cid) ? cid : 0;
            var addressId = int.TryParse(Request.Form["deleteAddressId"], out var aid) ? aid : 0;
            var error = _workflow.SubmitAddressDelete(addressId, contactId, out _);
            if (!string.IsNullOrEmpty(error))
            {
                SetAlert(error, "error");
                Response.Redirect("~/ContactMaster.aspx?editId=" + contactId);
                return;
            }
            SetAlert("Address delete was submitted for approval.");
            Response.Redirect("~/ContactMaster.aspx?editId=" + contactId);
        }

        private void LoadAddresses(int contactId)
        {
            Addresses = _addresses.ListAddresses(contactId);
            AddressCountries = _addresses.ListCountries(true);
            if (AddressInput.AddressID <= 0 && AddressInput.CountryID <= 0)
            {
                var editAddressId = QueryInt("addressId");
                if (editAddressId.HasValue && editAddressId.Value > 0)
                {
                    foreach (var a in Addresses)
                    {
                        if (a.AddressID == editAddressId.Value)
                        {
                            AddressInput = _workflow.OverlayPendingAddress(a);
                            PendingAddressRequest = _workflow.GetPendingAddress(a.AddressID);
                            break;
                        }
                    }
                }
            }
            if (AddressInput.CountryID > 0)
                AddressStates = _addresses.ListStates(AddressInput.CountryID, true);
            if (AddressInput.StateID > 0)
                AddressCities = _addresses.ListCities(AddressInput.StateID, true);
            if (AddressInput.CityID > 0)
                AddressPostals = _addresses.ListPostals(AddressInput.CityID, true);
            if (AddressInput.PostalID > 0)
                AddressStreets = _addresses.ListStreets(AddressInput.PostalID, true);
        }

        private void ServeGeoLookup()
        {
            var geo = (Request.QueryString["geo"] ?? "").Trim().ToLowerInvariant();
            var parentId = 0;
            int.TryParse(Request.QueryString["parentId"], out parentId);
            List<GeoLookupItem> rows;
            if (geo == "states") rows = _addresses.ListStates(parentId, true);
            else if (geo == "cities") rows = _addresses.ListCities(parentId, true);
            else if (geo == "postals") rows = _addresses.ListPostals(parentId, true);
            else if (geo == "streets") rows = _addresses.ListStreets(parentId, true);
            else rows = new List<GeoLookupItem>();

            Response.Clear();
            Response.ContentType = "application/json";
            Response.Write(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(rows));
            Response.End();
        }

        private static decimal? ParseCoordinate(string raw, out bool valid)
        {
            valid = true;
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                return v;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out v))
                return v;
            valid = false;
            return null;
        }

        private static bool TryEnsureUniqueContactChannels(SqlConnection conn, ContactInput input, out string error)
        {
            error = null;
            var email = InputValidators.NormalizeContactEmail(input.Email);
            var phone = InputValidators.NormalizeContactPhone(input.Phone);
            var mobile = InputValidators.NormalizeContactPhone(input.Mobile);
            var whatsapp = InputValidators.NormalizeContactPhone(input.Whatsapp);
            var fax = InputValidators.NormalizeContactPhone(input.Fax);

            using (var cmd = new SqlCommand(@"
SELECT ContactCode, ISNULL(Phone,''), ISNULL(Mobile,''), ISNULL(Email,''), ISNULL(Whatsapp,''), ISNULL(Fax,'')
FROM tblContactMaster
WHERE (@ExcludeId = 0 OR ContactID <> @ExcludeId);", conn))
            {
                cmd.Parameters.AddWithValue("@ExcludeId", input.ContactID);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.GetString(0);
                        if (phone.Length > 0 && phone == InputValidators.NormalizeContactPhone(dr.GetString(1)))
                        {
                            error = DuplicateChannelMessage("Phone", code);
                            return false;
                        }
                        if (mobile.Length > 0 && mobile == InputValidators.NormalizeContactPhone(dr.GetString(2)))
                        {
                            error = DuplicateChannelMessage("Mobile", code);
                            return false;
                        }
                        if (email.Length > 0 && email == InputValidators.NormalizeContactEmail(dr.GetString(3)))
                        {
                            error = DuplicateChannelMessage("Email", code);
                            return false;
                        }
                        if (whatsapp.Length > 0 && whatsapp == InputValidators.NormalizeContactPhone(dr.GetString(4)))
                        {
                            error = DuplicateChannelMessage("WhatsApp", code);
                            return false;
                        }
                        if (fax.Length > 0 && fax == InputValidators.NormalizeContactPhone(dr.GetString(5)))
                        {
                            error = DuplicateChannelMessage("Fax", code);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static string DuplicateChannelMessage(string field, string contactCode)
            => $"{field} is already used on contact {contactCode}. Each Phone, Mobile, Fax, WhatsApp, and Email must be unique per contact. Please recorrect the value.";

        private void SaveRecord(SqlConnection conn, ContactInput input, bool editMode)
        {
            if (editMode && input.ContactID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET CustomerID=@CustomerID, VendorID=@VendorID, EmployeeID=@EmployeeID, GuestName=@GuestName,
  ContactFor=@ContactFor, ContactType=@ContactType,
  ContactStatus=@ContactStatus, Name=@Name, SearchName=@SearchName, GenderID=@GenderID,
  MaritalStatus=@MaritalStatus, ProfessionalTitle=@ProfessionalTitle, Department=@Department,
  OfficeLocation=@OfficeLocation, AvailableFrom=@AvailableFrom, AvailableTo=@AvailableTo,
  ReportToManagerName=@ReportToManagerName, Phone=@Phone, Mobile=@Mobile, Email=@Email,
  Whatsapp=@Whatsapp, URL=@URL, Fax=@Fax,
  RecordType=@RecordType, VerificationStatus=@VerificationStatus, AccessStatus=@AccessStatus,
  WorkflowStatus=@WorkflowStatus,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
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
 (ContactCode, CustomerID, VendorID, EmployeeID, GuestName, ContactFor, ContactType, ContactStatus, Name, SearchName,
  GenderID, MaritalStatus, ProfessionalTitle, Department, OfficeLocation,
  AvailableFrom, AvailableTo, ReportToManagerName, Phone, Mobile, Email, Whatsapp, URL, Fax,
  RecordType, VerificationStatus, AccessStatus, WorkflowStatus,
  CreatedOn, CreatedByUserID)
VALUES
 (@ContactCode, @CustomerID, @VendorID, @EmployeeID, @GuestName, @ContactFor, @ContactType, @ContactStatus, @Name, @SearchName,
  @GenderID, @MaritalStatus, @ProfessionalTitle, @Department, @OfficeLocation,
  @AvailableFrom, @AvailableTo, @ReportToManagerName, @Phone, @Mobile, @Email, @Whatsapp, @URL, @Fax,
  @RecordType, @VerificationStatus, @AccessStatus, @WorkflowStatus,
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
            cmd.Parameters.AddWithValue("@VendorID", input.VendorID > 0 ? (object)input.VendorID : DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID > 0 ? (object)input.EmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@GuestName", string.IsNullOrWhiteSpace(input.GuestName) ? (object)DBNull.Value : input.GuestName.Trim());
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
            cmd.Parameters.AddWithValue("@RecordType", string.IsNullOrWhiteSpace(input.RecordType) ? "People" : input.RecordType);
            cmd.Parameters.AddWithValue("@VerificationStatus", string.IsNullOrWhiteSpace(input.VerificationStatus) ? ContactVerification.ToVerify : input.VerificationStatus);
            cmd.Parameters.AddWithValue("@AccessStatus", string.IsNullOrWhiteSpace(input.AccessStatus) ? "Open" : input.AccessStatus);
            cmd.Parameters.AddWithValue("@WorkflowStatus", string.IsNullOrWhiteSpace(input.WorkflowStatus) ? ContactWorkflowStatuses.InProcess : input.WorkflowStatus);
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
SELECT c.ContactID, c.ContactCode,
       CASE
         WHEN c.ContactType = N'Customer' THEN LTRIM(RTRIM(ISNULL(cu.CustomerCode,'') + N' ' + ISNULL(cu.Name,'')))
         WHEN c.ContactType = N'Vendor' THEN LTRIM(RTRIM(ISNULL(v.VendorCode,'') + N' ' + ISNULL(v.Name,'')))
         WHEN c.ContactType = N'Employee' THEN LTRIM(RTRIM(ISNULL(e.EmployeeCode,'') + N' ' + ISNULL(e.FirstName,'') + N' ' + ISNULL(e.LastName,'')))
         ELSE ISNULL(c.GuestName, c.Name)
       END,
       c.Name, c.ContactType, c.ContactStatus, ISNULL(c.Mobile,''), ISNULL(c.Email,'')
FROM tblContactMaster c
LEFT JOIN tblCustomer cu ON cu.CustomerID=c.CustomerID
LEFT JOIN tblVendor v ON v.VendorID=c.VendorID
LEFT JOIN tblEmployee e ON e.EmployeeID=c.EmployeeID
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
                            LinkedMaster = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            Name = dr.GetString(3),
                            ContactType = dr.GetString(4),
                            ContactStatus = dr.GetString(5),
                            Mobile = dr.GetString(6),
                            Email = dr.GetString(7)
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
       Phone, Mobile, Email, Whatsapp, URL, Fax,
       ISNULL(VendorID,0), ISNULL(EmployeeID,0), ISNULL(GuestName,''),
       ISNULL(RecordType,N'People'), ISNULL(VerificationStatus,N'Verified'), ISNULL(AccessStatus,N'Open'),
       ISNULL(WorkflowStatus,N'Approved')
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
                        Fax = dr.IsDBNull(21) ? "" : dr.GetString(21),
                        VendorID = dr.FieldCount > 22 && !dr.IsDBNull(22) ? Convert.ToInt32(dr.GetValue(22)) : 0,
                        EmployeeID = dr.FieldCount > 23 && !dr.IsDBNull(23) ? Convert.ToInt32(dr.GetValue(23)) : 0,
                        GuestName = dr.FieldCount > 24 && !dr.IsDBNull(24) ? dr.GetString(24) : "",
                        RecordType = dr.FieldCount > 25 && !dr.IsDBNull(25) ? dr.GetString(25) : "People",
                        VerificationStatus = dr.FieldCount > 26 && !dr.IsDBNull(26) ? dr.GetString(26) : "Verified",
                        AccessStatus = dr.FieldCount > 27 && !dr.IsDBNull(27) ? dr.GetString(27) : "Open",
                        WorkflowStatus = dr.FieldCount > 28 && !dr.IsDBNull(28) ? dr.GetString(28) : "Approved"
                    };
                    Input.ContactType = NormalizeContactType(Input.ContactType);
                }
            }
        }

        private void LoadLookups()
        {
            Customers = new List<LookupItem>();
            Vendors = new List<LookupItem>();
            Employees = new List<LookupItem>();
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
                if (TableExists(conn, "tblVendor"))
                {
                    using (var cmd = new SqlCommand("SELECT VendorID, VendorCode, Name FROM tblVendor WHERE IsActive=1 ORDER BY VendorCode;", conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            Vendors.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) + " – " + dr.GetString(2) });
                    }
                }
                using (var cmd = new SqlCommand(@"
SELECT EmployeeID, EmployeeCode, ISNULL(FirstName,''), ISNULL(LastName,'')
FROM tblEmployee
WHERE ISNULL(Status, N'Active') <> N'Inactive'
ORDER BY EmployeeCode;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Employees.Add(new LookupItem
                        {
                            Id = dr.GetInt32(0),
                            Name = dr.GetString(1) + " – " + (dr.GetString(2) + " " + dr.GetString(3)).Trim()
                        });
                }
                using (var cmd = new SqlCommand("SELECT GenderID, GenderName FROM tblGender WHERE IsActive=1 ORDER BY GenderName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Genders.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }

        public static string NormalizeContactType(string value)
        {
            var type = (value ?? "").Trim();
            if (string.Equals(type, TypeCustomer, StringComparison.OrdinalIgnoreCase)) return TypeCustomer;
            if (string.Equals(type, TypeVendor, StringComparison.OrdinalIgnoreCase)) return TypeVendor;
            if (string.Equals(type, TypeEmployee, StringComparison.OrdinalIgnoreCase)) return TypeEmployee;
            if (string.Equals(type, TypeGuest, StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Guest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Others", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Prospect", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Consultant", StringComparison.OrdinalIgnoreCase))
                return TypeGuest;
            return type;
        }

        private static void ApplyContactTypeLinks(ContactInput input)
        {
            if (string.Equals(input.ContactType, TypeCustomer, StringComparison.OrdinalIgnoreCase))
            {
                input.VendorID = 0;
                input.EmployeeID = 0;
                input.GuestName = "";
            }
            else if (string.Equals(input.ContactType, TypeVendor, StringComparison.OrdinalIgnoreCase))
            {
                input.CustomerID = 0;
                input.EmployeeID = 0;
                input.GuestName = "";
            }
            else if (string.Equals(input.ContactType, TypeEmployee, StringComparison.OrdinalIgnoreCase))
            {
                input.CustomerID = 0;
                input.VendorID = 0;
                input.GuestName = "";
            }
            else
            {
                input.CustomerID = 0;
                input.VendorID = 0;
                input.EmployeeID = 0;
            }
        }

        private void EnsureContactLinkSchema()
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'VendorID') IS NULL ALTER TABLE dbo.tblContactMaster ADD VendorID INT NULL;");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'EmployeeID') IS NULL ALTER TABLE dbo.tblContactMaster ADD EmployeeID INT NULL;");
                ExecIgnore(conn, "IF COL_LENGTH(N'dbo.tblContactMaster', N'GuestName') IS NULL ALTER TABLE dbo.tblContactMaster ADD GuestName NVARCHAR(200) NULL;");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblVendor', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactMaster_Vendor', N'F') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD CONSTRAINT FK_tblContactMaster_Vendor FOREIGN KEY (VendorID) REFERENCES dbo.tblVendor(VendorID);");
                ExecIgnore(conn, @"
IF OBJECT_ID(N'dbo.tblEmployee', N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.FK_tblContactMaster_Employee', N'F') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD CONSTRAINT FK_tblContactMaster_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.tblEmployee(EmployeeID);");
                ExecIgnore(conn, @"
UPDATE dbo.tblContactMaster
SET ContactType = N'Guest/Others'
WHERE ContactType IN (N'Guest', N'Others', N'Prospect', N'Consultant');");
            }
        }

        public List<ContactChangeRequest> RequestsForAddress(int addressId)
        {
            var list = new List<ContactChangeRequest>();
            if (WorkflowRequests == null || addressId <= 0) return list;
            foreach (var r in WorkflowRequests)
                if (r.AddressID == addressId) list.Add(r);
            return list;
        }

        private static bool TableExists(SqlConnection conn, string tableName)
        {
            using (var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@Name, N'U') IS NULL THEN 0 ELSE 1 END;", conn))
            {
                cmd.Parameters.AddWithValue("@Name", tableName);
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        private static void ExecIgnore(SqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
