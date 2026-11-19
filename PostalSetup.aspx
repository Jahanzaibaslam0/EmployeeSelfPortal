<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PostalSetup.aspx.cs" Inherits="HRMS.PostalSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
  <div class="card-header"><h2><%= Input.PostalID > 0 ? "Edit Postal Code" : "Add Postal Code" %></h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Each postal code belongs to a city. Used as a lookup on Contact Master addresses (Country &rarr; State &rarr; City &rarr; Postal &rarr; Street).</p>
    <input type="hidden" name="postalID" value="<%= Input.PostalID %>" />
    <div class="form-grid">
      <div class="form-group"><label>City *</label>
        <select name="cityID" class="form-control">
          <option value="0">-- Select city --</option>
          <% foreach (var c in Cities) { if (!c.IsActive && c.CityID != Input.CityID) continue; %>
          <option value="<%= c.CityID %>" <%= Input.CityID==c.CityID?"selected":"" %>><%= Server.HtmlEncode(c.CountryName + " / " + c.StateName + " / " + c.CityName) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Postal Code *</label>
        <input type="text" name="postalCode" class="form-control" value="<%= Server.HtmlEncode(Input.PostalCode) %>" maxlength="20" required /></div>
      <div class="form-group"><label>Area Name</label>
        <input type="text" name="areaName" class="form-control" value="<%= Server.HtmlEncode(Input.AreaName) %>" maxlength="150" placeholder="Area / locality against this code" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.PostalID > 0 ? "Update Postal" : "Save Postal" %></button>
    <a href="/PostalSetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/ContactCitySetup.aspx" class="btn btn-secondary">City Setup</a>
  </div>
</div>
<div class="card mt-4">
  <div class="card-header"><h2>Postal Code List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Country</th><th>State</th><th>City</th><th>Postal Code</th><th>Area Name</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var row in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(row.CountryName) %></td>
        <td><%= Server.HtmlEncode(row.StateName) %></td>
        <td><%= Server.HtmlEncode(row.CityName) %></td>
        <td><%= Server.HtmlEncode(row.PostalCode) %></td>
        <td><%= Server.HtmlEncode(row.AreaName) %></td>
        <td><%= row.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/PostalSetup.aspx?editId=<%= row.PostalID %>">Edit</a>
          <% if (row.IsActive) { %><button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= row.PostalID %>';return confirm('Deactivate this postal code?');">X</button><% } %>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="7">No postal codes found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
