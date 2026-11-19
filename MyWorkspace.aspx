<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MyWorkspace.aspx.cs" Inherits="HRMS.MyWorkspacePage" ResponseEncoding="utf-8" %>
<%@ Import Namespace="System.Linq" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.ws-dash { max-width: 1280px; margin: 0 auto; }
.ws-dash-kicker {
    font-size: .72rem; font-weight: 700; letter-spacing: .12em; text-transform: uppercase;
    color: var(--gb-blue); margin: 0 0 .35rem;
}
.ws-dash-head {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;
    margin-bottom: 1.25rem; flex-wrap: wrap;
}
.ws-dash-head h1 {
    font-family: var(--font-heading); font-size: 1.55rem; color: var(--gb-blue);
    margin: 0; font-weight: 700;
}
.ws-dash-head h1 span { font-weight: 600; color: var(--text-muted); }
.ws-dash-sub { color: var(--text-muted); margin: .25rem 0 0; font-size: .9rem; }
.ws-dash-actions { display: flex; gap: .5rem; flex-wrap: wrap; }
.ws-kpi-grid {
    display: grid; grid-template-columns: repeat(4, 1fr); gap: .85rem; margin-bottom: 1rem;
}
.ws-kpi {
    background: #fff; border: 1px solid var(--border); border-radius: 12px;
    padding: 1rem 1.1rem; box-shadow: var(--shadow); min-height: 118px;
}
.ws-kpi-icon {
    width: 32px; height: 32px; border-radius: 8px; display: inline-flex; align-items: center;
    justify-content: center; background: var(--gb-blue-light); color: var(--gb-blue);
    margin-bottom: .45rem;
}
.ws-kpi-icon svg, .ws-qa svg, .ws-action-go svg {
    width: 16px; height: 16px; display: block;
}
.ws-kpi strong { display: block; font-size: 1.6rem; line-height: 1.1; color: var(--gb-blue); }
.ws-kpi em { display: block; font-style: normal; font-weight: 600; color: var(--text-dark); margin-top: .35rem; }
.ws-kpi span { display: block; color: var(--text-muted); font-size: .78rem; }
.ws-dash-row {
    display: grid; grid-template-columns: 1.4fr .9fr; gap: .85rem; margin-bottom: .85rem;
}
.ws-panel {
    background: #fff; border: 1px solid var(--border); border-radius: 12px;
    box-shadow: var(--shadow); overflow: hidden; min-height: 180px;
}
.ws-panel-h {
    display: flex; justify-content: space-between; align-items: center; gap: .75rem;
    padding: .85rem 1.1rem; border-bottom: 1px solid var(--border);
}
.ws-panel-h h2 { margin: 0; font-size: 1rem; color: var(--gb-blue); font-family: var(--font-heading); }
.ws-panel-h a { font-size: .82rem; font-weight: 600; color: var(--gb-blue); text-decoration: none; }
.ws-panel-h a:hover { color: var(--gb-red); }
.ws-panel-b { padding: .4rem 0; }
.ws-action {
    display: flex; justify-content: space-between; align-items: center; gap: .75rem;
    padding: .75rem 1.1rem; text-decoration: none; color: inherit; border-bottom: 1px solid var(--border);
}
.ws-action:last-child { border-bottom: 0; }
.ws-action:hover { background: var(--gb-blue-light); }
.ws-action strong { display: block; color: var(--text-dark); }
.ws-action span { display: block; color: var(--text-muted); font-size: .78rem; }
.ws-qa { display: grid; grid-template-columns: 1fr 1fr; gap: .65rem; padding: 1rem; }
.ws-qa a {
    display: flex; align-items: center; gap: .55rem; padding: .85rem .9rem;
    border: 1px solid var(--border); border-radius: 10px; text-decoration: none;
    color: var(--text-dark); font-weight: 600; background: #fff;
}
.ws-qa a:hover { border-color: var(--gb-blue); color: var(--gb-blue); }
.ws-qa-ico {
    width: 28px; height: 28px; border-radius: 8px; display: inline-flex; align-items: center;
    justify-content: center; background: var(--gb-blue-light); color: var(--gb-blue); flex-shrink: 0;
}
.ws-action-go { color: var(--gb-blue); flex-shrink: 0; }
.ws-qa-note { padding: 0 1rem 1rem; color: var(--text-muted); font-size: .78rem; }
.ws-know { padding: .35rem 0 .5rem; }
.ws-know a {
    display: block; padding: .7rem 1.1rem; text-decoration: none; color: inherit;
    border-bottom: 1px solid var(--border);
}
.ws-know a:last-child { border-bottom: 0; }
.ws-know a:hover { background: var(--gb-blue-light); }
.ws-know strong { display: block; color: var(--gb-blue); }
.ws-know span { display: block; color: var(--text-muted); font-size: .78rem; }
.ws-empty { padding: 1.25rem 1.1rem; color: var(--text-muted); }
.ws-profile { padding: 1rem 1.1rem; display: grid; gap: .45rem; }
.ws-profile div { display: flex; justify-content: space-between; gap: 1rem; font-size: .88rem; }
.ws-profile dt { color: var(--text-muted); }
.ws-profile dd { margin: 0; font-weight: 600; text-align: right; }
.ws-bars { display: flex; gap: .55rem; flex-wrap: wrap; padding: .85rem 1.1rem 0; }
.ws-bar {
    flex: 1; min-width: 90px; background: #f4f5fb; border-radius: 8px; padding: .55rem .7rem;
}
.ws-bar b { display: block; color: var(--gb-blue); font-size: 1.05rem; }
.ws-bar span { color: var(--text-muted); font-size: .75rem; }
.req-badge {
    display: inline-block; padding: 2px 10px; border-radius: 999px;
    font-size: .72rem; font-weight: 700; background: #e5e7eb; color: #374151;
}
.req-approved { background: #dcfce7; color: #166534; }
.req-rejected { background: #fee2e2; color: #b91c1c; }
.req-returned { background: #fef3c7; color: #92400e; }
.req-draft { background: #e5e7eb; color: #4b5563; }
.req-pending { background: #dbeafe; color: #1d4ed8; }
.leave-badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: .72rem; font-weight: 700; background: #e5e7eb; }
.leave-pending-dh, .leave-pending-hr { background: #fef3c7; color: #92400e; }
.leave-approved { background: #dcfce7; color: #166534; }
.leave-rejected { background: #fee2e2; color: #b91c1c; }
@media (max-width: 1100px) {
    .ws-kpi-grid { grid-template-columns: 1fr 1fr; }
    .ws-dash-row { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
    .ws-kpi-grid, .ws-qa { grid-template-columns: 1fr; }
}
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="ws-dash">
    <div class="ws-dash-head">
        <div>
            <p class="ws-dash-kicker">Personalised workspace</p>
            <h1><%= Server.HtmlEncode(Model.Greeting) %> &middot; <span><%= Server.HtmlEncode(Model.RoleLabel) %></span></h1>
            <p class="ws-dash-sub">
                <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(Model.DisplayName) ? "Tasks, requests and everyday HR services in one workspace." : Model.DisplayName) %>
                <% if (!string.IsNullOrWhiteSpace(Model.DepartmentName)) { %>
                    &middot; <%= Server.HtmlEncode(Model.DepartmentName) %>
                <% } %>
                &middot; <%= Server.HtmlEncode(Model.ScopeCaption) %>
            </p>
        </div>
        <div class="ws-dash-actions">
            <% if (Model.QuickActions.Any(a => a.Visible && a.Url == "/EmployeeRequest.aspx")) { %>
            <a class="btn btn-primary" href="/EmployeeRequest.aspx">+ New request</a>
            <% } %>
            <a class="btn btn-secondary" href="/Home.aspx">Workstation home</a>
        </div>
    </div>

    <div class="ws-kpi-grid">
        <div class="ws-kpi">
            <div class="ws-kpi-icon"><%= WsIcon("check") %></div>
            <strong><%= Server.HtmlEncode(Model.ActionsKpi.Value) %></strong>
            <em><%= Server.HtmlEncode(Model.ActionsKpi.Label) %></em>
            <span><%= Server.HtmlEncode(Model.ActionsKpi.Hint) %></span>
        </div>
        <div class="ws-kpi">
            <div class="ws-kpi-icon"><%= WsIcon("doc") %></div>
            <strong><%= Server.HtmlEncode(Model.RequestsKpi.Value) %></strong>
            <em><%= Server.HtmlEncode(Model.RequestsKpi.Label) %></em>
            <span><%= Server.HtmlEncode(Model.RequestsKpi.Hint) %></span>
        </div>
        <div class="ws-kpi">
            <div class="ws-kpi-icon"><%= WsIcon("clock") %></div>
            <strong><%= Server.HtmlEncode(Model.AttendanceKpi.Value) %></strong>
            <em><%= Server.HtmlEncode(Model.AttendanceKpi.Label) %></em>
            <span><%= Server.HtmlEncode(Model.AttendanceKpi.Hint) %></span>
        </div>
        <div class="ws-kpi">
            <div class="ws-kpi-icon"><%= WsIcon("megaphone") %></div>
            <strong><%= Server.HtmlEncode(Model.NoticesKpi.Value) %></strong>
            <em><%= Server.HtmlEncode(Model.NoticesKpi.Label) %></em>
            <span><%= Server.HtmlEncode(Model.NoticesKpi.Hint) %></span>
        </div>
    </div>

    <div class="ws-dash-row">
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>Action inbox</h2>
                <a href="/Inbox.aspx">View all tasks</a>
            </div>
            <% if (Model.Actions.Count == 0) { %>
            <p class="ws-empty">Nothing waiting for you right now.</p>
            <% } else { foreach (var a in Model.Actions) { %>
            <a class="ws-action" href="<%= Server.HtmlEncode(a.Url) %>">
                <div>
                    <strong><%= Server.HtmlEncode(a.Title) %></strong>
                    <span><%= Server.HtmlEncode(a.Subtitle) %></span>
                </div>
                <span class="ws-action-go"><%= WsIcon("chevron") %></span>
            </a>
            <% } } %>
        </section>
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>Quick actions</h2>
                <span class="text-muted" style="font-size:.78rem;"><%= Model.IsManagerView ? "Role-aware" : "Personal" %></span>
            </div>
            <div class="ws-qa">
            <% foreach (var q in Model.QuickActions.Where(x => x.Visible).Take(4)) { %>
                <a href="<%= Server.HtmlEncode(q.Url) %>"><span class="ws-qa-ico"><%= WsIcon(q.Icon) %></span><%= Server.HtmlEncode(q.Title) %></a>
            <% } %>
            </div>
            <p class="ws-qa-note">Shortcuts open the existing modules. Approvals and saves still run on those pages.</p>
        </section>
    </div>

    <div class="ws-dash-row">
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>Recent requests</h2>
                <a href="/EmployeeRequest.aspx">Open request tracker</a>
            </div>
            <div class="table-responsive">
                <table class="data-table">
                    <thead class="grid-header">
                        <tr>
                            <th>Request</th>
                            <th>Type</th>
                            <% if (Model.IsManagerView) { %><th>Employee</th><% } %>
                            <th>Status</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                    <% if (Model.Requests.Count == 0) { %>
                        <tr class="empty-row"><td colspan="<%= Model.IsManagerView ? 5 : 4 %>">No visible requests.</td></tr>
                    <% } else { var i = 0; foreach (var r in Model.Requests) { %>
                        <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
                            <td><%= Server.HtmlEncode(r.RequestNumber) %></td>
                            <td><%= Server.HtmlEncode(r.Category) %></td>
                            <% if (Model.IsManagerView) { %><td><%= Server.HtmlEncode(r.EmployeeName) %></td><% } %>
                            <td><span class="<%= Server.HtmlEncode(r.StatusBadgeClass) %>"><%= Server.HtmlEncode(r.StatusDisplay) %></span></td>
                            <td><a href="<%= Server.HtmlEncode(r.OpenUrl) %>">Open</a></td>
                        </tr>
                    <% } } %>
                    </tbody>
                </table>
            </div>
        </section>
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>Announcements &amp; knowledge</h2>
                <a href="/Notifications.aspx">Browse library</a>
            </div>
            <div class="ws-know">
            <% if (Model.Announcements.Count == 0) { %>
                <p class="ws-empty">No published notices for your scope.</p>
            <% } else { foreach (var n in Model.Announcements) { %>
                <a href="<%= Server.HtmlEncode(n.Url) %>">
                    <strong><%= Server.HtmlEncode(n.Title) %></strong>
                    <span><%= Server.HtmlEncode(n.Scope) %><% if (!string.IsNullOrWhiteSpace(n.Summary)) { %> &middot; <%= Server.HtmlEncode(n.Summary) %><% } %></span>
                </a>
            <% } } %>
            </div>
        </section>
    </div>

    <div class="ws-dash-row">
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2><%= Model.IsManagerView ? "Team leave" : "My leave" %></h2>
                <a href="/LeaveMaster.aspx">Leave applications</a>
            </div>
            <% if (Model.PendingLeaveCount > 0 || Model.LeaveDaysThisYear > 0) { %>
            <div class="ws-bars">
                <div class="ws-bar"><b><%= Model.PendingLeaveCount %></b><span>Pending applications</span></div>
                <div class="ws-bar"><b><%= Model.LeaveDaysThisYear.ToString("0.##") %></b><span>Approved days this year</span></div>
            </div>
            <% } %>
            <div class="table-responsive">
                <table class="data-table">
                    <thead class="grid-header">
                        <tr>
                            <% if (Model.IsManagerView) { %><th>Employee</th><% } %>
                            <th>Type</th>
                            <th>Dates</th>
                            <th>Days</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                    <% if (Model.Leaves.Count == 0) { %>
                        <tr class="empty-row"><td colspan="<%= Model.IsManagerView ? 5 : 4 %>">No leave records to show.</td></tr>
                    <% } else { var i = 0; foreach (var l in Model.Leaves) { %>
                        <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
                            <% if (Model.IsManagerView) { %><td><%= Server.HtmlEncode(l.EmployeeName) %></td><% } %>
                            <td><%= Server.HtmlEncode(string.IsNullOrWhiteSpace(l.Category) ? l.LeaveType : l.Category) %></td>
                            <td><%= l.FromDate.HasValue ? l.FromDate.Value.ToString("dd-MMM") : "&ndash;" %>
                                &ndash; <%= l.ToDate.HasValue ? l.ToDate.Value.ToString("dd-MMM") : "&ndash;" %></td>
                            <td><%= l.Days.ToString("0.##") %></td>
                            <td><span class="<%= HRMS.Services.LeaveApprovalStatuses.BadgeClass(l.Status) %>"><%= Server.HtmlEncode(l.StatusDisplay) %></span></td>
                        </tr>
                    <% } } %>
                    </tbody>
                </table>
            </div>
        </section>
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2><%= Model.IsManagerView ? "Team attendance" : "My attendance" %></h2>
                <% if (Model.CanOpenAttendance) { %><a href="/EmployeeAttendance.aspx">Attendance records</a><% } %>
            </div>
            <% if (Model.AttendanceBreakdown.Count > 0) { %>
            <div class="ws-bars">
                <% foreach (var s in Model.AttendanceBreakdown.Take(4)) { %>
                <div class="ws-bar"><b><%= s.Count %></b><span><%= Server.HtmlEncode(string.IsNullOrWhiteSpace(s.Label) ? s.Code : s.Label) %></span></div>
                <% } %>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(Model.AttendanceNote) && Model.Attendance.Count == 0) { %>
            <p class="ws-empty"><%= Server.HtmlEncode(Model.AttendanceNote) %></p>
            <% } else { %>
            <div class="table-responsive">
                <table class="data-table">
                    <thead class="grid-header">
                        <tr><th>Employee</th><th>Status</th><th>In</th><th>Out</th></tr>
                    </thead>
                    <tbody>
                    <% if (Model.Attendance.Count == 0) { %>
                        <tr class="empty-row"><td colspan="4">No attendance rows for today.</td></tr>
                    <% } else { var i = 0; foreach (var a in Model.Attendance) { %>
                        <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
                            <td><%= Server.HtmlEncode(string.IsNullOrWhiteSpace(a.EmployeeName) ? a.EmpCode : a.EmployeeName) %></td>
                            <td><%= Server.HtmlEncode(a.Status) %></td>
                            <td><%= Server.HtmlEncode(a.InTime) %></td>
                            <td><%= Server.HtmlEncode(a.OutTime) %></td>
                        </tr>
                    <% } } %>
                    </tbody>
                </table>
            </div>
            <% } %>
        </section>
    </div>

    <div class="ws-dash-row">
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>Pending tasks</h2>
                <a href="/TaskMaster.aspx">Task management</a>
            </div>
            <div class="table-responsive">
                <table class="data-table">
                    <thead class="grid-header">
                        <tr>
                            <th>Task</th>
                            <th>Priority</th>
                            <th>Status</th>
                            <th>Due</th>
                            <% if (Model.IsManagerView) { %><th>Assigned</th><% } %>
                        </tr>
                    </thead>
                    <tbody>
                    <% if (Model.Tasks.Count == 0) { %>
                        <tr class="empty-row"><td colspan="<%= Model.IsManagerView ? 5 : 4 %>">No open tasks assigned in this scope.</td></tr>
                    <% } else { var i = 0; foreach (var t in Model.Tasks) { %>
                        <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
                            <td><a href="/TaskMaster.aspx?editId=<%= t.TaskID %>"><%= Server.HtmlEncode(string.IsNullOrWhiteSpace(t.Title) ? t.TaskCode : t.Title) %></a></td>
                            <td><%= Server.HtmlEncode(t.Priority) %></td>
                            <td><%= Server.HtmlEncode(t.TaskStatus) %></td>
                            <td><%= t.DueDate.HasValue ? t.DueDate.Value.ToString("dd-MMM-yyyy") : "&ndash;" %></td>
                            <% if (Model.IsManagerView) { %><td><%= Server.HtmlEncode(t.AssignedToName) %></td><% } %>
                        </tr>
                    <% } } %>
                    </tbody>
                </table>
            </div>
        </section>
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>My documents</h2>
                <a href="/MyDocuments.aspx">Open documents</a>
            </div>
            <div class="table-responsive">
                <table class="data-table">
                    <thead class="grid-header">
                        <tr>
                            <% if (Model.IsManagerView) { %><th>Employee</th><% } %>
                            <th>Type</th>
                            <th>Number</th>
                            <th>Expiry</th>
                        </tr>
                    </thead>
                    <tbody>
                    <% if (Model.Documents.Count == 0) { %>
                        <tr class="empty-row"><td colspan="<%= Model.IsManagerView ? 4 : 3 %>">No documents in this workspace.</td></tr>
                    <% } else { var i = 0; foreach (var d in Model.Documents) { %>
                        <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
                            <% if (Model.IsManagerView) { %><td><%= Server.HtmlEncode(d.EmployeeName) %></td><% } %>
                            <td><%= Server.HtmlEncode(d.DocumentType) %></td>
                            <td><%= Server.HtmlEncode(d.DocumentNumber) %></td>
                            <td><%= Server.HtmlEncode(d.ExpiryDate) %></td>
                        </tr>
                    <% } } %>
                    </tbody>
                </table>
            </div>
        </section>
    </div>

    <div class="ws-dash-row" style="grid-template-columns: 1fr;">
        <section class="ws-panel">
            <div class="ws-panel-h">
                <h2>My profile</h2>
                <a href="/UserProfile.aspx">Manage profile</a>
            </div>
            <% if (!Model.HasLinkedEmployee) { %>
            <p class="ws-empty">No employee record is linked to this login. Open My Profile to review account mapping.</p>
            <% } else { %>
            <dl class="ws-profile">
                <div><dt>Name</dt><dd><%= Server.HtmlEncode(Model.DisplayName) %></dd></div>
                <div><dt>Employee code</dt><dd><%= Server.HtmlEncode(Model.EmployeeCode) %></dd></div>
                <div><dt>Department</dt><dd><%= string.IsNullOrWhiteSpace(Model.DepartmentName) ? "&ndash;" : Server.HtmlEncode(Model.DepartmentName) %></dd></div>
                <div><dt>Job</dt><dd><%= string.IsNullOrWhiteSpace(Model.JobTitle) ? "&ndash;" : Server.HtmlEncode(Model.JobTitle) %></dd></div>
                <div><dt>Email</dt><dd><%= string.IsNullOrWhiteSpace(Model.Email) ? "&ndash;" : Server.HtmlEncode(Model.Email) %></dd></div>
                <div><dt>Mobile</dt><dd><%= string.IsNullOrWhiteSpace(Model.Mobile) ? "&ndash;" : Server.HtmlEncode(Model.Mobile) %></dd></div>
            </dl>
            <% } %>
        </section>
    </div>
</div>
</asp:Content>
