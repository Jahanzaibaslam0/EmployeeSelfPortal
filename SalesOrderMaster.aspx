<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SalesOrderMaster.aspx.cs" Inherits="HRMS.SalesOrderMasterPage" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.so-list-header { display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:.75rem; }
.so-list-header .right-controls { display:flex; align-items:center; gap:.6rem; }
.form-breadcrumb { display:flex; align-items:center; gap:.75rem; margin-bottom:1rem; padding:.5rem .75rem; background:#f9fafb; border:1px solid #e5e7eb; border-radius:6px; }
.readonly-field { background:#f3f4f6; color:#374151; cursor:not-allowed; }
.so-totals-row { display:flex; flex-wrap:wrap; justify-content:flex-end; gap:1.5rem; margin-top:1rem; padding:.75rem 1rem; background:#f9fafb; border:1px solid #e5e7eb; border-radius:6px; }
.so-totals-row .total-item { text-align:right; }
.so-totals-row .total-item .label { font-size:.78rem; color:#6b7280; display:block; }
.so-totals-row .total-item .value { font-weight:600; }
.so-totals-row .total-item.grand .value { font-size:1.25rem; font-weight:700; }
#soItemTable input.form-control, #soItemTable select.form-control { font-size:.78rem; padding:4px 6px; min-width:70px; }
.status-badge { display:inline-block; padding:2px 10px; border-radius:12px; font-size:.75rem; font-weight:600; }
.status-draft { background:#f3f4f6; color:#374151; }
.status-submitted { background:#dbeafe; color:#1e40af; }
.status-approved { background:#dcfce7; color:#166534; }
.status-delivered { background:#dbeafe; color:#1e3a8a; }
.status-cancelled { background:#fee2e2; color:#991b1b; }
.history-table { font-size:.82rem; }
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="cancelId" id="cancelId" value="0" />

<%
  var isDraft = Input.OrderStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase);
  var isSubmitted = Input.OrderStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase);
  var canEdit = ShowForm && !IsReadOnly;
  var canSubmit = canEdit && isDraft;
  var canCancel = EditMode && (isDraft || isSubmitted);
%>

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header so-list-header">
    <h2>Sales Order List</h2>
    <div class="right-controls">
      <input type="text" id="txtSearch" class="form-control" style="width:260px" placeholder="Search SO / customer / status…" onkeyup="searchSOTable(this.value)" />
      <a href="/SalesOrderMaster.aspx?newSO=1" class="btn btn-primary">+ New Sales Order</a>
    </div>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="soTable">
      <thead><tr><th>#</th><th>SO Number</th><th>Date</th><th>Customer</th><th>Status</th><th>Grand Total</th><th>Lines</th><th>Actions</th></tr></thead>
      <tbody>
      <% if (SalesOrders.Count == 0) { %>
      <tr><td colspan="8" style="text-align:center;padding:2rem;">No sales orders found.</td></tr>
      <% } else { int rowNum = 0; foreach (var so in SalesOrders) { rowNum++;
           var viewOnly = so.OrderStatus == "Cancelled" || so.OrderStatus == "Approved" || so.OrderStatus == "Delivered"; %>
      <tr>
        <td><%= rowNum %></td>
        <td><code><%= Server.HtmlEncode(so.SalesOrderCode) %></code></td>
        <td><%= so.SalesOrderDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(so.CustomerName) %></td>
        <td><span class="status-badge <%= StatusCss(so.OrderStatus) %>"><%= Server.HtmlEncode(so.OrderStatus) %></span></td>
        <td><%= so.GrandTotal.ToString("N2") %></td>
        <td><%= so.LineCount %></td>
        <td style="white-space:nowrap;">
          <a href="/SalesOrderMaster.aspx?editId=<%= so.SalesOrderID %>" class="btn btn-secondary" style="padding:3px 12px;font-size:.8rem;"><%= viewOnly ? "View" : "Edit" %></a>
          <% if (so.OrderStatus == "Draft") { %>
          <button type="submit" class="btn btn-danger" style="padding:3px 10px;font-size:.8rem;"
            onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= so.SalesOrderID %>';return confirm('Delete this sales order?');">Delete</button>
          <% } %>
        </td>
      </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
  <div class="card-footer"><span class="record-count">Total Records: <%= SalesOrders.Count %></span></div>
</div>
<% } else { %>
<div class="form-breadcrumb">
  <a href="/SalesOrderMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? (IsReadOnly ? "View Sales Order" : "Edit Sales Order") : "New Sales Order" %>
    <%= Server.HtmlEncode(Input.SalesOrderCode) %>
    <% if (EditMode) { %><span class="status-badge <%= StatusCss(Input.OrderStatus) %>"><%= Server.HtmlEncode(Input.OrderStatus) %></span><% } %>
  </span>
</div>
<div class="card">
  <div class="card-header"><h2><%= EditMode ? (IsReadOnly ? "View Sales Order" : "Edit Sales Order") : "Add Sales Order" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="SalesOrderID" value="<%= Input.SalesOrderID %>" />
    <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
    <input type="hidden" name="ItemsJson" id="ItemsJson" />
    <input type="hidden" name="OrderStatus" value="<%= Server.HtmlEncode(Input.OrderStatus) %>" />
    <input type="hidden" name="TotalQty" id="TotalQty" value="<%= Server.HtmlEncode(Input.TotalQty) %>" />
    <input type="hidden" name="TotalTax" id="TotalTax" value="<%= Server.HtmlEncode(Input.TotalTax) %>" />
    <input type="hidden" name="TotalDiscount" id="TotalDiscount" value="<%= Server.HtmlEncode(Input.TotalDiscount) %>" />
    <input type="hidden" name="GrandTotal" id="GrandTotal" value="<%= Server.HtmlEncode(Input.GrandTotal) %>" />
    <div id="soForm" data-readonly="<%= IsReadOnly ? "true" : "false" %>">
    <div class="form-grid">
      <div class="form-group"><label>Sales Order Number</label>
        <input type="text" name="SalesOrderCode" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.SalesOrderCode) %>" readonly /></div>
      <div class="form-group"><label>Sales Order Date *</label>
        <input type="date" name="SalesOrderDate" id="txtSODate" class="form-control" value="<%= Server.HtmlEncode(Input.SalesOrderDate) %>" required <%= IsReadOnly ? "readonly" : "" %> /></div>
      <div class="form-group"><label>Customer *</label>
        <select name="CustomerID" id="ddlCustomer" class="form-control" required <%= IsReadOnly ? "disabled" : "" %>>
          <option value="">-- Select Customer --</option>
          <% foreach (var c in Customers) { %><option value="<%= c.Id %>" <%= c.Id == Input.CustomerID ? "selected" : "" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select>
        <% if (IsReadOnly) { %><input type="hidden" name="CustomerID" value="<%= Input.CustomerID %>" /><% } %>
      </div>
      <div class="form-group"><label>Customer Name</label>
        <input type="text" name="CustomerName" id="txtCustomerName" class="form-control readonly-field" value="<%= Server.HtmlEncode(Input.CustomerName) %>" readonly maxlength="200" /></div>
      <div class="form-group full-width"><label>Remarks</label>
        <textarea name="Remarks" class="form-control" rows="2" maxlength="500" <%= IsReadOnly ? "readonly" : "" %>><%= Server.HtmlEncode(Input.Remarks) %></textarea></div>
    </div>
    <div class="card mt-4">
      <div class="card-header space-between"><h2>Order Lines</h2>
        <% if (canEdit) { %><button type="button" class="btn btn-secondary" onclick="addSOItemRow()">+ Add Line</button><% } %>
      </div>
      <div class="card-body table-responsive">
        <table class="data-table" id="soItemTable">
          <thead><tr><th>Product Code</th><th>Product Description</th><th>Quantity</th><th>Unit Price</th><th>Tax Amount</th><th>Discount Amount</th><th>Net Amount</th><th></th></tr></thead>
          <tbody></tbody>
        </table>
      </div>
    </div>
    <div class="so-totals-row">
      <div class="total-item"><span class="label">Total Quantity</span><span class="value" id="lblTotalQty"><%= Server.HtmlEncode(Input.TotalQty) %></span></div>
      <div class="total-item"><span class="label">Total Tax</span><span class="value" id="lblTotalTax"><%= Server.HtmlEncode(Input.TotalTax) %></span></div>
      <div class="total-item"><span class="label">Total Discount</span><span class="value" id="lblTotalDiscount"><%= Server.HtmlEncode(Input.TotalDiscount) %></span></div>
      <div class="total-item grand"><span class="label">Grand Total</span><span class="value" id="lblGrandTotal"><%= Server.HtmlEncode(Input.GrandTotal) %></span></div>
    </div>
    <% if (EditMode && OrderHistory.Count > 0) { %>
    <div class="card mt-4">
      <div class="card-header"><h2>Order History</h2></div>
      <div class="card-body table-responsive">
        <table class="data-table history-table">
          <thead><tr><th>Date / Time</th><th>Action</th><th>From</th><th>To</th><th>User</th><th>Remarks</th></tr></thead>
          <tbody>
          <% foreach (var h in OrderHistory) { %>
          <tr>
            <td><%= h.ActionAt.ToString("dd MMM yyyy HH:mm") %></td>
            <td><%= Server.HtmlEncode(h.ActionType) %></td>
            <td><%= Server.HtmlEncode(h.FromStatus) %></td>
            <td><%= Server.HtmlEncode(h.ToStatus) %></td>
            <td><%= Server.HtmlEncode(h.ActionByUsername) %></td>
            <td><%= Server.HtmlEncode(h.Remarks) %></td>
          </tr>
          <% } %>
          </tbody>
        </table>
      </div>
    </div>
    <% } %>
    </div>
  </div>
  <div class="card-footer" style="display:flex;gap:.75rem;flex-wrap:wrap;">
    <% if (canEdit) { %>
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return prepareSOPayload();"><%= EditMode ? "Save Changes" : "Save Draft" %></button>
    <% } %>
    <% if (canSubmit) { %>
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Submit';return prepareSOSubmit();">Submit Order</button>
    <% } %>
    <% if (canCancel) { %>
    <button type="submit" class="btn btn-danger"
      onclick="document.getElementById('__handler').value='Cancel';document.getElementById('cancelId').value='<%= Input.SalesOrderID %>';return confirm('Cancel this sales order?');">Cancel Order</button>
    <% } %>
    <a href="/SalesOrderMaster.aspx" class="btn btn-secondary"><%= IsReadOnly ? "Close" : "Back" %></a>
  </div>
</div>
<script type="application/json" id="productLookupData"><% Response.Write(ProductsJson); %></script>
<script type="application/json" id="initialSOItemsData"><% Response.Write(ItemsJsonInitial); %></script>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (!ShowForm) { %>
<script>
function searchSOTable(q) {
  q = (q || '').toLowerCase();
  document.querySelectorAll('#soTable tbody tr').forEach(function (r) {
    if (r.querySelector('td[colspan]')) return;
    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
  });
}
</script>
<% } else { %>
<script src="<%= ResolveUrl("~/js/salesorder.js") %>"></script>
<script>
window.loadCustomerDetails = function () {
  var ddl = document.getElementById('ddlCustomer');
  var nameEl = document.getElementById('txtCustomerName');
  if (!ddl || !ddl.value) { if (nameEl) nameEl.value = ''; return; }
  var text = ddl.options[ddl.selectedIndex].text || '';
  var parts = text.split('\u2013');
  if (nameEl) nameEl.value = parts.length > 1 ? parts[parts.length - 1].trim() : text.trim();
};
</script>
<% } %>
</asp:Content>
