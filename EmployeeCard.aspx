<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EmployeeCard.aspx.cs" Inherits="HRMS.EmployeeCardPage" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
@media print {
  .no-print, .app-header, .app-footer, header, footer, .ws-sidebar, .ws-menu-btn { display: none !important; }
  .ws-shell { display: block !important; }
  .ws-main { width: 100% !important; }
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (ShowPicker) { %>
<div class="card no-print">
  <div class="card-header space-between">
    <h2>Employee / Visitor Cards</h2>
    <span class="text-muted"><%= Employees.Count %> employee<%= Employees.Count==1?"":"s" %></span>
  </div>
  <div class="card-body">
    <p class="text-muted" style="margin-bottom:1rem;">Each employee has a unique QR code with name, mobile, email, company, and address. Open a card to print or share.</p>
    <div class="employee-card-picker" style="margin-bottom:1rem;">
      <label for="txtEmployeeCardSearch">Search</label>
      <input type="text" id="txtEmployeeCardSearch" class="form-control" placeholder="Search name, code, department, phone, or email..." />
    </div>
    <div class="table-responsive">
      <table class="data-table" id="employeeCardTable">
        <thead>
          <tr>
            <th>QR</th>
            <th>Employee ID</th>
            <th>Name</th>
            <th>Department</th>
            <th>Mobile</th>
            <th>Email</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% foreach (var emp in Employees) { %>
          <tr data-search="<%= Server.HtmlEncode((emp.EmployeeCode + " " + emp.FullName + " " + emp.DepartmentName + " " + emp.Phone + " " + emp.Email).ToLowerInvariant()) %>">
            <td class="hub-qr-cell">
              <a href="<%= ResolveUrl("~/EmployeeCard.aspx") %>?id=<%= emp.EmployeeID %>" title="Open ID card">
                <img class="hub-qr-thumb" alt="QR for <%= Server.HtmlEncode(emp.FullName) %>" width="48" height="48"
                     src="<%= emp.QrPngUrl %>" />
              </a>
            </td>
            <td><%= Server.HtmlEncode(emp.EmployeeCode) %></td>
            <td><%= Server.HtmlEncode(emp.FullName) %></td>
            <td><%= Server.HtmlEncode(emp.DepartmentName) %></td>
            <td><%= Server.HtmlEncode(emp.Phone) %></td>
            <td><%= Server.HtmlEncode(emp.Email) %></td>
            <td><a class="btn btn-secondary" style="padding:2px 8px;font-size:.75rem;" href="<%= ResolveUrl("~/EmployeeCard.aspx") %>?id=<%= emp.EmployeeID %>">View card</a></td>
          </tr>
        <% } %>
        <% if (Employees.Count == 0) { %><tr><td colspan="7">No active employees found.</td></tr><% } %>
        </tbody>
      </table>
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
    <a class="btn btn-secondary" href="<%= card.QrDownloadUrl %>">Download QR</a>
    <button type="button" class="btn btn-secondary" id="btnShareEmployeeQr">Share</button>
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
          <p class="id-card__designation"><%= string.IsNullOrWhiteSpace(card.Designation) ? "\u2014" : Server.HtmlEncode(card.Designation) %></p>

          <dl class="id-card__fields">
            <div class="id-card__field">
              <dt>Employee ID</dt>
              <dd><%= string.IsNullOrWhiteSpace(card.EmployeeCode) ? "\u2014" : Server.HtmlEncode(card.EmployeeCode) %></dd>
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
        <div class="id-card__qr-wrap" style="flex-direction:column;border-top:0;padding-top:.35rem;margin-top:.15rem;">
          <img src="<%= card.QrPngUrl(4) %>" alt="Employee QR code" width="72" height="72" />
          <div class="id-card__qr-label">Scan for contact</div>
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
<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js"></script>
<script>
  window.hrmsEmployeeCard = {
    qrPayload: <%= QrPayloadJson %>,
    employeeCode: <%= EmployeeCodeJson %>,
    defaultAvatarUrl: '<%= ResolveUrl("~/images/default-avatar.svg") %>'
  };
</script>
<script src="<%= ResolveUrl("~/js/employee-card.js") %>?v=5"></script>
<script>
(function () {
  var btn = document.getElementById('btnShareEmployeeQr');
  if (!btn) return;
  var title = <%= ShareTitleJson %>;
  var text = <%= ShareTextJson %>;
  btn.addEventListener('click', function () {
    var url = window.location.href.split('&handler')[0];
    if (navigator.share) {
      navigator.share({ title: title, text: text, url: url }).catch(function () {});
      return;
    }
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text + '\n' + url);
      alert('Employee details copied. You can paste them to share.');
      return;
    }
    alert(text);
  });
})();
</script>
<% } %>
<% if (ShowPicker) { %>
<script>
(function () {
  var input = document.getElementById('txtEmployeeCardSearch');
  var table = document.getElementById('employeeCardTable');
  if (!input || !table) return;
  input.addEventListener('input', function () {
    var q = (input.value || '').toLowerCase().trim();
    var rows = table.querySelectorAll('tbody tr');
    for (var i = 0; i < rows.length; i++) {
      var hay = rows[i].getAttribute('data-search') || '';
      rows[i].style.display = !q || hay.indexOf(q) >= 0 ? '' : 'none';
    }
  });
})();
</script>
<% } %>
</asp:Content>
