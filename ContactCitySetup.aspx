<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactCitySetup.aspx.cs" Inherits="HRMS.ContactCitySetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
  <div class="card-header"><h2><%= Input.CityID > 0 ? "Edit City" : "Add City" %></h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Each city belongs to a state. Used as a lookup on Contact Master addresses. This is separate from Organization City Setup.</p>
    <input type="hidden" name="cityID" value="<%= Input.CityID %>" />
    <div class="form-grid">
      <div class="form-group"><label>State *</label>
        <select name="stateID" class="form-control">
          <option value="0">-- Select state --</option>
          <% foreach (var s in States) { if (!s.IsActive && s.StateID != Input.StateID) continue; %>
          <option value="<%= s.StateID %>" <%= Input.StateID==s.StateID?"selected":"" %>><%= Server.HtmlEncode(s.CountryName + " / " + s.StateName) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>City *</label>
        <input type="text" name="cityName" class="form-control" value="<%= Server.HtmlEncode(Input.CityName) %>" maxlength="100" required /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.CityID > 0 ? "Update City" : "Save City" %></button>
    <a href="/ContactCitySetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/StateSetup.aspx" class="btn btn-secondary">State Setup</a>
    <a href="/PostalSetup.aspx" class="btn btn-secondary">Postal Setup</a>
  </div>
</div>
<div class="card mt-4">
  <div class="card-header"><h2>City List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Country</th><th>State</th><th>City</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var row in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(row.CountryName) %></td>
        <td><%= Server.HtmlEncode(row.StateName) %></td>
        <td><%= Server.HtmlEncode(row.CityName) %></td>
        <td><%= row.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/ContactCitySetup.aspx?editId=<%= row.CityID %>">Edit</a>
          <% if (row.IsActive) { %><button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= row.CityID %>';return confirm('Deactivate this city?');">X</button><% } %>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="5">No cities found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
