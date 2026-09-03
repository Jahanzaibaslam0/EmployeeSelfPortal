<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="InvoiceMaster.aspx.cs" Inherits="HRMS.InvoiceMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Invoice List</h2>
    <a href="/InvoiceMaster.aspx?newInvoice=1" class="btn btn-primary">+ New Invoice</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Code</th><th>Date</th><th>Type</th><th>Customer</th><th>Ref No</th><th>Total</th><th>Lines</th><th></th></tr></thead>
      <tbody>
      <% foreach (var inv in Invoices) { %>
      <tr>
        <td><%= Server.HtmlEncode(inv.InvoiceCode) %></td>
        <td><%= inv.InvoiceDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(inv.InvoiceType) %></td>
        <td><%= Server.HtmlEncode(inv.CustomerName) %></td>
        <td><%= Server.HtmlEncode(inv.InvoiceRefNo) %></td>
        <td><%= inv.TotalAmount.ToString("N2") %></td>
        <td><%= inv.LineCount %></td>
        <td>
          <a href="/InvoiceMaster.aspx?editId=<%= inv.InvoiceID %>">Edit</a>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= inv.InvoiceID %>';return confirm('Delete this invoice?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Invoices.Count == 0) { %><tr><td colspan="8">No invoices found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/InvoiceMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Edit Invoice" : "New Invoice" %> <%= Server.HtmlEncode(Input.InvoiceCode) %></span>
</div>
<input type="hidden" name="InvoiceID" value="<%= Input.InvoiceID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<input type="hidden" name="ItemsJson" id="ItemsJson" />
<input type="hidden" name="TotalAmount" id="TotalAmount" value="<%= Server.HtmlEncode(Input.TotalAmount) %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Invoice" : "Add Invoice" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Invoice Code</label>
        <input type="text" name="InvoiceCode" class="form-control" value="<%= Server.HtmlEncode(Input.InvoiceCode) %>" readonly /></div>
      <div class="form-group"><label>Invoice Date *</label>
        <input type="date" name="InvoiceDate" id="txtInvoiceDate" class="form-control" value="<%= Server.HtmlEncode(Input.InvoiceDate) %>" required /></div>
      <div class="form-group"><label>Invoice Type</label>
        <select name="InvoiceType" class="form-control"><option value="">-- Select --</option>
        <% foreach (var t in InvoiceTypeOptions) { %><option value="<%= t %>" <%= Input.InvoiceType==t?"selected":"" %>><%= t %></option><% } %>
        </select></div>
      <div class="form-group"><label>Invoice Ref No</label>
        <input type="text" name="InvoiceRefNo" class="form-control" value="<%= Server.HtmlEncode(Input.InvoiceRefNo) %>" /></div>
      <div class="form-group"><label>Buyer Name</label>
        <input type="text" name="BuyerName" class="form-control" value="<%= Server.HtmlEncode(Input.BuyerName) %>" /></div>
      <div class="form-group"><label>Buyer NTN/CNIC</label>
        <input type="text" name="BuyerNTNCNIC" class="form-control" value="<%= Server.HtmlEncode(Input.BuyerNTNCNIC) %>" /></div>
      <div class="form-group"><label>Buyer Province</label>
        <input type="text" name="BuyerProvince" class="form-control" value="<%= Server.HtmlEncode(Input.BuyerProvince) %>" /></div>
      <div class="form-group"><label>Buyer Registration Type</label>
        <select name="BuyerRegistrationType" class="form-control"><option value="">-- Select --</option>
        <% foreach (var t in BuyerRegistrationTypeOptions) { %><option value="<%= t %>" <%= Input.BuyerRegistrationType==t?"selected":"" %>><%= t %></option><% } %>
        </select></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Buyer Address</label>
        <textarea name="BuyerAddress" class="form-control" rows="2"><%= Server.HtmlEncode(Input.BuyerAddress) %></textarea></div>
      <div class="form-group"><label>Customer *</label>
        <select name="CustomerID" id="ddlCustomer" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var c in Customers) { %><option value="<%= c.Id %>" <%= Input.CustomerID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Customer Name</label>
        <input type="text" name="CustomerName" id="txtCustomerName" class="form-control" value="<%= Server.HtmlEncode(Input.CustomerName) %>" /></div>
      <div class="form-group"><label>Customer NTN/CNIC</label>
        <input type="text" name="CustomerNTNCNIC" id="txtCustomerNTNCNIC" class="form-control" value="<%= Server.HtmlEncode(Input.CustomerNTNCNIC) %>" /></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Customer Address</label>
        <textarea name="CustomerAddress" id="txtCustomerAddress" class="form-control" rows="2"><%= Server.HtmlEncode(Input.CustomerAddress) %></textarea></div>
    </div>
    <div class="card mt-4">
      <div class="card-header space-between"><h2>Invoice Line Items</h2>
        <button type="button" class="btn btn-secondary" onclick="addInvoiceItemRow()">+ Add Line</button></div>
      <div class="card-body table-responsive">
        <table class="data-table" id="invoiceItemTable">
          <thead><tr>
            <th>Product</th><th>Item ID</th><th>HS Code</th><th>Product Name</th><th>Qty</th><th>UOM</th>
            <th>Unit Price</th><th>Tax</th><th>Extra Tax</th><th>FED</th><th>Sales Type</th>
            <th>SRO Serial</th><th>Further Tax</th><th>Discount</th><th>Line Total</th><th></th>
          </tr></thead>
          <tbody></tbody>
        </table>
      </div>
    </div>
    <div style="text-align:right;margin-top:1rem;font-weight:700;">Grand Total: <span id="lblGrandTotal"><%= Server.HtmlEncode(Input.TotalAmount) %></span></div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return prepareInvoicePayload();"><%= EditMode ? "Update Invoice" : "Save Invoice" %></button>
    <a href="/InvoiceMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<script type="application/json" id="productLookupData"><%= ProductsJson %></script>
<script type="application/json" id="salesTypeOptionsData"><%= SalesTypesJson %></script>
<script type="application/json" id="initialInvoiceItemsData"><%= ItemsJsonInit %></script>
<script src="/js/invoice.js?v=1"></script>
<% } %>
</asp:Content>
