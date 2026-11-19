<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="StreetSetup.aspx.cs" Inherits="HRMS.StreetSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
  <div class="card-header"><h2><%= Input.StreetID > 0 ? "Edit Street" : "Add Street" %></h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Each street belongs to a postal code. Used as a lookup on Contact Master addresses.</p>
    <input type="hidden" name="streetID" value="<%= Input.StreetID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Postal Code *</label>
        <select name="postalID" class="form-control">
          <option value="0">-- Select postal code --</option>
          <% foreach (var p in Postals) { if (!p.IsActive && p.PostalID != Input.PostalID) continue; %>
          <option value="<%= p.PostalID %>" <%= Input.PostalID==p.PostalID?"selected":"" %>><%= Server.HtmlEncode(p.CountryName + " / " + p.StateName + " / " + p.CityName + " / " + p.PostalDisplay) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Street *</label>
        <input type="text" name="streetName" class="form-control" value="<%= Server.HtmlEncode(Input.StreetName) %>" maxlength="200" required /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.StreetID > 0 ? "Update Street" : "Save Street" %></button>
    <a href="/StreetSetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/PostalSetup.aspx" class="btn btn-secondary">Postal Setup</a>
  </div>
</div>
<div class="card mt-4">
  <div class="card-header"><h2>Street List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>State</th><th>City</th><th>Postal Code</th><th>Street</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var row in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(row.StateName) %></td>
        <td><%= Server.HtmlEncode(row.CityName) %></td>
        <td><%= Server.HtmlEncode(row.PostalCode) %></td>
        <td><%= Server.HtmlEncode(row.StreetName) %></td>
        <td><%= row.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/StreetSetup.aspx?editId=<%= row.StreetID %>">Edit</a>
          <% if (row.IsActive) { %><button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= row.StreetID %>';return confirm('Deactivate this street?');">X</button><% } %>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="6">No streets found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
