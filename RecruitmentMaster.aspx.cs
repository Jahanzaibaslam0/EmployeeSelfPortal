using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class RecruitmentListItem
    {
        public int RecruitmentID { get; set; }
        public string JobRequisitionNumber { get; set; } = "";
        public string CandidateName { get; set; } = "";
        public string PositionTitle { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string RecruitmentSource { get; set; } = "";
        public string InterviewStatus { get; set; } = "";
        public DateTime? JoiningDate { get; set; }
        public string OnboardingStatus { get; set; } = "";
    }

    public class RecruitmentInput
    {
        public int RecruitmentID { get; set; }
        public string JobRequisitionNumber { get; set; } = "";
        public string RecruitmentSource { get; set; } = "";
        public string PositionTitle { get; set; } = "";
        public int DepartmentID { get; set; }
        public int HiringManagerEmployeeID { get; set; }
        public string CandidateName { get; set; } = "";
        public string PersonalEmail { get; set; } = "";
        public string PersonalPhone { get; set; } = "";
        public string ApplicationDate { get; set; } = "";
        public string InterviewDate { get; set; } = "";
        public string InterviewStatus { get; set; } = "";
        public string SelectionDate { get; set; } = "";
        public string OfferLetterNumber { get; set; } = "";
        public string OfferedSalary { get; set; } = "";
        public string OfferDate { get; set; } = "";
        public string OfferAcceptedDate { get; set; } = "";
        public string BackgroundVerificationStatus { get; set; } = "";
        public string ReferenceCheckStatus { get; set; } = "";
        public string OnboardingStatus { get; set; } = "";
        public string JoiningDate { get; set; } = "";
        public bool InductionCompleted { get; set; }
        public string InductionDate { get; set; } = "";
        public bool DocumentsSubmitted { get; set; }
        public bool SystemAccessProvided { get; set; }
        public string OfficialEmailCreated { get; set; } = "";
        public bool EquipmentIssued { get; set; }
        public string AssetDetails { get; set; } = "";
        public bool TrainingScheduleAssigned { get; set; }
        public int BuddyMentorEmployeeID { get; set; }
        public string ProbationPeriod { get; set; } = "";
        public string ProbationReviewSchedule { get; set; } = "";
        public string ConfirmationStatus { get; set; } = "";
    }

    public partial class RecruitmentMasterPage : AppBasePage
    {
        public readonly string[] RecruitmentSourceOptions =
            { "Internal Referral", "Job Portal", "LinkedIn", "Agency", "Campus Hiring", "Walk-in", "Other" };
        public readonly string[] InterviewStatusOptions =
            { "Scheduled", "Completed", "Selected", "Rejected", "On Hold", "No Show" };
        public readonly string[] VerificationStatusOptions =
            { "Pending", "In Progress", "Verified", "Failed", "Not Required" };
        public readonly string[] OnboardingStatusOptions =
            { "Not Started", "In Progress", "Completed", "On Hold", "Cancelled" };
        public readonly string[] ProbationPeriodOptions =
            { "3 Months", "6 Months", "12 Months", "Other" };
        public readonly string[] ConfirmationStatusOptions =
            { "Pending", "Confirmed", "Extended", "Terminated" };

        public string PageTitle => "Recruitment Master";
        public List<RecruitmentListItem> Records { get; set; } = new List<RecruitmentListItem>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public RecruitmentInput Input { get; set; } = new RecruitmentInput();
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

            var newRecord = Request.QueryString["newRecord"] == "1" || Request.QueryString["newRecord"] == "true";
            OnGet(QueryInt("editId"), newRecord);
        }

        private void OnGet(int? editId, bool newRecord)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newRecord;
            if (ShowForm)
            {
                LoadLookups();
                if (editId.HasValue && editId > 0)
                {
                    LoadForEdit(editId.Value);
                    EditMode = true;
                }
            }
            else LoadRecords();
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new RecruitmentInput
            {
                RecruitmentID = int.TryParse(Request.Form["RecruitmentID"], out var rid) ? rid : 0,
                JobRequisitionNumber = FormString("JobRequisitionNumber"),
                RecruitmentSource = FormString("RecruitmentSource"),
                PositionTitle = FormString("PositionTitle"),
                DepartmentID = int.TryParse(Request.Form["DepartmentID"], out var did) ? did : 0,
                HiringManagerEmployeeID = int.TryParse(Request.Form["HiringManagerEmployeeID"], out var hid) ? hid : 0,
                CandidateName = FormString("CandidateName"),
                PersonalEmail = FormString("PersonalEmail"),
                PersonalPhone = FormString("PersonalPhone"),
                ApplicationDate = FormString("ApplicationDate"),
                InterviewDate = FormString("InterviewDate"),
                InterviewStatus = FormString("InterviewStatus"),
                SelectionDate = FormString("SelectionDate"),
                OfferLetterNumber = FormString("OfferLetterNumber"),
                OfferedSalary = FormString("OfferedSalary"),
                OfferDate = FormString("OfferDate"),
                OfferAcceptedDate = FormString("OfferAcceptedDate"),
                BackgroundVerificationStatus = FormString("BackgroundVerificationStatus"),
                ReferenceCheckStatus = FormString("ReferenceCheckStatus"),
                OnboardingStatus = FormString("OnboardingStatus"),
                JoiningDate = FormString("JoiningDate"),
                InductionCompleted = FormBool("InductionCompleted"),
                InductionDate = FormString("InductionDate"),
                DocumentsSubmitted = FormBool("DocumentsSubmitted"),
                SystemAccessProvided = FormBool("SystemAccessProvided"),
                OfficialEmailCreated = FormString("OfficialEmailCreated"),
                EquipmentIssued = FormBool("EquipmentIssued"),
                AssetDetails = FormString("AssetDetails"),
                TrainingScheduleAssigned = FormBool("TrainingScheduleAssigned"),
                BuddyMentorEmployeeID = int.TryParse(Request.Form["BuddyMentorEmployeeID"], out var bid) ? bid : 0,
                ProbationPeriod = FormString("ProbationPeriod"),
                ProbationReviewSchedule = FormString("ProbationReviewSchedule"),
                ConfirmationStatus = FormString("ConfirmationStatus")
            };

            if (string.IsNullOrWhiteSpace(Input.CandidateName))
            {
                SetFormError("Candidate Name is required.");
                return;
            }
            if (!InputValidators.TryValidateEmail(Input.PersonalEmail, out var validationError, required: false, "Personal Email")
                || !InputValidators.TryValidatePhone(Input.PersonalPhone, out validationError, required: false, "Personal Phone")
                || !InputValidators.TryValidateEmail(Input.OfficialEmailCreated, out validationError, required: false, "Official Email Created"))
            {
                SetFormError(validationError);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input);
                }
                SetAlert(EditMode ? "Recruitment record updated successfully." : "Recruitment record saved successfully.");
                Response.Redirect("~/RecruitmentMaster.aspx?editId=" + Input.RecruitmentID);
            }
            catch (Exception ex) { SetFormError("Error: " + ex.Message); }
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("DELETE FROM tblRecruitment WHERE RecruitmentID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Recruitment record deleted successfully.");
            }
            catch (Exception ex) { SetAlert("Error deleting record: " + ex.Message, "error"); }
            Response.Redirect("~/RecruitmentMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message; AlertType = "error";
            LoadLookups(); ShowForm = true;
        }

        private void SaveRecord(SqlConnection conn, RecruitmentInput input)
        {
            if (input.RecruitmentID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblRecruitment SET
  JobRequisitionNumber=@JobRequisitionNumber, RecruitmentSource=@RecruitmentSource, PositionTitle=@PositionTitle,
  DepartmentID=@DepartmentID, HiringManagerEmployeeID=@HiringManagerEmployeeID, CandidateName=@CandidateName,
  PersonalEmail=@PersonalEmail, PersonalPhone=@PersonalPhone, ApplicationDate=@ApplicationDate,
  InterviewDate=@InterviewDate, InterviewStatus=@InterviewStatus, SelectionDate=@SelectionDate,
  OfferLetterNumber=@OfferLetterNumber, OfferedSalary=@OfferedSalary, OfferDate=@OfferDate,
  OfferAcceptedDate=@OfferAcceptedDate, BackgroundVerificationStatus=@BackgroundVerificationStatus,
  ReferenceCheckStatus=@ReferenceCheckStatus, OnboardingStatus=@OnboardingStatus, JoiningDate=@JoiningDate,
  InductionCompleted=@InductionCompleted, InductionDate=@InductionDate, DocumentsSubmitted=@DocumentsSubmitted,
  SystemAccessProvided=@SystemAccessProvided, OfficialEmailCreated=@OfficialEmailCreated,
  EquipmentIssued=@EquipmentIssued, AssetDetails=@AssetDetails, TrainingScheduleAssigned=@TrainingScheduleAssigned,
  BuddyMentorEmployeeID=@BuddyMentorEmployeeID, ProbationPeriod=@ProbationPeriod,
  ProbationReviewSchedule=@ProbationReviewSchedule, ConfirmationStatus=@ConfirmationStatus,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE RecruitmentID=@RecruitmentID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@RecruitmentID", input.RecruitmentID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblRecruitment
 (JobRequisitionNumber, RecruitmentSource, PositionTitle, DepartmentID, HiringManagerEmployeeID,
  CandidateName, PersonalEmail, PersonalPhone, ApplicationDate, InterviewDate, InterviewStatus,
  SelectionDate, OfferLetterNumber, OfferedSalary, OfferDate, OfferAcceptedDate,
  BackgroundVerificationStatus, ReferenceCheckStatus, OnboardingStatus, JoiningDate,
  InductionCompleted, InductionDate, DocumentsSubmitted, SystemAccessProvided,
  OfficialEmailCreated, EquipmentIssued, AssetDetails, TrainingScheduleAssigned,
  BuddyMentorEmployeeID, ProbationPeriod, ProbationReviewSchedule, ConfirmationStatus,
  CreatedOn, CreatedByUserID)
VALUES
 (@JobRequisitionNumber, @RecruitmentSource, @PositionTitle, @DepartmentID, @HiringManagerEmployeeID,
  @CandidateName, @PersonalEmail, @PersonalPhone, @ApplicationDate, @InterviewDate, @InterviewStatus,
  @SelectionDate, @OfferLetterNumber, @OfferedSalary, @OfferDate, @OfferAcceptedDate,
  @BackgroundVerificationStatus, @ReferenceCheckStatus, @OnboardingStatus, @JoiningDate,
  @InductionCompleted, @InductionDate, @DocumentsSubmitted, @SystemAccessProvided,
  @OfficialEmailCreated, @EquipmentIssued, @AssetDetails, @TrainingScheduleAssigned,
  @BuddyMentorEmployeeID, @ProbationPeriod, @ProbationReviewSchedule, @ConfirmationStatus,
  GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.RecruitmentID = (int)ins.ExecuteScalar();
            }
        }

        private static void BindParams(SqlCommand cmd, RecruitmentInput input)
        {
            cmd.Parameters.AddWithValue("@JobRequisitionNumber", NullStr(input.JobRequisitionNumber));
            cmd.Parameters.AddWithValue("@RecruitmentSource", NullStr(input.RecruitmentSource));
            cmd.Parameters.AddWithValue("@PositionTitle", NullStr(input.PositionTitle));
            cmd.Parameters.AddWithValue("@DepartmentID", input.DepartmentID > 0 ? (object)input.DepartmentID : DBNull.Value);
            cmd.Parameters.AddWithValue("@HiringManagerEmployeeID", input.HiringManagerEmployeeID > 0 ? (object)input.HiringManagerEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@CandidateName", input.CandidateName.Trim());
            cmd.Parameters.AddWithValue("@PersonalEmail", NullStr(input.PersonalEmail));
            cmd.Parameters.AddWithValue("@PersonalPhone", NullStr(input.PersonalPhone));
            cmd.Parameters.AddWithValue("@ApplicationDate", ParseDate(input.ApplicationDate));
            cmd.Parameters.AddWithValue("@InterviewDate", ParseDate(input.InterviewDate));
            cmd.Parameters.AddWithValue("@InterviewStatus", NullStr(input.InterviewStatus));
            cmd.Parameters.AddWithValue("@SelectionDate", ParseDate(input.SelectionDate));
            cmd.Parameters.AddWithValue("@OfferLetterNumber", NullStr(input.OfferLetterNumber));
            cmd.Parameters.AddWithValue("@OfferedSalary", ParseDecimal(input.OfferedSalary));
            cmd.Parameters.AddWithValue("@OfferDate", ParseDate(input.OfferDate));
            cmd.Parameters.AddWithValue("@OfferAcceptedDate", ParseDate(input.OfferAcceptedDate));
            cmd.Parameters.AddWithValue("@BackgroundVerificationStatus", NullStr(input.BackgroundVerificationStatus));
            cmd.Parameters.AddWithValue("@ReferenceCheckStatus", NullStr(input.ReferenceCheckStatus));
            cmd.Parameters.AddWithValue("@OnboardingStatus", NullStr(input.OnboardingStatus));
            cmd.Parameters.AddWithValue("@JoiningDate", ParseDate(input.JoiningDate));
            cmd.Parameters.AddWithValue("@InductionCompleted", input.InductionCompleted);
            cmd.Parameters.AddWithValue("@InductionDate", ParseDate(input.InductionDate));
            cmd.Parameters.AddWithValue("@DocumentsSubmitted", input.DocumentsSubmitted);
            cmd.Parameters.AddWithValue("@SystemAccessProvided", input.SystemAccessProvided);
            cmd.Parameters.AddWithValue("@OfficialEmailCreated", NullStr(input.OfficialEmailCreated));
            cmd.Parameters.AddWithValue("@EquipmentIssued", input.EquipmentIssued);
            cmd.Parameters.AddWithValue("@AssetDetails", NullStr(input.AssetDetails));
            cmd.Parameters.AddWithValue("@TrainingScheduleAssigned", input.TrainingScheduleAssigned);
            cmd.Parameters.AddWithValue("@BuddyMentorEmployeeID", input.BuddyMentorEmployeeID > 0 ? (object)input.BuddyMentorEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ProbationPeriod", NullStr(input.ProbationPeriod));
            cmd.Parameters.AddWithValue("@ProbationReviewSchedule", ParseDate(input.ProbationReviewSchedule));
            cmd.Parameters.AddWithValue("@ConfirmationStatus", NullStr(input.ConfirmationStatus));
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RecruitmentID, ISNULL(r.JobRequisitionNumber,''), r.CandidateName,
       ISNULL(r.PositionTitle,''), ISNULL(d.DepartmentName,''), ISNULL(r.RecruitmentSource,''),
       ISNULL(r.InterviewStatus,''), r.JoiningDate, ISNULL(r.OnboardingStatus,'')
FROM tblRecruitment r
LEFT JOIN tblDepartment d ON d.DepartmentID=r.DepartmentID
ORDER BY r.RecruitmentID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new RecruitmentListItem
                        {
                            RecruitmentID = dr.GetInt32(0),
                            JobRequisitionNumber = dr.GetString(1),
                            CandidateName = dr.GetString(2),
                            PositionTitle = dr.GetString(3),
                            DepartmentName = dr.GetString(4),
                            RecruitmentSource = dr.GetString(5),
                            InterviewStatus = dr.GetString(6),
                            JoiningDate = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7),
                            OnboardingStatus = dr.GetString(8)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT RecruitmentID, JobRequisitionNumber, RecruitmentSource, PositionTitle, DepartmentID,
       HiringManagerEmployeeID, CandidateName, PersonalEmail, PersonalPhone, ApplicationDate,
       InterviewDate, InterviewStatus, SelectionDate, OfferLetterNumber, OfferedSalary,
       OfferDate, OfferAcceptedDate, BackgroundVerificationStatus, ReferenceCheckStatus,
       OnboardingStatus, JoiningDate, InductionCompleted, InductionDate, DocumentsSubmitted,
       SystemAccessProvided, OfficialEmailCreated, EquipmentIssued, AssetDetails,
       TrainingScheduleAssigned, BuddyMentorEmployeeID, ProbationPeriod,
       ProbationReviewSchedule, ConfirmationStatus
FROM tblRecruitment WHERE RecruitmentID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new RecruitmentInput
                    {
                        RecruitmentID = dr.GetInt32(0),
                        JobRequisitionNumber = Str(dr, 1),
                        RecruitmentSource = Str(dr, 2),
                        PositionTitle = Str(dr, 3),
                        DepartmentID = dr.IsDBNull(4) ? 0 : dr.GetInt32(4),
                        HiringManagerEmployeeID = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
                        CandidateName = dr.GetString(6),
                        PersonalEmail = Str(dr, 7),
                        PersonalPhone = Str(dr, 8),
                        ApplicationDate = DateStr(dr, 9),
                        InterviewDate = DateStr(dr, 10),
                        InterviewStatus = Str(dr, 11),
                        SelectionDate = DateStr(dr, 12),
                        OfferLetterNumber = Str(dr, 13),
                        OfferedSalary = dr.IsDBNull(14) ? "" : dr.GetDecimal(14).ToString("0.##"),
                        OfferDate = DateStr(dr, 15),
                        OfferAcceptedDate = DateStr(dr, 16),
                        BackgroundVerificationStatus = Str(dr, 17),
                        ReferenceCheckStatus = Str(dr, 18),
                        OnboardingStatus = Str(dr, 19),
                        JoiningDate = DateStr(dr, 20),
                        InductionCompleted = !dr.IsDBNull(21) && dr.GetBoolean(21),
                        InductionDate = DateStr(dr, 22),
                        DocumentsSubmitted = !dr.IsDBNull(23) && dr.GetBoolean(23),
                        SystemAccessProvided = !dr.IsDBNull(24) && dr.GetBoolean(24),
                        OfficialEmailCreated = Str(dr, 25),
                        EquipmentIssued = !dr.IsDBNull(26) && dr.GetBoolean(26),
                        AssetDetails = Str(dr, 27),
                        TrainingScheduleAssigned = !dr.IsDBNull(28) && dr.GetBoolean(28),
                        BuddyMentorEmployeeID = dr.IsDBNull(29) ? 0 : dr.GetInt32(29),
                        ProbationPeriod = Str(dr, 30),
                        ProbationReviewSchedule = DateStr(dr, 31),
                        ConfirmationStatus = Str(dr, 32)
                    };
                }
            }
        }

        private void LoadLookups()
        {
            Departments = new List<LookupItem>();
            Employees = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM tblDepartment WHERE IsActive=1 ORDER BY DepartmentName;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Departments.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });

                using (var cmd = new SqlCommand(@"
SELECT EmployeeID, EmployeeCode, FirstName, LastName FROM tblEmployee WHERE Status='Active' ORDER BY FirstName, LastName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        var name = (dr.GetString(2) + " " + dr.GetString(3)).Trim();
                        Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = string.IsNullOrEmpty(code) ? name : code + " – " + name });
                    }
                }
            }
        }

        private static object NullStr(string v) => string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v.Trim();
        private static object ParseDate(string v) => string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : DateTime.Parse(v);
        private static object ParseDecimal(string v) => string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : decimal.Parse(v);
        private static string Str(SqlDataReader dr, int i) => dr.IsDBNull(i) ? "" : dr.GetString(i);
        private static string DateStr(SqlDataReader dr, int i) => dr.IsDBNull(i) ? "" : dr.GetDateTime(i).ToString("yyyy-MM-dd");
    }
}
