<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EmployeeCard.aspx.cs" Inherits="HRMS.EmployeeCardPage" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
@media print {
  .no-print, .app-header, .app-footer, header, footer { display: none !important; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (ShowPicker) { %>
<div class="card no-print">
  <div class="card-header">
    <h2>Select Employee for ID Card</h2>
  </div>
  <div class="card-body">
    <p class="text-muted" style="margin-bottom:1rem;">Choose an employee to preview, print, or download their identity card.</p>
    <div class="employee-card-picker">
      <label for="ddlEmployeeCard">Employee</label>
      <select id="ddlEmployeeCard" class="form-control">
        <option value="">— Select employee —</option>
        <% foreach (var emp in Employees) { %>
        <option value="<%= emp.EmployeeID %>"><%= Server.HtmlEncode(emp.EmployeeCode) %> – <%= Server.HtmlEncode(emp.FullName) %> (<%= Server.HtmlEncode(emp.DepartmentName) %>)</option>
        <% } %>
      </select>
      <button type="button" id="btnViewCard" class="btn btn-primary" disabled>View Card</button>
    </div>
  </div>
</div>
<% } else if (!string.IsNullOrEmpty(ErrorMessage)) { %>
<div class="card no-print">
  <div class="card-body">
    <div class="alert alert-error"><%= Server.HtmlEncode(ErrorMessage) %></div>
    <a href="<%= ResolveUrl("~/EmployeeMaster.aspx") %>" class="btn btn-secondary">Back to Employee Master</a>
  </div>
</div>
<% } else if (Card != null) {
     var card = Card;
%>
<div class="employee-card-toolbar no-print">
  <div class="toolbar-left">
    <% if (CanBrowseCards) { %>
    <a href="<%= ResolveUrl("~/EmployeeCard.aspx") %>" class="btn btn-secondary">&#8592; Back to List</a>
    <a href="<%= ResolveUrl("~/EmployeeMaster.aspx") %>" class="btn btn-secondary">Employee Master</a>
    <% } else { %>
    <a href="<%= ResolveUrl("~/UserProfile.aspx") %>" class="btn btn-secondary">&#8592; Back</a>
    <% } %>
  </div>
  <div class="toolbar-right">
    <button type="button" class="btn btn-primary" id="btnPrintCard">Print</button>
    <button type="button" class="btn btn-primary" id="btnDownloadPdf">Download PDF</button>
  </div>
</div>

<div class="employee-card-stage" id="employeeCardStage">
  <article class="id-card <%= card.IsVisitorCard ? "id-card--visitor" : "id-card--employee" %>" id="idCardPrintArea">
    <header class="id-card__header">
      <div class="id-card__brand">
        <img src="<%= ResolveUrl("~/images/gb-logo.png") %>" alt="Company logo" class="id-card__logo"
             onerror="this.style.display='none'; if(this.nextElementSibling) this.nextElementSibling.style.display='flex';" />
        <div class="id-card__logo-fallback" style="display:none;">GB</div>
        <div class="id-card__company">
          <% if (!string.IsNullOrWhiteSpace(card.CompanyName)) { %>
          <span class="id-card__company-name"><%= Server.HtmlEncode(card.CompanyName) %></span>
          <% } else { %>
          <span class="id-card__company-name">Ghazi Brothers</span>
          <% } %>
          <span class="id-card__company-tag">Human Resource Management System</span>
        </div>
      </div>
      <span class="id-card__type-badge"><%= Server.HtmlEncode(card.CardTitle) %></span>
    </header>

    <div class="id-card__body">
      <div class="id-card__content">
        <div class="id-card__details">
          <h1 class="id-card__name"><%= Server.HtmlEncode(card.FullName) %></h1>
          <p class="id-card__designation"><%= string.IsNullOrWhiteSpace(card.Designation) ? "—" : Server.HtmlEncode(card.Designation) %></p>

          <dl class="id-card__fields">
            <div class="id-card__field">
              <dt>Employee ID</dt>
              <dd><%= string.IsNullOrWhiteSpace(card.EmployeeCode) ? "—" : Server.HtmlEncode(card.EmployeeCode) %></dd>
            </div>
            <% if (!string.IsNullOrWhiteSpace(card.DepartmentName)) { %>
            <div class="id-card__field">
              <dt>Department</dt>
              <dd><%= Server.HtmlEncode(card.DepartmentName) %></dd>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(card.OfficeLocation)) { %>
            <div class="id-card__field">
              <dt>Office Location</dt>
              <dd><%= Server.HtmlEncode(card.OfficeLocation) %></dd>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(card.Email)) { %>
            <div class="id-card__field">
              <dt>Email</dt>
              <dd><%= Server.HtmlEncode(card.Email) %></dd>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(card.Phone)) { %>
            <div class="id-card__field">
              <dt>Contact</dt>
              <dd><%= Server.HtmlEncode(card.Phone) %></dd>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(card.BloodGroup)) { %>
            <div class="id-card__field">
              <dt>Blood Group</dt>
              <dd><%= Server.HtmlEncode(card.BloodGroup) %></dd>
            </div>
            <% } %>
            <% if (!string.IsNullOrWhiteSpace(card.DateOfJoining)) { %>
            <div class="id-card__field">
              <dt>Date of Joining</dt>
              <dd><%= Server.HtmlEncode(card.DateOfJoining) %></dd>
            </div>
            <% } %>
            <% if (card.IsVisitorCard && !string.IsNullOrWhiteSpace(card.ValidityPeriod)) { %>
            <div class="id-card__field id-card__field--highlight">
              <dt>Validity</dt>
              <dd><%= Server.HtmlEncode(card.ValidityPeriod) %></dd>
            </div>
            <% } %>
          </dl>
        </div>
      </div>

      <aside class="id-card__photo-panel" aria-label="Profile picture">
        <span class="id-card__photo-label">Profile Picture</span>
        <div class="id-card__photo-slot" id="employeePhotoSlot">
          <% if (!string.IsNullOrWhiteSpace(card.PhotoUrl)) { %>
          <img src="<%= Server.HtmlEncode(card.PhotoUrl) %>" alt="<%= Server.HtmlEncode(card.FullName) %>" class="id-card__photo" id="cardEmployeePhoto" />
          <% } else { %>
          <img src="<%= ResolveUrl("~/images/default-avatar.svg") %>" alt="No profile photo" class="id-card__photo id-card__photo--default" />
          <% } %>
        </div>
      </aside>
    </div>

    <footer class="id-card__footer">
      <span>This card is property of <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(card.CompanyName) ? "Ghazi Brothers" : card.CompanyName) %>.</span>
      <% if (!string.IsNullOrWhiteSpace(card.Status)) {
           var statusClass = string.Equals(card.Status, "Active", StringComparison.OrdinalIgnoreCase) ? "active" : "inactive";
      %>
      <span class="id-card__status id-card__status--<%= statusClass %>"><%= Server.HtmlEncode(card.Status) %></span>
      <% } %>
    </footer>
  </article>
</div>
<% } %>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>document.body.classList.add('employee-card-page');</script>
<% if (Card != null) { %>
<script src="https://cdn.jsdelivr.net/npm/qrcode@1.5.3/build/qrcode.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js"></script>
<script>
  window.hrmsEmployeeCard = {
    qrPayload: <%= QrPayloadJson %>,
    defaultAvatarUrl: '<%= ResolveUrl("~/images/default-avatar.svg") %>'
  };
</script>
<script src="<%= ResolveUrl("~/js/employee-card.js") %>?v=3"></script>
<% } %>
<% if (ShowPicker) { %>
<script>
(function () {
  var ddl = document.getElementById('ddlEmployeeCard');
  var btn = document.getElementById('btnViewCard');
  if (!ddl || !btn) return;
  ddl.addEventListener('change', function () { btn.disabled = !ddl.value; });
  btn.addEventListener('click', function () {
    if (ddl.value) window.location.href = '<%= ResolveUrl("~/EmployeeCard.aspx") %>?id=' + encodeURIComponent(ddl.value);
  });
})();
</script>
<% } %>
</asp:Content>
