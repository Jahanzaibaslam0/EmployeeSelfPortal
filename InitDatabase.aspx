<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="InitDatabase.aspx.cs" Inherits="HRMS.InitDatabasePage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(Message)) { %>
    <div class="alert alert-<%= AlertType %>" style="white-space:pre-wrap;font-family:Consolas,monospace;"><%= Server.HtmlEncode(Message) %></div>
<% } %>
<p><a class="btn btn-primary" href="/InitDatabase.aspx?run=1">Run Init Again</a>
   <a class="btn btn-secondary" href="/Login.aspx">Go to Login</a></p>
</asp:Content>
