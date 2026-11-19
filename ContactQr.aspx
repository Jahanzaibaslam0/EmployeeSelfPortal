<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactQr.aspx.cs" Inherits="HRMS.ContactQrPage" ResponseEncoding="utf-8" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.contact-qr-wrap { max-width: 480px; margin: 0 auto; }
.contact-qr-card { background:#fff; padding:1.25rem; border:1px solid var(--border); border-radius:8px; text-align:center; }
.contact-qr-card img { display:block; margin:0 auto 1rem; image-rendering:pixelated; background:#fff; border:1px solid #e5e7eb; }
.contact-qr-meta { text-align:left; font-size:.9rem; line-height:1.5; color:#374151; }
.contact-qr-actions { display:flex; flex-wrap:wrap; gap:.5rem; justify-content:center; margin-top:1rem; }
@media print {
  .no-print, .ws-sidebar, .app-header, .app-footer, .ws-menu-btn { display:none !important; }
  .contact-qr-card { border:0; }
}
</style>
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-error"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } else if (Item != null) { %>
<div class="contact-qr-wrap">
  <div class="form-breadcrumb no-print" style="margin-bottom:1rem;">
    <a href="/ContactMaster.aspx?editId=<%= Item.ContactID %>" class="btn btn-secondary">&#8592; Back to contact</a>
  </div>
  <div class="contact-qr-card" id="contactQrPrintArea">
    <h2 style="margin-top:0;"><%= Server.HtmlEncode(Item.Name) %></h2>
    <p class="text-muted" style="margin-top:0;"><%= Server.HtmlEncode(Item.ContactCode) %></p>
    <img alt="Contact QR code" width="240" height="240"
         src="<%= Server.HtmlEncode(PngUrl(8)) %>" />
    <div class="contact-qr-meta">
      <% if (!string.IsNullOrEmpty(CompanyName)) { %><div><strong>Company:</strong> <%= Server.HtmlEncode(CompanyName) %></div><% } %>
      <% if (!string.IsNullOrEmpty(MobileNumber)) { %><div><strong>Mobile:</strong> <%= Server.HtmlEncode(MobileNumber) %></div><% } %>
      <% if (!string.IsNullOrEmpty(Item.Email)) { %><div><strong>Email:</strong> <%= Server.HtmlEncode(Item.Email) %></div><% } %>
      <% if (!string.IsNullOrEmpty(AddressText)) { %><div><strong>Address:</strong> <%= Server.HtmlEncode(AddressText) %></div><% } %>
    </div>
    <p class="text-muted no-print" style="font-size:.8rem;margin:1rem 0 0;">Scan this QR code to save the contact. It is generated from the live record and updates when the contact changes.</p>
  </div>
  <div class="contact-qr-actions no-print">
    <a class="btn btn-primary" href="<%= Server.HtmlEncode(DownloadUrl) %>">Download QR</a>
    <a class="btn btn-secondary" href="/ContactExport.aspx?id=<%= Item.ContactID %>">Download vCard</a>
    <button type="button" class="btn btn-secondary" id="btnShareContactQr">Share</button>
    <button type="button" class="btn btn-secondary" onclick="window.print();">Print</button>
  </div>
</div>
<script>
(function () {
  var btn = document.getElementById('btnShareContactQr');
  if (!btn) return;
  btn.addEventListener('click', function () {
    var title = <%= ShareTitleJson %>;
    var text = <%= ShareTextJson %>;
    var url = window.location.href;
    if (navigator.share) {
      navigator.share({ title: title, text: text, url: url }).catch(function () {});
      return;
    }
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text + '\n' + url);
      alert('Contact details copied. You can paste them to share.');
      return;
    }
    alert(text);
  });
})();
</script>
<% } %>
</asp:Content>
