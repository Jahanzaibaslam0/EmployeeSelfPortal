<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProductSerialBarcode.aspx.cs" Inherits="HRMS.ProductSerialBarcodePage" ResponseEncoding="utf-8" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.gs1-wrap { max-width: 520px; }
.gs1-card { background:#fff; padding:1.25rem; border:1px solid var(--border); border-radius:8px; }
.gs1-img { display:block; margin:0 auto 1rem; image-rendering:pixelated; }
.gs1-hri { font-family:Consolas,monospace; font-size:.9rem; line-height:1.45; }
.gs1-meta { font-size:.85rem; color:#374151; margin-top:1rem; }
@media print {
  .no-print, .app-sidebar, .topbar, .card-header, .btn { display:none !important; }
  .gs1-card { border:0; }
}
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-error"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } else if (Item != null) { %>
<div class="gs1-wrap">
  <div class="form-breadcrumb no-print" style="margin-bottom:1rem;">
    <a href="/ProductSerialization.aspx?run=<%= Server.UrlEncode(Item.GenerationKey) %>" class="btn btn-secondary">&larr; Back to generation</a>
  </div>
  <div class="gs1-card">
    <h2 style="text-align:center;margin-top:0;">GS1 DataMatrix</h2>
    <img class="gs1-img" alt="GS1 DataMatrix" src="/ProductSerialBarcode.aspx?id=<%= Item.ProductSerialID %>&amp;handler=Png&amp;plain=1&amp;scale=8" />
    <div class="gs1-hri"><%= HriHtml() %></div>
    <div class="gs1-meta">
      <div><strong>Generation ID:</strong> <%= Server.HtmlEncode(Item.GenerationKey) %></div>
      <div><strong>Product:</strong> <%= Server.HtmlEncode(Item.ProductCode) %> - <%= Server.HtmlEncode(Item.ProductName) %></div>
      <div><strong>GTIN (01):</strong> <%= Server.HtmlEncode(Item.GtinNumber) %></div>
      <div><strong>Batch (10):</strong> <%= Server.HtmlEncode(Item.BatchNumber) %></div>
      <div><strong>Manufacturing (11):</strong> <%= Item.ManufacturingDate.HasValue ? Item.ManufacturingDate.Value.ToString("yyMMdd") + " (" + Item.ManufacturingDate.Value.ToString("dd-MMM-yyyy") + ")" : "-" %></div>
      <div><strong>Expiry (17):</strong> <%= Item.ExpiryDate.HasValue ? Item.ExpiryDate.Value.ToString("yyMMdd") + " (" + Item.ExpiryDate.Value.ToString("dd-MMM-yyyy") + ")" : "-" %></div>
      <div><strong>Serial (21):</strong> <%= Server.HtmlEncode(Item.Hexacode) %></div>
      <div><strong>Generated:</strong> <%= Item.GeneratedOn.ToString("dd-MMM-yyyy HH:mm:ss") %> by <%= Server.HtmlEncode(Item.CreatedBy) %></div>
    </div>
    <div class="no-print" style="margin-top:1rem;display:flex;gap:.5rem;flex-wrap:wrap;">
      <a class="btn btn-primary" href="/ProductSerialBarcode.aspx?id=<%= Item.ProductSerialID %>&amp;handler=Download&amp;scale=8">Download PNG</a>
      <a class="btn btn-secondary" href="/ProductSerialBarcode.aspx?id=<%= Item.ProductSerialID %>&amp;print=1" onclick="window.print();return false;">Print</a>
    </div>
  </div>
</div>
<% } %>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (PrintMode && Item != null) { %>
<script>window.addEventListener('load', function () { window.print(); });</script>
<% } %>
</asp:Content>
