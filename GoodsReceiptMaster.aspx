<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GoodsReceiptMaster.aspx.cs" Inherits="HRMS.GoodsReceiptMasterPage" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>.readonly-field { background:#f3f4f6; color:#374151; cursor:not-allowed; }</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header" style="display:flex;justify-content:space-between;align-items:center;">
    <h2>Goods Receipts</h2>
    <div>
      <a href="/InventoryMaster.aspx" class="btn btn-secondary">&#8592; Inventory</a>
      <a href="/GoodsReceiptMaster.aspx?newReceipt=1" class="btn btn-primary">+ Receive Goods</a>
    </div>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>GRN #</th><th>Date</th><th>PO #</th><th>Vendor</th><th>Total Qty</th></tr></thead>
      <tbody>
      <% if (Receipts.Count == 0) { %><tr><td colspan="5" style="text-align:center;padding:2rem;">No goods receipts found.</td></tr><% } %>
      <% foreach (var r in Receipts) { %>
      <tr>
        <td><code><%= Server.HtmlEncode(r.GoodsReceiptCode) %></code></td>
        <td><%= r.ReceiptDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(r.PurchaseOrderCode) %></td>
        <td><%= Server.HtmlEncode(r.VendorName) %></td>
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
  <div class="card-header"><h2>Receive Goods Against Purchase Order</h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Receipt Date *</label>
        <input type="date" name="ReceiptDate" id="txtReceiptDate" class="form-control" value="<%= Server.HtmlEncode(ReceiptDate) %>" required /></div>
      <div class="form-group"><label>Purchase Order *</label>
        <select name="PurchaseOrderID" id="ddlPO" class="form-control" required>
          <option value="">-- Select PO --</option>
          <% foreach (var po in OpenPurchaseOrders) { %><option value="<%= po.Id %>" <%= po.Id == SelectedPoId ? "selected" : "" %>><%= Server.HtmlEncode(po.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Vendor</label>
        <input type="text" name="VendorName" id="txtVendor" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(VendorName) %>" /></div>
      <div class="form-group full-width"><label>Remarks</label>
        <textarea name="Remarks" class="form-control" rows="2"><%= Server.HtmlEncode(Remarks) %></textarea></div>
    </div>
    <div class="table-responsive" style="margin-top:1rem;">
      <table class="data-table" id="grLineTable">
        <thead><tr><th>Product</th><th>Description</th><th>Batch</th><th>Ordered</th><th>Received</th><th>Receive Now</th><th>Unit Cost</th></tr></thead>
        <tbody></tbody>
      </table>
    </div>
  </div>
  <div class="card-footer" style="display:flex;gap:.75rem;">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return prepareGR();">Post Receipt &amp; Update Stock</button>
    <a href="/GoodsReceiptMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<script type="application/json" id="initialGRLines"><% Response.Write(LinesJsonInitial); %></script>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (ShowForm) { %>
<script>
function buildGRRow(d) {
  d = d || {};
  var tr = document.createElement('tr');
  var batches = d.batches || d.Batches || [];
  var selected = d.productBatchID || d.ProductBatchID || 0;
  var requires = d.requiresBatch || d.RequiresBatch;
  var opts = '<option value="0">' + (requires ? '-- Select batch --' : '-- No batch --') + '</option>';
  batches.forEach(function (b) {
    var id = b.productBatchID || b.ProductBatchID;
    var label = b.label || b.Label || id;
    var usable = b.usable === true || b.Usable === true;
    opts += '<option value="' + id + '"' + (String(id) === String(selected) ? ' selected' : '') + (usable ? '' : ' disabled') + '>' + label + '</option>';
  });
  tr.innerHTML = '<td><input type="hidden" class="po-item-id" value="' + (d.purchaseOrderItemID || d.PurchaseOrderItemID || '') + '" />' +
    '<input type="hidden" class="product-id" value="' + (d.productID || d.ProductID || '') + '" />' +
    '<input type="hidden" class="requires-batch" value="' + (requires ? '1' : '0') + '" />' +
    '<input type="text" class="form-control readonly-field product-code" readonly value="' + (d.productCode || d.ProductCode || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field product-desc" readonly value="' + (d.productDescription || d.ProductDescription || '') + '" /></td>' +
    '<td><select class="form-control product-batch">' + opts + '</select></td>' +
    '<td><input type="text" class="form-control readonly-field" readonly value="' + (d.orderedQty || d.OrderedQty || '') + '" /></td>' +
    '<td><input type="text" class="form-control readonly-field" readonly value="' + (d.alreadyReceived || d.AlreadyReceived || '') + '" /></td>' +
    '<td><input type="number" step="0.0001" min="0" class="form-control recv-qty" value="' + (d.receiveQty || d.ReceiveQty || '') + '" /></td>' +
    '<td><input type="number" step="0.0001" min="0" class="form-control unit-cost" value="' + (d.unitCost || d.UnitCost || '') + '" /></td>';
  return tr;
}
function prepareGR() {
  var lines = Array.from(document.querySelectorAll('#grLineTable tbody tr')).map(function (tr) {
    return {
      purchaseOrderItemID: parseInt(tr.querySelector('.po-item-id').value, 10) || 0,
      productID: parseInt(tr.querySelector('.product-id').value, 10) || 0,
      productCode: tr.querySelector('.product-code').value,
      productDescription: tr.querySelector('.product-desc').value,
      receiveQty: tr.querySelector('.recv-qty').value,
      unitCost: tr.querySelector('.unit-cost').value,
      productBatchID: parseInt((tr.querySelector('.product-batch') || {}).value || '0', 10) || 0
    };
  }).filter(function (l) { return parseFloat(l.receiveQty) > 0; });
  if (!lines.length) { alert('Enter receive quantity for at least one line.'); return false; }
  var missing = Array.from(document.querySelectorAll('#grLineTable tbody tr')).some(function (tr) {
    var qty = parseFloat(tr.querySelector('.recv-qty').value) || 0;
    var req = tr.querySelector('.requires-batch') && tr.querySelector('.requires-batch').value === '1';
    var batch = parseInt((tr.querySelector('.product-batch') || {}).value || '0', 10) || 0;
    return qty > 0 && req && batch <= 0;
  });
  if (missing) { alert('Select a usable batch for each product that is batch-tracked. Expired and blocked batches cannot be received.'); return false; }
  document.getElementById('LinesJson').value = JSON.stringify(lines);
  return true;
}
document.addEventListener('DOMContentLoaded', function () {
  var ddl = document.getElementById('ddlPO');
  var initial = [];
  try { initial = JSON.parse(document.getElementById('initialGRLines').textContent || '[]'); } catch (e) {}
  if (initial.length) {
    var tbody = document.querySelector('#grLineTable tbody');
    initial.forEach(function (d) { tbody.appendChild(buildGRRow(d)); });
  }
  if (ddl) {
    ddl.addEventListener('change', function () {
      if (!this.value) return;
      window.location = '<%= ResolveUrl("~/GoodsReceiptMaster.aspx") %>?newReceipt=1&poId=' + encodeURIComponent(this.value);
    });
  }
});
</script>
<% } %>
</asp:Content>
