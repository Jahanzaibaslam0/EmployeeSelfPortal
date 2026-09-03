<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MyDocuments.aspx.cs" Inherits="HRMS.MyDocumentsPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>

<p style="margin-bottom:.75rem;">
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/Home.aspx") %>">Back to Home</a>
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/UserProfile.aspx") %>">My Profile</a>
  <% if (IsAdminView) { %>
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/EmployeeMaster.aspx") %>">Employee Master</a>
  <% } %>
</p>

<div class="card">
  <div class="card-header space-between">
    <h2><%= PageTitle %></h2>
    <% if (IsAdminView) { %><span class="badge badge-warning">Admin view</span><% } %>
  </div>
  <div class="card-body">
    <% if (!string.IsNullOrEmpty(ScopeNote)) { %>
    <p class="text-muted" style="margin-top:0;margin-bottom:1rem;"><%= Server.HtmlEncode(ScopeNote) %></p>
    <% } %>

    <% if (Documents.Count == 0) { %>
    <div class="empty-state">
      <span class="icon">&#128196;</span>
      <p>No documents found.</p>
    </div>
    <% } else { %>
    <div class="doc-toolbar">
      <div class="search-box">
        <input type="text" id="txtSearch" class="form-control" placeholder="Search documents..." />
        <select id="ddlStatusFilter" class="form-control">
          <option value="">All Status</option>
          <option value="Verified">Verified</option>
          <option value="Pending">Pending</option>
          <option value="Rejected">Rejected</option>
        </select>
        <% if (IsAdminView) { %>
        <select id="ddlEmployeeFilter" class="form-control">
          <option value="">All Employees</option>
          <% foreach (var emp in EmployeeOptions) { %>
          <option value="<%= Server.HtmlEncode(emp.EmployeeCode) %>"><%= Server.HtmlEncode(emp.EmployeeCode) %> – <%= Server.HtmlEncode(emp.EmployeeName) %></option>
          <% } %>
        </select>
        <% } %>
      </div>
      <span class="doc-count" id="docCount"><%= Documents.Count %> document(s)</span>
    </div>

    <div style="overflow-x:auto;">
      <table class="doc-table" id="docTable">
        <thead>
          <tr>
            <th>#</th>
            <% if (IsAdminView) { %><th>Employee</th><% } %>
            <th>Document Type</th>
            <th>Document No.</th>
            <th>Issue Date</th>
            <th>Expiry Date</th>
            <th>Status</th>
            <th>Remarks</th>
            <th>File</th>
          </tr>
        </thead>
        <tbody>
          <% for (var i = 0; i < Documents.Count; i++) {
               var doc = Documents[i];
               var statusClass = StatusBadgeClass(doc.VerificationStatus);
               var fileHref = DocumentHref(doc.DocumentPath);
          %>
          <tr data-emp="<%= Server.HtmlEncode(doc.EmployeeCode) %>" data-status="<%= Server.HtmlEncode(doc.VerificationStatus) %>">
            <td><%= i + 1 %></td>
            <% if (IsAdminView) { %>
            <td><%= Server.HtmlEncode(doc.EmployeeCode) %> – <%= Server.HtmlEncode(doc.EmployeeName) %></td>
            <% } %>
            <td><%= Server.HtmlEncode(doc.DocumentTypeName) %></td>
            <td><%= Server.HtmlEncode(doc.DocumentNumber) %></td>
            <td><%= Server.HtmlEncode(doc.IssueDate) %></td>
            <td><%= Server.HtmlEncode(doc.ExpiryDate) %></td>
            <td><span class="badge-status <%= statusClass %>"><%= Server.HtmlEncode(doc.VerificationStatus) %></span></td>
            <td><%= Server.HtmlEncode(doc.Remarks) %></td>
            <td>
              <% if (!string.IsNullOrEmpty(fileHref)) { %>
              <a href="<%= Server.HtmlEncode(fileHref) %>" target="_blank" rel="noopener noreferrer" class="doc-link" title="<%= Server.HtmlEncode(doc.OriginalFileName) %>">
                <span class="icon">&#128206;</span> View
              </a>
              <% } %>
            </td>
          </tr>
          <% } %>
        </tbody>
      </table>
    </div>
    <% } %>
  </div>
</div>

<script>
(function () {
    var search = document.getElementById('txtSearch');
    var statusFilter = document.getElementById('ddlStatusFilter');
    var empFilter = document.getElementById('ddlEmployeeFilter');
    var table = document.getElementById('docTable');
    var countEl = document.getElementById('docCount');

    if (!table) return;
    var rows = Array.prototype.slice.call(table.querySelectorAll('tbody tr'));

    function applyFilters() {
        var term = (search && search.value ? search.value : '').toLowerCase();
        var status = statusFilter ? statusFilter.value : '';
        var emp = empFilter ? empFilter.value : '';
        var visible = 0;

        rows.forEach(function (row) {
            var text = row.textContent.toLowerCase();
            var matchText = !term || text.indexOf(term) >= 0;
            var matchStatus = !status || row.getAttribute('data-status') === status;
            var matchEmp = !emp || row.getAttribute('data-emp') === emp;
            var show = matchText && matchStatus && matchEmp;
            row.style.display = show ? '' : 'none';
            if (show) visible++;
        });

        if (countEl) countEl.textContent = visible + ' document(s)';
    }

    if (search) search.addEventListener('input', applyFilters);
    if (statusFilter) statusFilter.addEventListener('change', applyFilters);
    if (empFilter) empFilter.addEventListener('change', applyFilters);
})();
</script>
</asp:Content>
