<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.AuditReportPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card"><div class="card-header"><h2>Audit Log (<%= TotalRecords %> shown)</h2></div>
<div class="card-body table-responsive"><table class="data-table">
<thead><tr><th>When</th><th>User</th><th>Action</th><th>Entity</th><th>Name</th><th>Details</th><th>OK</th></tr></thead>
<tbody>
<% foreach (var r in Records) { %>
<tr>
<td><%= r.ActionAt.ToString("dd MMM yyyy HH:mm") %></td>
<td><%= Server.HtmlEncode(r.Username) %></td>
<td><%= Server.HtmlEncode(r.ActionType) %></td>
<td><%= Server.HtmlEncode(r.EntityType) %></td>
<td><%= Server.HtmlEncode(r.EntityName) %></td>
<td><%= Server.HtmlEncode(r.Details) %></td>
<td><%= r.Success ? "Y" : "N" %></td>
</tr>
<% } %>
<% if (Records.Count==0) { %><tr><td colspan="7">No audit rows.</td></tr><% } %>
</tbody></table></div></div>
</asp:Content>
