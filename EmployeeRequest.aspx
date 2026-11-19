<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.EmployeeRequestPage" %>
<%@ Import Namespace="HRMS.Services" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.req-title { color: #1e3a5f; font-size: 1.35rem; font-weight: 700; margin: 0 0 1rem; }
.req-section { margin-bottom: 1.25rem; }
.req-section-title {
  font-size: .95rem; font-weight: 600; color: #1e3a5f;
  margin: 0 0 .65rem; padding-bottom: .35rem; border-bottom: 1px solid #e5e7eb;
}
.req-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem 1.25rem; }
.req-grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: .75rem 1.25rem; }
.req-field { display: flex; flex-direction: column; gap: .25rem; }
.req-field label { font-size: .8rem; font-weight: 600; color: #374151; }
.req-field .hint { font-size: .72rem; color: #6b7280; }
.readonly-field { background: #f3f4f6 !important; color: #374151; cursor: not-allowed; }
.form-breadcrumb {
  display: flex; align-items: center; gap: .75rem; margin-bottom: 1rem;
  padding: .5rem .75rem; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 6px;
}
.req-inbox {
  display: flex; flex-wrap: wrap; gap: .4rem; margin-bottom: 1rem;
}
.req-inbox a {
  padding: 5px 12px; border: 1px solid #d1d5db; border-radius: 4px; font-size: .8rem;
  text-decoration: none; color: #1e3a5f; background: #fff;
}
.req-inbox a.active { background: #1e3a5f; color: #fff; border-color: #1e3a5f; }
.req-badge {
  display: inline-block; padding: 2px 10px; border-radius: 999px;
  font-size: .72rem; font-weight: 700; background: #e5e7eb; color: #374151;
}
.req-approved { background: #dcfce7; color: #166534; }
.req-rejected { background: #fee2e2; color: #b91c1c; }
.req-returned { background: #fef3c7; color: #92400e; }
.req-draft { background: #e5e7eb; color: #4b5563; }
.req-pending { background: #dbeafe; color: #1d4ed8; }
.req-actions { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: 1rem; }
.req-alerts {
  background: #fff7ed; border: 1px solid #fed7aa; border-radius: 6px;
  padding: .75rem 1rem; margin-bottom: 1rem;
}
.req-alerts h3 { margin: 0 0 .5rem; font-size: .9rem; color: #9a3412; }
.req-alerts a { display: block; font-size: .82rem; color: #1e3a5f; margin: .25rem 0; }
.audit-table { width: 100%; border-collapse: collapse; font-size: .82rem; }
.audit-table th, .audit-table td {
  padding: .45rem .55rem; border-bottom: 1px solid #e5e7eb; text-align: left;
}
.audit-table th { background: #f3f4f6; color: #374151; }
.cat-block { display: none; }
.cat-block.active { display: block; }
@media (max-width: 768px) {
  .req-grid-2, .req-grid-3 { grid-template-columns: 1fr; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>

<% if (Alerts != null && Alerts.Count > 0 && !ShowForm) { %>
<div class="req-alerts">
  <h3>Action required</h3>
  <% foreach (var a in Alerts) { %>
  <a href="<%= ResolveUrl(string.IsNullOrEmpty(a.LinkUrl) ? "~/EmployeeRequest.aspx?editId=" + a.RequestID : a.LinkUrl) %>">
    <%= Server.HtmlEncode(a.Title) %><% if (!string.IsNullOrEmpty(a.Message)) { %> &mdash; <%= Server.HtmlEncode(a.Message) %><% } %>
  </a>
  <% } %>
</div>
<% } %>

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Employee Requests</h2>
    <a href="<%= ResolveUrl("~/EmployeeRequest.aspx?new=1") %>" class="btn btn-primary">+ New Request</a>
  </div>
  <div class="card-body">
    <div class="req-inbox">
      <a class="<%= Inbox == "all" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeRequest.aspx?inbox=all") %>">All</a>
      <a class="<%= Inbox == "mine" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeRequest.aspx?inbox=mine") %>">My Requests</a>
      <a class="<%= Inbox == "manager" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeRequest.aspx?inbox=manager") %>">Manager Inbox</a>
      <% if (IsHr) { %>
      <a class="<%= Inbox == "hr" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeRequest.aspx?inbox=hr") %>">HR Inbox</a>
      <% } %>
      <% if (IsManagement) { %>
      <a class="<%= Inbox == "management" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeRequest.aspx?inbox=management") %>">Management Inbox</a>
      <% } %>
    </div>

    <div class="table-responsive">
      <table class="data-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Number</th>
            <th>Employee</th>
            <th>Category</th>
            <th>Subject</th>
            <th>Amount</th>
            <th>Status</th>
            <th>Submitted</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% if (Requests.Count == 0) { %>
          <tr><td colspan="9" style="text-align:center;padding:1.5rem;color:#6b7280;">No requests found.</td></tr>
        <% } else {
             for (var i = 0; i < Requests.Count; i++) {
               var r = Requests[i]; %>
          <tr>
            <td><%= i + 1 %></td>
            <td><strong><%= Server.HtmlEncode(r.RequestNumber) %></strong></td>
            <td>
              <%= Server.HtmlEncode(r.EmployeeCode) %><br />
              <span style="font-size:.8rem;color:#6b7280;"><%= Server.HtmlEncode(r.EmployeeName) %></span>
            </td>
            <td><%= Server.HtmlEncode(r.CategoryDisplay) %></td>
            <td><%= Server.HtmlEncode(r.Subject) %></td>
            <td><%= r.Amount.HasValue ? r.Amount.Value.ToString("N2") : "\u2014" %></td>
            <td><span class="<%= r.StatusBadgeClass %>"><%= Server.HtmlEncode(r.StatusDisplay) %></span></td>
            <td><%= r.SubmittedOn.HasValue ? r.SubmittedOn.Value.ToString("dd MMM yyyy") : "\u2014" %></td>
            <td>
              <a class="btn btn-secondary" style="padding:3px 10px;font-size:.78rem;"
                 href="<%= ResolveUrl("~/EmployeeRequest.aspx?editId=" + r.RequestID) %>">Open</a>
            </td>
          </tr>
        <% } } %>
        </tbody>
      </table>
    </div>
  </div>
</div>

<% } else { %>

<div class="form-breadcrumb">
  <a href="<%= ResolveUrl("~/EmployeeRequest.aspx") %>" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Request " + Server.HtmlEncode(Input.RequestNumber) : "New Employee Request" %></span>
  <% if (EditMode) { %>
  <span class="<%= Input.StatusBadgeClass %>"><%= Server.HtmlEncode(Input.StatusDisplay) %></span>
  <% } %>
</div>

<input type="hidden" name="__handler" id="__handler" value="SaveDraft" />
<input type="hidden" name="RequestID" value="<%= Input.RequestID %>" />
<input type="hidden" name="CurrentStatus" value="<%= Server.HtmlEncode(Input.Status) %>" />

<div class="card">
  <div class="card-body">
    <h2 class="req-title">Employee request form</h2>

    <% if (EditMode) { %>
    <div class="req-section">
      <div class="req-grid-3">
        <div class="req-field">
          <label>Request number</label>
          <input type="text" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.RequestNumber) %>" readonly />
        </div>
        <div class="req-field">
          <label>Employee</label>
          <input type="text" class="form-control readonly-field"
                 value="<%= Server.HtmlEncode(Input.EmployeeCode + " \u2014 " + Input.EmployeeName) %>" readonly />
        </div>
        <div class="req-field">
          <label>Department</label>
          <input type="text" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.DepartmentName) %>" readonly />
        </div>
      </div>
    </div>
    <% } %>

    <div class="req-section">
      <div class="req-section-title">Request details</div>
      <div class="req-grid-2">
        <div class="req-field">
          <label>Category *</label>
          <select name="RequestCategory" id="RequestCategory" class="form-control"
                  <%= CanEditDraft ? "" : "disabled" %> onchange="toggleCategoryFields()">
            <% foreach (var cat in EmployeeRequestCategories.All) { %>
            <option value="<%= cat %>" <%= Input.RequestCategory == cat ? "selected" : "" %>>
              <%= Server.HtmlEncode(EmployeeRequestCategories.DisplayName(cat)) %>
            </option>
            <% } %>
          </select>
          <% if (!CanEditDraft) { %>
          <input type="hidden" name="RequestCategory" value="<%= Server.HtmlEncode(Input.RequestCategory) %>" />
          <% } %>
        </div>
        <div class="req-field">
          <label>Subject *</label>
          <input type="text" name="Subject" class="form-control" maxlength="200"
                 value="<%= Server.HtmlEncode(Input.Subject) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
      </div>
      <div class="req-field" style="margin-top:.75rem;">
        <label>Description</label>
        <textarea name="Description" class="form-control" rows="3"
                  <%= CanEditDraft ? "" : "readonly" %>><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
      <div class="req-grid-3" style="margin-top:.75rem;">
        <div class="req-field">
          <label>Amount</label>
          <input type="number" step="0.01" name="Amount" class="form-control"
                 value="<%= Input.Amount.HasValue ? Input.Amount.Value.ToString("0.##") : "" %>"
                 <%= CanEditDraft ? "" : "readonly" %> />
        </div>
        <div class="req-field">
          <label>Start date</label>
          <input type="date" name="StartDate" class="form-control"
                 value="<%= Server.HtmlEncode(Input.StartDate) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
        <div class="req-field">
          <label>End date</label>
          <input type="date" name="EndDate" class="form-control"
                 value="<%= Server.HtmlEncode(Input.EndDate) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
      </div>
    </div>

    <div class="req-section">
      <div class="req-section-title">Category-specific fields</div>

      <div id="cat_MobilePhone" class="cat-block">
        <div class="req-grid-2">
          <div class="req-field">
            <label>Phone model</label>
            <input type="text" name="PhoneModel" class="form-control" value="<%= Server.HtmlEncode(DetailValue("PhoneModel")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
        </div>
      </div>

      <div id="cat_HealthClaim" class="cat-block">
        <div class="req-grid-2">
          <div class="req-field">
            <label>Claim type</label>
            <input type="text" name="ClaimType" class="form-control" value="<%= Server.HtmlEncode(DetailValue("ClaimType")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
          <div class="req-field">
            <label>Hospital / clinic</label>
            <input type="text" name="HospitalName" class="form-control" value="<%= Server.HtmlEncode(DetailValue("HospitalName")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
        </div>
      </div>

      <div id="cat_Travelling" class="cat-block">
        <div class="req-grid-2">
          <div class="req-field">
            <label>Destination</label>
            <input type="text" name="Destination" class="form-control" value="<%= Server.HtmlEncode(DetailValue("Destination")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
          <div class="req-field">
            <label>Travel mode</label>
            <input type="text" name="TravelMode" class="form-control" value="<%= Server.HtmlEncode(DetailValue("TravelMode")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
        </div>
      </div>

      <div id="cat_EmployeeLoan" class="cat-block">
        <div class="req-field">
          <label>Loan purpose</label>
          <input type="text" name="LoanPurpose" class="form-control" value="<%= Server.HtmlEncode(DetailValue("LoanPurpose")) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
      </div>

      <div id="cat_Vehicle" class="cat-block">
        <div class="req-grid-2">
          <div class="req-field">
            <label>Vehicle type</label>
            <input type="text" name="VehicleType" class="form-control" value="<%= Server.HtmlEncode(DetailValue("VehicleType")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
          <div class="req-field">
            <label>Registration no.</label>
            <input type="text" name="VehicleRegNo" class="form-control" value="<%= Server.HtmlEncode(DetailValue("VehicleRegNo")) %>" <%= CanEditDraft ? "" : "readonly" %> />
          </div>
        </div>
      </div>

      <div id="cat_Fuel" class="cat-block">
        <div class="req-field">
          <label>Fuel liters / estimate</label>
          <input type="text" name="FuelLiters" class="form-control" value="<%= Server.HtmlEncode(DetailValue("FuelLiters")) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
      </div>

      <div id="cat_Other" class="cat-block">
        <div class="req-field">
          <label>Request type (other)</label>
          <input type="text" name="OtherType" class="form-control" value="<%= Server.HtmlEncode(DetailValue("OtherType")) %>" <%= CanEditDraft ? "" : "readonly" %> />
        </div>
      </div>
    </div>

    <% if (CanEditDraft) { %>
    <div class="req-section">
      <div class="req-section-title">Supporting documents</div>
      <div class="req-field">
        <label>Upload file</label>
        <input type="file" name="supportFile" class="form-control" />
        <span class="hint">PDF, Word, Excel, images, or text (max per server limits).</span>
      </div>
    </div>
    <% } %>

    <% if (Attachments != null && Attachments.Count > 0) { %>
    <div class="req-section">
      <div class="req-section-title">Attachments</div>
      <ul style="margin:0;padding-left:1.2rem;">
        <% foreach (var att in Attachments) { %>
        <li>
          <a href="<%= ResolveUrl("~" + att.DocumentPath) %>" target="_blank" rel="noopener">
            <%= Server.HtmlEncode(string.IsNullOrEmpty(att.OriginalFileName) ? att.DocumentPath : att.OriginalFileName) %>
          </a>
          <span style="color:#6b7280;font-size:.75rem;"> &mdash; <%= att.UploadedOn.ToString("dd MMM yyyy HH:mm") %></span>
        </li>
        <% } %>
      </ul>
    </div>
    <% } %>

    <% if (CanEditDraft) { %>
    <div class="req-section">
      <div class="req-field">
        <label>Submission comments (optional)</label>
        <textarea name="ApprovalComments" class="form-control" rows="2" placeholder="Notes for your manager..."></textarea>
      </div>
    </div>
    <div class="req-actions">
      <button type="submit" class="btn btn-secondary"
              onclick="document.getElementById('__handler').value='SaveDraft';">Save Draft</button>
      <button type="submit" class="btn btn-primary"
              onclick="document.getElementById('__handler').value='Submit';return confirm('Submit this request for manager approval?');">
        Submit for Approval
      </button>
    </div>
    <% } %>

    <% if (CanApprove) { %>
    <div class="req-section" style="margin-top:1.5rem;padding-top:1rem;border-top:1px solid #e5e7eb;">
      <div class="req-section-title">Approval action</div>
      <div class="req-field">
        <label>Comments</label>
        <textarea name="ApprovalComments" class="form-control" rows="3" placeholder="Required for reject / return; recommended for approve."></textarea>
      </div>
      <div class="req-actions">
        <button type="submit" class="btn btn-primary"
                onclick="document.getElementById('__handler').value='Approve';return confirm('Approve this request?');">
          Approve
        </button>
        <button type="submit" class="btn btn-secondary"
                onclick="document.getElementById('__handler').value='Return';return confirm('Return for correction?');">
          Return for Correction
        </button>
        <button type="submit" class="btn btn-secondary" style="background:#b91c1c;color:#fff;border-color:#b91c1c;"
                onclick="document.getElementById('__handler').value='Reject';return confirm('Reject this request permanently?');">
          Reject
        </button>
      </div>
    </div>
    <% } %>

    <% if (EditMode && !string.IsNullOrEmpty(Input.ManagerName)) { %>
    <p style="margin-top:1rem;font-size:.85rem;color:#6b7280;">
      Reporting manager: <strong><%= Server.HtmlEncode(Input.ManagerName) %></strong>
    </p>
    <% } %>
  </div>
</div>

<% if (Actions != null && Actions.Count > 0) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>Audit trail</h2></div>
  <div class="card-body table-responsive">
    <table class="audit-table">
      <thead>
        <tr><th>When</th><th>Stage</th><th>Action</th><th>By</th><th>Comments</th></tr>
      </thead>
      <tbody>
        <% foreach (var act in Actions) { %>
        <tr>
          <td><%= act.ActionOn.ToString("dd MMM yyyy HH:mm") %></td>
          <td><%= Server.HtmlEncode(act.ActionStage) %></td>
          <td><%= Server.HtmlEncode(act.ActionType) %></td>
          <td><%= Server.HtmlEncode(act.ActionByName) %></td>
          <td><%= Server.HtmlEncode(act.Comments) %></td>
        </tr>
        <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>

<script type="text/javascript">
function toggleCategoryFields() {
  var cat = document.getElementById('RequestCategory');
  var val = cat ? cat.value : '<%= Server.HtmlEncode(Input.RequestCategory) %>';
  var ids = ['MobilePhone','HealthClaim','Travelling','EmployeeLoan','Vehicle','Fuel','Other'];
  for (var i = 0; i < ids.length; i++) {
    var el = document.getElementById('cat_' + ids[i]);
    if (el) el.className = 'cat-block' + (ids[i] === val ? ' active' : '');
  }
}
toggleCategoryFields();
</script>

<% } %>
</asp:Content>
