<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GoodsIssueMaster.aspx.cs" Inherits="HRMS.GoodsIssueMasterPage" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>.readonly-field { background:#f3f4f6; color:#374151; cursor:not-allowed; }</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header" style="display:flex;justify-content:space-between;align-items:center;">
    <h2>Goods Issues</h2>
    <div>
      <a href="/InventoryMaster.aspx" class="btn btn-secondary">&#8592; Inventory</a>
      <a href="/GoodsIssueMaster.aspx?newIssue=1" class="btn btn-primary">+ Issue Goods</a>
    </div>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>GIN #</th><th>Date</th><th>SO #</th><th>Customer</th><th>Total Qty</th></tr></thead>
      <tbody>
      <% if (Issues.Count == 0) { %><tr><td colspan="5" style="text-align:center;padding:2rem;">No goods issues found.</td></tr><% } %>
      <% foreach (var r in Issues) { %>
      <tr>
        <td><code><%= Server.HtmlEncode(r.GoodsIssueCode) %></code></td>
        <td><%= r.IssueDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(r.SalesOrderCode) %></td>
        <td><%= Server.HtmlEncode(r.CustomerName) %></td>
        <td><%= r.TotalQty.ToString("N4") %></td>
      </tr>
      <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<input type="hidden" name="LinesJson" id="LinesJson" />
<div class="card">
  <div class="card-header"><h2>Issue Goods Against Sales Order</h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Issue Date *</label>
        <input type="date" name="IssueDate" class="form-control" value="<%= Server.HtmlEncode(IssueDate) %>" required /></div>
      <div class="form-group"><label>Sales Order *</label>
        <select name="SalesOrderID" id="ddlSO" class="form-control" required>
          <option value="">-- Select SO --</option>
          <% foreach (var so in OpenSalesOrders) { %><option value="<%= so.Id %>" <%= so.Id == SelectedSoId ? "selected" : "" %>><%= Server.HtmlEncode(so.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Customer</label>
        <input type="text" name="CustomerName" id="txtCustomer" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(CustomerName) %>" /></div>
      <div class="form-group full-width"><label>Remarks</label>
        <textarea name="Remarks" class="form-control" rows="2"><%= Server.HtmlEncode(Remarks) %></textarea></div>
    </div>
    <div class="table-responsive" style="margin-top:1rem;">
      <table class="data-table" id="giLineTable">
        <thead><tr><th>Product</th><th>Description</th><th>Ordered</th><th>Issued</th><th>On Hand</th><th>Issue Now</th></tr></thead>
        <tbody></tbody>
      </table>
    </div>
  </div>
  <div class="card-footer" style="display:flex;gap:.75rem;">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return prepareGI();">Post Issue &amp; Update Stock</button>
    <a href="/GoodsIssueMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<script type="application/json" id="initialGILines"><% Response.Write(LinesJsonInitial); %></script>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (ShowForm) { %>
<script>
function buildGIRow(d) {
  d = d || {};
  var tr = document.createElement('tr');
  tr.innerHTML = '<td><input type="hidden" class="so-item-id" value="' + (d.salesOrderItemID || d.SalesOrderItemID || '') + '" />' +
    '<input type="hidden" class="product-id" value="' + (d.productID || d.ProductID || '') + '" />' +
    '<input type="text" class="form-control readonly-field product-code" readonly value="' + (d.productCode || d.ProductCode || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field product-desc" readonly value="' + (d.productDescription || d.ProductDescription || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field" readonly value="' + (d.orderedQty || d.OrderedQty || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field" readonly value="' + (d.alreadyIssued || d.AlreadyIssued || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field stock-on-hand" readonly value="' + (d.stockOnHand || d.StockOnHand || '') + '" /></td>' +
    '<td><input type="number" step="0.0001" min="0" class="form-control issue-qty" value="' + (d.issueQty || d.IssueQty || '') + '" /></td>';
  return tr;
}
function prepareGI() {
  var lines = Array.from(document.querySelectorAll('#giLineTable tbody tr')).map(function (tr) {
    return {
      salesOrderItemID: parseInt(tr.querySelector('.so-item-id').value, 10) || 0,
      productID: parseInt(tr.querySelector('.product-id').value, 10) || 0,
      productCode: tr.querySelector('.product-code').value,
      productDescription: tr.querySelector('.product-desc').value,
      issueQty: tr.querySelector('.issue-qty').value
    };
  }).filter(function (l) { return parseFloat(l.issueQty) > 0; });
  if (!lines.length) { alert('Enter issue quantity for at least one line.'); return false; }
  document.getElementById('LinesJson').value = JSON.stringify(lines);
  return true;
}
document.addEventListener('DOMContentLoaded', function () {
  var ddl = document.getElementById('ddlSO');
  var initial = [];
  try { initial = JSON.parse(document.getElementById('initialGILines').textContent || '[]'); } catch (e) {}
  if (initial.length) {
    var tbody = document.querySelector('#giLineTable tbody');
    initial.forEach(function (d) { tbody.appendChild(buildGIRow(d)); });
  }
  if (ddl) {
    ddl.addEventListener('change', function () {
      if (!this.value) return;
      window.location = '<%= ResolveUrl("~/GoodsIssueMaster.aspx") %>?newIssue=1&soId=' + encodeURIComponent(this.value);
    });
  }
});
</script>
<% } %>
</asp:Content>
