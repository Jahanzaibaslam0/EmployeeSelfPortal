<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UserProfile.aspx.cs" Inherits="HRMS.UserProfilePage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<p><a class="btn btn-secondary" href="/Home.aspx">Back to Home</a></p>
</asp:Content>
