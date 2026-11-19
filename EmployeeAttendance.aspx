<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.EmployeeAttendancePage" %>
<%@ Import Namespace="HRMS.Services" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.att-page-title { color:#1e3a5f; font-size:1.25rem; font-weight:700; margin:0 0 1rem; }
.att-filters {
  border:1px solid #d1d5db; border-radius:8px; padding:1rem;
  background:#f8fafc; margin-bottom:1rem;
}
.att-filters h3 {
  margin:0 0 .75rem; font-size:.78rem; letter-spacing:.06em;
  text-transform:uppercase; color:#64748b; font-weight:700;
}
.att-filter-grid {
  display:grid;
  grid-template-columns:repeat(auto-fill,minmax(140px,1fr));
  gap:.65rem .75rem; align-items:end;
}
.att-filter-grid .form-group { margin:0; }
.att-filter-grid label {
  display:block; font-size:.7rem; font-weight:700; color:#475569;
  text-transform:uppercase; margin-bottom:.2rem;
}
.att-actions { display:flex; gap:.5rem; justify-content:flex-end; align-items:end; }
.att-tabs {
  display:flex; flex-wrap:wrap; gap:.4rem; margin:0 0 1rem;
  padding:.35rem; background:#eef2f7; border-radius:8px;
}
.att-tabs a {
  padding:.45rem .85rem; border-radius:6px; font-size:.82rem; font-weight:600;
  text-decoration:none; color:#1e3a5f; background:transparent;
}
.att-tabs a.active { background:#1e3a5f; color:#fff; }
.att-kpis {
  display:grid; grid-template-columns:repeat(auto-fit,minmax(160px,1fr));
  gap:.75rem; margin-bottom:1rem;
}
.att-kpi {
  background:#fff; border:1px solid #e2e8f0; border-radius:10px;
  padding:.85rem 1rem; box-shadow:0 1px 2px rgba(15,23,42,.04);
}
.att-kpi .val { font-size:1.6rem; font-weight:800; color:#1e3a5f; line-height:1.1; }
.att-kpi .lbl { font-size:.78rem; color:#64748b; margin-top:.25rem; }
.att-breakdown-title {
  font-size:.95rem; font-weight:700; color:#1e3a5f; margin:0 0 .65rem;
}
.att-breakdown {
  display:grid; grid-template-columns:repeat(auto-fill,minmax(110px,1fr));
  gap:.55rem; margin-bottom:1.1rem;
}
.att-st {
  background:#fff; border:1px solid #e5e7eb; border-radius:8px;
  padding:.55rem .6rem .45rem; text-align:center; border-bottom-width:3px;
}
.att-st .code { font-size:.85rem; font-weight:800; }
.att-st .cnt { font-size:1.25rem; font-weight:800; color:#0f172a; margin:.15rem 0; }
.att-st .name { font-size:.68rem; color:#64748b; }
.st-p .code, .c-p { color:#16a34a; } .st-p { border-bottom-color:#16a34a; }
.st-a .code, .c-a { color:#dc2626; } .st-a { border-bottom-color:#dc2626; }
.st-z .code, .c-z { color:#2563eb; } .st-z { border-bottom-color:#2563eb; }
.st-h .code, .c-h { color:#7c3aed; } .st-h { border-bottom-color:#7c3aed; }
.st-g .code, .c-g { color:#0891b2; } .st-g { border-bottom-color:#0891b2; }
.st-lt .code, .c-lt { color:#d97706; } .st-lt { border-bottom-color:#d97706; }
.st-k .code, .c-k { color:#ca8a04; } .st-k { border-bottom-color:#ca8a04; }
.st-e .code, .c-e { color:#0d9488; } .st-e { border-bottom-color:#0d9488; }
.st-b .code, .c-b { color:#1d4ed8; } .st-b { border-bottom-color:#1d4ed8; }
.st-f .code, .c-f { color:#db2777; } .st-f { border-bottom-color:#db2777; }
.st-er .code, .c-er { color:#ea580c; } .st-er { border-bottom-color:#ea580c; }
.att-toolbar {
  display:flex; justify-content:space-between; align-items:center;
  flex-wrap:wrap; gap:.75rem; margin-bottom:.75rem;
}
.att-table { width:100%; border-collapse:collapse; font-size:.8rem; }
.att-table th {
  background:#1e3a5f; color:#fff; text-align:left; padding:.5rem .55rem;
  font-weight:600; white-space:nowrap;
}
.att-table td {
  padding:.45rem .55rem; border-bottom:1px solid #e5e7eb; vertical-align:middle;
}
.att-table tr:nth-child(even) td { background:#f8fafc; }
.att-pct {
  display:inline-block; min-width:3.2rem; text-align:center;
  padding:2px 10px; border-radius:999px; font-size:.72rem; font-weight:700;
  background:#fee2e2; color:#b91c1c;
}
.att-pct.ok { background:#dcfce7; color:#166534; }
.att-badge {
  display:inline-block; padding:2px 10px; border-radius:999px;
  font-size:.72rem; font-weight:700; background:#e5e7eb; color:#374151;
}
.att-badge.st-p { background:#dcfce7; color:#166534; }
.att-badge.st-a { background:#fee2e2; color:#b91c1c; }
.att-badge.st-lt { background:#fef3c7; color:#92400e; }
.att-badge.st-z { background:#dbeafe; color:#1d4ed8; }
.att-badge.st-h { background:#ede9fe; color:#6d28d9; }
.att-badge.st-er { background:#ffedd5; color:#c2410c; }
.att-pager { display:flex; gap:.4rem; flex-wrap:wrap; margin-top:.85rem; align-items:center; }
.att-pager a, .att-pager span {
  padding:4px 10px; border:1px solid #d1d5db; border-radius:4px; font-size:.8rem;
  text-decoration:none; color:#1e3a5f; background:#fff;
}
.att-pager .current { background:#1e3a5f; color:#fff; border-color:#1e3a5f; }
.btn-export { background:#16a34a; color:#fff !important; border:none; }
.btn-pdf { background:#dc2626; color:#fff !important; border:none; opacity:.85; cursor:default; }
.att-link-note { font-size:.82rem; color:#4b5563; margin:0 0 .75rem; }
.att-map-note { font-size:.75rem; color:#64748b; margin:.35rem 0 0; }
@media (max-width:768px) {
  .att-filter-grid { grid-template-columns:1fr 1fr; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>

<h2 class="att-page-title">Reports &mdash; <%= Server.HtmlEncode(ViewTitle) %></h2>

<% if (!string.IsNullOrWhiteSpace(LinkedEmpCode)) { %>
<p class="att-link-note">Filtered by employee / biometric: <strong><%= Server.HtmlEncode(LinkedEmpCode) %></strong></p>
<% } %>

<div class="att-filters">
  <h3>Filters</h3>
  <div class="att-filter-grid">
    <div class="form-group">
      <label>Start Date</label>
      <input type="date" id="fStartDate" class="form-control" value="<%= FilterDate(Filter.StartDate) %>" />
    </div>
    <div class="form-group">
      <label>End Date</label>
      <input type="date" id="fEndDate" class="form-control" value="<%= FilterDate(Filter.EndDate) %>" />
    </div>
    <div class="form-group">
      <label>Emp Code</label>
      <input type="text" id="fEmpCode" class="form-control" placeholder="EMP001 / Biometric"
             value="<%= Server.HtmlEncode(Filter.EmpCode) %>" />
    </div>
    <div class="form-group">
      <label>Employee Name</label>
      <input type="text" id="fEmployeeName" class="form-control" placeholder="Name search"
             value="<%= Server.HtmlEncode(Filter.EmployeeName) %>" />
    </div>
    <div class="form-group">
      <label>Department</label>
      <input type="text" id="fDepartment" class="form-control" value="<%= Server.HtmlEncode(Filter.Department) %>" />
    </div>
    <div class="form-group">
      <label>Location</label>
      <input type="text" id="fLocation" class="form-control" value="<%= Server.HtmlEncode(Filter.Location) %>" />
    </div>
    <div class="form-group">
      <label>Division</label>
      <input type="text" id="fDivision" class="form-control" value="<%= Server.HtmlEncode(Filter.Division) %>" />
    </div>
    <div class="form-group">
      <label>Designation</label>
      <select id="fDesignation" class="form-control">
        <option value="All">All</option>
        <% foreach (var d in DesignationOptions) { %>
        <option value="<%= Server.HtmlEncode(d) %>" <%= Filter.Designation == d ? "selected" : "" %>><%= Server.HtmlEncode(d) %></option>
        <% } %>
      </select>
    </div>
    <div class="att-actions">
      <button type="button" class="btn btn-primary" id="btnProcess">Process</button>
      <a class="btn btn-secondary" href="<%= ResolveUrl("~/EmployeeAttendance.aspx") %>">Reset</a>
    </div>
  </div>
</div>

<div class="att-tabs">
  <a class="<%= ActiveView == "daily" ? "active" : "" %>" href="<%= BuildPageUrl(1, "daily") %>">Daily Attendance</a>
  <a class="<%= ActiveView == "monthly" ? "active" : "" %>" href="<%= BuildPageUrl(1, "monthly") %>">Monthly Summary</a>
  <a class="<%= ActiveView == "late" ? "active" : "" %>" href="<%= BuildPageUrl(1, "late") %>">Late Arrival</a>
  <a class="<%= ActiveView == "early" ? "active" : "" %>" href="<%= BuildPageUrl(1, "early") %>">Early Departure</a>
  <a class="<%= ActiveView == "absent" ? "active" : "" %>" href="<%= BuildPageUrl(1, "absent") %>">Absentee</a>
  <a class="<%= ActiveView == "leave" ? "active" : "" %>" href="<%= BuildPageUrl(1, "leave") %>">Leave</a>
  <a class="<%= ActiveView == "detail" ? "active" : "" %>" href="<%= BuildPageUrl(1, "detail") %>">Punch Detail</a>
</div>

<% if (HasData) { %>
<div class="att-kpis">
  <div class="att-kpi">
    <div class="val"><%= Report.DistinctEmployees %></div>
    <div class="lbl">Distinct employees in range</div>
  </div>
  <div class="att-kpi">
    <div class="val"><%= Report.PresentPercent.ToString("0.#") %>%</div>
    <div class="lbl">Present vs total records</div>
  </div>
  <div class="att-kpi">
    <div class="val" style="font-size:1.05rem;padding-top:.35rem;"><%= Server.HtmlEncode(Report.DateRangeLabel) %></div>
    <div class="lbl"><%= Report.DaySpan %> day(s) selected</div>
  </div>
  <div class="att-kpi">
    <div class="val"><%= Report.MatchedRows %></div>
    <div class="lbl">Attendance rows matched</div>
  </div>
</div>

<div class="att-breakdown-title">Status Breakdown</div>
<div class="att-breakdown">
  <% foreach (var s in StatusBreakdown) { %>
  <div class="att-st <%= s.CssClass %>">
    <div class="code"><%= Server.HtmlEncode(s.Code) %></div>
    <div class="cnt"><%= s.Count %></div>
    <div class="name"><%= Server.HtmlEncode(s.Label) %></div>
  </div>
  <% } %>
</div>
<% } %>

<div class="att-toolbar">
  <div>Showing <strong><%= TotalCount %></strong> row(s)</div>
  <div style="display:flex;gap:.45rem;">
    <a class="btn btn-export" href="<%= BuildPageUrl(Filter.Page) + "&export=1" %>">Export Excel</a>
    <span class="btn btn-pdf" title="PDF export coming soon">Export PDF</span>
  </div>
</div>

<p class="att-map-note">
  Join: Attendance EmpCode &harr; EmployeeMaster.BiometricID.
  Mapped columns &mdash; InTime: <strong><%= Server.HtmlEncode(Report.MappedInTimeColumn) %></strong>,
  OutTime: <strong><%= Server.HtmlEncode(Report.MappedOutTimeColumn) %></strong>,
  Status: <strong><%= Server.HtmlEncode(Report.MappedStatusColumn) %></strong>
</p>

<div style="overflow-x:auto;">
<% if (!ShowDetailGrid) { %>
  <table class="att-table">
    <thead>
      <tr>
        <th>#</th>
        <% if (ActiveView == "monthly") { %>
        <th>Emp Code</th><th>Employee Name</th><th>Designation</th>
        <th>Department</th><th>Division</th><th>Location</th>
        <% } else { %>
        <th>Date</th>
        <% } %>
        <th>TOTAL</th><th>P</th><th>A</th><th>LT</th><th>Z</th><th>H</th>
        <th>G</th><th>K</th><th>E</th><th>ER</th><th>B</th><th>F</th><th>ATT%</th>
      </tr>
    </thead>
    <tbody>
    <% if (!HasData || AggregateRows.Count == 0) { %>
      <tr><td colspan="20" style="text-align:center;padding:1.5rem;color:#6b7280;">No attendance records found for the selected filters.</td></tr>
    <% } else {
         var start = (Filter.Page - 1) * Filter.PageSize;
         for (var i = 0; i < AggregateRows.Count; i++) {
           var r = AggregateRows[i];
    %>
      <tr>
        <td><%= start + i + 1 %></td>
        <% if (ActiveView == "monthly") { %>
        <td><%= Server.HtmlEncode(r.EmpCode) %></td>
        <td><%= Server.HtmlEncode(r.EmployeeName) %></td>
        <td><%= Server.HtmlEncode(r.Designation) %></td>
        <td><%= Server.HtmlEncode(r.Department) %></td>
        <td><%= Server.HtmlEncode(r.Division) %></td>
        <td><%= Server.HtmlEncode(r.Location) %></td>
        <% } else { %>
        <td><%= Server.HtmlEncode(r.AttDate) %></td>
        <% } %>
        <td><%= r.Total %></td>
        <td class="c-p"><%= r.P %></td>
        <td class="c-a"><%= r.A %></td>
        <td class="c-lt"><%= r.LT %></td>
        <td class="c-z"><%= r.Z %></td>
        <td class="c-h"><%= r.H %></td>
        <td class="c-g"><%= r.G %></td>
        <td class="c-k"><%= r.K %></td>
        <td class="c-e"><%= r.E %></td>
        <td class="c-er"><%= r.ER %></td>
        <td class="c-b"><%= r.B %></td>
        <td class="c-f"><%= r.F %></td>
        <td><span class="att-pct <%= r.AttPercent >= 50 ? "ok" : "" %>"><%= r.AttPercent.ToString("0.0") %>%</span></td>
      </tr>
    <% } } %>
    </tbody>
  </table>
<% } else { %>
  <table class="att-table">
    <thead>
      <tr>
        <th>#</th>
        <th>Emp Code</th>
        <th>Biometric ID</th>
        <th>Employee Name</th>
        <th>Designation</th>
        <th>Department</th>
        <th>Division</th>
        <th>Region</th>
        <th>Location</th>
        <th>Date</th>
        <th>In Time</th>
        <th>Out Time</th>
        <th>Status</th>
      </tr>
    </thead>
    <tbody>
    <% if (!HasData || DetailRows.Count == 0) { %>
      <tr><td colspan="13" style="text-align:center;padding:1.5rem;color:#6b7280;">No attendance records found for the selected filters.</td></tr>
    <% } else {
         var start = (Filter.Page - 1) * Filter.PageSize;
         for (var i = 0; i < DetailRows.Count; i++) {
           var r = DetailRows[i];
    %>
      <tr>
        <td><%= start + i + 1 %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.EmpCode) ? "\u2014" : r.EmpCode) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.BiometricId) ? "\u2014" : r.BiometricId) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.EmployeeName) ? "\u2014" : r.EmployeeName) %><% if (!r.MatchedEmployee) { %> <span style="color:#94a3b8;font-size:.7rem;">(unmatched)</span><% } %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.Designation) ? "\u2014" : r.Designation) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.Department) ? "\u2014" : r.Department) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.Division) ? "\u2014" : r.Division) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.Region) ? "\u2014" : r.Region) %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(r.Location) ? "\u2014" : r.Location) %></td>
        <td><%= Server.HtmlEncode(r.AttDate) %></td>
        <td><%= Server.HtmlEncode(r.InTime) %></td>
        <td><%= Server.HtmlEncode(r.OutTime) %></td>
        <td><span class="att-badge <%= StatusCodeClass(r.StatusCode) %>"><%= Server.HtmlEncode(r.StatusCode) %> &mdash; <%= Server.HtmlEncode(r.StatusLabel) %></span></td>
      </tr>
    <% } } %>
    </tbody>
  </table>
<% } %>
</div>

<% if (HasData && TotalPages > 1) { %>
<div class="att-pager">
  <% if (Filter.Page > 1) { %><a href="<%= BuildPageUrl(Filter.Page - 1) %>">Prev</a><% } %>
  <span class="current">Page <%= Filter.Page %> / <%= TotalPages %></span>
  <% if (Filter.Page < TotalPages) { %><a href="<%= BuildPageUrl(Filter.Page + 1) %>">Next</a><% } %>
</div>
<% } %>

<script>
(function () {
  var base = '<%= ResolveUrl("~/EmployeeAttendance.aspx") %>';
  var view = '<%= Server.HtmlEncode(ActiveView) %>';
  var btn = document.getElementById('btnProcess');
  if (!btn) return;
  btn.addEventListener('click', function () {
    function v(id) { var el = document.getElementById(id); return el ? el.value : ''; }
    var q = [
      'process=1',
      'page=1',
      'view=' + encodeURIComponent(view),
      'startDate=' + encodeURIComponent(v('fStartDate')),
      'endDate=' + encodeURIComponent(v('fEndDate')),
      'empCode=' + encodeURIComponent(v('fEmpCode')),
      'employeeName=' + encodeURIComponent(v('fEmployeeName')),
      'department=' + encodeURIComponent(v('fDepartment')),
      'location=' + encodeURIComponent(v('fLocation')),
      'division=' + encodeURIComponent(v('fDivision')),
      'designation=' + encodeURIComponent(v('fDesignation') || 'All')
    ].join('&');
    window.location.href = base + '?' + q;
  });
})();
</script>
</asp:Content>
