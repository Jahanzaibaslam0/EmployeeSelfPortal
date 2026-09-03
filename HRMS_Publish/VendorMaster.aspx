<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="VendorMaster.aspx.cs" Inherits="HRMS.VendorMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Vendor List</h2>
    <a href="/VendorMaster.aspx?newVendor=1" class="btn btn-primary">+ New Vendor</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Code</th><th>Name</th><th>Search</th><th>City</th><th>Group</th><th>NTN</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var v in Vendors) { %>
      <tr>
        <td><%= Server.HtmlEncode(v.VendorCode) %></td>
        <td><%= Server.HtmlEncode(v.Name) %></td>
        <td><%= Server.HtmlEncode(v.SearchName) %></td>
        <td><%= Server.HtmlEncode(v.CityName) %></td>
        <td><%= Server.HtmlEncode(v.CustomerGroupName) %></td>
        <td><%= Server.HtmlEncode(v.NTN) %></td>
        <td><%= v.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/VendorMaster.aspx?editId=<%= v.VendorID %>">Edit</a>
          <% if (v.IsActive) { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= v.VendorID %>';return confirm('Remove this vendor?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Vendors.Count == 0) { %><tr><td colspan="8">No vendors found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/VendorMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Edit Vendor" : "New Vendor" %> <%= Server.HtmlEncode(Input.VendorCode) %></span>
</div>
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Vendor" : "Add Vendor" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="VendorID" value="<%= Input.VendorID %>" />
    <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
    <div class="form-grid">
      <div class="form-group"><label>Vendor ID</label>
        <input type="text" class="form-control" value="<%= Server.HtmlEncode(Input.VendorCode) %>" readonly /></div>
      <div class="form-group"><label>Name *</label>
        <input type="text" name="Name" class="form-control" value="<%= Server.HtmlEncode(Input.Name) %>" required /></div>
      <div class="form-group"><label>Search Name</label>
        <input type="text" name="SearchName" class="form-control" value="<%= Server.HtmlEncode(Input.SearchName) %>" /></div>

      <div class="form-group"><label>Deal For Branch</label>
        <select name="DealForBranchID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Locations) { %><option value="<%= item.Id %>" <%= Input.DealForBranchID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>City</label>
        <select name="CityID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Cities) { %><option value="<%= item.Id %>" <%= Input.CityID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Province</label>
        <select name="ProvinceID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Provinces) { %><option value="<%= item.Id %>" <%= Input.ProvinceID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>

      <div class="form-group"><label>Mode of Delivery</label>
        <select name="ModeOfDeliveryID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in ModeOfDeliveries) { %><option value="<%= item.Id %>" <%= Input.ModeOfDeliveryID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Group</label>
        <select name="CustomerGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in CustomerGroups) { %><option value="<%= item.Id %>" <%= Input.CustomerGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Classification</label>
        <select name="CustomerClassID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in CustomerClasses) { %><option value="<%= item.Id %>" <%= Input.CustomerClassID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Method of Payment</label>
        <select name="MethodOfPaymentID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in MethodOfPayments) { %><option value="<%= item.Id %>" <%= Input.MethodOfPaymentID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Terms of Payment</label>
        <select name="TermsOfPaymentID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in TermsOfPayments) { %><option value="<%= item.Id %>" <%= Input.TermsOfPaymentID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Currency</label>
        <select name="CurrencyID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Currencies) { %><option value="<%= item.Id %>" <%= Input.CurrencyID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Bill Preference</label>
        <select name="BillPreferenceID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in BillPreferences) { %><option value="<%= item.Id %>" <%= Input.BillPreferenceID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>FBR Status</label>
        <select name="FBRStatusID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in FBRStatuses) { %><option value="<%= item.Id %>" <%= Input.FBRStatusID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Tax Group</label>
        <select name="TaxGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in TaxGroups) { %><option value="<%= item.Id %>" <%= Input.TaxGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>

      <div class="form-group"><label>CNIC</label><input type="text" name="CNIC" class="form-control" value="<%= Server.HtmlEncode(Input.CNIC) %>" /></div>
      <div class="form-group"><label>NTN</label><input type="text" name="NTN" class="form-control" value="<%= Server.HtmlEncode(Input.NTN) %>" /></div>

      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsCAP" value="true" <%= Input.IsCAP?"checked":"" %> /> CAP</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsMandatoryCreditLimit" value="true" <%= Input.IsMandatoryCreditLimit?"checked":"" %> /> Mandatory Credit Limit</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsInvoiceHold" value="true" <%= Input.IsInvoiceHold?"checked":"" %> /> Invoice Hold</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>

      <div class="form-group"><label>Total Business Potential</label><input type="text" name="TotalBusinessPotential" class="form-control" value="<%= Server.HtmlEncode(Input.TotalBusinessPotential) %>" /></div>
      <div class="form-group"><label>Target Share %</label><input type="text" name="TargetBusinessSharePercent" class="form-control" value="<%= Server.HtmlEncode(Input.TargetBusinessSharePercent) %>" /></div>
      <div class="form-group"><label>Target Amount</label><input type="text" name="TargetBusinessAmount" class="form-control" value="<%= Server.HtmlEncode(Input.TargetBusinessAmount) %>" /></div>
      <div class="form-group"><label>Credit Limit</label><input type="text" name="CreditLimit" class="form-control" value="<%= Server.HtmlEncode(Input.CreditLimit) %>" /></div>
      <div class="form-group"><label>AHD Credit Limit</label><input type="text" name="AHDCreditLimit" class="form-control" value="<%= Server.HtmlEncode(Input.AHDCreditLimit) %>" /></div>
      <div class="form-group"><label>PHD Credit Limit</label><input type="text" name="PHDCreditLimit" class="form-control" value="<%= Server.HtmlEncode(Input.PHDCreditLimit) %>" /></div>
      <div class="form-group"><label>HHD Credit Limit</label><input type="text" name="HHDCreditLimit" class="form-control" value="<%= Server.HtmlEncode(Input.HHDCreditLimit) %>" /></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <a href="/VendorMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
