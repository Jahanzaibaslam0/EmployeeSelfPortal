<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.EmployeeDisciplinePage" %>
<%@ Import Namespace="HRMS.Services" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.disc-title { color: #1e3a5f; font-size: 1.3rem; font-weight: 700; margin: 0 0 1rem; }
.disc-section { margin-bottom: 1.25rem; }
.disc-section-title {
  font-size: .95rem; font-weight: 600; color: #1e3a5f;
  margin: 0 0 .65rem; padding-bottom: .35rem; border-bottom: 1px solid #e5e7eb;
}
.disc-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem 1.25rem; }
.disc-grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: .75rem 1.25rem; }
.disc-field { display: flex; flex-direction: column; gap: .25rem; }
.disc-field label { font-size: .8rem; font-weight: 600; color: #374151; }
.disc-field .hint { font-size: .72rem; color: #6b7280; }
.readonly-field { background: #f3f4f6 !important; color: #374151; cursor: not-allowed; }
.form-breadcrumb {
  display: flex; align-items: center; gap: .75rem; margin-bottom: 1rem; flex-wrap: wrap;
  padding: .5rem .75rem; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 6px;
}
.disc-inbox { display: flex; flex-wrap: wrap; gap: .4rem; margin-bottom: 1rem; }
.disc-inbox a {
  padding: 5px 12px; border: 1px solid #d1d5db; border-radius: 4px; font-size: .8rem;
  text-decoration: none; color: #1e3a5f; background: #fff;
}
.disc-inbox a.active { background: #1e3a5f; color: #fff; border-color: #1e3a5f; }
.disc-badge {
  display: inline-block; padding: 2px 10px; border-radius: 999px;
  font-size: .72rem; font-weight: 700; background: #e5e7eb; color: #374151;
}
.disc-open { background: #dbeafe; color: #1d4ed8; }
.disc-invest { background: #fef3c7; color: #92400e; }
.disc-action { background: #ffedd5; color: #c2410c; }
.disc-closed { background: #dcfce7; color: #166534; }
.disc-actions { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: 1rem; }
.audit-table, .find-table { width: 100%; border-collapse: collapse; font-size: .82rem; }
.audit-table th, .audit-table td, .find-table th, .find-table td {
  padding: .45rem .55rem; border-bottom: 1px solid #e5e7eb; text-align: left; vertical-align: top;
}
.audit-table th, .find-table th { background: #f3f4f6; color: #374151; }
.emp-snap {
  background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px;
  padding: .75rem 1rem; margin-bottom: 1rem; font-size: .85rem;
}
@media (max-width: 768px) {
  .disc-grid-2, .disc-grid-3 { grid-template-columns: 1fr; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Employee Discipline &amp; Non-Conformance</h2>
    <% if (CanWrite) { %>
    <a href="<%= ResolveUrl("~/EmployeeDiscipline.aspx?new=1") %>" class="btn btn-primary">+ New Case</a>
    <% } %>
  </div>
  <div class="card-body">
    <div class="disc-inbox">
      <a class="<%= StatusFilter == "all" ? "active" : "" %>" href="<%= ResolveUrl("~/EmployeeDiscipline.aspx?status=all") %>">All</a>
      <% foreach (var st in DisciplineStatuses.All) { %>
      <a class="<%= StatusFilter == st ? "active" : "" %>"
         href="<%= ResolveUrl("~/EmployeeDiscipline.aspx?status=" + st) %>">
        <%= Server.HtmlEncode(DisciplineStatuses.DisplayName(st)) %>
      </a>
      <% } %>
    </div>
    <div class="table-responsive">
      <table class="data-table">
        <thead>
          <tr>
            <th>#</th><th>Case</th><th>Employee</th><th>Type</th><th>Category</th>
            <th>Incident</th><th>Status</th><th>Action</th><th></th>
          </tr>
        </thead>
        <tbody>
        <% if (Cases.Count == 0) { %>
          <tr><td colspan="9" style="text-align:center;padding:1.5rem;color:#6b7280;">No cases found.</td></tr>
        <% } else {
             for (var i = 0; i < Cases.Count; i++) {
               var c = Cases[i]; %>
          <tr>
            <td><%= i + 1 %></td>
            <td><strong><%= Server.HtmlEncode(c.CaseNumber) %></strong></td>
            <td>
              <%= Server.HtmlEncode(c.EmployeeCode) %><br />
              <span style="font-size:.8rem;color:#6b7280;"><%= Server.HtmlEncode(c.EmployeeName) %></span>
            </td>
            <td><%= Server.HtmlEncode(c.DisciplineType) %></td>
            <td><%= Server.HtmlEncode(c.DisciplineCategory) %></td>
            <td><%= string.IsNullOrEmpty(c.IncidentDate) ? "\u2014" : Convert.ToDateTime(c.IncidentDate).ToString("dd MMM yyyy") %></td>
            <td><span class="<%= c.StatusBadgeClass %>"><%= Server.HtmlEncode(c.StatusDisplay) %></span></td>
            <td><%= Server.HtmlEncode(string.IsNullOrEmpty(c.DisciplinaryAction) ? "\u2014" : c.DisciplinaryAction) %></td>
            <td>
              <a class="btn btn-secondary" style="padding:3px 10px;font-size:.78rem;"
                 href="<%= ResolveUrl("~/EmployeeDiscipline.aspx?editId=" + c.DisciplineID) %>">Open</a>
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
  <a href="<%= ResolveUrl("~/EmployeeDiscipline.aspx") %>" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Case " + Server.HtmlEncode(Input.CaseNumber) : "New Disciplinary / Non-Conformance Case" %></span>
  <% if (EditMode) { %>
  <span class="<%= Input.StatusBadgeClass %>"><%= Server.HtmlEncode(Input.StatusDisplay) %></span>
  <% } %>
</div>

<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="DisciplineID" value="<%= Input.DisciplineID %>" />

<div class="card">
  <div class="card-body">
    <h2 class="disc-title">Case record</h2>

    <div class="disc-section">
      <div class="disc-section-title">Employee</div>
      <div class="disc-grid-2">
        <div class="disc-field">
          <label>Employee *</label>
          <select name="EmployeeID" id="EmployeeID" class="form-control" <%= CanWrite ? "" : "disabled" %>
                  onchange="loadEmployeeSnap()">
            <option value="0">-- Select Employee --</option>
            <% foreach (var e in Employees) { %>
            <option value="<%= e.Id %>" <%= Input.EmployeeID == e.Id ? "selected" : "" %>><%= Server.HtmlEncode(e.Name) %></option>
            <% } %>
          </select>
          <% if (!CanWrite) { %><input type="hidden" name="EmployeeID" value="<%= Input.EmployeeID %>" /><% } %>
        </div>
        <% if (EditMode) { %>
        <div class="disc-field">
          <label>Case number</label>
          <input type="text" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.CaseNumber) %>" readonly />
        </div>
        <% } %>
      </div>
      <% if (EditMode) { %>
      <div class="emp-snap" id="empSnap">
        <strong><%= Server.HtmlEncode(Input.EmployeeCode) %></strong> &mdash; <%= Server.HtmlEncode(Input.EmployeeName) %><br />
        Department: <%= Server.HtmlEncode(Input.DepartmentName) %> &nbsp;|&nbsp;
        Designation: <%= Server.HtmlEncode(Input.Designation) %><br />
        Reporting manager: <%= Server.HtmlEncode(string.IsNullOrEmpty(Input.ManagerName) ? "\u2014" : Input.ManagerName) %>
      </div>
      <% } else { %>
      <div class="emp-snap hint" id="empSnap">Select an employee to capture department, designation, and reporting manager.</div>
      <% } %>
    </div>

    <div class="disc-section">
      <div class="disc-section-title">Incident details</div>
      <div class="disc-grid-3">
        <div class="disc-field">
          <label>Incident date</label>
          <input type="date" name="IncidentDate" class="form-control" value="<%= Server.HtmlEncode(Input.IncidentDate) %>" <%= CanWrite ? "" : "readonly" %> />
        </div>
        <div class="disc-field">
          <label>Date reported</label>
          <input type="date" name="ReportedDate" class="form-control" value="<%= Server.HtmlEncode(Input.ReportedDate) %>" <%= CanWrite ? "" : "readonly" %> />
        </div>
        <div class="disc-field">
          <label>Case status *</label>
          <select name="CaseStatus" class="form-control" <%= CanWrite ? "" : "disabled" %>>
            <% foreach (var st in DisciplineStatuses.All) { %>
            <option value="<%= st %>" <%= Input.CaseStatus == st ? "selected" : "" %>><%= Server.HtmlEncode(DisciplineStatuses.DisplayName(st)) %></option>
            <% } %>
          </select>
          <% if (!CanWrite) { %><input type="hidden" name="CaseStatus" value="<%= Server.HtmlEncode(Input.CaseStatus) %>" /><% } %>
        </div>
      </div>
      <div class="disc-grid-2" style="margin-top:.75rem;">
        <div class="disc-field">
          <label>Type *</label>
          <select name="DisciplineType" class="form-control" <%= CanWrite ? "" : "disabled" %>>
            <option value="">-- Select --</option>
            <% foreach (var t in DisciplineTypes.All) { %>
            <option value="<%= Server.HtmlEncode(t) %>" <%= Input.DisciplineType == t ? "selected" : "" %>><%= Server.HtmlEncode(t) %></option>
            <% } %>
          </select>
          <% if (!CanWrite) { %><input type="hidden" name="DisciplineType" value="<%= Server.HtmlEncode(Input.DisciplineType) %>" /><% } %>
        </div>
        <div class="disc-field">
          <label>Category</label>
          <select name="DisciplineCategory" class="form-control" <%= CanWrite ? "" : "disabled" %>>
            <option value="">-- Select --</option>
            <% foreach (var cat in DisciplineCategories.All) { %>
            <option value="<%= Server.HtmlEncode(cat) %>" <%= Input.DisciplineCategory == cat ? "selected" : "" %>><%= Server.HtmlEncode(cat) %></option>
            <% } %>
          </select>
          <% if (!CanWrite) { %><input type="hidden" name="DisciplineCategory" value="<%= Server.HtmlEncode(Input.DisciplineCategory) %>" /><% } %>
        </div>
      </div>
      <div class="disc-field" style="margin-top:.75rem;">
        <label>Detailed description</label>
        <textarea name="Description" class="form-control" rows="3" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
    </div>

    <div class="disc-section">
      <div class="disc-section-title">Investigation</div>
      <div class="disc-field">
        <label>Investigation details</label>
        <textarea name="InvestigationDetails" class="form-control" rows="3" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.InvestigationDetails) %></textarea>
      </div>
      <div class="disc-field" style="margin-top:.65rem;">
        <label>Investigation findings</label>
        <textarea name="InvestigationFindings" class="form-control" rows="3" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.InvestigationFindings) %></textarea>
      </div>
      <div class="disc-field" style="margin-top:.65rem;">
        <label>Employee explanation / response</label>
        <textarea name="EmployeeExplanation" class="form-control" rows="3" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.EmployeeExplanation) %></textarea>
      </div>
    </div>

    <div class="disc-section">
      <div class="disc-section-title">Findings &amp; observations</div>
      <div class="disc-field">
        <label>Findings and observations</label>
        <textarea name="FindingsObservations" class="form-control" rows="3" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.FindingsObservations) %></textarea>
      </div>
      <div class="disc-field" style="margin-top:.65rem;">
        <label>Finding remarks / comments</label>
        <textarea name="FindingRemarks" class="form-control" rows="2" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.FindingRemarks) %></textarea>
      </div>
      <label class="checkbox-label" style="margin-top:.5rem;display:inline-flex;gap:.4rem;align-items:center;">
        <input type="checkbox" name="IncludeInPerformance" value="true" <%= Input.IncludeInPerformance ? "checked" : "" %> <%= CanWrite ? "" : "disabled" %> />
        Include findings in employee performance profile
      </label>
    </div>

    <div class="disc-section">
      <div class="disc-section-title">Corrective / disciplinary action</div>
      <div class="disc-field">
        <label>Corrective or preventive actions</label>
        <textarea name="CorrectiveActions" class="form-control" rows="2" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.CorrectiveActions) %></textarea>
      </div>
      <div class="disc-grid-3" style="margin-top:.75rem;">
        <div class="disc-field">
          <label>Warning / notice / penalty / action</label>
          <input type="text" name="DisciplinaryAction" class="form-control" maxlength="200"
                 value="<%= Server.HtmlEncode(Input.DisciplinaryAction) %>" <%= CanWrite ? "" : "readonly" %> />
        </div>
        <div class="disc-field">
          <label>Action date</label>
          <input type="date" name="ActionDate" class="form-control" value="<%= Server.HtmlEncode(Input.ActionDate) %>" <%= CanWrite ? "" : "readonly" %> />
        </div>
        <div class="disc-field">
          <label>Responsible person</label>
          <input type="text" name="ActionResponsiblePerson" class="form-control" maxlength="200"
                 value="<%= Server.HtmlEncode(Input.ActionResponsiblePerson) %>" <%= CanWrite ? "" : "readonly" %> />
        </div>
      </div>
    </div>

    <div class="disc-section">
      <div class="disc-section-title">Final decision</div>
      <div class="disc-field">
        <label>Final decision</label>
        <textarea name="FinalDecision" class="form-control" rows="2" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.FinalDecision) %></textarea>
      </div>
      <div class="disc-field" style="margin-top:.65rem;">
        <label>Management / HR remarks</label>
        <textarea name="ManagementRemarks" class="form-control" rows="2" <%= CanWrite ? "" : "readonly" %>><%= Server.HtmlEncode(Input.ManagementRemarks) %></textarea>
      </div>
    </div>

    <% if (CanWrite) { %>
    <div class="disc-section">
      <div class="disc-section-title">Supporting documents / evidence</div>
      <div class="disc-field">
        <label>Upload file</label>
        <input type="file" name="evidenceFile" class="form-control" />
        <span class="hint">PDF, Word, Excel, images, or text.</span>
      </div>
    </div>
    <% } %>

    <% if (Attachments != null && Attachments.Count > 0) { %>
    <div class="disc-section">
      <div class="disc-section-title">Attachments</div>
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

    <% if (CanWrite) { %>
    <div class="disc-actions">
      <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
        <%= EditMode ? "Update Case" : "Create Case" %>
      </button>
      <a href="<%= ResolveUrl("~/EmployeeDiscipline.aspx") %>" class="btn btn-secondary">Cancel</a>
    </div>
    <% } %>
  </div>
</div>

<% if (EditMode && CanWrite) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>Add findings &amp; remarks (performance-linked)</h2></div>
  <div class="card-body">
    <div class="disc-grid-3">
      <div class="disc-field">
        <label>Source</label>
        <select name="FindingSource" class="form-control">
          <% foreach (var s in FindingSources.All) { %>
          <option value="<%= Server.HtmlEncode(s) %>"><%= Server.HtmlEncode(s) %></option>
          <% } %>
        </select>
      </div>
      <div class="disc-field">
        <label>Category</label>
        <input type="text" name="FindingCategory" class="form-control" maxlength="100" />
      </div>
      <div class="disc-field">
        <label>Date</label>
        <input type="date" name="FindingDate" class="form-control" value="<%= DateTime.Today.ToString("yyyy-MM-dd") %>" />
      </div>
    </div>
    <div class="disc-grid-2" style="margin-top:.65rem;">
      <div class="disc-field">
        <label>Title *</label>
        <input type="text" name="FindingTitle" class="form-control" maxlength="200" />
      </div>
      <div class="disc-field">
        <label>Severity / impact</label>
        <input type="text" name="FindingSeverity" class="form-control" maxlength="50" placeholder="e.g. Low / Medium / High" />
      </div>
    </div>
    <div class="disc-field" style="margin-top:.65rem;">
      <label>Remarks</label>
      <textarea name="FindingRemarksText" class="form-control" rows="2"></textarea>
    </div>
    <label class="checkbox-label" style="margin-top:.5rem;display:inline-flex;gap:.4rem;align-items:center;">
      <input type="checkbox" name="FindingIncludeInPerformance" value="true" checked />
      Show on performance profile
    </label>
    <div class="disc-actions">
      <button type="submit" class="btn btn-secondary"
              onclick="document.getElementById('__handler').value='AddFinding';">Add Finding</button>
    </div>
  </div>
</div>
<% } %>

<% if (Findings != null && Findings.Count > 0) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>Employee findings history</h2></div>
  <div class="card-body table-responsive">
    <table class="find-table">
      <thead>
        <tr><th>Date</th><th>Source</th><th>Title</th><th>Remarks</th><th>Case</th><th>By</th><th>Perf.</th></tr>
      </thead>
      <tbody>
        <% foreach (var f in Findings) { %>
        <tr>
          <td><%= string.IsNullOrEmpty(f.FindingDate) ? f.CreatedOn.ToString("dd MMM yyyy") : Convert.ToDateTime(f.FindingDate).ToString("dd MMM yyyy") %></td>
          <td><%= Server.HtmlEncode(f.FindingSource) %></td>
          <td><strong><%= Server.HtmlEncode(f.Title) %></strong><% if (!string.IsNullOrEmpty(f.Severity)) { %><br /><span style="font-size:.75rem;color:#6b7280;"><%= Server.HtmlEncode(f.Severity) %></span><% } %></td>
          <td><%= Server.HtmlEncode(f.Remarks) %></td>
          <td>
            <% if (f.DisciplineID.HasValue && f.DisciplineID.Value > 0) { %>
            <a href="<%= ResolveUrl("~/EmployeeDiscipline.aspx?editId=" + f.DisciplineID.Value) %>"><%= Server.HtmlEncode(f.CaseNumber) %></a>
            <% } else { %>&mdash;<% } %>
          </td>
          <td><%= Server.HtmlEncode(f.CreatedByName) %></td>
          <td><%= f.IncludeInPerformance ? "Yes" : "No" %></td>
        </tr>
        <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>

<% if (AuditTrail != null && AuditTrail.Count > 0) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>Audit trail</h2></div>
  <div class="card-body table-responsive">
    <table class="audit-table">
      <thead>
        <tr><th>When</th><th>Action</th><th>Summary</th><th>By</th><th>Remarks</th></tr>
      </thead>
      <tbody>
        <% foreach (var a in AuditTrail) { %>
        <tr>
          <td><%= a.ActionOn.ToString("dd MMM yyyy HH:mm") %></td>
          <td><%= Server.HtmlEncode(a.ActionType) %></td>
          <td><%= Server.HtmlEncode(a.ActionSummary) %></td>
          <td><%= Server.HtmlEncode(a.ActionByName) %></td>
          <td><%= Server.HtmlEncode(a.Remarks) %></td>
        </tr>
        <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>

<% } %>
</asp:Content>
