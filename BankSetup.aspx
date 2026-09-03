<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BankSetup.aspx.cs" Inherits="HRMS.BankSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<div class="card">
  <div class="card-header"><h2><%= Input.BankID > 0 ? "Edit Bank Master" : "Add Bank Master" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="bankID" value="<%= Input.BankID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Bank Name *</label>
        <input type="text" name="bankName" class="form-control" value="<%= Server.HtmlEncode(Input.BankName) %>" maxlength="150" /></div>
      <div class="form-group"><label>Bank Code</label>
        <input type="text" name="bankCode" class="form-control" value="<%= Server.HtmlEncode(Input.BankCode) %>" maxlength="50" /></div>
      <div class="form-group"><label>Location Name</label>
        <input type="text" name="locationName" class="form-control" value="<%= Server.HtmlEncode(Input.LocationName) %>" maxlength="150" /></div>
      <div class="form-group"><label>Account Title</label>
        <input type="text" name="accountTitle" class="form-control" value="<%= Server.HtmlEncode(Input.AccountTitle) %>" maxlength="150" /></div>
      <div class="form-group"><label>Bank Group</label>
        <select name="bankGroupID" class="form-control"><option value="0">-- Select Bank Group --</option>
        <% foreach (var g in BankGroups) { %><option value="<%= g.Id %>" <%= Input.BankGroupID==g.Id?"selected":"" %>><%= Server.HtmlEncode(g.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>IBAN</label>
        <input type="text" name="iban" class="form-control" value="<%= Server.HtmlEncode(Input.IBAN) %>" maxlength="50" /></div>
      <div class="form-group"><label>Swift/BIC Code</label>
        <input type="text" name="swiftBICCode" class="form-control" value="<%= Server.HtmlEncode(Input.SwiftBICCode) %>" maxlength="50" /></div>
      <div class="form-group"><label>Currency Code</label>
        <select name="currencyCode" class="form-control"><option value="">-- Select Currency --</option>
        <% foreach (var c in Currencies) { %><option value="<%= Server.HtmlEncode(c.CurrencyCode) %>" <%= Input.CurrencyCode==c.CurrencyCode?"selected":"" %>><%= Server.HtmlEncode(c.CurrencyCode + " - " + c.CurrencyName) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Account Verification Status</label>
        <select name="accountVerificationStatus" class="form-control">
          <% foreach (var status in new[] { "Pending", "Verified", "Rejected", "Not Required" }) { %>
          <option value="<%= status %>" <%= Input.AccountVerificationStatus==status?"selected":"" %>><%= status %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';"><%= Input.BankID > 0 ? "Update Bank" : "Save Bank" %></button>
    <a href="/BankSetup.aspx" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header"><h2>Bank Master List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr>
        <th>Bank Name</th><th>Bank Code</th><th>Location</th><th>Bank Group</th>
        <th>IBAN</th><th>Swift/BIC</th><th>Currency</th><th>Verification</th><th>Status</th><th></th>
      </tr></thead>
      <tbody>
      <% foreach (var bank in Banks) { %>
      <tr>
        <td><%= Server.HtmlEncode(bank.BankName) %></td>
        <td><%= Server.HtmlEncode(bank.BankCode) %></td>
        <td><%= Server.HtmlEncode(bank.LocationName) %></td>
        <td><%= Server.HtmlEncode(bank.BankGroupName) %></td>
        <td><%= Server.HtmlEncode(bank.IBAN) %></td>
        <td><%= Server.HtmlEncode(bank.SwiftBICCode) %></td>
        <td><%= Server.HtmlEncode(bank.CurrencyCode) %></td>
        <td><%= Server.HtmlEncode(bank.AccountVerificationStatus) %></td>
        <td><%= bank.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/BankSetup.aspx?editId=<%= bank.BankID %>">Edit</a>
          <% if (bank.IsActive) { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= bank.BankID %>';return confirm('Remove?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Banks.Count == 0) { %><tr><td colspan="10">No bank records found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
