<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CountrySetup.aspx.cs" Inherits="HRMS.CountrySetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
  <div class="card-header"><h2><%= Input.CountryID > 0 ? "Edit Country" : "Add Country" %></h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Maintain country master data. Used as a lookup on Contact Master addresses.</p>
    <input type="hidden" name="countryID" value="<%= Input.CountryID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Country *</label>
        <input type="text" name="countryName" class="form-control" value="<%= Server.HtmlEncode(Input.CountryName) %>" maxlength="100" required /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.CountryID > 0 ? "Update Country" : "Save Country" %></button>
    <a href="/CountrySetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/ContactLocationSetup.aspx" class="btn btn-secondary">All address lookups</a>
  </div>
</div>
<div class="card mt-4">
  <div class="card-header"><h2>Country List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Country</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var row in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(row.CountryName) %></td>
        <td><%= row.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/CountrySetup.aspx?editId=<%= row.CountryID %>">Edit</a>
          <% if (row.IsActive) { %><button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= row.CountryID %>';return confirm('Deactivate this country?');">X</button><% } %>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="3">No countries found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
