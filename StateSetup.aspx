<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="StateSetup.aspx.cs" Inherits="HRMS.StateSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
  <div class="card-header"><h2><%= Input.StateID > 0 ? "Edit State" : "Add State" %></h2></div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Each state belongs to a country. Used as a lookup on Contact Master addresses.</p>
    <input type="hidden" name="stateID" value="<%= Input.StateID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Country *</label>
        <select name="countryID" class="form-control">
          <option value="0">-- Select country --</option>
          <% foreach (var c in Countries) { %><option value="<%= c.Id %>" <%= Input.CountryID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>State *</label>
        <input type="text" name="stateName" class="form-control" value="<%= Server.HtmlEncode(Input.StateName) %>" maxlength="100" required /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.StateID > 0 ? "Update State" : "Save State" %></button>
    <a href="/StateSetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/CountrySetup.aspx" class="btn btn-secondary">Country Setup</a>
    <a href="/ContactCitySetup.aspx" class="btn btn-secondary">City Setup</a>
  </div>
</div>
<div class="card mt-4">
  <div class="card-header"><h2>State List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Country</th><th>State</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var row in Records) { %>
      <tr>
        <td><%= Server.HtmlEncode(row.CountryName) %></td>
        <td><%= Server.HtmlEncode(row.StateName) %></td>
        <td><%= row.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/StateSetup.aspx?editId=<%= row.StateID %>">Edit</a>
          <% if (row.IsActive) { %><button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= row.StateID %>';return confirm('Deactivate this state?');">X</button><% } %>
        </td>
      </tr>
      <% } %>
      <% if (Records.Count == 0) { %><tr><td colspan="4">No states found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
