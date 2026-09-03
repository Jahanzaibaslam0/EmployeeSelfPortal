<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecruitmentMaster.aspx.cs" Inherits="HRMS.RecruitmentMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Recruitment Records</h2>
    <a href="/RecruitmentMaster.aspx?newRecord=1" class="btn btn-primary">+ New Record</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Req No</th><th>Candidate</th><th>Position</th><th>Dept</th><th>Source</th><th>Interview</th><th>Joining</th><th>Onboarding</th><th></th></tr></thead>
      <tbody>
      <% foreach (var r in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(r.JobRequisitionNumber) %></td>
        <td><%= Server.HtmlEncode(r.CandidateName) %></td>
        <td><%= Server.HtmlEncode(r.PositionTitle) %></td>
        <td><%= Server.HtmlEncode(r.DepartmentName) %></td>
        <td><%= Server.HtmlEncode(r.RecruitmentSource) %></td>
        <td><%= Server.HtmlEncode(r.InterviewStatus) %></td>
        <td><%= r.JoiningDate.HasValue ? r.JoiningDate.Value.ToString("dd MMM yyyy") : "" %></td>
        <td><%= Server.HtmlEncode(r.OnboardingStatus) %></td>
        <td>
          <a href="/RecruitmentMaster.aspx?editId=<%= r.RecruitmentID %>">Edit</a>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= r.RecruitmentID %>';return confirm('Delete?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="9">No records found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/RecruitmentMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Recruitment" : "New Recruitment" %></span>
</div>
<input type="hidden" name="RecruitmentID" value="<%= Input.RecruitmentID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Recruitment" : "Add Recruitment" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Job Requisition No</label>
        <input type="text" name="JobRequisitionNumber" class="form-control" value="<%= Server.HtmlEncode(Input.JobRequisitionNumber) %>" /></div>
      <div class="form-group"><label>Source</label>
        <select name="RecruitmentSource" class="form-control"><option value="">--</option>
        <% foreach (var s in RecruitmentSourceOptions) { %><option value="<%= s %>" <%= Input.RecruitmentSource==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Position Title</label>
        <input type="text" name="PositionTitle" class="form-control" value="<%= Server.HtmlEncode(Input.PositionTitle) %>" /></div>
      <div class="form-group"><label>Department</label>
        <select name="DepartmentID" class="form-control"><option value="0">--</option>
        <% foreach (var d in Departments) { %><option value="<%= d.Id %>" <%= Input.DepartmentID==d.Id?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Hiring Manager</label>
        <select name="HiringManagerEmployeeID" class="form-control"><option value="0">--</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.HiringManagerEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Candidate Name *</label>
        <input type="text" name="CandidateName" class="form-control" value="<%= Server.HtmlEncode(Input.CandidateName) %>" required /></div>
      <div class="form-group"><label>Personal Email</label>
        <input type="text" name="PersonalEmail" class="form-control" value="<%= Server.HtmlEncode(Input.PersonalEmail) %>" /></div>
      <div class="form-group"><label>Personal Phone</label>
        <input type="text" name="PersonalPhone" class="form-control" value="<%= Server.HtmlEncode(Input.PersonalPhone) %>" /></div>
      <div class="form-group"><label>Application Date</label>
        <input type="date" name="ApplicationDate" class="form-control" value="<%= Server.HtmlEncode(Input.ApplicationDate) %>" /></div>
      <div class="form-group"><label>Interview Date</label>
        <input type="date" name="InterviewDate" class="form-control" value="<%= Server.HtmlEncode(Input.InterviewDate) %>" /></div>
      <div class="form-group"><label>Interview Status</label>
        <select name="InterviewStatus" class="form-control"><option value="">--</option>
        <% foreach (var s in InterviewStatusOptions) { %><option value="<%= s %>" <%= Input.InterviewStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Selection Date</label>
        <input type="date" name="SelectionDate" class="form-control" value="<%= Server.HtmlEncode(Input.SelectionDate) %>" /></div>
      <div class="form-group"><label>Offer Letter No</label>
        <input type="text" name="OfferLetterNumber" class="form-control" value="<%= Server.HtmlEncode(Input.OfferLetterNumber) %>" /></div>
      <div class="form-group"><label>Offered Salary</label>
        <input type="text" name="OfferedSalary" class="form-control" value="<%= Server.HtmlEncode(Input.OfferedSalary) %>" /></div>
      <div class="form-group"><label>Offer Date</label>
        <input type="date" name="OfferDate" class="form-control" value="<%= Server.HtmlEncode(Input.OfferDate) %>" /></div>
      <div class="form-group"><label>Offer Accepted</label>
        <input type="date" name="OfferAcceptedDate" class="form-control" value="<%= Server.HtmlEncode(Input.OfferAcceptedDate) %>" /></div>
      <div class="form-group"><label>Background Verification</label>
        <select name="BackgroundVerificationStatus" class="form-control"><option value="">--</option>
        <% foreach (var s in VerificationStatusOptions) { %><option value="<%= s %>" <%= Input.BackgroundVerificationStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Reference Check</label>
        <select name="ReferenceCheckStatus" class="form-control"><option value="">--</option>
        <% foreach (var s in VerificationStatusOptions) { %><option value="<%= s %>" <%= Input.ReferenceCheckStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Onboarding Status</label>
        <select name="OnboardingStatus" class="form-control"><option value="">--</option>
        <% foreach (var s in OnboardingStatusOptions) { %><option value="<%= s %>" <%= Input.OnboardingStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Joining Date</label>
        <input type="date" name="JoiningDate" class="form-control" value="<%= Server.HtmlEncode(Input.JoiningDate) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="InductionCompleted" value="true" <%= Input.InductionCompleted?"checked":"" %> /> Induction Completed</label></div>
      <div class="form-group"><label>Induction Date</label>
        <input type="date" name="InductionDate" class="form-control" value="<%= Server.HtmlEncode(Input.InductionDate) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="DocumentsSubmitted" value="true" <%= Input.DocumentsSubmitted?"checked":"" %> /> Documents Submitted</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="SystemAccessProvided" value="true" <%= Input.SystemAccessProvided?"checked":"" %> /> System Access</label></div>
      <div class="form-group"><label>Official Email</label>
        <input type="text" name="OfficialEmailCreated" class="form-control" value="<%= Server.HtmlEncode(Input.OfficialEmailCreated) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="EquipmentIssued" value="true" <%= Input.EquipmentIssued?"checked":"" %> /> Equipment Issued</label></div>
      <div class="form-group"><label>Asset Details</label>
        <input type="text" name="AssetDetails" class="form-control" value="<%= Server.HtmlEncode(Input.AssetDetails) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="TrainingScheduleAssigned" value="true" <%= Input.TrainingScheduleAssigned?"checked":"" %> /> Training Assigned</label></div>
      <div class="form-group"><label>Buddy / Mentor</label>
        <select name="BuddyMentorEmployeeID" class="form-control"><option value="0">--</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.BuddyMentorEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Probation Period</label>
        <select name="ProbationPeriod" class="form-control"><option value="">--</option>
        <% foreach (var s in ProbationPeriodOptions) { %><option value="<%= s %>" <%= Input.ProbationPeriod==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Probation Review</label>
        <input type="date" name="ProbationReviewSchedule" class="form-control" value="<%= Server.HtmlEncode(Input.ProbationReviewSchedule) %>" /></div>
      <div class="form-group"><label>Confirmation Status</label>
        <select name="ConfirmationStatus" class="form-control"><option value="">--</option>
        <% foreach (var s in ConfirmationStatusOptions) { %><option value="<%= s %>" <%= Input.ConfirmationStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <a href="/RecruitmentMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
