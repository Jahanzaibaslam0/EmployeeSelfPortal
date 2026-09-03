<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="HRMS.DashboardPage" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
<style>
.dashboard-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: .75rem;
    margin-bottom: 1rem;
}
.dashboard-meta {
    font-size: .78rem;
    color: var(--text-muted, #6b7280);
}
.dashboard-filters .form-grid {
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
}
.kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: .85rem;
    margin-bottom: 1.25rem;
}
.kpi-card {
    background: var(--bg-card, #fff);
    border: 1px solid var(--border, #e5e7eb);
    border-radius: var(--radius, 8px);
    padding: .85rem 1rem;
    box-shadow: var(--shadow, 0 1px 2px rgba(0,0,0,.05));
}
.kpi-card .kpi-label {
    font-size: .72rem;
    font-weight: 600;
    color: var(--text-muted, #6b7280);
    text-transform: uppercase;
    letter-spacing: .04em;
    margin-bottom: .25rem;
}
.kpi-card .kpi-value {
    font-size: 1.45rem;
    font-weight: 700;
    color: var(--gb-blue, #2E3192);
    line-height: 1.2;
}
.kpi-card.kpi-alert .kpi-value { color: var(--gb-red, #E31E24); }
.kpi-card.kpi-success .kpi-value { color: var(--success, #16a34a); }
.chart-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 1rem;
    margin-bottom: 1.25rem;
}
.chart-card {
    background: var(--bg-card, #fff);
    border: 1px solid var(--border, #e5e7eb);
    border-radius: var(--radius, 8px);
    box-shadow: var(--shadow, 0 1px 2px rgba(0,0,0,.05));
    overflow: hidden;
}
.chart-card-header {
    padding: .65rem 1rem;
    border-bottom: 1px solid var(--border, #e5e7eb);
    font-weight: 700;
    font-size: .88rem;
    color: var(--gb-blue, #2E3192);
}
.chart-card-body {
    padding: .75rem 1rem 1rem;
    height: 260px;
    position: relative;
}
.chart-card-body.tall { height: 320px; }
.alert-panel { margin-bottom: 1.25rem; }
.alert-item {
    display: flex;
    align-items: flex-start;
    gap: .75rem;
    padding: .65rem .85rem;
    border-radius: 8px;
    border: 1px solid var(--border, #e5e7eb);
    background: #fff;
    margin-bottom: .5rem;
    font-size: .82rem;
}
.alert-item.severity-danger { border-left: 4px solid var(--danger, #dc2626); background: #fef2f2; }
.alert-item.severity-warning { border-left: 4px solid var(--warning, #d97706); background: #fffbeb; }
.alert-item.severity-info { border-left: 4px solid var(--gb-blue, #2E3192); background: rgba(46,49,146,.04); }
.alert-item-title { font-weight: 700; color: var(--text-dark, #111827); }
.alert-item-detail { color: var(--text-muted, #6b7280); font-size: .78rem; margin-top: .15rem; }
.alert-cat {
    font-size: .68rem;
    font-weight: 700;
    text-transform: uppercase;
    color: var(--text-muted, #6b7280);
}
.leave-list { list-style: none; padding: 0; margin: 0; }
.leave-list li {
    display: flex;
    justify-content: space-between;
    padding: .45rem 0;
    border-bottom: 1px solid var(--border, #e5e7eb);
    font-size: .82rem;
}
.leave-list li:last-child { border-bottom: none; }
.dashboard-actions { display: flex; gap: .5rem; flex-wrap: wrap; }
.two-col {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    margin-bottom: 1.25rem;
}
@media (max-width: 900px) {
    .two-col { grid-template-columns: 1fr; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<%
    var k = Data.Kpis;
%>
<div class="dashboard-toolbar">
  <div class="dashboard-meta">
    Last updated: <span id="dashboardUpdated"><%= Data.GeneratedAt.ToString("dd MMM yyyy HH:mm") %></span>
  </div>
  <div class="dashboard-actions">
    <button type="button" class="btn btn-secondary" id="btnRefresh">Refresh</button>
    <a href="<%= BuildHandlerUrl("ExportExcel") %>" class="btn btn-secondary">Export Excel</a>
    <a href="<%= BuildHandlerUrl("ExportCsv") %>" class="btn btn-secondary">Export CSV</a>
    <% if (!string.IsNullOrEmpty(Data.AttendancePortalUrl)) { %>
    <a href="<%= Server.HtmlEncode(Data.AttendancePortalUrl) %>" class="btn btn-primary" target="_blank" rel="noopener">Attendance Portal</a>
    <% } %>
  </div>
</div>

<div class="card">
  <div class="card-header"><h2>Dashboard Filters</h2></div>
  <div class="dashboard-filters">
    <div class="card-body">
      <div class="form-grid">
        <div class="form-group">
          <label>Division</label>
          <select id="fltDivisionId" name="divisionId" class="form-control">
            <option value="0">All Divisions</option>
            <% foreach (var o in Divisions) { %>
            <option value="<%= o.Id %>" <%= Filters.DivisionId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Department</label>
          <select id="fltDepartmentId" name="departmentId" class="form-control">
            <option value="0">All Departments</option>
            <% foreach (var o in Departments) { %>
            <option value="<%= o.Id %>" <%= Filters.DepartmentId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Region</label>
          <select id="fltRegionId" name="regionId" class="form-control">
            <option value="0">All Regions</option>
            <% foreach (var o in Regions) { %>
            <option value="<%= o.Id %>" <%= Filters.RegionId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Location</label>
          <select id="fltLocationId" name="locationId" class="form-control">
            <option value="0">All Locations</option>
            <% foreach (var o in Locations) { %>
            <option value="<%= o.Id %>" <%= Filters.LocationId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Employment Type</label>
          <select id="fltEmploymentTypeId" name="employmentTypeId" class="form-control">
            <option value="0">All Types</option>
            <% foreach (var o in EmploymentTypes) { %>
            <option value="<%= o.Id %>" <%= Filters.EmploymentTypeId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Worker Category</label>
          <select id="fltWorkerCategoryId" name="workerCategoryId" class="form-control">
            <option value="0">All Categories</option>
            <% foreach (var o in WorkerCategories) { %>
            <option value="<%= o.Id %>" <%= Filters.WorkerCategoryId == o.Id ? "selected" : "" %>><%= Server.HtmlEncode(o.Name) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Date From</label>
          <input type="date" id="fltDateFrom" name="dateFrom" class="form-control" value="<%= Filters.DateFrom.ToString("yyyy-MM-dd") %>" />
        </div>
        <div class="form-group">
          <label>Date To</label>
          <input type="date" id="fltDateTo" name="dateTo" class="form-control" value="<%= Filters.DateTo.ToString("yyyy-MM-dd") %>" />
        </div>
      </div>
    </div>
    <div class="card-footer">
      <button type="button" class="btn btn-primary" id="btnApplyFilters">Apply Filters</button>
      <a href="<%= ResolveUrl("~/Dashboard.aspx") %>" class="btn btn-secondary">Reset</a>
    </div>
  </div>
</div>

<div class="kpi-grid" id="kpiGrid">
  <div class="kpi-card">
    <div class="kpi-label">Total Employees</div>
    <div class="kpi-value" data-kpi="totalEmployees"><%= k.TotalEmployees %></div>
  </div>
  <div class="kpi-card kpi-success">
    <div class="kpi-label">Active Employees</div>
    <div class="kpi-value" data-kpi="activeEmployees"><%= k.ActiveEmployees %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Inactive Employees</div>
    <div class="kpi-value" data-kpi="inactiveEmployees"><%= k.InactiveEmployees %></div>
  </div>
  <div class="kpi-card kpi-success">
    <div class="kpi-label">New Hires (Month)</div>
    <div class="kpi-value" data-kpi="newHiresThisMonth"><%= k.NewHiresThisMonth %></div>
  </div>
  <div class="kpi-card kpi-alert">
    <div class="kpi-label">Separations (Month)</div>
    <div class="kpi-value" data-kpi="separationsThisMonth"><%= k.SeparationsThisMonth %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">On Leave Today</div>
    <div class="kpi-value" data-kpi="employeesOnLeaveToday"><%= k.EmployeesOnLeaveToday %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Leave Apps (Period)</div>
    <div class="kpi-value" data-kpi="pendingLeaveRequests"><%= k.PendingLeaveRequests %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Total Leave Apps</div>
    <div class="kpi-value" data-kpi="totalLeaveApplications"><%= k.TotalLeaveApplications %></div>
  </div>
  <div class="kpi-card kpi-success">
    <div class="kpi-label">Attendance Rate %</div>
    <div class="kpi-value" data-kpi="attendanceRate"><%= k.AttendanceRate.HasValue ? k.AttendanceRate.Value.ToString("0.0") : "—" %></div>
  </div>
  <div class="kpi-card kpi-alert">
    <div class="kpi-label">Absenteeism Rate %</div>
    <div class="kpi-value" data-kpi="absenteeismRate"><%= k.AbsenteeismRate.HasValue ? k.AbsenteeismRate.Value.ToString("0.0") : "—" %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Open Vacancies</div>
    <div class="kpi-value" data-kpi="openVacancies"><%= k.OpenVacancies %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">On Probation</div>
    <div class="kpi-value" data-kpi="probationEmployees"><%= k.ProbationEmployees %></div>
  </div>
  <div class="kpi-card kpi-alert">
    <div class="kpi-label">Contract Expiry (30d)</div>
    <div class="kpi-value" data-kpi="contractExpiryAlerts"><%= k.ContractExpiryAlerts %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Birthdays (30d)</div>
    <div class="kpi-value" data-kpi="upcomingBirthdays"><%= k.UpcomingBirthdays %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Anniversaries (30d)</div>
    <div class="kpi-value" data-kpi="upcomingAnniversaries"><%= k.UpcomingAnniversaries %></div>
  </div>
  <div class="kpi-card kpi-alert">
    <div class="kpi-label">Pending Expenses</div>
    <div class="kpi-value" data-kpi="pendingExpenses"><%= k.PendingExpenses %></div>
  </div>
  <div class="kpi-card kpi-alert">
    <div class="kpi-label">Doc Expiry (30d)</div>
    <div class="kpi-value" data-kpi="documentExpiryAlerts"><%= k.DocumentExpiryAlerts %></div>
  </div>
  <div class="kpi-card">
    <div class="kpi-label">Recruitment Pipeline</div>
    <div class="kpi-value" data-kpi="recruitmentInPipeline"><%= k.RecruitmentInPipeline %></div>
  </div>
</div>

<div class="chart-grid">
  <div class="chart-card">
    <div class="chart-card-header">Employees by Division</div>
    <div class="chart-card-body"><canvas id="chartDivision"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Employees by Department (Top 10)</div>
    <div class="chart-card-body"><canvas id="chartDepartment"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Employees by Region</div>
    <div class="chart-card-body"><canvas id="chartRegion"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Employees by Gender</div>
    <div class="chart-card-body"><canvas id="chartGender"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Employment Type</div>
    <div class="chart-card-body"><canvas id="chartEmploymentType"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Age Groups</div>
    <div class="chart-card-body"><canvas id="chartAgeGroup"></canvas></div>
  </div>
  <div class="chart-card">
    <div class="chart-card-header">Leave by Category</div>
    <div class="chart-card-body"><canvas id="chartLeaveCategory"></canvas></div>
  </div>
</div>

<div class="chart-card" style="margin-bottom:1.25rem;">
  <div class="chart-card-header">Monthly Trends</div>
  <div class="chart-card-body tall"><canvas id="chartMonthlyTrends"></canvas></div>
</div>

<div class="two-col">
  <div class="card alert-panel">
    <div class="card-header space-between">
      <h2>Alerts</h2>
      <span class="dashboard-meta"><%= Data.Alerts.Count %> alert(s)</span>
    </div>
    <div class="card-body" id="alertsPanel">
      <% if (Data.Alerts.Count == 0) { %>
      <p class="empty-row">No alerts at this time.</p>
      <% } else {
           foreach (var alert in Data.Alerts) {
             var link = AspxLink(alert.Link);
      %>
      <div class="alert-item severity-<%= Server.HtmlEncode(alert.Severity) %>">
        <div style="flex:1;">
          <div class="alert-cat"><%= Server.HtmlEncode(alert.Category) %></div>
          <div class="alert-item-title"><%= Server.HtmlEncode(alert.Title) %></div>
          <div class="alert-item-detail"><%= Server.HtmlEncode(alert.Detail) %></div>
        </div>
        <% if (!string.IsNullOrEmpty(link)) { %>
        <a href="<%= Server.HtmlEncode(link) %>" class="btn btn-secondary" style="font-size:.75rem; white-space:nowrap;">View</a>
        <% } %>
      </div>
      <% } } %>
    </div>
  </div>

  <div class="card">
    <div class="card-header space-between">
      <h2>Employees on Leave Today</h2>
      <span class="dashboard-meta"><%= Data.EmployeesOnLeaveList.Count %> employee(s)</span>
    </div>
    <div class="card-body">
      <% if (Data.EmployeesOnLeaveList.Count == 0) { %>
      <p class="empty-row">No employees on leave today.</p>
      <% } else { %>
      <ul class="leave-list">
        <% foreach (var emp in Data.EmployeesOnLeaveList) { %>
        <li>
          <span><%= Server.HtmlEncode(emp.Label) %></span>
          <span><%= emp.Count %> day(s)</span>
        </li>
        <% } %>
      </ul>
      <% } %>
    </div>
  </div>
</div>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script type="application/json" id="chartData"><%= ChartJson %></script>
<script src="<%= ResolveUrl("~/js/dashboard.js") %>?v=2"></script>
<script>
(function () {
    function val(id) {
        var el = document.getElementById(id);
        return el ? el.value : '';
    }
    function applyFilters() {
        var params = new URLSearchParams();
        var map = {
            divisionId: 'fltDivisionId',
            departmentId: 'fltDepartmentId',
            regionId: 'fltRegionId',
            locationId: 'fltLocationId',
            employmentTypeId: 'fltEmploymentTypeId',
            workerCategoryId: 'fltWorkerCategoryId',
            dateFrom: 'fltDateFrom',
            dateTo: 'fltDateTo'
        };
        Object.keys(map).forEach(function (key) {
            var v = val(map[key]);
            if (v === null || v === undefined || v === '') return;
            params.set(key, v);
        });
        window.location.href = '<%= ResolveUrl("~/Dashboard.aspx") %>?' + params.toString();
    }
    var btn = document.getElementById('btnApplyFilters');
    if (btn) btn.addEventListener('click', applyFilters);
})();
</script>
</asp:Content>
