<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.EmployeeReportPage" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
#reportTable { font-size: .8rem; white-space: nowrap; }
#reportTable th {
  position: sticky; top: 0; z-index: 1;
  background: #2E3192; color: #fff; font-weight: 600;
}
.report-toolbar { display: flex; flex-wrap: wrap; gap: .75rem; align-items: center; }
.report-toolbar form { display: flex; gap: .5rem; align-items: center; }
.report-toolbar .form-control { max-width: 320px; }
.dir-count { font-size: .85rem; color: #6b7280; }
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card">
  <div class="card-header space-between">
    <h2>Internal Employee Directory</h2>
    <div class="report-toolbar">
      <form method="get" action="<%= ResolveUrl("~/EmployeeReport.aspx") %>">
        <input type="text" name="search" id="txtSearch" class="form-control"
               value="<%= Server.HtmlEncode(Search) %>" placeholder="Search employees&hellip;" />
        <button type="submit" class="btn btn-secondary">Search</button>
        <% if (!string.IsNullOrEmpty(Search)) { %>
        <a href="<%= ResolveUrl("~/EmployeeReport.aspx") %>" class="btn btn-secondary">Clear</a>
        <% } %>
      </form>
      <% if (CanExport) { %>
      <a id="exportLink" class="btn btn-primary"
         href="<%= ResolveUrl("~/EmployeeReport.aspx") %>?handler=DownloadExcel<%= string.IsNullOrEmpty(Search) ? "" : "&search=" + Server.UrlEncode(Search) %>">
        Export to Excel
      </a>
      <% } %>
    </div>
  </div>
  <div class="card-body table-responsive" style="max-height:70vh;overflow:auto;">
    <table class="data-table" id="reportTable">
      <thead class="grid-header">
        <tr>
          <% foreach (var h in DirectoryHeaders) { %>
          <th><%= Server.HtmlEncode(h) %></th>
          <% } %>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="18">No employee records found.</td></tr>
        <% } else {
             foreach (var row in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(row.Extension) %></td>
          <td><%= Server.HtmlEncode(row.CellNo) %></td>
          <td><%= Server.HtmlEncode(row.Email) %></td>
          <td><%= Server.HtmlEncode(row.PowerID) %></td>
          <td><%= Server.HtmlEncode(row.UserID) %></td>
          <td><%= Server.HtmlEncode(row.Title) %></td>
          <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(row.EmployeeCode) %></code></td>
          <td><%= Server.HtmlEncode(row.PositionCode) %></td>
          <td><%= Server.HtmlEncode(row.Position) %></td>
          <td><%= Server.HtmlEncode(row.TitleGrade) %></td>
          <td><%= Server.HtmlEncode(row.Department) %></td>
          <td><%= Server.HtmlEncode(row.Division) %></td>
          <td><%= Server.HtmlEncode(row.Region) %></td>
          <td><%= Server.HtmlEncode(row.Team) %></td>
          <td><%= Server.HtmlEncode(row.Location) %></td>
          <td><%= Server.HtmlEncode(row.SalesGroup) %></td>
          <td><%= Server.HtmlEncode(row.State) %></td>
          <td><%= Server.HtmlEncode(row.BasedCity) %></td>
        </tr>
        <% } } %>
      </tbody>
    </table>
  </div>
  <div class="card-footer dir-count">
    Showing <%= Records.Count %> record(s)
    <% if (!string.IsNullOrEmpty(Search)) { %> matching &ldquo;<%= Server.HtmlEncode(Search) %>&rdquo;<% } %>
  </div>
</div>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
(function () {
  var input = document.getElementById('txtSearch');
  var exportLink = document.getElementById('exportLink');
  if (!input || !exportLink) return;
  input.addEventListener('input', function () {
    var q = input.value.trim();
    exportLink.href = '<%= ResolveUrl("~/EmployeeReport.aspx") %>?handler=DownloadExcel' +
      (q ? '&search=' + encodeURIComponent(q) : '');
  });
})();
</script>
</asp:Content>
