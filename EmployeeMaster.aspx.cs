using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class EmployeeViewModel
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string LegalEntityName { get; set; } = "";
        public string EmploymentType { get; set; } = "";
        public string EmploymentStatus { get; set; } = "";
        public string Designation { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime? DateOfJoining { get; set; }
        public decimal BasicSalary { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class DepartmentItem
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
    }

    public class CurrencyLookupItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class EmployeeInput
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FathersHusbandsName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string NationalIDNumber { get; set; } = "";
        public string Gender { get; set; } = "";
        public int GenderID { get; set; }
        public string DateOfBirth { get; set; } = "";
        public string MaritalStatus { get; set; } = "";
        public int DepartmentID { get; set; }
        public int DivisionID { get; set; }
        public int NationalityID { get; set; }
        public int ReligionID { get; set; }
        public int LanguageID { get; set; }
        public int WorkerCategoryID { get; set; }
        public int EmploymentTypeID { get; set; }
        public int EmploymentStatusID { get; set; }
        public int WorkforceSegmentID { get; set; }
        public int LegalEntityID { get; set; }
        public int BusinessUnitID { get; set; }
        public int SalesTeamID { get; set; }
        public int CostCenterID { get; set; }
        public int RegionID { get; set; }
        public int LocationID { get; set; }
        public int JobID { get; set; }
        public int WorkerLocationID { get; set; }
        public int CityID { get; set; }
        public int ProvinceID { get; set; }
        public int SalesGroupID { get; set; }
        public int GradeID { get; set; }
        public int ExtensionID { get; set; }
        public string Domicile { get; set; } = "";
        public int BloodGroupID { get; set; }
        public int BenefitEntitlementID { get; set; }
        public int UserID { get; set; }
        public int TemporaryResponsibleEmployeeID { get; set; }
        public int PermanentResponsibleEmployeeID { get; set; }
        public string Designation { get; set; } = "";
        public string DateOfJoining { get; set; } = "";
        public string EmploymentStartDate { get; set; } = "";
        public string ProbationPeriodDays { get; set; } = "";
        public string ProbationEndDate { get; set; } = "";
        public string ConfirmationDate { get; set; } = "";
        public string BasicSalary { get; set; } = "";
        public string Status { get; set; } = "Active";
        public string PhotoPath { get; set; } = "";

        public string TotalTenureDisplay
        {
            get { return EmployeeMasterPage.FormatTenure(ParseDate(DateOfJoining)); }
        }

        public string CurrentRoleTenureDisplay
        {
            get
            {
                var src = !string.IsNullOrWhiteSpace(EmploymentStartDate) ? EmploymentStartDate : DateOfJoining;
                return EmployeeMasterPage.FormatTenure(ParseDate(src));
            }
        }

        public string AgeDisplay
        {
            get { return EmployeeMasterPage.FormatAge(ParseDate(DateOfBirth)); }
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime d;
            return DateTime.TryParse(value, out d) ? d : (DateTime?)null;
        }
    }

    public class EmployeeContactInput
    {
        public int ContactID { get; set; }
        public string ContactType { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string Relationship { get; set; } = "";
        public string ContactValue { get; set; } = "";
        public bool IsPrimary { get; set; }
    }

    public class EmployeeAddressInput
    {
        public int AddressID { get; set; }
        public string AddressType { get; set; } = "";
        public string AddressLine { get; set; } = "";
        public string City { get; set; } = "";
        public string ProvinceState { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public bool IsPrimary { get; set; }
    }

    public class EmployeeFamilyMemberInput
    {
        public int FamilyMemberID { get; set; }
        public string MemberName { get; set; } = "";
        public string Relationship { get; set; } = "";
        public string Gender { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public bool IsDependent { get; set; }
    }

    public class EmployeeBankInput
    {
        public int EmployeeBankID { get; set; }
        public int BankID { get; set; }
        public string BankCode { get; set; } = "";
        public string LocationName { get; set; } = "";
        public int BankGroupID { get; set; }
        public string IBAN { get; set; } = "";
        public string SwiftBICCode { get; set; } = "";
        public string CurrencyCode { get; set; } = "";
        public string AccountVerificationStatus { get; set; } = "Pending";
        public bool IsPrimary { get; set; }
    }

    public class EmployeeEducationInput
    {
        public int EducationID { get; set; }
        public string HighestQualification { get; set; } = "";
        public string DegreeCertificate { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string Institution { get; set; } = "";
        public string YearOfPassing { get; set; } = "";
        public string GradeCGPA { get; set; } = "";
    }

    public class EmployeeCertificateInput
    {
        public int CertificateID { get; set; }
        public string CertificationName { get; set; } = "";
        public string CertificationBody { get; set; } = "";
        public string CertificateNumber { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public bool RenewalRequired { get; set; }
        public string CertificateCopyPath { get; set; } = "";
    }

    public class EmployeeDocumentInput
    {
        public int EmployeeDocumentID { get; set; }
        public int DocumentTypeID { get; set; }
        public string DocumentTypeName { get; set; } = "";
        public string DocumentNumber { get; set; } = "";
        public string IssueDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string VerificationStatus { get; set; } = "Pending";
    }

    public partial class EmployeeMasterPage : AppBasePage
    {
        private readonly EmployeeProfileAccessService _profileAccess = new EmployeeProfileAccessService();
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();
        private readonly MasterExcelService _excel = new MasterExcelService();

        private static readonly HashSet<string> ProfilePhotoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public string PageTitle => "Employee Master";
        public List<EmployeeViewModel> Employees { get; private set; } = new List<EmployeeViewModel>();
        public List<DepartmentItem> Departments { get; private set; } = new List<DepartmentItem>();
        public List<LookupItem> Genders { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Nationalities { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Religions { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Languages { get; private set; } = new List<LookupItem>();
        public List<LookupItem> WorkerCategories { get; private set; } = new List<LookupItem>();
        public List<LookupItem> EmploymentTypes { get; private set; } = new List<LookupItem>();
        public List<LookupItem> EmploymentStatuses { get; private set; } = new List<LookupItem>();
        public List<LookupItem> WorkforceSegments { get; private set; } = new List<LookupItem>();
        public List<LookupItem> LegalEntities { get; private set; } = new List<LookupItem>();
        public List<LookupItem> BusinessUnits { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Divisions { get; private set; } = new List<LookupItem>();
        public List<LookupItem> SalesTeams { get; private set; } = new List<LookupItem>();
        public List<LookupItem> CostCenters { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Regions { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Locations { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Jobs { get; private set; } = new List<LookupItem>();
        public List<LookupItem> WorkerLocations { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Cities { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Provinces { get; private set; } = new List<LookupItem>();
        public List<LookupItem> SalesGroups { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Grades { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Extensions { get; private set; } = new List<LookupItem>();
        public List<LookupItem> BloodGroups { get; private set; } = new List<LookupItem>();
        public List<LookupItem> BenefitEntitlements { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Users { get; private set; } = new List<LookupItem>();
        public List<LookupItem> EmployeeLookups { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Banks { get; private set; } = new List<LookupItem>();
        public List<CurrencyLookupItem> Currencies { get; private set; } = new List<CurrencyLookupItem>();
        public List<LookupItem> BankGroups { get; private set; } = new List<LookupItem>();
        public List<LookupItem> DocumentTypes { get; private set; } = new List<LookupItem>();
        public EmployeeInput Input { get; set; } = new EmployeeInput();
        public List<EmployeeContactInput> ContactRecords { get; private set; } = new List<EmployeeContactInput>();
        public List<EmployeeAddressInput> AddressRecords { get; private set; } = new List<EmployeeAddressInput>();
        public List<EmployeeFamilyMemberInput> FamilyRecords { get; private set; } = new List<EmployeeFamilyMemberInput>();
        public List<EmployeeBankInput> BankRecords { get; private set; } = new List<EmployeeBankInput>();
        public List<EmployeeEducationInput> EducationRecords { get; private set; } = new List<EmployeeEducationInput>();
        public List<EmployeeCertificateInput> CertificateRecords { get; private set; } = new List<EmployeeCertificateInput>();
        public List<EmployeeDocumentInput> DocumentRecords { get; private set; } = new List<EmployeeDocumentInput>();
        public bool EditMode { get; private set; }
        public bool ShowForm { get; private set; }
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";
        public bool HasFullEmployeeAccess { get; private set; }
        public bool IsProfileOnlyMode { get; private set; }
        public bool CanEditCurrentRecord { get; private set; }
        public string PhotoPath => Input != null ? (Input.PhotoPath ?? "") : "";
        public int ActiveEmployeeCount => Employees.Count(e => e.Status == "Active");
        public int InactiveEmployeeCount => Employees.Count - ActiveEmployeeCount;

        public string ContactsJsonInit => WebFormsJson.Serialize(ContactRecords);
        public string AddressesJsonInit => WebFormsJson.Serialize(AddressRecords);
        public string FamilyJsonInit => WebFormsJson.Serialize(FamilyRecords);
        public string EducationJsonInit => WebFormsJson.Serialize(EducationRecords);
        public string CertificatesJsonInit => WebFormsJson.Serialize(CertificateRecords);
        public string DocumentsJsonInit => WebFormsJson.Serialize(DocumentRecords);
        public string BanksJsonInit => WebFormsJson.Serialize(BankRecords);
        public string BanksLookupJson => WebFormsJson.Serialize(Banks);
        public string CurrenciesLookupJson => WebFormsJson.Serialize(Currencies);
        public string BankGroupsLookupJson => WebFormsJson.Serialize(BankGroups);
        public string DocumentTypesLookupJson => WebFormsJson.Serialize(DocumentTypes);

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (Form != null)
                Form.Enctype = "multipart/form-data";

            if (!_profileAccess.CanAccessEmployeeMasterPage())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                switch ((handler ?? "").Trim())
                {
                    case "Delete":
                        OnPostDelete(FormInt("deleteId"));
                        return;
                    case "SaveContacts":
                        OnPostSaveContacts();
                        return;
                    case "SaveAddresses":
                        OnPostSaveAddresses();
                        return;
                    case "SaveFamilyMembers":
                        OnPostSaveFamilyMembers();
                        return;
                    case "SaveBanks":
                        OnPostSaveBanks();
                        return;
                    case "SaveEducation":
                        OnPostSaveEducation();
                        return;
                    case "SaveCertificates":
                        OnPostSaveCertificates();
                        return;
                    case "SaveDocuments":
                        OnPostSaveDocuments();
                        return;
                    case "ExportExcel":
                        OnPostExportExcel();
                        return;
                    case "ImportExcel":
                        OnPostImportExcel();
                        return;
                    default:
                        OnPostSave();
                        return;
                }
            }

            var newEmp = string.Equals(Request.QueryString["newEmployee"], "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Request.QueryString["newEmployee"], "true", StringComparison.OrdinalIgnoreCase);
            OnGet(QueryInt("editId"), newEmp);
        }

        private void OnGet(int? editId, bool newEmployee)
        {
            string msg, typ;
            LoadAlert(out msg, out typ);
            AlertMessage = msg;
            AlertType = typ;

            InitializeAccessFlags(editId);

            if (newEmployee && !_profileAccess.CanCreateEmployee())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            if (editId.HasValue && editId.Value > 0 && !_profileAccess.CanViewEmployee(editId.Value))
            {
                SetAlert(_profileAccess.IsEmployeeProfileSynchronized()
                    ? "You can only access your own employee profile."
                    : EmployeeProfileAccessService.NotSynchronizedMessage, "warning");
                Response.Redirect("~/UserProfile.aspx");
                return;
            }

            ShowForm = (editId.HasValue && editId > 0) || newEmployee;

            if (!ShowForm && !_profileAccess.CanViewEmployeeList())
            {
                var ownId = _profileAccess.GetLinkedEmployeeId();
                if (ownId.HasValue && ownId.Value > 0)
                    Response.Redirect("~/UserProfile.aspx");
                else
                {
                    SetAlert(EmployeeProfileAccessService.NotSynchronizedMessage, "warning");
                    Response.Redirect("~/UserProfile.aspx");
                }
                return;
            }

            if (ShowForm)
            {
                LoadDepartments();
                LoadLookupLists();
                EnsureDefaultRows();

                if (editId.HasValue && editId > 0)
                {
                    LoadForEdit(editId.Value);
                    EditMode = true;
                }

                EmployeeLookups = LoadEmployeeLookups(Input.EmployeeID > 0 ? (int?)Input.EmployeeID : null);
                InitializeAccessFlags(Input.EmployeeID > 0 ? (int?)Input.EmployeeID : null);
            }
            else
            {
                LoadEmployees();
            }
        }

        private void OnPostSave()
        {
            var employeeId = FormInt("EmployeeID");
            InitializeAccessFlags(employeeId > 0 ? (int?)employeeId : null);

            if (employeeId > 0 && !_profileAccess.CanEditEmployee(employeeId))
            {
                SetAlert("You can only update your own employee profile.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }
            if (employeeId <= 0 && !_profileAccess.CanCreateEmployee())
            {
                SetAlert("You do not have permission to create employees.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }

            EditMode = FormBool("EditMode") || employeeId > 0;
            Input = BindInputFromForm(employeeId);
            ApplyTenureCalculations(Input);

            if (IsProfileOnlyMode && employeeId > 0)
                PreserveHrManagedFields(Input);

            ContactRecords = WebFormsJson.DeserializeList<EmployeeContactInput>(FormString("ContactsJson"));
            AddressRecords = WebFormsJson.DeserializeList<EmployeeAddressInput>(FormString("AddressesJson"));
            FamilyRecords = WebFormsJson.DeserializeList<EmployeeFamilyMemberInput>(FormString("FamilyMembersJson"));

            var existingByCode = GetEmployeeIdByCode(Input.EmployeeCode);
            if (employeeId <= 0 && existingByCode > 0)
            {
                employeeId = existingByCode;
                Input.EmployeeID = existingByCode;
            }

            EditMode = employeeId > 0;

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var removePhoto = FormBool("RemovePhoto");
                        var savedId = SaveEmployeeCore(conn, tx, Input, removePhoto);
                        tx.Commit();
                        SetAlert(EditMode ? "Employee updated successfully." : "Employee added successfully.");
                        Response.Redirect("~/EmployeeMaster.aspx?editId=" + savedId);
                        return;
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    AlertMessage = "Duplicate Employee ID. Please use a unique employee ID.";
                    AlertType = "error";
                }
                else
                {
                    AlertMessage = "Error: " + ex.Message;
                    AlertType = "error";
                }
            }
            catch (Exception ex)
            {
                AlertMessage = "Error: " + ex.Message;
                AlertType = "error";
            }

            ShowForm = true;
            LoadDepartments();
            LoadLookupLists();
            EmployeeLookups = LoadEmployeeLookups(Input.EmployeeID > 0 ? (int?)Input.EmployeeID : null);
            EnsureDefaultRows();
            InitializeAccessFlags(Input.EmployeeID > 0 ? (int?)Input.EmployeeID : null);
        }

        private EmployeeInput BindInputFromForm(int employeeId)
        {
            var genderId = FormInt("GenderID");
            return new EmployeeInput
            {
                EmployeeID = employeeId,
                EmployeeCode = FormString("EmployeeCode"),
                FirstName = FormString("FirstName"),
                LastName = FormString("LastName"),
                FathersHusbandsName = FormString("FathersHusbandsName"),
                DisplayName = FormString("DisplayName"),
                NationalIDNumber = FormString("NationalIDNumber"),
                GenderID = genderId,
                Gender = GetLookupName("tblGender", "GenderID", "GenderName", genderId),
                DateOfBirth = FormString("DateOfBirth"),
                MaritalStatus = FormString("MaritalStatus"),
                DepartmentID = FormInt("DepartmentID"),
                DivisionID = FormInt("DivisionID"),
                NationalityID = FormInt("NationalityID"),
                ReligionID = FormInt("ReligionID"),
                LanguageID = FormInt("LanguageID"),
                WorkerCategoryID = FormInt("WorkerCategoryID"),
                EmploymentTypeID = FormInt("EmploymentTypeID"),
                EmploymentStatusID = FormInt("EmploymentStatusID"),
                WorkforceSegmentID = FormInt("WorkforceSegmentID"),
                LegalEntityID = FormInt("LegalEntityID"),
                BusinessUnitID = FormInt("BusinessUnitID"),
                SalesTeamID = FormInt("SalesTeamID"),
                CostCenterID = FormInt("CostCenterID"),
                RegionID = FormInt("RegionID"),
                LocationID = FormInt("LocationID"),
                JobID = FormInt("JobID"),
                WorkerLocationID = FormInt("WorkerLocationID"),
                CityID = FormInt("CityID"),
                ProvinceID = FormInt("ProvinceID"),
                SalesGroupID = FormInt("SalesGroupID"),
                GradeID = FormInt("GradeID"),
                ExtensionID = FormInt("ExtensionID"),
                Domicile = FormString("Domicile"),
                BloodGroupID = FormInt("BloodGroupID"),
                BenefitEntitlementID = FormInt("BenefitEntitlementID"),
                UserID = FormInt("UserID"),
                TemporaryResponsibleEmployeeID = FormInt("TemporaryResponsibleEmployeeID"),
                PermanentResponsibleEmployeeID = FormInt("PermanentResponsibleEmployeeID"),
                Designation = FormString("Designation"),
                DateOfJoining = FormString("DateOfJoining"),
                EmploymentStartDate = FormString("EmploymentStartDate"),
                ProbationPeriodDays = FormString("ProbationPeriodDays"),
                ProbationEndDate = FormString("ProbationEndDate"),
                ConfirmationDate = FormString("ConfirmationDate"),
                BasicSalary = FormString("BasicSalary"),
                Status = string.IsNullOrEmpty(FormString("Status")) ? "Active" : FormString("Status"),
                PhotoPath = FormString("PhotoPath")
            };
        }

        private void OnPostDelete(int deleteId)
        {
            if (!_profileAccess.CanDeleteEmployee())
            {
                SetAlert("You do not have permission to delete employees.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }
            if (!_profileAccess.CanViewEmployee(deleteId))
            {
                SetAlert("You can only access your own employee profile.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
DELETE FROM tblEmployeeContact WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeAddress WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeFamilyMember WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeBank WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeEducation WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeCertificate WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployeeDocument WHERE EmployeeID = @EmployeeID;
DELETE FROM tblEmployee WHERE EmployeeID = @EmployeeID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Employee deleted successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error deleting record: " + ex.Message, "error");
            }
            Response.Redirect("~/EmployeeMaster.aspx");
        }

        private void OnPostSaveContacts()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            List<EmployeeContactInput> contacts;
            if (!WebFormsJson.TryDeserializeList(FormString("ContactsJson"), out contacts))
            {
                SetAlert("Could not read contact data from the form. Please refresh the page and try again.", "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            string contactError;
            if (!TryValidateContacts(contacts, out contactError))
            {
                SetAlert(contactError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }
            if (!TryValidateContactDuplicates(contacts, out contactError))
            {
                SetAlert(contactError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeContacts(conn, tx, employeeId, contacts);
                        tx.Commit();
                    }
                }
                SetAlert("Employee contact details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("contact", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving contact details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private void RedirectToEmployeeEdit(int employeeId)
        {
            Response.Redirect("~/EmployeeMaster.aspx?editId=" + employeeId, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static string FormatChildSaveSqlError(string section, SqlException ex)
        {
            if (ex == null) return "Error saving " + section + " details.";
            // 229/230 = permission denied; 207 = invalid column
            if (ex.Number == 229 || ex.Number == 230)
                return "Database permission denied while saving " + section
                    + " details. Grant INSERT/UPDATE/DELETE on the related table to the application login.";
            if (ex.Number == 207)
                return "Database schema mismatch while saving " + section
                    + " details: " + ex.Message
                    + " Run the production tblEmployeeContact patch script, then recycle the app pool.";
            return "Error saving " + section + " details: " + ex.Message;
        }

        private static bool TryValidateContacts(List<EmployeeContactInput> contacts, out string error)
        {
            error = null;
            foreach (var c in contacts)
            {
                if (!InputValidators.TryValidateEmployeeContact(c.ContactType, c.ContactValue, out error))
                    return false;
            }
            return true;
        }

        private static bool TryValidateContactDuplicates(List<EmployeeContactInput> contacts, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in contacts.Where(x =>
                !string.IsNullOrWhiteSpace(x.ContactType) &&
                (!string.IsNullOrWhiteSpace(x.ContactValue) || !string.IsNullOrWhiteSpace(x.ContactName))))
            {
                var key = (c.ContactType ?? "").Trim() + "|" + (c.ContactValue ?? "").Trim() + "|" + (c.ContactName ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate contact entry detected. Each contact type/value combination must be unique.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveAddresses()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            List<EmployeeAddressInput> addresses;
            if (!WebFormsJson.TryDeserializeList(FormString("AddressesJson"), out addresses))
            {
                SetAlert("Could not read address data from the form. Please refresh the page and try again.", "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }
            string dupError;
            if (!TryValidateAddressDuplicates(addresses, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeAddresses(conn, tx, employeeId, addresses);
                        tx.Commit();
                    }
                }
                SetAlert("Employee address details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("address", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving address details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateAddressDuplicates(List<EmployeeAddressInput> addresses, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in addresses.Where(x => !string.IsNullOrWhiteSpace(x.AddressLine)))
            {
                var key = (a.AddressType ?? "").Trim() + "|" + (a.AddressLine ?? "").Trim() + "|" + (a.City ?? "").Trim() + "|" + (a.PostalCode ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate address entry detected. Remove the duplicate before saving.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveFamilyMembers()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            List<EmployeeFamilyMemberInput> members;
            if (!WebFormsJson.TryDeserializeList(FormString("FamilyMembersJson"), out members))
            {
                SetAlert("Could not read family member data from the form. Please refresh the page and try again.", "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }
            foreach (var member in members.Where(m => !string.IsNullOrWhiteSpace(m.ContactNumber)))
            {
                string phoneError;
                if (!InputValidators.TryValidatePhone(member.ContactNumber, out phoneError, false, "Family contact number"))
                {
                    SetAlert(phoneError, "error");
                    RedirectToEmployeeEdit(employeeId);
                    return;
                }
            }
            string dupError;
            if (!TryValidateFamilyDuplicates(members, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeFamilyMembers(conn, tx, employeeId, members);
                        tx.Commit();
                    }
                }
                SetAlert("Employee family member details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("family member", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving family member details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateFamilyDuplicates(List<EmployeeFamilyMemberInput> members, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in members.Where(x => !string.IsNullOrWhiteSpace(x.MemberName)))
            {
                var key = (m.MemberName ?? "").Trim() + "|" + (m.Relationship ?? "").Trim() + "|" + (m.DateOfBirth ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate family member entry detected. Remove the duplicate before saving.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveBanks()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            var banks = WebFormsJson.DeserializeList<EmployeeBankInput>(FormString("BanksJson"));
            string dupError;
            if (!TryValidateBankDuplicates(banks, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeBanks(conn, tx, employeeId, banks);
                        tx.Commit();
                    }
                }
                SetAlert("Employee bank details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("bank", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving bank details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateBankDuplicates(List<EmployeeBankInput> banks, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in banks.Where(x => x.BankID > 0))
            {
                var key = b.BankID + "|" + (b.IBAN ?? "").Trim() + "|" + (b.BankCode ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate bank account entry detected. Each bank/IBAN combination must be unique.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveEducation()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            var records = WebFormsJson.DeserializeList<EmployeeEducationInput>(FormString("EducationJson"));
            string dupError;
            if (!TryValidateEducationDuplicates(records, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeEducation(conn, tx, employeeId, records);
                        tx.Commit();
                    }
                }
                SetAlert("Employee education details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("education", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving education details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateEducationDuplicates(List<EmployeeEducationInput> records, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in records.Where(x =>
                !string.IsNullOrWhiteSpace(x.HighestQualification)
                || !string.IsNullOrWhiteSpace(x.DegreeCertificate)
                || !string.IsNullOrWhiteSpace(x.Institution)))
            {
                var key = (e.HighestQualification ?? "").Trim() + "|" + (e.DegreeCertificate ?? "").Trim()
                    + "|" + (e.Institution ?? "").Trim() + "|" + (e.YearOfPassing ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate education entry detected. Remove the duplicate before saving.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveCertificates()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            var records = WebFormsJson.DeserializeList<EmployeeCertificateInput>(FormString("CertificatesJson"));
            string dupError;
            if (!TryValidateCertificateDuplicates(records, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeCertificates(conn, tx, employeeId, records);
                        tx.Commit();
                    }
                }
                SetAlert("Employee certificate details saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("certificate", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving certificate details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateCertificateDuplicates(List<EmployeeCertificateInput> records, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in records.Where(x =>
                !string.IsNullOrWhiteSpace(x.CertificationName)
                || !string.IsNullOrWhiteSpace(x.CertificateNumber)
                || !string.IsNullOrWhiteSpace(x.CertificationBody)))
            {
                var key = (c.CertificationName ?? "").Trim() + "|" + (c.CertificateNumber ?? "").Trim()
                    + "|" + (c.CertificationBody ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate certificate entry detected. Remove the duplicate before saving.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostSaveDocuments()
        {
            var employeeId = ResolveEmployeeId(FormInt("EmployeeID"), FormString("EmployeeCode"));
            if (!EnsureCanEditEmployee(employeeId)) return;

            var records = WebFormsJson.DeserializeList<EmployeeDocumentInput>(FormString("DocumentsJson"));
            string dupError;
            if (!TryValidateDocumentDuplicates(records, out dupError))
            {
                SetAlert(dupError, "error");
                RedirectToEmployeeEdit(employeeId);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        SyncEmployeeDocuments(conn, tx, employeeId, records);
                        tx.Commit();
                    }
                }
                SetAlert("Employee documents saved successfully.");
            }
            catch (SqlException ex)
            {
                SetAlert(FormatChildSaveSqlError("document", ex), "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving document details: " + ex.Message, "error");
            }
            RedirectToEmployeeEdit(employeeId);
        }

        private static bool TryValidateDocumentDuplicates(List<EmployeeDocumentInput> records, out string error)
        {
            error = null;
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in records.Where(x =>
                x.DocumentTypeID > 0
                || !string.IsNullOrWhiteSpace(x.DocumentNumber)
                || !string.IsNullOrWhiteSpace(x.DocumentPath)
                || !string.IsNullOrWhiteSpace(x.Remarks)))
            {
                var key = d.DocumentTypeID + "|" + (d.DocumentNumber ?? "").Trim();
                if (!keys.Add(key))
                {
                    error = "Duplicate document entry detected. Each document type/number must be unique.";
                    return false;
                }
            }
            return true;
        }

        private void OnPostExportExcel()
        {
            if (!_profileAccess.CanViewEmployeeList())
            {
                SetAlert("Not allowed.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }
            try
            {
                var file = _excel.ExportEmployees();
                Response.Clear();
                Response.ContentType = file.ContentType;
                Response.AddHeader("Content-Disposition", "attachment; filename=\"" + file.FileName + "\"");
                Response.BinaryWrite(file.Content);
                Response.End();
            }
            catch (Exception ex)
            {
                SetAlert("Excel export failed: " + ex.Message, "error");
                Response.Redirect("~/EmployeeMaster.aspx");
            }
        }

        private void OnPostImportExcel()
        {
            if (!_profileAccess.CanCreateEmployee())
            {
                SetAlert("You do not have permission to import employees.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }

            var file = Request.Files["excelFile"];
            if (file == null || file.ContentLength == 0)
            {
                SetAlert("Please select an Excel file to upload.", "error");
                Response.Redirect("~/EmployeeMaster.aspx");
                return;
            }

            try
            {
                var result = _excel.ImportEmployees(file, Auth.CurrentUserId);
                SetAlert(result.Message, result.Success ? "success" : "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error importing Excel: " + ex.Message, "error");
            }
            Response.Redirect("~/EmployeeMaster.aspx");
        }

        private void InitializeAccessFlags(int? employeeId)
        {
            HasFullEmployeeAccess = _profileAccess.HasFullEmployeeMasterAccess();
            IsProfileOnlyMode = _profileAccess.IsProfileOnlyUser()
                && employeeId.HasValue
                && employeeId.Value > 0
                && _profileAccess.OwnsEmployee(employeeId.Value);
            CanEditCurrentRecord = !employeeId.HasValue || employeeId.Value <= 0
                ? _profileAccess.CanCreateEmployee()
                : _profileAccess.CanEditEmployee(employeeId.Value);
        }

        private bool EnsureCanEditEmployee(int employeeId)
        {
            if (employeeId <= 0)
            {
                SetAlert("Please save or select an employee before saving details.", "error");
                Response.Redirect("~/EmployeeMaster.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }
            if (!_profileAccess.CanEditEmployee(employeeId))
            {
                SetAlert("You can only update your own employee profile.", "error");
                Response.Redirect("~/Home.aspx?accessDenied=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }
            return true;
        }

        private int ResolveEmployeeId(int employeeID, string employeeCode)
        {
            if (employeeID > 0) return employeeID;
            return GetEmployeeIdByCode(employeeCode);
        }

        private int GetEmployeeIdByCode(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return 0;
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 EmployeeID FROM tblEmployee WHERE EmployeeCode = @EmployeeCode;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
            catch { return 0; }
        }

        private void PreserveHrManagedFields(EmployeeInput input)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT EmployeeCode, DepartmentID, DivisionID,
       WorkerCategoryID, EmploymentTypeID, EmploymentStatusID, WorkforceSegmentID,
       LegalEntityID, BusinessUnitID, SalesTeamID, CostCenterID,
       RegionID, LocationID, JobID, WorkerLocationID,
       CityID, ProvinceID, SalesGroupID, GradeID, ExtensionID,
       BenefitEntitlementID, UserID,
       TemporaryResponsibleEmployeeID, PermanentResponsibleEmployeeID,
       Designation, DateOfJoining, EmploymentStartDate,
       ProbationPeriodDays, ProbationEndDate, ConfirmationDate,
       BasicSalary, Status
FROM tblEmployee WHERE EmployeeID = @EmployeeID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return;
                        input.EmployeeCode = dr["EmployeeCode"].ToString() ?? "";
                        input.DepartmentID = IntOrZero(dr["DepartmentID"]);
                        input.DivisionID = IntOrZero(dr["DivisionID"]);
                        input.WorkerCategoryID = IntOrZero(dr["WorkerCategoryID"]);
                        input.EmploymentTypeID = IntOrZero(dr["EmploymentTypeID"]);
                        input.EmploymentStatusID = IntOrZero(dr["EmploymentStatusID"]);
                        input.WorkforceSegmentID = IntOrZero(dr["WorkforceSegmentID"]);
                        input.LegalEntityID = IntOrZero(dr["LegalEntityID"]);
                        input.BusinessUnitID = IntOrZero(dr["BusinessUnitID"]);
                        input.SalesTeamID = IntOrZero(dr["SalesTeamID"]);
                        input.CostCenterID = IntOrZero(dr["CostCenterID"]);
                        input.RegionID = IntOrZero(dr["RegionID"]);
                        input.LocationID = IntOrZero(dr["LocationID"]);
                        input.JobID = IntOrZero(dr["JobID"]);
                        input.WorkerLocationID = IntOrZero(dr["WorkerLocationID"]);
                        input.CityID = IntOrZero(dr["CityID"]);
                        input.ProvinceID = IntOrZero(dr["ProvinceID"]);
                        input.SalesGroupID = IntOrZero(dr["SalesGroupID"]);
                        input.GradeID = IntOrZero(dr["GradeID"]);
                        input.ExtensionID = IntOrZero(dr["ExtensionID"]);
                        input.BenefitEntitlementID = IntOrZero(dr["BenefitEntitlementID"]);
                        input.UserID = IntOrZero(dr["UserID"]);
                        input.TemporaryResponsibleEmployeeID = IntOrZero(dr["TemporaryResponsibleEmployeeID"]);
                        input.PermanentResponsibleEmployeeID = IntOrZero(dr["PermanentResponsibleEmployeeID"]);
                        input.Designation = dr["Designation"] == DBNull.Value ? "" : dr["Designation"].ToString() ?? "";
                        input.DateOfJoining = DateOrEmpty(dr["DateOfJoining"]);
                        input.EmploymentStartDate = DateOrEmpty(dr["EmploymentStartDate"]);
                        input.ProbationPeriodDays = dr["ProbationPeriodDays"] == DBNull.Value ? "" : IntOrZero(dr["ProbationPeriodDays"]).ToString();
                        input.ProbationEndDate = DateOrEmpty(dr["ProbationEndDate"]);
                        input.ConfirmationDate = DateOrEmpty(dr["ConfirmationDate"]);
                        input.BasicSalary = StrOrEmpty(dr["BasicSalary"]);
                        input.Status = dr["Status"].ToString() ?? "Active";
                    }
                }
            }
            catch { /* optional columns */ }
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            if (!_profileAccess.CanViewEmployeeList() && !_dataScope.BypassesDataScope())
            {
                var ownId = _profileAccess.GetLinkedEmployeeId();
                if (!ownId.HasValue || ownId.Value <= 0) return;
            }

            try
            {
                var scope = _dataScope.GetEmployeeFilter("e");
                var filterSql = scope.IsUnrestricted ? "" : scope.Sql;
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT
    e.EmployeeID, e.EmployeeCode,
    e.FirstName + ' ' + e.LastName AS FullName,
    d.DepartmentName, e.Designation,
    ISNULL(et.EmploymentTypeName, '') AS EmploymentType,
    ISNULL(es.EmploymentStatusName, '') AS EmploymentStatus,
    ISNULL(le.LegalEntityName, '') AS LegalEntityName,
    ISNULL(cPhone.ContactValue, '') AS Phone,
    ISNULL(cEmail.ContactValue, '') AS Email,
    e.DateOfJoining, e.BasicSalary, e.Status
FROM tblEmployee e
INNER JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = e.EmploymentTypeID
LEFT JOIN tblEmploymentStatus es ON es.EmploymentStatusID = e.EmploymentStatusID
LEFT JOIN tblLegalEntity le ON le.LegalEntityID = e.LegalEntityID
OUTER APPLY (
    SELECT TOP 1 ContactValue FROM tblEmployeeContact
    WHERE EmployeeID = e.EmployeeID AND ContactType = 'PersonalMobile'
    ORDER BY IsPrimary DESC, ContactID DESC
) cPhone
OUTER APPLY (
    SELECT TOP 1 ContactValue FROM tblEmployeeContact
    WHERE EmployeeID = e.EmployeeID AND ContactType = 'OfficialEmail'
    ORDER BY IsPrimary DESC, ContactID DESC
) cEmail
WHERE 1=1 " + filterSql + @"
ORDER BY e.EmployeeID DESC;", conn))
                {
                    scope.ApplyTo(cmd);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Employees.Add(new EmployeeViewModel
                            {
                                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                                EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                                FullName = dr["FullName"].ToString() ?? "",
                                DepartmentName = dr["DepartmentName"].ToString() ?? "",
                                LegalEntityName = dr["LegalEntityName"].ToString() ?? "",
                                EmploymentType = dr["EmploymentType"].ToString() ?? "",
                                EmploymentStatus = dr["EmploymentStatus"].ToString() ?? "",
                                Designation = dr["Designation"] == DBNull.Value ? "" : dr["Designation"].ToString() ?? "",
                                Phone = dr["Phone"].ToString() ?? "",
                                Email = dr["Email"].ToString() ?? "",
                                DateOfJoining = dr["DateOfJoining"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["DateOfJoining"]),
                                BasicSalary = dr["BasicSalary"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["BasicSalary"]),
                                Status = dr["Status"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch
            {
                // Fallback without optional joins
                try
                {
                    var scope = _dataScope.GetEmployeeFilter("e");
                    var filterSql = scope.IsUnrestricted ? "" : scope.Sql;
                    using (var conn = new SqlConnection(Conn))
                    using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName + ' ' + e.LastName AS FullName,
       d.DepartmentName, e.Designation, e.DateOfJoining, e.BasicSalary, e.Status
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE 1=1 " + filterSql + @"
ORDER BY e.EmployeeID DESC;", conn))
                    {
                        scope.ApplyTo(cmd);
                        conn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                Employees.Add(new EmployeeViewModel
                                {
                                    EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                                    EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                                    FullName = dr["FullName"].ToString() ?? "",
                                    DepartmentName = dr["DepartmentName"] == DBNull.Value ? "" : dr["DepartmentName"].ToString() ?? "",
                                    Designation = dr["Designation"] == DBNull.Value ? "" : dr["Designation"].ToString() ?? "",
                                    DateOfJoining = dr["DateOfJoining"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["DateOfJoining"]),
                                    BasicSalary = dr["BasicSalary"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["BasicSalary"]),
                                    Status = dr["Status"].ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch { /* leave empty */ }
            }
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT DepartmentID, DepartmentName FROM tblDepartment
WHERE ISNULL(IsActive, 1) = 1 ORDER BY DepartmentName;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Departments.Add(new DepartmentItem
                            {
                                DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                                DepartmentName = dr["DepartmentName"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadLookupLists()
        {
            Genders = LoadLookup("tblGender", "GenderID", "GenderName");
            Nationalities = LoadLookup("tblNationality", "NationalityID", "NationalityName");
            Religions = LoadLookup("tblReligion", "ReligionID", "ReligionName");
            Languages = LoadLookup("tblLanguage", "LanguageID", "LanguageName");
            WorkerCategories = LoadLookup("tblWorkerCategory", "WorkerCategoryID", "WorkerCategoryName");
            EmploymentTypes = LoadLookup("tblEmploymentType", "EmploymentTypeID", "EmploymentTypeName");
            EmploymentStatuses = LoadLookup("tblEmploymentStatus", "EmploymentStatusID", "EmploymentStatusName");
            WorkforceSegments = LoadLookup("tblWorkforceSegment", "WorkforceSegmentID", "WorkforceSegmentName");
            LegalEntities = LoadLookup("tblLegalEntity", "LegalEntityID", "LegalEntityName");
            BusinessUnits = LoadLookup("tblBusinessUnit", "BusinessUnitID", "BusinessUnitName");
            Divisions = LoadLookup("tblDivision", "DivisionID", "DivisionName");
            SalesTeams = LoadLookup("tblSalesTeam", "SalesTeamID", "SalesTeamName");
            CostCenters = LoadLookup("tblCostCenter", "CostCenterID", "CostCenterName");
            Regions = LoadLookup("tblRegion", "RegionID", "RegionName");
            Locations = LoadLookup("tblLocation", "LocationID", "LocationName");
            Jobs = LoadJobs();
            WorkerLocations = LoadWorkerLocations();
            Cities = LoadLookup("tblCity", "CityID", "CityName");
            Provinces = LoadLookup("tblProvince", "ProvinceID", "ProvinceName");
            SalesGroups = LoadLookup("tblSalesGroup", "SalesGroupID", "SalesGroupName");
            Grades = LoadLookup("tblGrade", "GradeID", "GradeName");
            Extensions = LoadExtensions();
            BloodGroups = LoadLookup("tblBloodGroup", "BloodGroupID", "BloodGroupName");
            BenefitEntitlements = LoadLookup("tblBenefitEntitlement", "BenefitEntitlementID", "BenefitEntitlementName");
            Users = LoadUsers();
            Banks = LoadBanks();
            Currencies = LoadCurrencies();
            BankGroups = LoadLookup("tblBankGroup", "BankGroupID", "BankGroupName");
            DocumentTypes = LoadDocumentTypes();
        }

        private List<LookupItem> LoadLookup(string tableName, string idColumn, string nameColumn)
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT " + idColumn + ", " + nameColumn + " FROM " + tableName +
                    " WHERE IsActive = 1 ORDER BY " + nameColumn + ";", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr[idColumn]),
                                Name = dr[nameColumn].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadUsers()
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT UserID, UserCode, Username, FullName FROM tblUser
WHERE IsActive = 1 ORDER BY FullName, Username;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString();
                            var username = dr["Username"].ToString() ?? "";
                            var fullName = dr["FullName"].ToString() ?? "";
                            var display = string.IsNullOrWhiteSpace(code)
                                ? username + " – " + fullName
                                : code + " – " + fullName + " (" + username + ")";
                            items.Add(new LookupItem { Id = Convert.ToInt32(dr["UserID"]), Name = display });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadEmployeeLookups(int? excludeEmployeeId = null)
        {
            var items = new List<LookupItem>();
            try
            {
                var scope = _dataScope.GetEmployeeFilter("e");
                var sql = @"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName + ' ' + e.LastName AS FullName
FROM tblEmployee e WHERE e.Status = 'Active'";
                if (!scope.IsUnrestricted) sql += scope.Sql;
                if (excludeEmployeeId.HasValue && excludeEmployeeId > 0)
                    sql += " AND e.EmployeeID <> @ExcludeId";
                sql += " ORDER BY e.FirstName, e.LastName;";

                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    scope.ApplyTo(cmd);
                    if (excludeEmployeeId.HasValue && excludeEmployeeId > 0)
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeEmployeeId.Value);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr["EmployeeCode"].ToString() ?? "";
                            var name = dr["FullName"].ToString() ?? "";
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr["EmployeeID"]),
                                Name = code + " – " + name
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadJobs()
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT JobID, JobCode, JobTitle FROM tblJob WHERE IsActive = 1 ORDER BY JobCode;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr["JobID"]),
                                Name = (dr["JobCode"].ToString() ?? "") + " – " + (dr["JobTitle"].ToString() ?? "")
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadExtensions()
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT ExtensionID, ExtensionCode, ExtensionName FROM tblExtension WHERE IsActive = 1 ORDER BY ExtensionCode;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr["ExtensionID"]),
                                Name = (dr["ExtensionCode"].ToString() ?? "") + " – " + (dr["ExtensionName"].ToString() ?? "")
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadWorkerLocations()
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT wl.WorkerLocationID, e.EmployeeCode, pl.LocationName AS PrimaryLocationName
FROM tblWorkerLocation wl
INNER JOIN tblEmployee e ON e.EmployeeID = wl.EmployeeID
LEFT JOIN tblLocation pl ON pl.LocationID = wl.PrimaryLocationID
WHERE wl.IsActive = 1 ORDER BY e.EmployeeCode;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr["EmployeeCode"].ToString() ?? "";
                            var loc = dr["PrimaryLocationName"] == DBNull.Value ? "" : dr["PrimaryLocationName"].ToString();
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr["WorkerLocationID"]),
                                Name = string.IsNullOrEmpty(loc) ? code : code + " – " + loc
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadBanks()
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT BankID, BankName, BankCode, LocationName FROM tblBankMaster
WHERE IsActive = 1 ORDER BY BankName, LocationName;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var bankCode = dr["BankCode"] == DBNull.Value ? "" : dr["BankCode"].ToString();
                            var locationName = dr["LocationName"] == DBNull.Value ? "" : dr["LocationName"].ToString();
                            var displayName = dr["BankName"].ToString() ?? "";
                            var parts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(bankCode)) parts.Add(bankCode);
                            if (!string.IsNullOrWhiteSpace(locationName)) parts.Add(locationName);
                            if (parts.Count > 0) displayName = displayName + " (" + string.Join(" - ", parts) + ")";
                            items.Add(new LookupItem { Id = Convert.ToInt32(dr["BankID"]), Name = displayName });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<CurrencyLookupItem> LoadCurrencies()
        {
            var items = new List<CurrencyLookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT CurrencyCode, CurrencyName FROM tblCurrency WHERE IsActive = 1 ORDER BY CurrencyCode;", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            items.Add(new CurrencyLookupItem
                            {
                                Code = dr["CurrencyCode"].ToString() ?? "",
                                Name = dr["CurrencyName"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private List<LookupItem> LoadDocumentTypes()
        {
            return LoadLookup("tblDocumentType", "DocumentTypeID", "DocumentTypeName");
        }

        private string GetLookupName(string tableName, string idColumn, string nameColumn, int id)
        {
            if (id <= 0) return "";
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 " + nameColumn + " FROM " + tableName + " WHERE " + idColumn + " = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    return cmd.ExecuteScalar()?.ToString() ?? "";
                }
            }
            catch { return ""; }
        }

        private int GetGenderIdByName(string genderName)
        {
            if (string.IsNullOrWhiteSpace(genderName)) return 0;
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 GenderID FROM tblGender WHERE GenderName = @GenderName;", conn))
                {
                    cmd.Parameters.AddWithValue("@GenderName", genderName.Trim());
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
            catch { return 0; }
        }

        private void LoadForEdit(int employeeID)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
SELECT EmployeeID, EmployeeCode, FirstName, LastName,
       FathersHusbandsName, DisplayName, NationalIDNumber,
       Gender, DateOfBirth, GenderID, MaritalStatus, DepartmentID, DivisionID,
       NationalityID, ReligionID, LanguageID,
       WorkerCategoryID, EmploymentTypeID, EmploymentStatusID,
       WorkforceSegmentID, LegalEntityID, BusinessUnitID,
       SalesTeamID, CostCenterID, RegionID, LocationID, JobID, WorkerLocationID,
       CityID, ProvinceID, SalesGroupID, GradeID, ExtensionID, Domicile,
       BloodGroupID, BenefitEntitlementID, UserID,
       TemporaryResponsibleEmployeeID, PermanentResponsibleEmployeeID,
       Designation, DateOfJoining, EmploymentStartDate, ProbationPeriodDays,
       ProbationEndDate, ConfirmationDate, BasicSalary, Status, PhotoPath
FROM tblEmployee WHERE EmployeeID = @EmployeeID;", conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                var genderName = dr["Gender"] == DBNull.Value ? "" : dr["Gender"].ToString() ?? "";
                                var genderId = IntOrZero(dr["GenderID"]);
                                if (genderId == 0) genderId = GetGenderIdByName(genderName);

                                Input = new EmployeeInput
                                {
                                    EmployeeID = employeeID,
                                    EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                                    FirstName = dr["FirstName"].ToString() ?? "",
                                    LastName = dr["LastName"].ToString() ?? "",
                                    FathersHusbandsName = StrOrEmpty(dr["FathersHusbandsName"]),
                                    DisplayName = StrOrEmpty(dr["DisplayName"]),
                                    NationalIDNumber = StrOrEmpty(dr["NationalIDNumber"]),
                                    Gender = genderName,
                                    GenderID = genderId,
                                    DateOfBirth = DateOrEmpty(dr["DateOfBirth"]),
                                    MaritalStatus = StrOrEmpty(dr["MaritalStatus"]),
                                    DepartmentID = IntOrZero(dr["DepartmentID"]),
                                    DivisionID = IntOrZero(dr["DivisionID"]),
                                    NationalityID = IntOrZero(dr["NationalityID"]),
                                    ReligionID = IntOrZero(dr["ReligionID"]),
                                    LanguageID = IntOrZero(dr["LanguageID"]),
                                    WorkerCategoryID = IntOrZero(dr["WorkerCategoryID"]),
                                    EmploymentTypeID = IntOrZero(dr["EmploymentTypeID"]),
                                    EmploymentStatusID = IntOrZero(dr["EmploymentStatusID"]),
                                    WorkforceSegmentID = IntOrZero(dr["WorkforceSegmentID"]),
                                    LegalEntityID = IntOrZero(dr["LegalEntityID"]),
                                    BusinessUnitID = IntOrZero(dr["BusinessUnitID"]),
                                    SalesTeamID = IntOrZero(dr["SalesTeamID"]),
                                    CostCenterID = IntOrZero(dr["CostCenterID"]),
                                    RegionID = IntOrZero(dr["RegionID"]),
                                    LocationID = IntOrZero(dr["LocationID"]),
                                    JobID = IntOrZero(dr["JobID"]),
                                    WorkerLocationID = IntOrZero(dr["WorkerLocationID"]),
                                    CityID = IntOrZero(dr["CityID"]),
                                    ProvinceID = IntOrZero(dr["ProvinceID"]),
                                    SalesGroupID = IntOrZero(dr["SalesGroupID"]),
                                    GradeID = IntOrZero(dr["GradeID"]),
                                    ExtensionID = IntOrZero(dr["ExtensionID"]),
                                    Domicile = StrOrEmpty(dr["Domicile"]),
                                    BloodGroupID = IntOrZero(dr["BloodGroupID"]),
                                    BenefitEntitlementID = IntOrZero(dr["BenefitEntitlementID"]),
                                    UserID = IntOrZero(dr["UserID"]),
                                    TemporaryResponsibleEmployeeID = IntOrZero(dr["TemporaryResponsibleEmployeeID"]),
                                    PermanentResponsibleEmployeeID = IntOrZero(dr["PermanentResponsibleEmployeeID"]),
                                    Designation = StrOrEmpty(dr["Designation"]),
                                    DateOfJoining = DateOrEmpty(dr["DateOfJoining"]),
                                    EmploymentStartDate = DateOrEmpty(dr["EmploymentStartDate"]),
                                    ProbationPeriodDays = dr["ProbationPeriodDays"] == DBNull.Value ? "" : Convert.ToInt32(dr["ProbationPeriodDays"]).ToString(),
                                    ProbationEndDate = DateOrEmpty(dr["ProbationEndDate"]),
                                    ConfirmationDate = DateOrEmpty(dr["ConfirmationDate"]),
                                    BasicSalary = StrOrEmpty(dr["BasicSalary"]),
                                    Status = dr["Status"].ToString() ?? "Active",
                                    PhotoPath = StrOrEmpty(dr["PhotoPath"])
                                };
                            }
                        }
                    }

                    ContactRecords = LoadEmployeeContacts(conn, employeeID);
                    AddressRecords = LoadEmployeeAddresses(conn, employeeID);
                    FamilyRecords = LoadEmployeeFamilyMembers(conn, employeeID);
                    BankRecords = LoadEmployeeBanks(conn, employeeID);
                    EducationRecords = LoadEmployeeEducation(conn, employeeID);
                    CertificateRecords = LoadEmployeeCertificates(conn, employeeID);
                    DocumentRecords = LoadEmployeeDocuments(conn, employeeID);
                    EnsureDefaultRows();
                }
            }
            catch (Exception ex)
            {
                AlertMessage = "Error loading employee: " + ex.Message;
                AlertType = "error";
            }
        }

        private int SaveEmployeeCore(SqlConnection conn, SqlTransaction tx, EmployeeInput e, bool removePhoto)
        {
            var photoFile = Request.Files["ProfilePhoto"];
            if (removePhoto && !string.IsNullOrWhiteSpace(e.PhotoPath))
            {
                DeleteProfilePhotoFile(e.PhotoPath);
                e.PhotoPath = "";
            }

            if (e.EmployeeID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblEmployee SET
    EmployeeCode=@EmployeeCode, FirstName=@FirstName, LastName=@LastName,
    FathersHusbandsName=@FathersHusbandsName, DisplayName=@DisplayName, NationalIDNumber=@NationalIDNumber,
    Gender=@Gender, GenderID=@GenderID, DateOfBirth=@DateOfBirth, MaritalStatus=@MaritalStatus,
    DepartmentID=@DepartmentID, DivisionID=@DivisionID, NationalityID=@NationalityID,
    ReligionID=@ReligionID, LanguageID=@LanguageID, WorkerCategoryID=@WorkerCategoryID,
    EmploymentTypeID=@EmploymentTypeID, EmploymentStatusID=@EmploymentStatusID,
    WorkforceSegmentID=@WorkforceSegmentID, LegalEntityID=@LegalEntityID, BusinessUnitID=@BusinessUnitID,
    SalesTeamID=@SalesTeamID, CostCenterID=@CostCenterID, RegionID=@RegionID, LocationID=@LocationID,
    JobID=@JobID, WorkerLocationID=@WorkerLocationID, CityID=@CityID, ProvinceID=@ProvinceID,
    SalesGroupID=@SalesGroupID, GradeID=@GradeID, ExtensionID=@ExtensionID, Domicile=@Domicile,
    BloodGroupID=@BloodGroupID, BenefitEntitlementID=@BenefitEntitlementID, UserID=@UserID,
    TemporaryResponsibleEmployeeID=@TemporaryResponsibleEmployeeID,
    PermanentResponsibleEmployeeID=@PermanentResponsibleEmployeeID,
    Designation=@Designation, DateOfJoining=@DateOfJoining, EmploymentStartDate=@EmploymentStartDate,
    ProbationPeriodDays=@ProbationPeriodDays, ProbationEndDate=@ProbationEndDate,
    ConfirmationDate=@ConfirmationDate, BasicSalary=@BasicSalary, Status=@Status, PhotoPath=@PhotoPath,
    ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE EmployeeID=@EmployeeID;", conn, tx))
                {
                    ApplyProfilePhotoChange(e, photoFile, removePhoto);
                    AddEmployeeParams(cmd, e);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                    return e.EmployeeID;
                }
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblEmployee
    (EmployeeCode, FirstName, LastName, FathersHusbandsName, DisplayName, NationalIDNumber,
     Gender, GenderID, DateOfBirth, MaritalStatus, DepartmentID, DivisionID,
     NationalityID, ReligionID, LanguageID, WorkerCategoryID, EmploymentTypeID, EmploymentStatusID,
     WorkforceSegmentID, LegalEntityID, BusinessUnitID, SalesTeamID, CostCenterID,
     RegionID, LocationID, JobID, WorkerLocationID, CityID, ProvinceID, SalesGroupID, GradeID,
     ExtensionID, Domicile, BloodGroupID, BenefitEntitlementID, UserID,
     TemporaryResponsibleEmployeeID, PermanentResponsibleEmployeeID,
     Designation, DateOfJoining, EmploymentStartDate, ProbationPeriodDays, ProbationEndDate,
     ConfirmationDate, BasicSalary, Status, PhotoPath, CreatedOn, CreatedByUserID)
VALUES
    (@EmployeeCode, @FirstName, @LastName, @FathersHusbandsName, @DisplayName, @NationalIDNumber,
     @Gender, @GenderID, @DateOfBirth, @MaritalStatus, @DepartmentID, @DivisionID,
     @NationalityID, @ReligionID, @LanguageID, @WorkerCategoryID, @EmploymentTypeID, @EmploymentStatusID,
     @WorkforceSegmentID, @LegalEntityID, @BusinessUnitID, @SalesTeamID, @CostCenterID,
     @RegionID, @LocationID, @JobID, @WorkerLocationID, @CityID, @ProvinceID, @SalesGroupID, @GradeID,
     @ExtensionID, @Domicile, @BloodGroupID, @BenefitEntitlementID, @UserID,
     @TemporaryResponsibleEmployeeID, @PermanentResponsibleEmployeeID,
     @Designation, @DateOfJoining, @EmploymentStartDate, @ProbationPeriodDays, @ProbationEndDate,
     @ConfirmationDate, @BasicSalary, @Status, @PhotoPath, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
            {
                AddEmployeeParams(ins, e);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                var newId = Convert.ToInt32(ins.ExecuteScalar());
                e.EmployeeID = newId;

                if (photoFile != null && photoFile.ContentLength > 0)
                {
                    e.PhotoPath = SaveProfilePhotoFile(photoFile, newId) ?? "";
                    using (var photoCmd = new SqlCommand(
                        "UPDATE tblEmployee SET PhotoPath=@PhotoPath WHERE EmployeeID=@EmployeeID;", conn, tx))
                    {
                        photoCmd.Parameters.AddWithValue("@PhotoPath",
                            string.IsNullOrWhiteSpace(e.PhotoPath) ? (object)DBNull.Value : e.PhotoPath);
                        photoCmd.Parameters.AddWithValue("@EmployeeID", newId);
                        photoCmd.ExecuteNonQuery();
                    }
                }
                return newId;
            }
        }

        private void ApplyProfilePhotoChange(EmployeeInput e, HttpPostedFile photoFile, bool removePhoto)
        {
            if (photoFile != null && photoFile.ContentLength > 0)
            {
                if (!string.IsNullOrWhiteSpace(e.PhotoPath))
                    DeleteProfilePhotoFile(e.PhotoPath);
                e.PhotoPath = SaveProfilePhotoFile(photoFile, e.EmployeeID) ?? e.PhotoPath;
                return;
            }
            if (removePhoto) e.PhotoPath = "";
        }

        private string SaveProfilePhotoFile(HttpPostedFile file, int employeeId)
        {
            if (file == null || file.ContentLength <= 0) return null;
            if (file.ContentLength > 5 * 1024 * 1024)
                throw new InvalidOperationException("Profile photo must be 5 MB or smaller.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ProfilePhotoExtensions.Contains(ext))
                throw new InvalidOperationException("Profile photo must be JPG, PNG, or WEBP.");

            var uploads = Server.MapPath("~/uploads/employees");
            Directory.CreateDirectory(uploads);
            var safeName = "emp_" + employeeId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            var fullPath = Path.Combine(uploads, safeName);
            file.SaveAs(fullPath);
            return "/uploads/employees/" + safeName;
        }

        private void DeleteProfilePhotoFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            try
            {
                var normalized = relativePath.Trim().TrimStart('~').TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Server.MapPath("~/" + normalized.Replace('\\', '/'));
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch { }
        }

        private static void AddEmployeeParams(SqlCommand cmd, EmployeeInput e)
        {
            Func<int, object> Fk = id => id <= 0 ? (object)DBNull.Value : id;
            Func<string, object> Str = s => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

            cmd.Parameters.AddWithValue("@EmployeeID", e.EmployeeID);
            cmd.Parameters.AddWithValue("@EmployeeCode", e.EmployeeCode.Trim());
            cmd.Parameters.AddWithValue("@FirstName", e.FirstName.Trim());
            cmd.Parameters.AddWithValue("@LastName", e.LastName.Trim());
            cmd.Parameters.AddWithValue("@FathersHusbandsName", Str(e.FathersHusbandsName));
            cmd.Parameters.AddWithValue("@DisplayName", Str(e.DisplayName));
            cmd.Parameters.AddWithValue("@NationalIDNumber", Str(e.NationalIDNumber));
            cmd.Parameters.AddWithValue("@Gender", Str(e.Gender));
            cmd.Parameters.AddWithValue("@GenderID", Fk(e.GenderID));
            cmd.Parameters.AddWithValue("@DateOfBirth", ParseDateParam(e.DateOfBirth));
            cmd.Parameters.AddWithValue("@MaritalStatus", Str(e.MaritalStatus));
            cmd.Parameters.AddWithValue("@DepartmentID", e.DepartmentID);
            cmd.Parameters.AddWithValue("@DivisionID", Fk(e.DivisionID));
            cmd.Parameters.AddWithValue("@NationalityID", Fk(e.NationalityID));
            cmd.Parameters.AddWithValue("@ReligionID", Fk(e.ReligionID));
            cmd.Parameters.AddWithValue("@LanguageID", Fk(e.LanguageID));
            cmd.Parameters.AddWithValue("@WorkerCategoryID", Fk(e.WorkerCategoryID));
            cmd.Parameters.AddWithValue("@EmploymentTypeID", Fk(e.EmploymentTypeID));
            cmd.Parameters.AddWithValue("@EmploymentStatusID", Fk(e.EmploymentStatusID));
            cmd.Parameters.AddWithValue("@WorkforceSegmentID", Fk(e.WorkforceSegmentID));
            cmd.Parameters.AddWithValue("@LegalEntityID", Fk(e.LegalEntityID));
            cmd.Parameters.AddWithValue("@BusinessUnitID", Fk(e.BusinessUnitID));
            cmd.Parameters.AddWithValue("@SalesTeamID", Fk(e.SalesTeamID));
            cmd.Parameters.AddWithValue("@CostCenterID", Fk(e.CostCenterID));
            cmd.Parameters.AddWithValue("@RegionID", Fk(e.RegionID));
            cmd.Parameters.AddWithValue("@LocationID", Fk(e.LocationID));
            cmd.Parameters.AddWithValue("@JobID", Fk(e.JobID));
            cmd.Parameters.AddWithValue("@WorkerLocationID", Fk(e.WorkerLocationID));
            cmd.Parameters.AddWithValue("@CityID", Fk(e.CityID));
            cmd.Parameters.AddWithValue("@ProvinceID", Fk(e.ProvinceID));
            cmd.Parameters.AddWithValue("@SalesGroupID", Fk(e.SalesGroupID));
            cmd.Parameters.AddWithValue("@GradeID", Fk(e.GradeID));
            cmd.Parameters.AddWithValue("@ExtensionID", Fk(e.ExtensionID));
            cmd.Parameters.AddWithValue("@Domicile", Str(e.Domicile));
            cmd.Parameters.AddWithValue("@BloodGroupID", Fk(e.BloodGroupID));
            cmd.Parameters.AddWithValue("@BenefitEntitlementID", Fk(e.BenefitEntitlementID));
            cmd.Parameters.AddWithValue("@UserID", Fk(e.UserID));
            cmd.Parameters.AddWithValue("@TemporaryResponsibleEmployeeID", Fk(e.TemporaryResponsibleEmployeeID));
            cmd.Parameters.AddWithValue("@PermanentResponsibleEmployeeID", Fk(e.PermanentResponsibleEmployeeID));
            cmd.Parameters.AddWithValue("@Designation", e.Designation.Trim());
            cmd.Parameters.AddWithValue("@DateOfJoining", DateTime.Parse(e.DateOfJoining));
            cmd.Parameters.AddWithValue("@EmploymentStartDate", ParseDateParam(e.EmploymentStartDate));
            cmd.Parameters.AddWithValue("@ProbationPeriodDays", ParseIntParam(e.ProbationPeriodDays));
            cmd.Parameters.AddWithValue("@ProbationEndDate", ParseDateParam(e.ProbationEndDate));
            cmd.Parameters.AddWithValue("@ConfirmationDate", ParseDateParam(e.ConfirmationDate));
            cmd.Parameters.AddWithValue("@BasicSalary", decimal.Parse(e.BasicSalary));
            cmd.Parameters.AddWithValue("@Status", e.Status);
            cmd.Parameters.AddWithValue("@PhotoPath",
                string.IsNullOrWhiteSpace(e.PhotoPath) ? (object)DBNull.Value : e.PhotoPath.Trim());
        }

        private static void EnsureSinglePrimary<T>(List<T> items, Func<T, bool> getPrimary, Action<T, bool> setPrimary)
        {
            var seen = false;
            foreach (var item in items)
            {
                if (!getPrimary(item)) continue;
                if (seen) setPrimary(item, false);
                else seen = true;
            }
        }

        private void DeleteChildRowsNotInList(SqlConnection conn, SqlTransaction tx, string table, string idColumn, int employeeID, IEnumerable<int> keepIds)
        {
            var ids = keepIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                using (var delAll = new SqlCommand("DELETE FROM " + table + " WHERE EmployeeID=@EmployeeID;", conn, tx))
                {
                    delAll.Parameters.AddWithValue("@EmployeeID", employeeID);
                    delAll.ExecuteNonQuery();
                }
                return;
            }

            var paramNames = new List<string>();
            using (var delCmd = new SqlCommand())
            {
                delCmd.Connection = conn;
                delCmd.Transaction = tx;
                delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                for (var i = 0; i < ids.Count; i++)
                {
                    var p = "@id" + i;
                    paramNames.Add(p);
                    delCmd.Parameters.AddWithValue(p, ids[i]);
                }
                delCmd.CommandText = "DELETE FROM " + table + " WHERE EmployeeID=@EmployeeID AND "
                    + idColumn + " NOT IN (" + string.Join(",", paramNames) + ");";
                delCmd.ExecuteNonQuery();
            }
        }

        private bool ChildColumn(SqlConnection conn, SqlTransaction tx, string table, string column)
            => DbSchemaHelper.HasColumn(conn, tx, table, column);

        private void SyncEmployeeContacts(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeContactInput> contacts)
        {
            const string table = "tblEmployeeContact";
            var hasContactName = ChildColumn(conn, tx, table, "ContactName");
            var hasRelationship = ChildColumn(conn, tx, table, "Relationship");
            var hasSortOrder = ChildColumn(conn, tx, table, "SortOrder");
            var hasModifiedOn = ChildColumn(conn, tx, table, "ModifiedOn");
            var hasCreatedOn = ChildColumn(conn, tx, table, "CreatedOn");
            var hasCreatedBy = ChildColumn(conn, tx, table, "CreatedByUserID");

            var rows = contacts.Where(c =>
                !string.IsNullOrWhiteSpace(c.ContactType) &&
                (!string.IsNullOrWhiteSpace(c.ContactValue) || !string.IsNullOrWhiteSpace(c.ContactName))).ToList();
            EnsureSinglePrimary(rows, r => r.IsPrimary, (r, v) => r.IsPrimary = v);
            DeleteChildRowsNotInList(conn, tx, table, "ContactID", employeeID, rows.Select(r => r.ContactID));

            int sortOrder = 0;
            foreach (var contact in rows)
            {
                sortOrder++;
                if (contact.ContactID > 0)
                {
                    var setParts = new List<string>
                    {
                        "ContactType=@ContactType",
                        "ContactValue=@ContactValue",
                        "IsPrimary=@IsPrimary"
                    };
                    if (hasContactName) setParts.Add("ContactName=@ContactName");
                    if (hasRelationship) setParts.Add("Relationship=@Relationship");
                    if (hasSortOrder) setParts.Add("SortOrder=@SortOrder");
                    if (hasModifiedOn) setParts.Add("ModifiedOn=GETDATE()");

                    using (var upd = new SqlCommand(
                        "UPDATE " + table + " SET " + string.Join(", ", setParts)
                        + " WHERE ContactID=@ContactID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@ContactID", contact.ContactID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@ContactType", contact.ContactType.Trim());
                        upd.Parameters.AddWithValue("@ContactValue", NullIfEmpty(contact.ContactValue));
                        upd.Parameters.AddWithValue("@IsPrimary", contact.IsPrimary);
                        if (hasContactName) upd.Parameters.AddWithValue("@ContactName", NullIfEmpty(contact.ContactName));
                        if (hasRelationship) upd.Parameters.AddWithValue("@Relationship", NullIfEmpty(contact.Relationship));
                        if (hasSortOrder) upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string> { "EmployeeID", "ContactType", "ContactValue", "IsPrimary" };
                    var vals = new List<string> { "@EmployeeID", "@ContactType", "@ContactValue", "@IsPrimary" };
                    if (hasContactName) { cols.Add("ContactName"); vals.Add("@ContactName"); }
                    if (hasRelationship) { cols.Add("Relationship"); vals.Add("@Relationship"); }
                    if (hasSortOrder) { cols.Add("SortOrder"); vals.Add("@SortOrder"); }
                    if (hasCreatedOn) { cols.Add("CreatedOn"); vals.Add("GETDATE()"); }
                    if (hasCreatedBy) { cols.Add("CreatedByUserID"); vals.Add("@CreatedByUserID"); }

                    using (var insCmd = new SqlCommand(
                        "INSERT INTO " + table + " (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@ContactType", contact.ContactType.Trim());
                        insCmd.Parameters.AddWithValue("@ContactValue", NullIfEmpty(contact.ContactValue));
                        insCmd.Parameters.AddWithValue("@IsPrimary", contact.IsPrimary);
                        if (hasContactName) insCmd.Parameters.AddWithValue("@ContactName", NullIfEmpty(contact.ContactName));
                        if (hasRelationship) insCmd.Parameters.AddWithValue("@Relationship", NullIfEmpty(contact.Relationship));
                        if (hasSortOrder) insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        if (hasCreatedBy) AuditHelper.AddCreatedBy(insCmd, Auth.CurrentUserId);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SyncEmployeeAddresses(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeAddressInput> addresses)
        {
            const string table = "tblEmployeeAddress";
            var hasSortOrder = ChildColumn(conn, tx, table, "SortOrder");
            var hasModifiedOn = ChildColumn(conn, tx, table, "ModifiedOn");
            var hasCreatedOn = ChildColumn(conn, tx, table, "CreatedOn");
            var hasCreatedBy = ChildColumn(conn, tx, table, "CreatedByUserID");

            var rows = addresses.Where(a => !string.IsNullOrWhiteSpace(a.AddressLine)).ToList();
            EnsureSinglePrimary(rows, r => r.IsPrimary, (r, v) => r.IsPrimary = v);
            DeleteChildRowsNotInList(conn, tx, table, "AddressID", employeeID, rows.Select(r => r.AddressID));

            int sortOrder = 0;
            foreach (var address in rows)
            {
                sortOrder++;
                var addressType = string.IsNullOrWhiteSpace(address.AddressType) ? "Other" : address.AddressType.Trim();
                if (address.AddressID > 0)
                {
                    var setParts = new List<string>
                    {
                        "AddressType=@AddressType",
                        "AddressLine=@AddressLine",
                        "City=@City",
                        "ProvinceState=@ProvinceState",
                        "PostalCode=@PostalCode",
                        "IsPrimary=@IsPrimary"
                    };
                    if (hasSortOrder) setParts.Add("SortOrder=@SortOrder");
                    if (hasModifiedOn) setParts.Add("ModifiedOn=GETDATE()");

                    using (var upd = new SqlCommand(
                        "UPDATE " + table + " SET " + string.Join(", ", setParts)
                        + " WHERE AddressID=@AddressID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@AddressID", address.AddressID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@AddressType", addressType);
                        upd.Parameters.AddWithValue("@AddressLine", address.AddressLine.Trim());
                        upd.Parameters.AddWithValue("@City", NullIfEmpty(address.City));
                        upd.Parameters.AddWithValue("@ProvinceState", NullIfEmpty(address.ProvinceState));
                        upd.Parameters.AddWithValue("@PostalCode", NullIfEmpty(address.PostalCode));
                        upd.Parameters.AddWithValue("@IsPrimary", address.IsPrimary);
                        if (hasSortOrder) upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "AddressType", "AddressLine", "City", "ProvinceState", "PostalCode", "IsPrimary"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@AddressType", "@AddressLine", "@City", "@ProvinceState", "@PostalCode", "@IsPrimary"
                    };
                    if (hasSortOrder) { cols.Add("SortOrder"); vals.Add("@SortOrder"); }
                    if (hasCreatedOn) { cols.Add("CreatedOn"); vals.Add("GETDATE()"); }
                    if (hasCreatedBy) { cols.Add("CreatedByUserID"); vals.Add("@CreatedByUserID"); }

                    using (var insCmd = new SqlCommand(
                        "INSERT INTO " + table + " (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@AddressType", addressType);
                        insCmd.Parameters.AddWithValue("@AddressLine", address.AddressLine.Trim());
                        insCmd.Parameters.AddWithValue("@City", NullIfEmpty(address.City));
                        insCmd.Parameters.AddWithValue("@ProvinceState", NullIfEmpty(address.ProvinceState));
                        insCmd.Parameters.AddWithValue("@PostalCode", NullIfEmpty(address.PostalCode));
                        insCmd.Parameters.AddWithValue("@IsPrimary", address.IsPrimary);
                        if (hasSortOrder) insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        if (hasCreatedBy) AuditHelper.AddCreatedBy(insCmd, Auth.CurrentUserId);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Appends CreatedOn / CreatedByUserID only when those columns exist (production schema drift).
        /// </summary>
        private void AppendOptionalAuditColumns(SqlConnection conn, SqlTransaction tx, string table,
            List<string> cols, List<string> vals)
        {
            if (ChildColumn(conn, tx, table, "CreatedOn"))
            {
                cols.Add("CreatedOn");
                vals.Add("GETDATE()");
            }
            if (ChildColumn(conn, tx, table, "CreatedByUserID"))
            {
                cols.Add("CreatedByUserID");
                vals.Add("@CreatedByUserID");
            }
        }

        private void AddOptionalCreatedByParam(SqlConnection conn, SqlTransaction tx, string table, SqlCommand cmd)
        {
            if (ChildColumn(conn, tx, table, "CreatedByUserID"))
                AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
        }

        private string OptionalModifiedOnSet(SqlConnection conn, SqlTransaction tx, string table)
            => ChildColumn(conn, tx, table, "ModifiedOn") ? ", ModifiedOn=GETDATE()" : "";

        private string OptionalSortOrderSet(SqlConnection conn, SqlTransaction tx, string table)
            => ChildColumn(conn, tx, table, "SortOrder") ? ", SortOrder=@SortOrder" : "";

        private void SyncEmployeeFamilyMembers(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeFamilyMemberInput> members)
        {
            const string table = "tblEmployeeFamilyMember";
            var hasSortOrder = ChildColumn(conn, tx, table, "SortOrder");
            var rows = members.Where(m => !string.IsNullOrWhiteSpace(m.MemberName)).ToList();
            DeleteChildRowsNotInList(conn, tx, table, "FamilyMemberID", employeeID, rows.Select(r => r.FamilyMemberID));

            int sortOrder = 0;
            foreach (var member in rows)
            {
                sortOrder++;
                if (member.FamilyMemberID > 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblEmployeeFamilyMember SET
    MemberName=@MemberName, Relationship=@Relationship, Gender=@Gender, DateOfBirth=@DateOfBirth,
    ContactNumber=@ContactNumber, IsDependent=@IsDependent"
    + (hasSortOrder ? ", SortOrder=@SortOrder" : "")
    + OptionalModifiedOnSet(conn, tx, table) + @"
WHERE FamilyMemberID=@FamilyMemberID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@FamilyMemberID", member.FamilyMemberID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@MemberName", member.MemberName.Trim());
                        upd.Parameters.AddWithValue("@Relationship", NullIfEmpty(member.Relationship));
                        upd.Parameters.AddWithValue("@Gender", NullIfEmpty(member.Gender));
                        upd.Parameters.AddWithValue("@DateOfBirth", ParseDateParam(member.DateOfBirth));
                        upd.Parameters.AddWithValue("@ContactNumber", NullIfEmpty(member.ContactNumber));
                        upd.Parameters.AddWithValue("@IsDependent", member.IsDependent);
                        if (hasSortOrder) upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "MemberName", "Relationship", "Gender", "DateOfBirth",
                        "ContactNumber", "IsDependent"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@MemberName", "@Relationship", "@Gender", "@DateOfBirth",
                        "@ContactNumber", "@IsDependent"
                    };
                    if (hasSortOrder) { cols.Add("SortOrder"); vals.Add("@SortOrder"); }
                    AppendOptionalAuditColumns(conn, tx, table, cols, vals);

                    using (var insCmd = new SqlCommand(
                        "INSERT INTO " + table + " (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@MemberName", member.MemberName.Trim());
                        insCmd.Parameters.AddWithValue("@Relationship", NullIfEmpty(member.Relationship));
                        insCmd.Parameters.AddWithValue("@Gender", NullIfEmpty(member.Gender));
                        insCmd.Parameters.AddWithValue("@DateOfBirth", ParseDateParam(member.DateOfBirth));
                        insCmd.Parameters.AddWithValue("@ContactNumber", NullIfEmpty(member.ContactNumber));
                        insCmd.Parameters.AddWithValue("@IsDependent", member.IsDependent);
                        if (hasSortOrder) insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        AddOptionalCreatedByParam(conn, tx, table, insCmd);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SyncEmployeeBanks(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeBankInput> banks)
        {
            var rows = banks.Where(b => b.BankID > 0).ToList();
            EnsureSinglePrimary(rows, r => r.IsPrimary, (r, v) => r.IsPrimary = v);
            DeleteChildRowsNotInList(conn, tx, "tblEmployeeBank", "EmployeeBankID", employeeID, rows.Select(r => r.EmployeeBankID));

            int sortOrder = 0;
            foreach (var bank in rows)
            {
                sortOrder++;
                var status = string.IsNullOrWhiteSpace(bank.AccountVerificationStatus) ? "Pending" : bank.AccountVerificationStatus.Trim();
                if (bank.EmployeeBankID > 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblEmployeeBank SET
    BankID=@BankID, BankCode=@BankCode, LocationName=@LocationName, BankGroupID=@BankGroupID,
    IBAN=@IBAN, SwiftBICCode=@SwiftBICCode, CurrencyCode=@CurrencyCode,
    AccountVerificationStatus=@AccountVerificationStatus, IsPrimary=@IsPrimary, SortOrder=@SortOrder"
    + OptionalModifiedOnSet(conn, tx, "tblEmployeeBank") + @"
WHERE EmployeeBankID=@EmployeeBankID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@EmployeeBankID", bank.EmployeeBankID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@BankID", bank.BankID);
                        upd.Parameters.AddWithValue("@BankGroupID", bank.BankGroupID > 0 ? (object)bank.BankGroupID : DBNull.Value);
                        upd.Parameters.AddWithValue("@IBAN", NullIfEmpty(bank.IBAN));
                        upd.Parameters.AddWithValue("@BankCode", NullIfEmpty(bank.BankCode));
                        upd.Parameters.AddWithValue("@LocationName", NullIfEmpty(bank.LocationName));
                        upd.Parameters.AddWithValue("@SwiftBICCode", NullIfEmpty(bank.SwiftBICCode));
                        upd.Parameters.AddWithValue("@CurrencyCode", NullIfEmpty(bank.CurrencyCode));
                        upd.Parameters.AddWithValue("@AccountVerificationStatus", status);
                        upd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
                        upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "BankID", "BankCode", "LocationName", "BankGroupID", "IBAN", "SwiftBICCode",
                        "CurrencyCode", "AccountVerificationStatus", "IsPrimary", "SortOrder"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@BankID", "@BankCode", "@LocationName", "@BankGroupID", "@IBAN", "@SwiftBICCode",
                        "@CurrencyCode", "@AccountVerificationStatus", "@IsPrimary", "@SortOrder"
                    };
                    AppendOptionalAuditColumns(conn, tx, "tblEmployeeBank", cols, vals);
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO tblEmployeeBank (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@BankID", bank.BankID);
                        insCmd.Parameters.AddWithValue("@BankGroupID", bank.BankGroupID > 0 ? (object)bank.BankGroupID : DBNull.Value);
                        insCmd.Parameters.AddWithValue("@IBAN", NullIfEmpty(bank.IBAN));
                        insCmd.Parameters.AddWithValue("@BankCode", NullIfEmpty(bank.BankCode));
                        insCmd.Parameters.AddWithValue("@LocationName", NullIfEmpty(bank.LocationName));
                        insCmd.Parameters.AddWithValue("@SwiftBICCode", NullIfEmpty(bank.SwiftBICCode));
                        insCmd.Parameters.AddWithValue("@CurrencyCode", NullIfEmpty(bank.CurrencyCode));
                        insCmd.Parameters.AddWithValue("@AccountVerificationStatus", status);
                        insCmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
                        insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        AddOptionalCreatedByParam(conn, tx, "tblEmployeeBank", insCmd);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SyncEmployeeEducation(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeEducationInput> records)
        {
            var rows = records.Where(e =>
                !string.IsNullOrWhiteSpace(e.HighestQualification)
                || !string.IsNullOrWhiteSpace(e.DegreeCertificate)
                || !string.IsNullOrWhiteSpace(e.Institution)).ToList();
            DeleteChildRowsNotInList(conn, tx, "tblEmployeeEducation", "EducationID", employeeID, rows.Select(r => r.EducationID));

            int sortOrder = 0;
            foreach (var edu in rows)
            {
                sortOrder++;
                if (edu.EducationID > 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblEmployeeEducation SET
    HighestQualification=@HighestQualification, DegreeCertificate=@DegreeCertificate,
    Specialization=@Specialization, Institution=@Institution, YearOfPassing=@YearOfPassing,
    GradeCGPA=@GradeCGPA, SortOrder=@SortOrder"
    + OptionalModifiedOnSet(conn, tx, "tblEmployeeEducation") + @"
WHERE EducationID=@EducationID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@EducationID", edu.EducationID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@HighestQualification", NullIfEmpty(edu.HighestQualification));
                        upd.Parameters.AddWithValue("@DegreeCertificate", NullIfEmpty(edu.DegreeCertificate));
                        upd.Parameters.AddWithValue("@Specialization", NullIfEmpty(edu.Specialization));
                        upd.Parameters.AddWithValue("@Institution", NullIfEmpty(edu.Institution));
                        upd.Parameters.AddWithValue("@YearOfPassing", ParseIntParam(edu.YearOfPassing));
                        upd.Parameters.AddWithValue("@GradeCGPA", NullIfEmpty(edu.GradeCGPA));
                        upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "HighestQualification", "DegreeCertificate", "Specialization", "Institution",
                        "YearOfPassing", "GradeCGPA", "SortOrder"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@HighestQualification", "@DegreeCertificate", "@Specialization", "@Institution",
                        "@YearOfPassing", "@GradeCGPA", "@SortOrder"
                    };
                    AppendOptionalAuditColumns(conn, tx, "tblEmployeeEducation", cols, vals);
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO tblEmployeeEducation (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@HighestQualification", NullIfEmpty(edu.HighestQualification));
                        insCmd.Parameters.AddWithValue("@DegreeCertificate", NullIfEmpty(edu.DegreeCertificate));
                        insCmd.Parameters.AddWithValue("@Specialization", NullIfEmpty(edu.Specialization));
                        insCmd.Parameters.AddWithValue("@Institution", NullIfEmpty(edu.Institution));
                        insCmd.Parameters.AddWithValue("@YearOfPassing", ParseIntParam(edu.YearOfPassing));
                        insCmd.Parameters.AddWithValue("@GradeCGPA", NullIfEmpty(edu.GradeCGPA));
                        insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        AddOptionalCreatedByParam(conn, tx, "tblEmployeeEducation", insCmd);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SyncEmployeeCertificates(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeCertificateInput> records)
        {
            var rows = new List<Tuple<EmployeeCertificateInput, int>>();
            for (int i = 0; i < records.Count; i++)
            {
                var cert = records[i];
                if (string.IsNullOrWhiteSpace(cert.CertificationName)
                    && string.IsNullOrWhiteSpace(cert.CertificateNumber)
                    && string.IsNullOrWhiteSpace(cert.CertificationBody)
                    && string.IsNullOrWhiteSpace(cert.CertificateCopyPath)
                    && (Request.Files["CertCopy_" + i] == null || Request.Files["CertCopy_" + i].ContentLength == 0))
                    continue;
                rows.Add(Tuple.Create(cert, i));
            }

            DeleteChildRowsNotInList(conn, tx, "tblEmployeeCertificate", "CertificateID", employeeID, rows.Select(r => r.Item1.CertificateID));

            int sortOrder = 0;
            foreach (var pair in rows)
            {
                var cert = pair.Item1;
                var i = pair.Item2;
                var docPath = cert.CertificateCopyPath;
                var uploaded = SaveCertificateFile(Request.Files["CertCopy_" + i], employeeID, i);
                if (!string.IsNullOrEmpty(uploaded)) docPath = uploaded;

                sortOrder++;
                if (cert.CertificateID > 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblEmployeeCertificate SET
    CertificationName=@CertificationName, CertificationBody=@CertificationBody, CertificateNumber=@CertificateNumber,
    IssueDate=@IssueDate, ExpiryDate=@ExpiryDate, RenewalRequired=@RenewalRequired,
    CertificateCopyPath=@CertificateCopyPath, SortOrder=@SortOrder"
    + OptionalModifiedOnSet(conn, tx, "tblEmployeeCertificate") + @"
WHERE CertificateID=@CertificateID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@CertificateID", cert.CertificateID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@CertificationName", NullIfEmpty(cert.CertificationName));
                        upd.Parameters.AddWithValue("@CertificationBody", NullIfEmpty(cert.CertificationBody));
                        upd.Parameters.AddWithValue("@CertificateNumber", NullIfEmpty(cert.CertificateNumber));
                        upd.Parameters.AddWithValue("@IssueDate", ParseDateParam(cert.IssueDate));
                        upd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(cert.ExpiryDate));
                        upd.Parameters.AddWithValue("@RenewalRequired", cert.RenewalRequired);
                        upd.Parameters.AddWithValue("@CertificateCopyPath",
                            string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
                        upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "CertificationName", "CertificationBody", "CertificateNumber",
                        "IssueDate", "ExpiryDate", "RenewalRequired", "CertificateCopyPath", "SortOrder"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@CertificationName", "@CertificationBody", "@CertificateNumber",
                        "@IssueDate", "@ExpiryDate", "@RenewalRequired", "@CertificateCopyPath", "@SortOrder"
                    };
                    AppendOptionalAuditColumns(conn, tx, "tblEmployeeCertificate", cols, vals);
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO tblEmployeeCertificate (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@CertificationName", NullIfEmpty(cert.CertificationName));
                        insCmd.Parameters.AddWithValue("@CertificationBody", NullIfEmpty(cert.CertificationBody));
                        insCmd.Parameters.AddWithValue("@CertificateNumber", NullIfEmpty(cert.CertificateNumber));
                        insCmd.Parameters.AddWithValue("@IssueDate", ParseDateParam(cert.IssueDate));
                        insCmd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(cert.ExpiryDate));
                        insCmd.Parameters.AddWithValue("@RenewalRequired", cert.RenewalRequired);
                        insCmd.Parameters.AddWithValue("@CertificateCopyPath",
                            string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
                        insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        AddOptionalCreatedByParam(conn, tx, "tblEmployeeCertificate", insCmd);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private string SaveCertificateFile(HttpPostedFile file, int employeeId, int rowIndex)
        {
            if (file == null || file.ContentLength == 0) return null;
            var uploads = Server.MapPath("~/uploads/employee-certificates");
            Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(file.FileName);
            var safeName = employeeId + "_" + rowIndex + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(uploads, safeName));
            return "/uploads/employee-certificates/" + safeName;
        }

        private void SyncEmployeeDocuments(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeDocumentInput> records)
        {
            var rows = new List<Tuple<EmployeeDocumentInput, int>>();
            for (int i = 0; i < records.Count; i++)
            {
                var doc = records[i];
                var hasFile = Request.Files["DocFile_" + i] != null && Request.Files["DocFile_" + i].ContentLength > 0;
                if (doc.DocumentTypeID <= 0
                    && string.IsNullOrWhiteSpace(doc.DocumentNumber)
                    && string.IsNullOrWhiteSpace(doc.DocumentPath)
                    && string.IsNullOrWhiteSpace(doc.Remarks)
                    && !hasFile)
                    continue;
                rows.Add(Tuple.Create(doc, i));
            }

            DeleteChildRowsNotInList(conn, tx, "tblEmployeeDocument", "EmployeeDocumentID", employeeID, rows.Select(r => r.Item1.EmployeeDocumentID));

            int sortOrder = 0;
            foreach (var pair in rows)
            {
                var doc = pair.Item1;
                var i = pair.Item2;
                var docPath = doc.DocumentPath;
                var originalName = doc.OriginalFileName;
                var uploaded = SaveDocumentFile(Request.Files["DocFile_" + i], employeeID, i);
                if (!string.IsNullOrEmpty(uploaded.Item1))
                {
                    docPath = uploaded.Item1;
                    originalName = uploaded.Item2;
                }

                var status = string.IsNullOrWhiteSpace(doc.VerificationStatus) ? "Pending" : doc.VerificationStatus.Trim();
                object verifiedOn = status == "Verified" ? (object)DateTime.Now : DBNull.Value;
                object verifiedBy = status == "Verified" && Auth.CurrentUserId.HasValue && Auth.CurrentUserId.Value > 0
                    ? (object)Auth.CurrentUserId.Value : DBNull.Value;

                sortOrder++;
                if (doc.EmployeeDocumentID > 0)
                {
                    using (var upd = new SqlCommand(@"
UPDATE tblEmployeeDocument SET
    DocumentTypeID=@DocumentTypeID, DocumentNumber=@DocumentNumber, IssueDate=@IssueDate, ExpiryDate=@ExpiryDate,
    Remarks=@Remarks, DocumentPath=@DocumentPath, OriginalFileName=@OriginalFileName,
    VerificationStatus=@VerificationStatus, VerifiedOn=@VerifiedOn, VerifiedByUserID=@VerifiedByUserID,
    SortOrder=@SortOrder"
    + OptionalModifiedOnSet(conn, tx, "tblEmployeeDocument") + @"
WHERE EmployeeDocumentID=@EmployeeDocumentID AND EmployeeID=@EmployeeID;", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@EmployeeDocumentID", doc.EmployeeDocumentID);
                        upd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        upd.Parameters.AddWithValue("@DocumentTypeID", doc.DocumentTypeID <= 0 ? (object)DBNull.Value : doc.DocumentTypeID);
                        upd.Parameters.AddWithValue("@DocumentNumber", NullIfEmpty(doc.DocumentNumber));
                        upd.Parameters.AddWithValue("@IssueDate", ParseDateParam(doc.IssueDate));
                        upd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(doc.ExpiryDate));
                        upd.Parameters.AddWithValue("@Remarks", NullIfEmpty(doc.Remarks));
                        upd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
                        upd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? (object)DBNull.Value : originalName);
                        upd.Parameters.AddWithValue("@VerificationStatus", status);
                        upd.Parameters.AddWithValue("@VerifiedOn", verifiedOn);
                        upd.Parameters.AddWithValue("@VerifiedByUserID", verifiedBy);
                        upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var cols = new List<string>
                    {
                        "EmployeeID", "DocumentTypeID", "DocumentNumber", "IssueDate", "ExpiryDate", "Remarks",
                        "DocumentPath", "OriginalFileName", "VerificationStatus", "VerifiedOn", "VerifiedByUserID", "SortOrder"
                    };
                    var vals = new List<string>
                    {
                        "@EmployeeID", "@DocumentTypeID", "@DocumentNumber", "@IssueDate", "@ExpiryDate", "@Remarks",
                        "@DocumentPath", "@OriginalFileName", "@VerificationStatus", "@VerifiedOn", "@VerifiedByUserID", "@SortOrder"
                    };
                    AppendOptionalAuditColumns(conn, tx, "tblEmployeeDocument", cols, vals);
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO tblEmployeeDocument (" + string.Join(", ", cols) + ") VALUES ("
                        + string.Join(", ", vals) + ");", conn, tx))
                    {
                        insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                        insCmd.Parameters.AddWithValue("@DocumentTypeID", doc.DocumentTypeID <= 0 ? (object)DBNull.Value : doc.DocumentTypeID);
                        insCmd.Parameters.AddWithValue("@DocumentNumber", NullIfEmpty(doc.DocumentNumber));
                        insCmd.Parameters.AddWithValue("@IssueDate", ParseDateParam(doc.IssueDate));
                        insCmd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(doc.ExpiryDate));
                        insCmd.Parameters.AddWithValue("@Remarks", NullIfEmpty(doc.Remarks));
                        insCmd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(docPath) ? (object)DBNull.Value : docPath);
                        insCmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? (object)DBNull.Value : originalName);
                        insCmd.Parameters.AddWithValue("@VerificationStatus", status);
                        insCmd.Parameters.AddWithValue("@VerifiedOn", verifiedOn);
                        insCmd.Parameters.AddWithValue("@VerifiedByUserID", verifiedBy);
                        insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        AddOptionalCreatedByParam(conn, tx, "tblEmployeeDocument", insCmd);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private Tuple<string, string> SaveDocumentFile(HttpPostedFile file, int employeeId, int rowIndex)
        {
            if (file == null || file.ContentLength == 0) return Tuple.Create<string, string>(null, null);
            var uploads = Server.MapPath("~/uploads/employee-documents");
            Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(file.FileName);
            var safeName = employeeId + "_" + rowIndex + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(uploads, safeName));
            return Tuple.Create("/uploads/employee-documents/" + safeName, file.FileName);
        }

        private List<EmployeeContactInput> LoadEmployeeContacts(SqlConnection conn, int employeeID)
        {
            var contacts = new List<EmployeeContactInput>();
            var hasName = ChildColumn(conn, null, "tblEmployeeContact", "ContactName");
            var hasRel = ChildColumn(conn, null, "tblEmployeeContact", "Relationship");
            var hasSort = ChildColumn(conn, null, "tblEmployeeContact", "SortOrder");

            var selectCols = "ContactID, ContactType, ContactValue, IsPrimary";
            if (hasName) selectCols += ", ContactName";
            if (hasRel) selectCols += ", Relationship";
            var orderBy = hasSort ? "SortOrder, ContactID" : "ContactID";

            using (var cmd = new SqlCommand(
                "SELECT " + selectCols + " FROM tblEmployeeContact WHERE EmployeeID=@EmployeeID ORDER BY " + orderBy + ";", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        contacts.Add(new EmployeeContactInput
                        {
                            ContactID = IntOrZero(dr["ContactID"]),
                            ContactType = dr["ContactType"].ToString() ?? "",
                            ContactName = hasName ? StrOrEmpty(dr["ContactName"]) : "",
                            Relationship = hasRel ? StrOrEmpty(dr["Relationship"]) : "",
                            ContactValue = StrOrEmpty(dr["ContactValue"]),
                            IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
                        });
                    }
                }
            }
            return contacts;
        }

        private List<EmployeeAddressInput> LoadEmployeeAddresses(SqlConnection conn, int employeeID)
        {
            var addresses = new List<EmployeeAddressInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT AddressID, AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary
FROM tblEmployeeAddress WHERE EmployeeID=@EmployeeID ORDER BY SortOrder, AddressID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            addresses.Add(new EmployeeAddressInput
                            {
                                AddressID = IntOrZero(dr["AddressID"]),
                                AddressType = dr["AddressType"].ToString() ?? "",
                                AddressLine = StrOrEmpty(dr["AddressLine"]),
                                City = StrOrEmpty(dr["City"]),
                                ProvinceState = StrOrEmpty(dr["ProvinceState"]),
                                PostalCode = StrOrEmpty(dr["PostalCode"]),
                                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
                            });
                        }
                    }
                }
            }
            catch { }
            return addresses;
        }

        private List<EmployeeFamilyMemberInput> LoadEmployeeFamilyMembers(SqlConnection conn, int employeeID)
        {
            var members = new List<EmployeeFamilyMemberInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT FamilyMemberID, MemberName, Relationship, Gender, DateOfBirth, ContactNumber, IsDependent
FROM tblEmployeeFamilyMember WHERE EmployeeID=@EmployeeID ORDER BY SortOrder, FamilyMemberID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            members.Add(new EmployeeFamilyMemberInput
                            {
                                FamilyMemberID = IntOrZero(dr["FamilyMemberID"]),
                                MemberName = StrOrEmpty(dr["MemberName"]),
                                Relationship = StrOrEmpty(dr["Relationship"]),
                                Gender = StrOrEmpty(dr["Gender"]),
                                DateOfBirth = DateOrEmpty(dr["DateOfBirth"]),
                                ContactNumber = StrOrEmpty(dr["ContactNumber"]),
                                IsDependent = dr["IsDependent"] != DBNull.Value && Convert.ToBoolean(dr["IsDependent"])
                            });
                        }
                    }
                }
            }
            catch { }
            return members;
        }

        private List<EmployeeBankInput> LoadEmployeeBanks(SqlConnection conn, int employeeID)
        {
            var banks = new List<EmployeeBankInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT EmployeeBankID, BankID, BankCode, LocationName, BankGroupID, IBAN, SwiftBICCode, CurrencyCode,
       AccountVerificationStatus, IsPrimary
FROM tblEmployeeBank WHERE EmployeeID=@EmployeeID ORDER BY SortOrder, EmployeeBankID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            banks.Add(new EmployeeBankInput
                            {
                                EmployeeBankID = IntOrZero(dr["EmployeeBankID"]),
                                BankID = IntOrZero(dr["BankID"]),
                                BankCode = StrOrEmpty(dr["BankCode"]),
                                LocationName = StrOrEmpty(dr["LocationName"]),
                                BankGroupID = IntOrZero(dr["BankGroupID"]),
                                IBAN = StrOrEmpty(dr["IBAN"]),
                                SwiftBICCode = StrOrEmpty(dr["SwiftBICCode"]),
                                CurrencyCode = StrOrEmpty(dr["CurrencyCode"]),
                                AccountVerificationStatus = dr["AccountVerificationStatus"] == DBNull.Value
                                    ? "Pending" : dr["AccountVerificationStatus"].ToString() ?? "Pending",
                                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
                            });
                        }
                    }
                }
            }
            catch { }
            return banks;
        }

        private List<EmployeeEducationInput> LoadEmployeeEducation(SqlConnection conn, int employeeID)
        {
            var records = new List<EmployeeEducationInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT EducationID, HighestQualification, DegreeCertificate, Specialization, Institution, YearOfPassing, GradeCGPA
FROM tblEmployeeEducation WHERE EmployeeID=@EmployeeID ORDER BY SortOrder, EducationID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            records.Add(new EmployeeEducationInput
                            {
                                EducationID = IntOrZero(dr["EducationID"]),
                                HighestQualification = StrOrEmpty(dr["HighestQualification"]),
                                DegreeCertificate = StrOrEmpty(dr["DegreeCertificate"]),
                                Specialization = StrOrEmpty(dr["Specialization"]),
                                Institution = StrOrEmpty(dr["Institution"]),
                                YearOfPassing = dr["YearOfPassing"] == DBNull.Value ? "" : Convert.ToInt32(dr["YearOfPassing"]).ToString(),
                                GradeCGPA = StrOrEmpty(dr["GradeCGPA"])
                            });
                        }
                    }
                }
            }
            catch { }
            return records;
        }

        private List<EmployeeCertificateInput> LoadEmployeeCertificates(SqlConnection conn, int employeeID)
        {
            var records = new List<EmployeeCertificateInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT CertificateID, CertificationName, CertificationBody, CertificateNumber, IssueDate, ExpiryDate,
       RenewalRequired, CertificateCopyPath
FROM tblEmployeeCertificate WHERE EmployeeID=@EmployeeID ORDER BY SortOrder, CertificateID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            records.Add(new EmployeeCertificateInput
                            {
                                CertificateID = IntOrZero(dr["CertificateID"]),
                                CertificationName = StrOrEmpty(dr["CertificationName"]),
                                CertificationBody = StrOrEmpty(dr["CertificationBody"]),
                                CertificateNumber = StrOrEmpty(dr["CertificateNumber"]),
                                IssueDate = DateOrEmpty(dr["IssueDate"]),
                                ExpiryDate = DateOrEmpty(dr["ExpiryDate"]),
                                RenewalRequired = dr["RenewalRequired"] != DBNull.Value && Convert.ToBoolean(dr["RenewalRequired"]),
                                CertificateCopyPath = StrOrEmpty(dr["CertificateCopyPath"])
                            });
                        }
                    }
                }
            }
            catch { }
            return records;
        }

        private List<EmployeeDocumentInput> LoadEmployeeDocuments(SqlConnection conn, int employeeID)
        {
            var records = new List<EmployeeDocumentInput>();
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT d.EmployeeDocumentID, d.DocumentTypeID, dt.DocumentTypeName, d.DocumentNumber, d.IssueDate, d.ExpiryDate, d.Remarks,
       d.DocumentPath, d.OriginalFileName, d.VerificationStatus
FROM tblEmployeeDocument d
LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
WHERE d.EmployeeID=@EmployeeID ORDER BY d.SortOrder, d.EmployeeDocumentID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            records.Add(new EmployeeDocumentInput
                            {
                                EmployeeDocumentID = IntOrZero(dr["EmployeeDocumentID"]),
                                DocumentTypeID = IntOrZero(dr["DocumentTypeID"]),
                                DocumentTypeName = StrOrEmpty(dr["DocumentTypeName"]),
                                DocumentNumber = StrOrEmpty(dr["DocumentNumber"]),
                                IssueDate = DateOrEmpty(dr["IssueDate"]),
                                ExpiryDate = DateOrEmpty(dr["ExpiryDate"]),
                                Remarks = StrOrEmpty(dr["Remarks"]),
                                DocumentPath = StrOrEmpty(dr["DocumentPath"]),
                                OriginalFileName = StrOrEmpty(dr["OriginalFileName"]),
                                VerificationStatus = dr["VerificationStatus"] == DBNull.Value
                                    ? "Pending" : dr["VerificationStatus"].ToString() ?? "Pending"
                            });
                        }
                    }
                }
            }
            catch { }
            return records;
        }

        private void EnsureDefaultRows()
        {
            // Intentionally empty: contact/address/profile child rows are user-controlled.
            // Do not auto-create OfficialEmail / Current address placeholders after load or save.
        }

        private static void ApplyTenureCalculations(EmployeeInput e)
        {
            var startText = !string.IsNullOrWhiteSpace(e.EmploymentStartDate) ? e.EmploymentStartDate : e.DateOfJoining;
            int days;
            if (string.IsNullOrWhiteSpace(startText) || !int.TryParse(e.ProbationPeriodDays, out days) || days <= 0)
                return;
            DateTime start;
            if (!DateTime.TryParse(startText, out start)) return;
            e.ProbationEndDate = start.AddDays(days).ToString("yyyy-MM-dd");
        }

        public static string FormatAge(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return "";
            var today = DateTime.Today;
            var dob = dateOfBirth.Value.Date;
            if (dob > today) return "";
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return age >= 0 ? age + " yr" + (age == 1 ? "" : "s") : "";
        }

        public static string FormatTenure(DateTime? from)
        {
            if (!from.HasValue) return "";
            var start = from.Value.Date;
            var end = DateTime.Today;
            if (start > end) return "";
            var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
            if (end.Day < start.Day) months--;
            if (months < 0) return "";
            var years = months / 12;
            months %= 12;
            var parts = new List<string>();
            if (years > 0) parts.Add(years + " yr" + (years == 1 ? "" : "s"));
            if (months > 0) parts.Add(months + " mo" + (months == 1 ? "" : "s"));
            if (parts.Count == 0)
            {
                var days = (end - start).Days;
                parts.Add(days + " day" + (days == 1 ? "" : "s"));
            }
            return string.Join(", ", parts);
        }

        private int FormInt(string name)
        {
            int v;
            return int.TryParse(Request.Form[name], out v) ? v : 0;
        }

        private static int IntOrZero(object v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
        private static string StrOrEmpty(object v) => v == null || v == DBNull.Value ? "" : v.ToString() ?? "";
        private static string DateOrEmpty(object v) =>
            v == null || v == DBNull.Value ? "" : Convert.ToDateTime(v).ToString("yyyy-MM-dd");
        private static object NullIfEmpty(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        private static object ParseDateParam(string value)
        {
            DateTime d;
            return string.IsNullOrWhiteSpace(value) || !DateTime.TryParse(value, out d)
                ? (object)DBNull.Value : d;
        }
        private static object ParseIntParam(string value)
        {
            int n;
            return string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out n) || n < 0
                ? (object)DBNull.Value : n;
        }
    }
}
