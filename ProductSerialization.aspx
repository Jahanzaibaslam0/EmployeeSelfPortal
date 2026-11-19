<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProductSerialization.aspx.cs" Inherits="HRMS.ProductSerializationPage" ResponseEncoding="utf-8" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.serial-note { font-size:.78rem; color:#6b7280; margin:.25rem 0 0; }
.filter-bar { display:flex; flex-wrap:wrap; gap:.6rem; align-items:flex-end; margin-bottom:1rem; }
.filter-bar .form-group { margin:0; min-width:160px; }
.hex-cell { font-family:Consolas,monospace; letter-spacing:.04em; }
.history-table td { font-size:.82rem; }
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="" />
<input type="hidden" name="serialId" id="serialId" value="0" />
<input type="hidden" name="serialStatus" id="serialStatus" value="" />
<input type="hidden" name="serialRemarks" id="serialRemarks" value="" />

<div class="card">
  <div class="card-header"><h2>Product Serialization</h2></div>
  <div class="card-body">
    <p class="text-muted" style="margin-top:0;">
      Generate unique hexacodes and a GS1 DataMatrix for each unit. GTIN comes from Product Master; manufacturing and expiry dates come from the batch.
      Each DataMatrix encodes (01) GTIN, (11) manufacturing date, (10) batch, (17) expiry, and (21) hexacode with GS1 FNC1.
    </p>
    <div class="form-grid">
      <div class="form-group">
        <label>Product <span class="required">*</span></label>
        <select name="ProductID" id="ddlProduct" class="form-control">
          <option value="0">-- Select product --</option>
          <% foreach (var p in Products) { %>
          <option value="<%= p.Id %>" data-gtin="<%= Server.HtmlEncode(p.Gtin) %>" <%= SelectedProductID == p.Id ? "selected" : "" %>><%= Server.HtmlEncode(p.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>GTIN Number</label>
        <input type="text" id="txtGtin" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(GtinNumber) %>" placeholder="From Product Master" />
        <p class="serial-note">Read-only. Maintain GTIN on Product Master (8, 12, 13 or 14 digits).</p>
      </div>
      <div class="form-group">
        <label>Batch Number <span class="required">*</span></label>
        <select name="ProductBatchID" id="ddlBatch" class="form-control" <%= SelectedProductID > 0 ? "" : "disabled" %>>
          <option value="0">-- Select batch --</option>
          <% foreach (var b in Batches) { %>
          <option value="<%= b.ProductBatchID %>" <%= SelectedBatchID == b.ProductBatchID ? "selected" : "" %>><%= Server.HtmlEncode(b.BatchNumber) %> (<%= Server.HtmlEncode(b.BatchStatus) %>)</option>
          <% } %>
        </select>
        <p class="serial-note">Only batches for the selected product.</p>
      </div>
      <div class="form-group">
        <label>Manufacturing Date</label>
        <input type="text" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(ManufacturingDate) %>" />
      </div>
      <div class="form-group">
        <label>Expiry Date</label>
        <input type="text" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(ExpiryDate) %>" />
      </div>
      <div class="form-group">
        <label>Manufacturer</label>
        <input type="text" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(Manufacturer) %>" />
      </div>
      <div class="form-group">
        <label>Vendor / Supplier</label>
        <input type="text" class="form-control readonly-field" readonly value="<%= Server.HtmlEncode(VendorName) %>" />
      </div>
      <div class="form-group">
        <label>Number of Codes to Generate</label>
        <input type="number" name="GenerateQty" class="form-control" min="1" max="2000" value="<%= GenerateQty > 0 ? GenerateQty : 1 %>" />
      </div>
      <div class="form-group">
        <label>Hexacode Length</label>
        <select name="CodeLength" class="form-control">
          <% for (var len = 14; len <= 20; len++) { %>
          <option value="<%= len %>" <%= CodeLength == len ? "selected" : "" %>><%= len %> characters</option>
          <% } %>
        </select>
      </div>
    </div>
  </div>
  <div class="card-footer" style="display:flex;gap:.75rem;flex-wrap:wrap;">
    <% if (CanWrite) { %>
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Generate';">Generate Hexacodes</button>
    <% } %>
    <span class="serial-note">Each click creates a new Generation ID. Previous series for the same product and batch are kept.</span>
  </div>
</div>

<div class="card" style="margin-top:1.25rem;">
  <div class="card-header"><h2>Hexacode Generation History</h2></div>
  <div class="card-body">
    <p class="text-muted" style="margin-top:0;">All generation requests are stored permanently. The same product and batch can have many Generation IDs.</p>
    <div class="filter-bar">
      <div class="form-group">
        <label>Generation ID</label>
        <input type="text" id="histGid" class="form-control" value="<%= Server.HtmlEncode(RunFilter.GenerationKey) %>" placeholder="Search Generation ID" />
      </div>
      <div class="form-group">
        <label>Product Name</label>
        <input type="text" id="histProduct" class="form-control" value="<%= Server.HtmlEncode(RunFilter.ProductName) %>" />
      </div>
      <div class="form-group">
        <label>GTIN Number</label>
        <input type="text" id="histGtin" class="form-control" value="<%= Server.HtmlEncode(RunFilter.Gtin) %>" />
      </div>
      <div class="form-group">
        <label>Batch Number</label>
        <input type="text" id="histBatch" class="form-control" value="<%= Server.HtmlEncode(RunFilter.BatchNumber) %>" />
      </div>
      <div class="form-group">
        <label>From Date</label>
        <input type="date" id="histFrom" class="form-control" value="<%= Iso(RunFilter.DateFrom) %>" />
      </div>
      <div class="form-group">
        <label>To Date</label>
        <input type="date" id="histTo" class="form-control" value="<%= Iso(RunFilter.DateTo) %>" />
      </div>
      <button type="button" class="btn btn-secondary" onclick="applyHistFilter();">Search</button>
      <a href="/ProductSerialization.aspx<%= SelectedProductID > 0 ? "?productId=" + SelectedProductID + (SelectedBatchID > 0 ? "&batchId=" + SelectedBatchID : "") : "" %>" class="btn btn-secondary">Reset</a>
    </div>
    <div class="table-responsive">
      <table class="data-table">
        <thead class="grid-header">
          <tr>
            <th>Generation ID</th>
            <th>Product Name</th>
            <th>GTIN Number</th>
            <th>Batch Number</th>
            <th>Mfg Date</th>
            <th>Expiry Date</th>
            <th>Requested</th>
            <th>Generated</th>
            <th>Generation Date / Time</th>
            <th>Generated By</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% if (Runs.Count == 0) { %>
          <tr class="empty-row"><td colspan="12">No generation history yet.</td></tr>
        <% } else { var i = 0; foreach (var run in Runs) { i++; %>
          <tr class="<%= i % 2 == 0 ? "grid-alt-row" : "grid-row" %> <%= OpenedRun != null && string.Equals(OpenedRun.GenerationKey, run.GenerationKey, StringComparison.OrdinalIgnoreCase) ? "grid-row" : "" %>">
            <td class="hex-cell"><a href="<%= OpenRunUrl(run.GenerationKey) %>"><%= Server.HtmlEncode(run.GenerationKey) %></a></td>
            <td><%= Server.HtmlEncode(run.ProductName) %></td>
            <td><%= Server.HtmlEncode(run.GtinNumber) %></td>
            <td><%= Server.HtmlEncode(run.BatchNumber) %></td>
            <td><%= DisplayDate(run.ManufacturingDate) %></td>
            <td><%= DisplayDate(run.ExpiryDate) %></td>
            <td><%= run.RequestedQuantity %></td>
            <td><%= run.GeneratedQuantity %></td>
            <td><%= DisplayDateTime(run.GeneratedOn) %></td>
            <td><%= Server.HtmlEncode(run.GeneratedBy) %></td>
            <td><span class="<%= run.StatusBadgeClass %>"><%= Server.HtmlEncode(run.GenerationStatus) %></span></td>
            <td><a href="<%= OpenRunUrl(run.GenerationKey) %>">Open</a></td>
          </tr>
        <% } } %>
        </tbody>
      </table>
    </div>
    <% if (RunTotalCount > RunFilter.PageSize) { %>
    <p class="serial-note">
      <%= RunTotalCount %> generation request(s).
      <% if (RunFilter.Page > 1) { %><a href="<%= HistPageUrl(RunFilter.Page - 1) %>">Previous</a><% } %>
      Page <%= RunFilter.Page %> of <%= RunPageCount %>
      <% if (RunFilter.Page < RunPageCount) { %><a href="<%= HistPageUrl(RunFilter.Page + 1) %>">Next</a><% } %>
    </p>
    <% } %>
  </div>
</div>

<% if (OpenedRun != null) { %>
<div class="card" style="margin-top:1.25rem;">
  <div class="card-header space-between">
    <h2>Hexacodes for Generation ID <%= Server.HtmlEncode(OpenedRun.GenerationKey) %></h2>
    <% if (CanExport) { %>
    <a class="btn btn-secondary" href="<%= CurrentListUrl() + (CurrentListUrl().Contains("?") ? "&" : "?") %>handler=ExportExcel">Export Excel</a>
    <% } %>
  </div>
  <div class="card-body">
    <p class="serial-note">
      <%= Server.HtmlEncode(OpenedRun.ProductName) %>
      &middot; GTIN <%= Server.HtmlEncode(OpenedRun.GtinNumber) %>
      &middot; Batch <%= Server.HtmlEncode(OpenedRun.BatchNumber) %>
      &middot; <%= OpenedRun.GeneratedQuantity %> of <%= OpenedRun.RequestedQuantity %> codes
      &middot; <%= DisplayDateTime(OpenedRun.GeneratedOn) %>
    </p>
    <div class="table-responsive">
      <table class="data-table">
        <thead class="grid-header">
          <tr>
            <th>Sr. No.</th>
            <th>Product Name</th>
            <th>GTIN Number</th>
            <th>Batch Number</th>
            <th>Hexacode</th>
            <th>GS1 DataMatrix</th>
            <th>Code Length</th>
            <th>Generation Date</th>
            <th>Status</th>
            <th>Remarks</th>
            <th>Created By</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% if (Serials.Count == 0) { %>
          <tr class="empty-row"><td colspan="12">No hexacodes found for this Generation ID.</td></tr>
        <% } else { var start = ((Filter.Page < 1 ? 1 : Filter.Page) - 1) * Filter.PageSize; var i = 0; foreach (var row in Serials) { i++; %>
          <tr class="<%= i % 2 == 0 ? "grid-alt-row" : "grid-row" %>">
            <td><%= start + i %></td>
            <td><%= Server.HtmlEncode(row.ProductCode) %> &ndash; <%= Server.HtmlEncode(row.ProductName) %></td>
            <td><%= Server.HtmlEncode(row.GtinNumber) %></td>
            <td><%= Server.HtmlEncode(row.BatchNumber) %></td>
            <td class="hex-cell"><%= Server.HtmlEncode(row.Hexacode) %></td>
            <td>
              <a href="/ProductSerialBarcode.aspx?id=<%= row.ProductSerialID %>">
                <img src="/ProductSerialBarcode.aspx?id=<%= row.ProductSerialID %>&amp;handler=Png&amp;plain=1&amp;scale=4" alt="GS1 DataMatrix" width="96" height="96" style="image-rendering:pixelated;border:1px solid #e5e7eb;background:#fff;" />
              </a>
              <div class="serial-note">
                <a href="/ProductSerialBarcode.aspx?id=<%= row.ProductSerialID %>">View</a>
                &middot; <a href="/ProductSerialBarcode.aspx?id=<%= row.ProductSerialID %>&amp;handler=Download&amp;scale=8">Download</a>
                &middot; <a href="/ProductSerialBarcode.aspx?id=<%= row.ProductSerialID %>&amp;print=1" target="_blank">Print</a>
              </div>
            </td>
            <td><%= row.CodeLength %></td>
            <td><%= row.GeneratedOn.ToString("dd-MMM-yyyy HH:mm") %></td>
            <td>
              <select class="form-control serial-status" data-id="<%= row.ProductSerialID %>" <%= (CanWrite || CanApprove) ? "" : "disabled" %>>
                <%= StatusOption(row.Status, ProductSerialStatuses.Draft) %>
                <%= StatusOption(row.Status, ProductSerialStatuses.InProcess) %>
                <%= StatusOption(row.Status, ProductSerialStatuses.Approved) %>
                <%= StatusOption(row.Status, ProductSerialStatuses.Rejected) %>
              </select>
            </td>
            <td>
              <input type="text" class="form-control serial-remarks" data-id="<%= row.ProductSerialID %>" maxlength="500" value="<%= Server.HtmlEncode(row.Remarks) %>" <%= (CanWrite || CanApprove) ? "" : "readonly" %> />
            </td>
            <td><%= Server.HtmlEncode(row.CreatedBy) %></td>
            <td>
              <% if (CanWrite || CanApprove) { %>
              <button type="button" class="btn btn-secondary" onclick="saveSerialRow(<%= row.ProductSerialID %>);">Save</button>
              <% } %>
              <a href="<%= CurrentListUrl() + (CurrentListUrl().Contains("?") ? "&" : "?") %>historyId=<%= row.ProductSerialID %>">History</a>
            </td>
          </tr>
        <% } } %>
        </tbody>
      </table>
    </div>
    <% if (TotalCount > Filter.PageSize) { %>
    <p class="serial-note">
      <%= TotalCount %> record(s).
      <% if (Filter.Page > 1) { %><a href="<%= PageUrl(Filter.Page - 1) %>">Previous</a><% } %>
      Page <%= Filter.Page %> of <%= PageCount %>
      <% if (Filter.Page < PageCount) { %><a href="<%= PageUrl(Filter.Page + 1) %>">Next</a><% } %>
    </p>
    <% } %>

    <% if (HistorySerialID > 0) { %>
    <h3 style="margin-top:1.25rem;">Status history</h3>
    <table class="data-table history-table">
      <thead><tr><th>When</th><th>From</th><th>To</th><th>User</th><th>Remarks</th></tr></thead>
      <tbody>
      <% if (History.Count == 0) { %><tr><td colspan="5">No history.</td></tr>
      <% } else { foreach (var h in History) { %>
      <tr>
        <td><%= h.ChangedOn.ToString("dd-MMM-yyyy HH:mm") %></td>
        <td><%= Server.HtmlEncode(string.IsNullOrEmpty(h.OldStatus) ? "—" : h.OldStatus) %></td>
        <td><%= Server.HtmlEncode(h.NewStatus) %></td>
        <td><%= Server.HtmlEncode(h.ChangedBy) %></td>
        <td><%= Server.HtmlEncode(h.Remarks) %></td>
      </tr>
      <% } } %>
      </tbody>
    </table>
    <% } %>
  </div>
</div>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
document.addEventListener('DOMContentLoaded', function () {
  var ddlP = document.getElementById('ddlProduct');
  var ddlB = document.getElementById('ddlBatch');
  if (ddlP) {
    ddlP.addEventListener('change', function () {
      var id = this.value || '0';
      window.location = '/ProductSerialization.aspx?productId=' + encodeURIComponent(id);
    });
  }
  if (ddlB) {
    ddlB.addEventListener('change', function () {
      var p = (ddlP && ddlP.value) ? ddlP.value : '0';
      window.location = '/ProductSerialization.aspx?productId=' + encodeURIComponent(p) + '&batchId=' + encodeURIComponent(this.value || '0');
    });
  }
});
function applyHistFilter() {
  var params = [];
  var gid = document.getElementById('histGid');
  var prodName = document.getElementById('histProduct');
  var g = document.getElementById('histGtin');
  var b = document.getElementById('histBatch');
  var from = document.getElementById('histFrom');
  var to = document.getElementById('histTo');
  if (gid && gid.value) params.push('histGid=' + encodeURIComponent(gid.value));
  if (prodName && prodName.value) params.push('histProduct=' + encodeURIComponent(prodName.value));
  if (g && g.value) params.push('histGtin=' + encodeURIComponent(g.value));
  if (b && b.value) params.push('histBatch=' + encodeURIComponent(b.value));
  if (from && from.value) params.push('histFrom=' + encodeURIComponent(from.value));
  if (to && to.value) params.push('histTo=' + encodeURIComponent(to.value));
  var prod = document.getElementById('ddlProduct');
  var batch = document.getElementById('ddlBatch');
  if (prod && prod.value && prod.value !== '0') params.push('productId=' + encodeURIComponent(prod.value));
  if (batch && batch.value && batch.value !== '0') params.push('batchId=' + encodeURIComponent(batch.value));
  window.location = '/ProductSerialization.aspx' + (params.length ? '?' + params.join('&') : '');
}
function saveSerialRow(id) {
  var status = document.querySelector('.serial-status[data-id="' + id + '"]');
  var remarks = document.querySelector('.serial-remarks[data-id="' + id + '"]');
  document.getElementById('__handler').value = 'UpdateStatus';
  document.getElementById('serialId').value = id;
  document.getElementById('serialStatus').value = status ? status.value : '';
  document.getElementById('serialRemarks').value = remarks ? remarks.value : '';
  document.getElementById('__handler').form.submit();
}
</script>
</asp:Content>
