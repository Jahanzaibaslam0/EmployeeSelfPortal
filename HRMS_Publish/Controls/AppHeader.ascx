<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AppHeader.ascx.cs" Inherits="HRMS.Controls.AppHeader" %>
<header class="app-header">
    <div class="app-brand-band">
        <a href="/Home.aspx" class="app-logo" aria-label="HRMS Home">
            <img src="/images/gb-logo.png" alt="Ghazi Brothers" class="app-logo-img" />
        </a>
        <div class="app-title-block">
            <a href="/Home.aspx" class="brand-link">HRMS</a>
            <div class="app-subtitle">Human Resource Management System</div>
        </div>
        <div class="navbar-right">
            <span class="page-title"><%= PageTitleText %></span>
            <% if (ShowUserInfo) { %>
            <span class="user-info">
                <%= Username %>
                <% if (IsAdmin) { %><span class="badge badge-warning" style="font-size:.65rem;padding:1px 6px;">Admin</span><% } %>
            </span>
            <a href="/Login.aspx?logout=1" class="btn btn-secondary" style="padding:3px 12px;font-size:.78rem;margin-right:.5rem;">Logout</a>
            <% } %>
            <span id="clock" class="clock"></span>
        </div>
    </div>
    <nav class="app-nav">
        <a href="/Home.aspx" class="nav-link">Home</a>
        <% if (ShowDashboard) { %><a href="/Dashboard.aspx" class="nav-link">Dashboard</a><% } %>
        <a href="/UserProfile.aspx" class="nav-link">My Profile</a>
        <% if (ShowMyDocuments) { %><a href="/MyDocuments.aspx" class="nav-link">My Documents</a><% } %>
        <% if (ShowLmsLibrary) { %><a href="/LmsLibrary.aspx" class="nav-link">Knowledge Library</a><% } %>
        <% if (ShowMasterMenu) { %>
        <div class="nav-menu">
            <button type="button" class="nav-menu-button" data-menu="master">Master <span class="nav-caret">&#9662;</span></button>
            <div class="nav-menu-content">
                <% if (CanEmployeeMaster) { %><a href="/EmployeeMaster.aspx">Employee Master</a><% } %>
                <% if (CanPositionMaster) { %><a href="/PositionMaster.aspx">Position Master</a><% } %>
                <% if (CanCustomerMaster) { %><a href="/CustomerMaster.aspx">Customer Master</a><% } %>
                <% if (CanContactMaster) { %><a href="/ContactMaster.aspx">Contact Master</a><% } %>
                <% if (CanProductMaster) { %><a href="/ProductMaster.aspx">Product Master</a><% } %>
                <% if (CanInvoiceMaster) { %><a href="/InvoiceMaster.aspx">Invoice Master</a><% } %>
            </div>
        </div>
        <% } %>
        <a href="/QuickLinks.aspx" class="nav-link">Quick Links</a>
        <% if (ShowOrgSetupMenu) { %>
        <div class="nav-menu">
            <button type="button" class="nav-menu-button" data-menu="org">Organization Setup <span class="nav-caret">&#9662;</span></button>
            <div class="nav-menu-content">
                <% if (CanDivisionSetup) { %><a href="/DivisionSetup.aspx">Division</a><% } %>
                <% if (CanDepartmentSetup) { %><a href="/DepartmentSetup.aspx">Department</a><% } %>
                <% if (CanGenderSetup) { %><a href="/GenderSetup.aspx">Gender</a><% } %>
                <% if (CanBankSetup) { %><a href="/BankSetup.aspx">Bank Master</a><% } %>
                <% if (CanCurrencySetup) { %><a href="/CurrencySetup.aspx">Currency</a><% } %>
                <% if (CanCitySetup) { %><a href="/CitySetup.aspx">City</a><% } %>
                <% if (CanSkillSetup) { %><a href="/SkillSetup.aspx">Skill</a><% } %>
                <% if (CanLmsDocumentSetup) { %><a href="/LmsDocumentSetup.aspx">LMS Documents</a><% } %>
            </div>
        </div>
        <% } %>
        <% if (ShowSecurityMenu) { %>
        <div class="nav-menu">
            <button type="button" class="nav-menu-button" data-menu="sec">Security <span class="nav-caret">&#9662;</span></button>
            <div class="nav-menu-content">
                <% if (CanUserSetup) { %><a href="/UserSetup.aspx">User Setup</a><% } %>
                <% if (CanUserRightsSetup) { %><a href="/UserRightsSetup.aspx">User Rights</a><% } %>
                <% if (IsAdmin) { %><a href="/AuditReport.aspx">Audit Log Report</a><% } %>
            </div>
        </div>
        <% } %>
    </nav>
</header>

<% if (ShowLoginAlertsPopup) { %>
<div id="loginAlertsPopup" class="notif-popup-overlay" role="dialog" aria-modal="true">
  <div class="notif-popup-card" style="max-width:560px;">
    <div class="notif-popup-header">
      <h3>Latest Updates</h3>
      <button type="button" class="notif-popup-close" onclick="closeLoginAlertsPopup()" aria-label="Close">&times;</button>
    </div>
    <div class="notif-popup-body">
      <% if (PopupNotifications.Count > 0) {
           var latestNotif = PopupNotifications[0]; %>
        <h4 style="margin:0 0 .5rem; color:var(--gb-blue); font-size:.9rem;">Notifications</h4>
        <p class="notif-popup-intro"><%= PopupNotifications.Count %> active notification(s). Latest:</p>
        <a href="/Notifications.aspx?id=<%= latestNotif.NotificationID %>" class="notif-popup-link">
          <strong><%= Server.HtmlEncode(latestNotif.Name) %></strong>
          <span><%= Server.HtmlEncode(latestNotif.DepartmentName) %> · <%= latestNotif.StartDate.ToString("dd-MMM-yyyy") %> – <%= latestNotif.ValidTillDate.ToString("dd-MMM-yyyy") %></span>
        </a>
        <% if (PopupMemorandums.Count > 0) { %><hr style="margin:1rem 0; border:0; border-top:1px solid var(--border);" /><% } %>
      <% } %>

      <% if (PopupMemorandums.Count > 0) {
           var latestMemo = PopupMemorandums[0]; %>
        <h4 style="margin:0 0 .5rem; color:var(--gb-blue); font-size:.9rem;">Memorandums</h4>
        <p class="notif-popup-intro"><%= PopupMemorandums.Count %> active memorandum(s). Latest:</p>
        <a href="/Memorandums.aspx?id=<%= latestMemo.MemorandumID %>" class="notif-popup-link">
          <strong><%= Server.HtmlEncode(latestMemo.Name) %></strong>
          <span><%= Server.HtmlEncode(latestMemo.DepartmentName) %> · <%= latestMemo.StartDate.ToString("dd-MMM-yyyy") %> – <%= latestMemo.ValidTillDate.ToString("dd-MMM-yyyy") %></span>
        </a>
        <% if (latestMemo.HasDocument) { %>
        <p style="margin:.5rem 0 0;">
          <a href="/Memorandums.aspx?id=<%= latestMemo.MemorandumID %>" class="btn btn-secondary" style="font-size:.78rem; padding:4px 10px;">View Attached Document</a>
        </p>
        <% } %>
      <% } %>
    </div>
    <div class="notif-popup-footer">
      <% if (PopupNotifications.Count > 0) { %><a href="/Notifications.aspx" class="btn btn-primary">All Notifications</a><% } %>
      <% if (PopupMemorandums.Count > 0) { %><a href="/Memorandums.aspx" class="btn btn-primary">All Memorandums</a><% } %>
      <button type="button" class="btn btn-secondary" onclick="closeLoginAlertsPopup()">Dismiss</button>
    </div>
  </div>
</div>
<script>
function closeLoginAlertsPopup() {
    var el = document.getElementById('loginAlertsPopup');
    if (el) el.style.display = 'none';
}
</script>
<% } %>
