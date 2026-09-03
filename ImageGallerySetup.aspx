<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.ImageGallerySetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card"><div class="card-header"><h2><%= PageTitle %></h2></div>
<div class="card-body table-responsive"><table class="data-table"><thead><tr>
<% foreach (System.Data.DataColumn c in Rows.Columns) { %><th><%= c.ColumnName %></th><% } %></tr></thead><tbody>
<% foreach (System.Data.DataRow r in Rows.Rows) { %><tr>
<% foreach (System.Data.DataColumn c in Rows.Columns) { %><td><%= Server.HtmlEncode(r[c]==System.DBNull.Value?"":r[c].ToString()) %></td><% } %></tr><% } %>
<% if (Rows.Rows.Count==0) { %><tr><td colspan="99">No records.</td></tr><% } %>
</tbody></table></div></div>
</asp:Content>
