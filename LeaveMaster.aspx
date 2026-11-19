<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LeaveMaster.aspx.cs" Inherits="HRMS.LeaveMasterPage" %>
<%@ Import Namespace="HRMS.Services" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.leave-app-title { color: #1e3a5f; font-size: 1.35rem; font-weight: 700; margin: 0 0 1rem; }
.leave-section { margin-bottom: 1.25rem; }
.leave-section-title {
  font-size: .95rem; font-weight: 600; color: #1e3a5f;
  margin: 0 0 .65rem; padding-bottom: .35rem; border-bottom: 1px solid #e5e7eb;
}
.leave-grid-2 {
  display: grid; grid-template-columns: 1fr 1fr; gap: .75rem 1.25rem;
}
.leave-grid-3 {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: .75rem 1.25rem;
}
.leave-field { display: flex; flex-direction: column; gap: .25rem; }
.leave-field label { font-size: .8rem; font-weight: 600; color: #374151; }
.leave-field .hint { font-size: .72rem; color: #6b7280; }
.readonly-field {
  background: #f3f4f6 !important; color: #374151; cursor: not-allowed;
}
.leave-inline {
  display: flex; flex-wrap: wrap; align-items: flex-end; gap: .75rem 1rem;
}
.leave-inline .leave-field { min-width: 140px; flex: 1; }
.resp-row {
  display: grid; grid-template-columns: 1fr 1.4fr; gap: .5rem .75rem; margin-bottom: .65rem;
}
@media (max-width: 768px) {
  .leave-grid-2, .leave-grid-3, .resp-row { grid-template-columns: 1fr; }
}
.leave-badge {
  display: inline-block; padding: 2px 10px; border-radius: 999px;
  font-size: .72rem; font-weight: 700;
}
.leave-pending-dh { background: #fef3c7; color: #92400e; }
.leave-pending-hr { background: #dbeafe; color: #1d4ed8; }
.leave-approved { background: #dcfce7; color: #166534; }
.leave-rejected { background: #fee2e2; color: #991b1b; }
.leave-inbox { display: flex; flex-wrap: wrap; gap: .4rem; margin-bottom: 1rem; }
.leave-inbox a {
  padding: 5px 12px; border: 1px solid #d1d5db; border-radius: 4px; font-size: .8rem;
  text-decoration: none; color: #1e3a5f; background: #fff;
}
.leave-inbox a.active { background: #1e3a5f; color: #fff; border-color: #1e3a5f; }
.hist-table { width: 100%; border-collapse: collapse; font-size: .82rem; }
.hist-table th, .hist-table td {
  padding: .45rem .55rem; border-bottom: 1px solid #e5e7eb; text-align: left; vertical-align: top;
}
.hist-table th { background: #f3f4f6; color: #374151; }
.leave-alert { background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: .65rem .85rem; margin-bottom: .75rem; }

</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<% if (LeaveAlerts.Count > 0) { %>
  <% foreach (var a in LeaveAlerts) { %>
  <div class="leave-alert">
    <strong><%= Server.HtmlEncode(a.Title) %></strong>
    &mdash; <%= Server.HtmlEncode(a.Message) %>
    <a href="<%= ResolveUrl(a.LinkUrl) %>">Open</a>
  </div>
  <% } %>
<% } %>
<div class="card">
  <div class="card-header space-between">
    <h2>Leave Applications</h2>
    <a href="/LeaveMaster.aspx?newLeave=1" class="btn btn-primary">+ New Leave Application</a>
  </div>
  <div class="card-body table-responsive">
    <div class="leave-inbox">
      <a class="<%= Inbox == "all" ? "active" : "" %>" href="/LeaveMaster.aspx?inbox=all">All</a>
      <a class="<%= Inbox == "mine" ? "active" : "" %>" href="/LeaveMaster.aspx?inbox=mine">My requests</a>
      <a class="<%= Inbox == "departmentHead" ? "active" : "" %>" href="/LeaveMaster.aspx?inbox=departmentHead">Department Head inbox</a>
      <% if (IsHrApprover) { %>
      <a class="<%= Inbox == "hr" ? "active" : "" %>" href="/LeaveMaster.aspx?inbox=hr">HR Manager inbox</a>
      <% } %>
    </div>
    <table class="data-table" id="leaveTable">
      <thead>
        <tr>
          <th>#</th><th>Number</th><th>Applied</th><th>Employee</th><th>Type</th>
          <th>Category</th><th>From</th><th>To</th><th>Days</th><th>Status</th><th></th>
        </tr>
      </thead>
      <tbody>
      <% if (Leaves.Count == 0) { %>
        <tr><td colspan="11" style="text-align:center;padding:1.5rem;color:#6b7280;">No leave applications found.</td></tr>
      <% } else {
           for (var i = 0; i < Leaves.Count; i++) {
             var l = Leaves[i]; %>
        <tr>
          <td><%= i + 1 %></td>
          <td><strong>LV-<%= l.LeaveID.ToString("D7") %></strong></td>
          <td><%= l.ApplyingDate.ToString("dd MMM yyyy") %></td>
          <td><%= Server.HtmlEncode(l.EmployeeCode) %><br /><span style="font-size:.8rem;color:#6b7280;"><%= Server.HtmlEncode(l.EmployeeName) %></span></td>
          <td><%= Server.HtmlEncode(l.LeaveType) %></td>
          <td><%= Server.HtmlEncode(l.LeaveCategoryName) %></td>
          <td><%= l.LeaveFromDate.ToString("dd MMM yyyy") %></td>
          <td><%= l.LeaveToDate.ToString("dd MMM yyyy") %></td>
          <td><%= l.NumberOfDays %></td>
          <td><span class="<%= l.StatusBadgeClass %>"><%= Server.HtmlEncode(l.StatusDisplay) %></span></td>
          <td style="white-space:nowrap;">
            <a class="btn btn-secondary" style="padding:3px 10px;font-size:.78rem;" href="/LeaveMaster.aspx?editId=<%= l.LeaveID %>"><%= l.Status == "PendingDepartmentHead" || l.Status == "PendingHR" ? "Review" : "Open" %></a>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>

<div class="form-breadcrumb">
  <a href="/LeaveMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Leave Application" : "New Leave Application" %></span>
  <% if (EditMode && !string.IsNullOrEmpty(Input.Status)) { %>
  <span class="<%= LeaveApprovalStatuses.BadgeClass(Input.Status) %>"><%= Server.HtmlEncode(LeaveApprovalStatuses.DisplayName(Input.Status)) %></span>
  <% } %>
</div>

<input type="hidden" name="LeaveID" value="<%= Input.LeaveID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<input type="hidden" name="NumberOfDays" id="hdnNumberOfDays" value="<%= Input.NumberOfDays %>" />

<div class="card">
  <div class="card-body">
    <h2 class="leave-app-title">Leave application form</h2>

    <!-- General -->
    <div class="leave-section">
      <div class="leave-grid-3">
        <div class="leave-field">
          <label>Number</label>
          <input type="text" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.ApplicationNumber) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Applying date</label>
          <input type="date" name="ApplyingDate" class="form-control" value="<%= Server.HtmlEncode(Input.ApplyingDate) %>" />
        </div>
        <div class="leave-field">
          <label>Personnel number</label>
          <input type="text" id="txtPersonnelNumber" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.EmployeeCode) %>" readonly />
        </div>
      </div>
    </div>

    <!-- Employee lookup + profile -->
    <div class="leave-section">
      <h3 class="leave-section-title">Employee</h3>
      <div class="leave-field" style="margin-bottom:.85rem;">
        <label>Employee *</label>
        <select name="EmployeeID" id="ddlEmployee" class="form-control"
                <%= LockEmployeeSelection ? "disabled" : "" %>>
          <option value="0">-- Select employee --</option>
          <% foreach (var e in Employees) { %>
          <option value="<%= e.Id %>" <%= Input.EmployeeID == e.Id ? "selected" : "" %>><%= Server.HtmlEncode(e.Name) %></option>
          <% } %>
        </select>
        <% if (LockEmployeeSelection) { %>
        <input type="hidden" name="EmployeeID" value="<%= Input.EmployeeID %>" />
        <span class="hint">Your linked employee profile is selected automatically.</span>
        <% } %>
      </div>

      <div class="leave-grid-2">
        <div class="leave-field">
          <label>Employee name</label>
          <input type="text" id="txtEmployeeName" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.EmployeeName) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Father name</label>
          <input type="text" id="txtFatherName" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.FatherName) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Division</label>
          <input type="text" id="txtDivision" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.Division) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Department</label>
          <input type="text" id="txtDepartment" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.Department) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Location</label>
          <input type="text" id="txtLocation" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.Location) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Designation</label>
          <input type="text" id="txtDesignation" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.Designation) %>" readonly />
        </div>
        <div class="leave-field">
          <label>Region</label>
          <input type="text" id="txtRegion" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmployeeInfo.Region) %>" readonly />
        </div>
      </div>
    </div>

    <!-- Leave type / category -->
    <div class="leave-section">
      <h3 class="leave-section-title">Leave details</h3>
      <div class="leave-grid-2">
        <div class="leave-field">
          <label>Leave type</label>
          <select name="LeaveType" id="ddlLeaveType" class="form-control">
            <% foreach (var t in LeaveTypes) { %>
            <option value="<%= t %>" <%= Input.LeaveType == t ? "selected" : "" %>><%= t %></option>
            <% } %>
          </select>
        </div>
        <div class="leave-field">
          <label class="checkbox-label" style="margin-top:1.4rem;">
            <input type="checkbox" name="IsFutureUnplannedLeave" value="true" <%= Input.IsFutureUnplannedLeave ? "checked" : "" %> />
            Future unplanned
          </label>
        </div>
        <div class="leave-field" style="grid-column:1/-1;">
          <label>Leave category *</label>
          <select name="LeaveCategoryID" id="ddlLeaveCategory" class="form-control">
            <option value="0">-- Select leave category --</option>
            <% foreach (var c in LeaveCategories) { %>
            <option value="<%= c.Id %>" <%= Input.LeaveCategoryID == c.Id ? "selected" : "" %>><%= Server.HtmlEncode(c.Name) %></option>
            <% } %>
          </select>
          <span class="hint">Categories are maintained in Leave Category Setup.</span>
        </div>
      </div>
    </div>

    <!-- Period -->
    <div class="leave-section">
      <h3 class="leave-section-title">Leave period</h3>
      <div class="leave-inline">
        <div class="leave-field">
          <label>From date *</label>
          <input type="date" name="LeaveFromDate" id="txtFromDate" class="form-control"
                 value="<%= Server.HtmlEncode(Input.LeaveFromDate) %>" />
        </div>
        <div class="leave-field">
          <label>To date *</label>
          <input type="date" name="LeaveToDate" id="txtToDate" class="form-control"
                 value="<%= Server.HtmlEncode(Input.LeaveToDate) %>" />
        </div>
        <div class="leave-field" style="max-width:120px;">
          <label>No of days</label>
          <input type="text" id="txtNumberOfDays" class="form-control readonly-field"
                 value="<%= Input.NumberOfDays > 0 ? Input.NumberOfDays.ToString() : "" %>" readonly />
        </div>
      </div>
      <div class="leave-field" style="margin-top:.85rem;">
        <label>Reason of leaving</label>
        <input type="text" name="ReasonForLeave" class="form-control" maxlength="1000"
               value="<%= Server.HtmlEncode(Input.ReasonForLeave) %>" />
      </div>
    </div>

    <!-- Task responsibility -->
    <div class="leave-section">
      <h3 class="leave-section-title">Who will be Responsible for tasks During Leave Period</h3>
      <div class="resp-row">
        <div class="leave-field">
          <label>Responsible-1</label>
          <select name="TempResponsibleEmployeeID" id="ddlResp1" class="form-control">
            <option value="0">-- Select --</option>
            <% foreach (var e in Employees) { %>
            <option value="<%= e.Id %>" <%= Input.TempResponsibleEmployeeID == e.Id ? "selected" : "" %>><%= Server.HtmlEncode(e.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="leave-field">
          <label>Employee name</label>
          <input type="text" id="txtResp1Name" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmpName(Input.TempResponsibleEmployeeID)) %>" readonly />
        </div>
      </div>
      <div class="resp-row">
        <div class="leave-field">
          <label>Responsible-2</label>
          <select name="PermanentResponsibleEmployeeID" id="ddlResp2" class="form-control">
            <option value="0">-- Select --</option>
            <% foreach (var e in Employees) { %>
            <option value="<%= e.Id %>" <%= Input.PermanentResponsibleEmployeeID == e.Id ? "selected" : "" %>><%= Server.HtmlEncode(e.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="leave-field">
          <label>Employee name</label>
          <input type="text" id="txtResp2Name" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(EmpName(Input.PermanentResponsibleEmployeeID)) %>" readonly />
        </div>
      </div>
    </div>
  </div>
  <div class="card-footer" style="display:flex;justify-content:flex-end;gap:.5rem;flex-wrap:wrap;">
    <% if (Input.EmployeeID > 0) { %>
    <a class="btn btn-secondary" href="<%= ResolveUrl("~/EmployeeAttendance.aspx?employeeId=" + Input.EmployeeID) %>">View Attendance</a>
    <% } %>
    <a href="/LeaveMaster.aspx" class="btn btn-secondary">Cancel</a>
    <% if (CanEditApplication) { %>
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return validateLeaveForm();">Submit</button>
    <% } %>
  </div>
</div>

<% if (EditMode) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>Approval</h2></div>
  <div class="card-body">
    <p class="hint" style="font-size:.8rem;color:#6b7280;margin-bottom:.75rem;">
      Leave is fully approved only after the Department Head and the HR Manager both approve.
      A rejection at either level ends the request and notifies the applicant.
    </p>
    <% if (CanApproveCurrent) { %>
    <div class="leave-field" style="margin-bottom:.75rem;">
      <label>Comments <% if (Input.Status == LeaveApprovalStatuses.PendingDepartmentHead || Input.Status == LeaveApprovalStatuses.PendingHR) { %>(required when rejecting)<% } %></label>
      <textarea name="approvalComments" class="form-control" rows="3" maxlength="1000"></textarea>
    </div>
    <div style="display:flex;gap:.5rem;flex-wrap:wrap;">
      <button type="submit" class="btn btn-primary"
              onclick="document.getElementById('__handler').value='Approve';">Approve</button>
      <button type="submit" class="btn btn-secondary"
              onclick="document.getElementById('__handler').value='Reject';return confirm('Reject this leave request? The applicant will be notified.');">Reject</button>
    </div>
    <% } else if (Input.Status == LeaveApprovalStatuses.PendingDepartmentHead) { %>
    <p>Waiting for Department Head approval.</p>
    <% } else if (Input.Status == LeaveApprovalStatuses.PendingHR) { %>
    <p>Waiting for HR Manager approval.</p>
    <% } %>

    <h3 class="leave-section-title" style="margin-top:1.25rem;">Approval history</h3>
    <table class="hist-table">
      <thead>
        <tr>
          <th>Date / time</th>
          <th>Stage</th>
          <th>Approver</th>
          <th>Action</th>
          <th>Comments</th>
        </tr>
      </thead>
      <tbody>
      <% if (ApprovalHistory.Count == 0) { %>
        <tr><td colspan="5">No approval actions yet.</td></tr>
      <% } else {
           foreach (var h in ApprovalHistory) { %>
        <tr>
          <td><%= h.ActionOn.ToString("dd MMM yyyy HH:mm") %></td>
          <td><%= Server.HtmlEncode(h.StageDisplay) %></td>
          <td><%= Server.HtmlEncode(h.ActorName) %></td>
          <td><%= Server.HtmlEncode(h.Action) %></td>
          <td><%= Server.HtmlEncode(string.IsNullOrEmpty(h.Comments) ? "\u2014" : h.Comments) %></td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>

<script>
(function () {
  var empUrl = '<%= ResolveUrl("~/LeaveMaster.aspx") %>?ajax=employeeInfo&employeeId=';

  function clearProfile() {
    ['txtPersonnelNumber','txtEmployeeName','txtFatherName','txtDivision','txtDepartment','txtLocation','txtDesignation','txtRegion']
      .forEach(function (id) { var el = document.getElementById(id); if (el) el.value = ''; });
  }

  function fillProfile(data) {
    if (!data) { clearProfile(); return; }
    var map = {
      txtPersonnelNumber: data.EmployeeCode,
      txtEmployeeName: data.EmployeeName,
      txtFatherName: data.FatherName,
      txtDivision: data.Division,
      txtDepartment: data.Department,
      txtLocation: data.Location,
      txtDesignation: data.Designation,
      txtRegion: data.Region
    };
    Object.keys(map).forEach(function (id) {
      var el = document.getElementById(id);
      if (el) el.value = map[id] || '';
    });
  }

  function loadEmployee(id) {
    if (!id || id === '0') { clearProfile(); return; }
    var xhr = new XMLHttpRequest();
    xhr.open('GET', empUrl + encodeURIComponent(id), true);
    xhr.onreadystatechange = function () {
      if (xhr.readyState !== 4) return;
      if (xhr.status >= 200 && xhr.status < 300) {
        try { fillProfile(JSON.parse(xhr.responseText)); }
        catch (e) { clearProfile(); }
      }
    };
    xhr.send();
  }

  function calcDays() {
    var fromEl = document.getElementById('txtFromDate');
    var toEl = document.getElementById('txtToDate');
    var daysEl = document.getElementById('txtNumberOfDays');
    var hdn = document.getElementById('hdnNumberOfDays');
    if (!fromEl || !toEl || !daysEl) return;
    if (!fromEl.value || !toEl.value) { daysEl.value = ''; if (hdn) hdn.value = '0'; return; }
    var from = new Date(fromEl.value + 'T00:00:00');
    var to = new Date(toEl.value + 'T00:00:00');
    if (isNaN(from.getTime()) || isNaN(to.getTime()) || to < from) {
      daysEl.value = '';
      if (hdn) hdn.value = '0';
      return;
    }
    var days = Math.floor((to - from) / 86400000) + 1;
    daysEl.value = String(days);
    if (hdn) hdn.value = String(days);
  }

  function syncRespName(ddlId, txtId) {
    var ddl = document.getElementById(ddlId);
    var txt = document.getElementById(txtId);
    if (!ddl || !txt) return;
    var opt = ddl.options[ddl.selectedIndex];
    txt.value = (!opt || opt.value === '0') ? '' : opt.text;
  }

  var emp = document.getElementById('ddlEmployee');
  if (emp) emp.addEventListener('change', function () { loadEmployee(this.value); });

  var fromEl = document.getElementById('txtFromDate');
  var toEl = document.getElementById('txtToDate');
  if (fromEl) fromEl.addEventListener('change', calcDays);
  if (toEl) toEl.addEventListener('change', calcDays);

  var r1 = document.getElementById('ddlResp1');
  var r2 = document.getElementById('ddlResp2');
  if (r1) r1.addEventListener('change', function () { syncRespName('ddlResp1', 'txtResp1Name'); });
  if (r2) r2.addEventListener('change', function () { syncRespName('ddlResp2', 'txtResp2Name'); });

  calcDays();
})();

function validateLeaveForm() {
  var emp = document.getElementById('ddlEmployee');
  var cat = document.getElementById('ddlLeaveCategory');
  var fromEl = document.getElementById('txtFromDate');
  var toEl = document.getElementById('txtToDate');
  var empVal = emp ? emp.value : (document.querySelector('input[name="EmployeeID"]') || {}).value;
  if (!empVal || empVal === '0') { alert('Please select an employee.'); return false; }
  if (!cat || cat.value === '0') { alert('Please select a leave category.'); return false; }
  if (!fromEl || !fromEl.value || !toEl || !toEl.value) { alert('Please enter leave from and to dates.'); return false; }
  if (toEl.value < fromEl.value) { alert('To date cannot be before from date.'); return false; }
  return true;
}
</script>
<% } %>
</asp:Content>
