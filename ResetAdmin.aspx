<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ResetAdmin.aspx.cs" Inherits="HRMS.ResetAdminPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(Message)) { %>
    <div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(Message) %></div>
<% } %>
<p><a class="btn btn-primary" href="/ResetAdmin.aspx?run=1">Reset Admin Password</a>
   <a class="btn btn-secondary" href="/Login.aspx">Go to Login</a></p>
</asp:Content>
