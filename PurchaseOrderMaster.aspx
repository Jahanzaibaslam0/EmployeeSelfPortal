<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PurchaseOrderMaster.aspx.cs" Inherits="HRMS.PurchaseOrderMasterPage" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.po-list-header { display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:.75rem; }
.po-list-header .right-controls { display:flex; align-items:center; gap:.6rem; }
.form-breadcrumb { display:flex; align-items:center; gap:.75rem; margin-bottom:1rem; padding:.5rem .75rem; background:#f9fafb; border:1px solid #e5e7eb; border-radius:6px; }
.readonly-field { background:#f3f4f6; color:#374151; cursor:not-allowed; }
.po-totals-row { display:flex; flex-wrap:wrap; justify-content:flex-end; gap:1.5rem; margin-top:1rem; padding:.75rem 1rem; background:#f9fafb; border:1px solid #e5e7eb; border-radius:6px; }
.po-totals-row .total-item { text-align:right; }
.po-totals-row .total-item .label { font-size:.78rem; color:#6b7280; display:block; }
.po-totals-row .total-item .value { font-weight:600; color:#374151; }
.po-totals-row .total-item.grand .value { font-size:1.25rem; font-weight:700; }
#poItemTable input.form-control, #poItemTable select.form-control { font-size:.78rem; padding:4px 6px; min-width:70px; }
.status-badge { display:inline-block; padding:2px 10px; border-radius:12px; font-size:.75rem; font-weight:600; }
.status-draft { background:#f3f4f6; color:#374151; }
.status-pending { background:#fef3c7; color:#92400e; }
.status-approved { background:#dbeafe; color:#1e40af; }
.status-received { background:#dcfce7; color:#166534; }
.status-cancelled { background:#fee2e2; color:#991b1b; }
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header po-list-header">
    <h2>Purchase Order List</h2>
    <div class="right-controls">
      <input type="text" id="txtSearch" class="form-control" style="width:260px" placeholder="Search PO / vendor / status…" onkeyup="searchPOTable(this.value)" />
      <a href="/PurchaseOrderMaster.aspx?newPO=1" class="btn btn-primary">+ New Purchase Order</a>
    </div>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="poTable">
      <thead><tr><th>#</th><th>PO Number</th><th>Date</th><th>Vendor</th><th>Status</th><th>Grand Total</th><th>Lines</th><th>Actions</th></tr></thead>
      <tbody>
      <% if (PurchaseOrders.Count == 0) { %>
      <tr><td colspan="8" style="text-align:center;padding:2rem;">No purchase orders found.</td></tr>
      <% } else { int rowNum = 0; foreach (var po in PurchaseOrders) { rowNum++; %>
      <tr>
        <td><%= rowNum %></td>
        <td><code><%= Server.HtmlEncode(po.PurchaseOrderCode) %></code></td>
        <td><%= po.PurchaseOrderDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(po.VendorName) %></td>
        <td><span class="status-badge <%= StatusCss(po.OrderStatus) %>"><%= Server.HtmlEncode(po.OrderStatus) %></span></td>
        <td><%= po.GrandTotal.ToString("N2") %></td>
        <td><%= po.LineCount %></td>
        <td style="white-space:nowrap;">
          <a href="/PurchaseOrderMaster.aspx?editId=<%= po.PurchaseOrderID %>" class="btn btn-secondary" style="padding:3px 12px;font-size:.8rem;">Edit</a>
          <button type="submit" class="btn btn-danger" style="padding:3px 10px;font-size:.8rem;"
            onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= po.PurchaseOrderID %>';return confirm('Delete this purchase order and all line items?');">Delete</button>
        </td>
      </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
  <div class="card-footer"><span class="record-count">Total Records: <%= PurchaseOrders.Count %></span></div>
</div>
<% } else { %>
<div class="form-breadcrumb">
  <a href="/PurchaseOrderMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Edit Purchase Order" : "New Purchase Order" %> <%= Server.HtmlEncode(Input.PurchaseOrderCode) %></span>
</div>
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Purchase Order" : "Add Purchase Order" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="PurchaseOrderID" value="<%= Input.PurchaseOrderID %>" />
    <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
    <input type="hidden" name="ItemsJson" id="ItemsJson" />
    <input type="hidden" name="TotalQty" id="TotalQty" value="<%= Server.HtmlEncode(Input.TotalQty) %>" />
    <input type="hidden" name="TotalTax" id="TotalTax" value="<%= Server.HtmlEncode(Input.TotalTax) %>" />
    <input type="hidden" name="TotalDiscount" id="TotalDiscount" value="<%= Server.HtmlEncode(Input.TotalDiscount) %>" />
    <input type="hidden" name="GrandTotal" id="GrandTotal" value="<%= Server.HtmlEncode(Input.GrandTotal) %>" />
    <div class="form-grid">
      <div class="form-group"><label>Purchase Order Number</label>
        <input type="text" name="PurchaseOrderCode" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.PurchaseOrderCode) %>" readonly /></div>
      <div class="form-group"><label>Purchase Order Date *</label>
        <input type="date" name="PurchaseOrderDate" id="txtPODate" class="form-control" value="<%= Server.HtmlEncode(Input.PurchaseOrderDate) %>" required /></div>
      <div class="form-group"><label>Order Status</label>
        <select name="OrderStatus" class="form-control">
        <% foreach (var s in OrderStatusOptions) { %><option value="<%= s %>" <%= s == Input.OrderStatus ? "selected" : "" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Vendor *</label>
        <select name="VendorID" id="ddlVendor" class="form-control" required>
          <option value="">-- Select Vendor --</option>
          <% foreach (var v in Vendors) { %><option value="<%= v.Id %>" <%= v.Id == Input.VendorID ? "selected" : "" %>><%= Server.HtmlEncode(v.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Vendor Name</label>
        <input type="text" name="VendorName" id="txtVendorName" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.VendorName) %>" readonly maxlength="200" /></div>
      <div class="form-group full-width"><label>Remarks</label>
        <textarea name="Remarks" class="form-control" rows="2" maxlength="500"><%= Server.HtmlEncode(Input.Remarks) %></textarea></div>
    </div>
    <div class="card mt-4">
      <div class="card-header space-between"><h2>Order Lines</h2>
        <button type="button" class="btn btn-secondary" onclick="addPOItemRow()">+ Add Line</button></div>
      <div class="card-body table-responsive">
        <table class="data-table" id="poItemTable">
          <thead><tr><th>Product Code</th><th>Product Description</th><th>Quantity</th><th>Unit Price</th><th>Tax Amount</th><th>Discount Amount</th><th>Net Amount</th><th></th></tr></thead>
          <tbody></tbody>
        </table>
      </div>
    </div>
    <div class="po-totals-row">
      <div class="total-item"><span class="label">Total Quantity</span><span class="value" id="lblTotalQty"><%= Server.HtmlEncode(Input.TotalQty) %></span></div>
      <div class="total-item"><span class="label">Total Tax</span><span class="value" id="lblTotalTax"><%= Server.HtmlEncode(Input.TotalTax) %></span></div>
      <div class="total-item"><span class="label">Total Discount</span><span class="value" id="lblTotalDiscount"><%= Server.HtmlEncode(Input.TotalDiscount) %></span></div>
      <div class="total-item grand"><span class="label">Grand Total</span><span class="value" id="lblGrandTotal"><%= Server.HtmlEncode(Input.GrandTotal) %></span></div>
    </div>
  </div>
  <div class="card-footer" style="display:flex;gap:.75rem;">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return preparePOPayload();"><%= EditMode ? "Update Purchase Order" : "Save Purchase Order" %></button>
    <a href="/PurchaseOrderMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<script type="application/json" id="productLookupData"><% Response.Write(ProductsJson); %></script>
<script type="application/json" id="initialPOItemsData"><% Response.Write(ItemsJsonInitial); %></script>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (!ShowForm) { %>
<script>
function searchPOTable(q) {
  q = (q || '').toLowerCase();
  document.querySelectorAll('#poTable tbody tr').forEach(function (r) {
    if (r.querySelector('td[colspan]')) return;
    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
  });
}
</script>
<% } else { %>
<script src="<%= ResolveUrl("~/js/purchaseorder.js") %>"></script>
<script>
window.loadVendorDetails = function () {
  var ddl = document.getElementById('ddlVendor');
  var nameEl = document.getElementById('txtVendorName');
  if (!ddl || !ddl.value) { if (nameEl) nameEl.value = ''; return; }
  var text = ddl.options[ddl.selectedIndex].text || '';
  var parts = text.split('\u2013');
  if (nameEl) nameEl.value = parts.length > 1 ? parts[parts.length - 1].trim() : text.trim();
};
</script>
<% } %>
</asp:Content>
