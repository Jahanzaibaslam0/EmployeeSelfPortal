<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PositionMaster.aspx.cs" Inherits="HRMS.PositionMasterPage" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.pos-section { margin-top: 1.25rem; }
.pos-section-title {
  font-size: .95rem; font-weight: 600; color: #1e3a5f;
  margin: 0 0 .65rem; padding-bottom: .35rem; border-bottom: 1px solid #e5e7eb;
}
.pos-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem 1.25rem; }
.pos-grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: .75rem 1.25rem; }
.pos-hint { font-size: .75rem; color: #6b7280; margin: .25rem 0 .5rem; }
.pos-badge {
  display: inline-block; padding: 2px 10px; border-radius: 999px;
  font-size: .72rem; font-weight: 700;
}
.pos-badge-active { background: #dcfce7; color: #166534; }
.pos-badge-closed { background: #e5e7eb; color: #374151; }
.pos-badge-vacant { background: #fef3c7; color: #92400e; }
.pos-active-box {
  background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px;
  padding: .85rem 1rem; margin-bottom: 1rem;
}
.hist-table { width: 100%; border-collapse: collapse; font-size: .82rem; }
.hist-table th, .hist-table td {
  padding: .45rem .55rem; border-bottom: 1px solid #e5e7eb; text-align: left; vertical-align: top;
}
.hist-table th { background: #f3f4f6; color: #374151; }
@media (max-width: 768px) {
  .pos-grid-2, .pos-grid-3 { grid-template-columns: 1fr; }
}
</style>
<script type="text/javascript">
function toggleCloseOther() {
  var sel = document.getElementById('closeReason');
  var other = document.getElementById('closeReasonOtherWrap');
  if (!sel || !other) return;
  other.style.display = (sel.value === 'Other') ? 'block' : 'none';
}

/** Inclusive overlap: startA <= endB && startB <= endA; null/empty end = open-ended. */
function positionPeriodsOverlap(startA, endA, startB, endB) {
  function parseYmd(v) {
    if (!v) return null;
    var p = String(v).substring(0, 10).split('-');
    if (p.length !== 3) return null;
    return new Date(parseInt(p[0], 10), parseInt(p[1], 10) - 1, parseInt(p[2], 10));
  }
  var a0 = parseYmd(startA), b0 = parseYmd(startB);
  if (!a0 || !b0) return false;
  var a1 = parseYmd(endA) || new Date(9999, 11, 31);
  var b1 = parseYmd(endB) || new Date(9999, 11, 31);
  return a0 <= b1 && b0 <= a1;
}

function validateAssignBeforeSubmit() {
  var emp = document.querySelector('[name="assignEmployeeID"]');
  var startEl = document.querySelector('[name="assignStartDate"]');
  if (!emp || !startEl) return false;
  if (!emp.value || emp.value === '0') {
    alert('Select an employee from Employee Master.');
    return false;
  }
  if (!startEl.value) {
    alert('Assignment start date is required.');
    return false;
  }
  var periods = window.__positionAssignmentPeriods || [];
  for (var i = 0; i < periods.length; i++) {
    var p = periods[i];
    if (positionPeriodsOverlap(startEl.value, null, p.start, p.end)) {
      alert('This position is already assigned to another employee during the selected period. Please select a start date after the current assignment ends or close the existing assignment first.');
      return false;
    }
  }
  document.getElementById('__handler').value = 'Assign';
  return true;
}
</script>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="closeAssignmentId" id="closeAssignmentId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Positions</h2>
    <a href="/PositionMaster.aspx?newPosition=1" class="btn btn-primary">+ New Position</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>No</th><th>Description</th><th>Job</th><th>Dept</th><th>Current Assignee</th><th>Type</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var p in Positions) { %>
      <tr>
        <td><%= Server.HtmlEncode(p.PositionNo) %></td>
        <td><%= Server.HtmlEncode(p.Description) %></td>
        <td><%= Server.HtmlEncode(p.JobTitle) %></td>
        <td><%= Server.HtmlEncode(p.DepartmentName) %></td>
        <td>
          <% if (!string.IsNullOrEmpty(p.CurrentAssignee)) { %>
            <%= Server.HtmlEncode(p.CurrentAssignee) %>
          <% } else { %>
            <span class="pos-badge pos-badge-vacant">Vacant</span>
          <% } %>
        </td>
        <td><%= Server.HtmlEncode(p.PositionTypeName) %></td>
        <td><%= p.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/PositionMaster.aspx?editId=<%= p.PositionID %>">Edit</a>
          <% if (p.IsActive) { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= p.PositionID %>';return confirm('Deactivate?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Positions.Count == 0) { %><tr><td colspan="8">No positions found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/PositionMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Position" : "New Position" %> <%= Server.HtmlEncode(Input.PositionNo) %></span>
</div>
<input type="hidden" name="positionID" value="<%= Input.PositionID %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Position" : "Add Position" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Position No *</label>
        <input type="text" name="positionNo" class="form-control" value="<%= Server.HtmlEncode(Input.PositionNo) %>" /></div>
      <div class="form-group"><label>Description</label>
        <input type="text" name="description" class="form-control" value="<%= Server.HtmlEncode(Input.Description) %>" /></div>
      <div class="form-group"><label>Email (Employee)</label>
        <select name="emailEmployeeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var e in EmailLookups) { %><option value="<%= e.EmployeeID %>" <%= Input.EmailEmployeeID==e.EmployeeID?"selected":"" %>><%= Server.HtmlEncode(e.Label) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Job</label>
        <select name="jobID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var j in Jobs) { %><option value="<%= j.Id %>" <%= Input.JobID==j.Id?"selected":"" %>><%= Server.HtmlEncode(j.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Department</label>
        <select name="departmentID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var d in Departments) { %><option value="<%= d.Id %>" <%= Input.DepartmentID==d.Id?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Reports To</label>
        <select name="reportsToPositionID" class="form-control"><option value="0">-- None --</option>
        <% foreach (var r in ReportToPositions) { %><option value="<%= r.Id %>" <%= Input.ReportsToPositionID==r.Id?"selected":"" %>><%= Server.HtmlEncode(r.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Title</label>
        <select name="titleID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var t in Titles) { %><option value="<%= t.Id %>" <%= Input.TitleID==t.Id?"selected":"" %>><%= Server.HtmlEncode(t.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Position Type</label>
        <select name="positionTypeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var t in PositionTypes) { %><option value="<%= t.Id %>" <%= Input.PositionTypeID==t.Id?"selected":"" %>><%= Server.HtmlEncode(t.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Duration</label>
        <select name="positionDuration" class="form-control"><option value="">--</option>
        <% foreach (var d in PositionDurations) { %><option value="<%= d %>" <%= Input.PositionDuration==d?"selected":"" %>><%= d %></option><% } %>
        </select></div>
      <div class="form-group"><label>Start Date</label>
        <input type="date" name="positionStartDate" class="form-control" value="<%= Server.HtmlEncode(Input.PositionStartDate) %>" /></div>
      <div class="form-group"><label>End Date</label>
        <input type="date" name="positionEndDate" class="form-control" value="<%= Server.HtmlEncode(Input.PositionEndDate) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save Position</button>
    <a href="/PositionMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>

<% if (EditMode) { %>
<div class="card pos-section">
  <div class="card-header"><h2>Current Assignment</h2></div>
  <div class="card-body">
    <p class="pos-hint">Each position can have only one active employee at a time. Closing an assignment records the end date and keeps history; assign a new employee creates a new tenure record.</p>

    <% if (ActiveAssignment != null) { %>
    <div class="pos-active-box">
      <div class="pos-grid-3">
        <div><strong>Employee</strong><br /><%= Server.HtmlEncode(ActiveAssignment.EmployeeLabel) %></div>
        <div><strong>Start date</strong><br /><%= Server.HtmlEncode(ActiveAssignment.StartDateDisplay) %></div>
        <div><strong>Tenure</strong><br /><%= Server.HtmlEncode(ActiveAssignment.TenureDisplay) %>
          <span class="pos-badge pos-badge-active">Active</span></div>
      </div>
    </div>

    <% if (CanWrite) { %>
    <h3 class="pos-section-title">Close current assignment</h3>
    <p class="pos-hint">Use when the employee goes on leave, is promoted, transferred, resigns, or otherwise vacates the position.</p>
    <div class="pos-grid-3">
      <div class="form-group">
        <label>End date *</label>
        <input type="date" name="closeEndDate" class="form-control" value="<%= DateTime.Today.ToString("yyyy-MM-dd") %>" />
      </div>
      <div class="form-group">
        <label>Reason for leaving *</label>
        <select name="closeReason" id="closeReason" class="form-control" onchange="toggleCloseOther();">
          <option value="">-- Select --</option>
          <% foreach (var r in LeaveReasons) { %>
          <option value="<%= Server.HtmlEncode(r) %>"><%= Server.HtmlEncode(r) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group" id="closeReasonOtherWrap" style="display:none;">
        <label>Other reason</label>
        <input type="text" name="closeReasonOther" class="form-control" maxlength="400" />
      </div>
    </div>
    <div style="margin-top:.75rem;">
      <button type="submit" class="btn btn-secondary"
              onclick="document.getElementById('__handler').value='CloseAssignment';document.getElementById('closeAssignmentId').value='<%= ActiveAssignment.PositionWorkerAssignmentID %>';return confirm('Close this assignment and free the position?');">
        Close Assignment
      </button>
    </div>
    <% } %>

    <% } else { %>
    <div class="pos-active-box">
      <span class="pos-badge pos-badge-vacant">Vacant</span>
      &mdash; No active employee assigned to this position.
    </div>

    <% if (CanWrite) { %>
    <h3 class="pos-section-title">Assign employee</h3>
    <p class="pos-hint">Start date must not overlap any period in Position History. After closing an assignment, choose a start date after that end date.</p>
    <script type="text/javascript">
      window.__positionAssignmentPeriods = [
      <% for (var hi = 0; hi < AssignmentHistory.Count; hi++) {
           var h = AssignmentHistory[hi];
           var startJs = h.AssignmentStartDate.HasValue ? h.AssignmentStartDate.Value.ToString("yyyy-MM-dd") : "";
           var endJs = h.AssignmentEndDate.HasValue ? h.AssignmentEndDate.Value.ToString("yyyy-MM-dd") : "";
      %>
        { start: '<%= startJs %>', end: <%= string.IsNullOrEmpty(endJs) ? "null" : ("'" + endJs + "'") %> }<%= hi < AssignmentHistory.Count - 1 ? "," : "" %>
      <% } %>
      ];
    </script>
    <div class="pos-grid-3">
      <div class="form-group">
        <label>Employee (from Employee Master) *</label>
        <select name="assignEmployeeID" class="form-control">
          <option value="0">-- Select employee --</option>
          <% foreach (var e in EmployeeLookups) { %>
          <option value="<%= e.EmployeeID %>"><%= Server.HtmlEncode(e.Label) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Assignment start date *</label>
        <input type="date" name="assignStartDate" class="form-control" value="<%= SuggestedAssignStartDate %>" />
      </div>
    </div>
    <div style="margin-top:.75rem;">
      <button type="submit" class="btn btn-primary"
              onclick="return validateAssignBeforeSubmit();">
        Assign Employee
      </button>
    </div>
    <% } %>
    <% } %>
  </div>
</div>

<div class="card pos-section">
  <div class="card-header"><h2>Position History</h2></div>
  <div class="card-body table-responsive">
    <table class="hist-table">
      <thead>
        <tr>
          <th>Position</th>
          <th>Employee</th>
          <th>Start date</th>
          <th>End date</th>
          <th>Tenure</th>
          <th>Reason for leaving</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
      <% foreach (var h in AssignmentHistory) { %>
        <tr>
          <td><%= Server.HtmlEncode(h.PositionNo) %><% if (!string.IsNullOrEmpty(h.PositionDescription)) { %> &mdash; <%= Server.HtmlEncode(h.PositionDescription) %><% } %></td>
          <td><%= Server.HtmlEncode(h.EmployeeLabel) %></td>
          <td><%= Server.HtmlEncode(h.StartDateDisplay) %></td>
          <td><%= Server.HtmlEncode(h.EndDateDisplay) %></td>
          <td><%= Server.HtmlEncode(h.TenureDisplay) %></td>
          <td><%= Server.HtmlEncode(string.IsNullOrEmpty(h.Reason) ? "\u2014" : h.Reason) %></td>
          <td>
            <% if (h.IsActive) { %><span class="pos-badge pos-badge-active">Active</span>
            <% } else { %><span class="pos-badge pos-badge-closed">Closed</span><% } %>
          </td>
        </tr>
      <% } %>
      <% if (AssignmentHistory.Count == 0) { %>
        <tr><td colspan="7">No assignment history for this position yet.</td></tr>
      <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>
<% } %>
</asp:Content>
