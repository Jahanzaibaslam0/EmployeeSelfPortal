<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProductBatchMaster.aspx.cs" Inherits="HRMS.ProductBatchMasterPage" ResponseEncoding="utf-8" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.batch-kpi { display:flex; gap:.75rem; flex-wrap:wrap; margin:0 0 1rem; }
.batch-kpi a, .batch-kpi span {
    display:inline-flex; align-items:center; gap:.4rem; padding:.4rem .75rem; border-radius:999px;
    border:1px solid var(--border); background:#fff; font-size:.82rem; text-decoration:none; color:var(--text-dark);
}
.batch-kpi a:hover { border-color:var(--gb-blue); color:var(--gb-blue); }
.filter-bar { display:flex; flex-wrap:wrap; gap:.6rem; align-items:flex-end; margin-bottom:1rem; }
.filter-bar .form-group { margin:0; min-width:180px; }
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Product Batch Management</h2>
    <% if (CanWrite) { %><a href="/ProductBatchMaster.aspx?newBatch=1<%= FilterProductID > 0 ? "&productId=" + FilterProductID : "" %>" class="btn btn-primary">+ New Batch</a><% } %>
  </div>
  <div class="card-body">
    <p class="text-muted" style="margin-top:0;">Batches belong to a product from Product Master. The same batch number cannot be used twice for one product.</p>
    <div class="batch-kpi">
      <span><strong><%= Batches.Count %></strong> batch<%= Batches.Count == 1 ? "" : "es" %></span>
      <a href="/ProductBatchMaster.aspx?status=Expired<%= FilterProductID > 0 ? "&productId=" + FilterProductID : "" %>"><strong><%= ExpiredCount %></strong> expired</a>
      <a href="/ProductBatchMaster.aspx<%= FilterProductID > 0 ? "?productId=" + FilterProductID : "" %>"><strong><%= ExpiringSoonCount %></strong> expiring within 30 days</a>
    </div>
    <div class="filter-bar">
      <div class="form-group">
        <label>Product</label>
        <select id="fltProduct" class="form-control">
          <option value="">All products</option>
          <% foreach (var p in Products) { %>
          <option value="<%= p.Id %>" <%= FilterProductID == p.Id ? "selected" : "" %>><%= Server.HtmlEncode(p.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Status</label>
        <select id="fltStatus" class="form-control">
          <option value="">All statuses</option>
          <% foreach (var s in ProductBatchStatuses.All) { %>
          <option value="<%= s %>" <%= string.Equals(FilterStatus, s, StringComparison.OrdinalIgnoreCase) ? "selected" : "" %>><%= s %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Search</label>
        <input type="text" id="fltSearch" class="form-control" value="<%= Server.HtmlEncode(FilterSearch) %>" placeholder="Batch, product, manufacturer" />
      </div>
      <button type="button" class="btn btn-secondary" onclick="applyBatchFilter();">Filter</button>
      <a href="/ProductBatchMaster.aspx" class="btn btn-secondary">Reset</a>
    </div>
    <div class="table-responsive">
      <table class="data-table">
        <thead class="grid-header">
          <tr>
            <th>Product</th>
            <th>Batch No</th>
            <th>Mfg Date</th>
            <th>Expiry</th>
            <th>Best Before</th>
            <th>Vendor</th>
            <th>MRP</th>
            <th>Qty</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% if (Batches.Count == 0) { %>
          <tr class="empty-row"><td colspan="10">No batches found. Create a batch for a product from Product Master.</td></tr>
        <% } else { var i = 0; foreach (var b in Batches) { %>
          <tr class="<%= i++ % 2 == 0 ? "grid-row" : "grid-alt-row" %>">
            <td><%= Server.HtmlEncode(b.ProductCode) %> &ndash; <%= Server.HtmlEncode(b.ProductName) %></td>
            <td><strong><%= Server.HtmlEncode(b.BatchNumber) %></strong></td>
            <td><%= DisplayDate(b.ManufacturingDate) %></td>
            <td><%= DisplayDate(b.ExpiryDate) %><% if (b.IsExpiringSoon) { %> <span class="badge badge-warning">Soon</span><% } %></td>
            <td><%= DisplayDate(b.BestBeforeDate) %></td>
            <td><%= Server.HtmlEncode(b.VendorName) %></td>
            <td><%= b.BatchMrp.HasValue ? b.BatchMrp.Value.ToString("N2") : "&ndash;" %></td>
            <td><%= b.QtyOnHand.ToString("0.####") %></td>
            <td><span class="<%= b.StatusBadgeClass %>"><%= Server.HtmlEncode(b.BatchStatus) %></span></td>
            <td>
              <a href="/ProductBatchMaster.aspx?editId=<%= b.ProductBatchID %>">Open</a>
              <% if (CanWrite) { %>
              <button type="submit" class="btn-icon btn-delete" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= b.ProductBatchID %>';return confirm('Remove this batch from the active list?');">X</button>
              <% } %>
            </td>
          </tr>
        <% } } %>
        </tbody>
      </table>
    </div>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/ProductBatchMaster.aspx" class="btn btn-secondary">&larr; Back to List</a>
  <span><%= EditMode ? "Edit batch" : "New batch" %></span>
</div>
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Batch" : "Add Batch" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="ProductBatchID" value="<%= Input.ProductBatchID %>" />
    <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
    <div class="form-grid">
      <div class="form-group">
        <label>Product <span class="required">*</span></label>
        <select name="ProductID" class="form-control" required <%= EditMode ? "disabled" : "" %>>
          <option value="0">-- Select product --</option>
          <% foreach (var p in Products) { %>
          <option value="<%= p.Id %>" <%= Input.ProductID == p.Id ? "selected" : "" %>><%= Server.HtmlEncode(p.Name) %></option>
          <% } %>
        </select>
        <% if (EditMode) { %><input type="hidden" name="ProductID" value="<%= Input.ProductID %>" /><% } %>
        <span class="text-muted" style="font-size:.75rem;">Lookup from Product Master. Multiple batches are allowed per product.</span>
      </div>
      <div class="form-group">
        <label>Batch Number <span class="required">*</span></label>
        <input type="text" name="BatchNumber" class="form-control" maxlength="50" required
               value="<%= Server.HtmlEncode(Input.BatchNumber) %>" placeholder="Alphanumeric, unique for this product" />
      </div>
      <div class="form-group">
        <label>Manufacturing Date</label>
        <input type="date" name="ManufacturingDate" id="txtMfg" class="form-control" value="<%= Iso(Input.ManufacturingDate) %>" />
      </div>
      <div class="form-group">
        <label>Expiry Date</label>
        <input type="date" name="ExpiryDate" id="txtExp" class="form-control" value="<%= Iso(Input.ExpiryDate) %>" />
      </div>
      <div class="form-group">
        <label>Shelf Life / Best Before Date</label>
        <input type="date" name="BestBeforeDate" class="form-control" value="<%= Iso(Input.BestBeforeDate) %>" />
      </div>
      <div class="form-group">
        <label>Manufacturer</label>
        <input type="text" name="Manufacturer" class="form-control" maxlength="150" value="<%= Server.HtmlEncode(Input.Manufacturer) %>" />
      </div>
      <div class="form-group">
        <label>Vendor / Supplier</label>
        <select name="VendorID" class="form-control">
          <option value="0">-- Select vendor --</option>
          <% foreach (var v in Vendors) { %>
          <option value="<%= v.Id %>" <%= Input.VendorID == v.Id ? "selected" : "" %>><%= Server.HtmlEncode(v.Name) %></option>
          <% } %>
        </select>
        <span class="text-muted" style="font-size:.75rem;">Lookup from Vendor Master.</span>
      </div>
      <div class="form-group">
        <label>Batch Status</label>
        <select name="BatchStatus" class="form-control">
          <% foreach (var s in ProductBatchStatuses.All) { %>
          <option value="<%= s %>" <%= string.Equals(Input.BatchStatus, s, StringComparison.OrdinalIgnoreCase) ? "selected" : "" %>><%= s %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Batch MRP</label>
        <input type="number" step="0.0001" min="0" name="BatchMrp" class="form-control" value="<%= Input.BatchMrp.HasValue ? Input.BatchMrp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "" %>" />
      </div>
      <div class="form-group">
        <label>Assessable Rate</label>
        <input type="number" step="0.0001" min="0" name="AssessableRate" class="form-control" value="<%= Input.AssessableRate.HasValue ? Input.AssessableRate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "" %>" />
      </div>
      <div class="form-group full-width">
        <label>Remarks</label>
        <textarea name="Remarks" class="form-control" rows="3"><%= Server.HtmlEncode(Input.Remarks) %></textarea>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <% if (CanWrite) { %><button type="submit" class="btn btn-primary" onclick="return validateBatchDates();">Save Batch</button><% } %>
    <a href="/ProductBatchMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
function applyBatchFilter() {
  var p = document.getElementById('fltProduct');
  var s = document.getElementById('fltStatus');
  var q = document.getElementById('fltSearch');
  if (!p) return;
  var params = [];
  if (p.value) params.push('productId=' + encodeURIComponent(p.value));
  if (s && s.value) params.push('status=' + encodeURIComponent(s.value));
  if (q && q.value) params.push('q=' + encodeURIComponent(q.value));
  window.location = '/ProductBatchMaster.aspx' + (params.length ? '?' + params.join('&') : '');
}
function validateBatchDates() {
  var mfg = document.getElementById('txtMfg');
  var exp = document.getElementById('txtExp');
  if (mfg && exp && mfg.value && exp.value && exp.value < mfg.value) {
    alert('Expiry date cannot be earlier than the manufacturing date.');
    exp.focus();
    return false;
  }
  return true;
}
</script>
</asp:Content>
