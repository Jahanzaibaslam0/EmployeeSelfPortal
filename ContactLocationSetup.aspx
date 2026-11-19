<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactLocationSetup.aspx.cs" Inherits="HRMS.ContactLocationSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<div class="card">
  <div class="card-header"><h2>Contact Address Lookups</h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Maintain each address part separately. Contact Master then uses these as lookups, in order: Country &rarr; State &rarr; City &rarr; Postal Code &rarr; Street.</p>
    <div class="form-grid">
      <div class="form-group"><a class="btn btn-primary" href="/CountrySetup.aspx">Country Setup</a></div>
      <div class="form-group"><a class="btn btn-primary" href="/StateSetup.aspx">State Setup</a></div>
      <div class="form-group"><a class="btn btn-primary" href="/ContactCitySetup.aspx">City Setup</a></div>
      <div class="form-group"><a class="btn btn-primary" href="/PostalSetup.aspx">Postal Setup</a></div>
      <div class="form-group"><a class="btn btn-primary" href="/StreetSetup.aspx">Street Setup</a></div>
    </div>
  </div>
</div>
</asp:Content>
