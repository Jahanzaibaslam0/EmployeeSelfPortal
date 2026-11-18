<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AppHeader.ascx.cs" Inherits="HRMS.Controls.AppHeader" %>
<header class="app-header ws-topbar">
    <div class="app-brand-band">
        <button type="button" class="ws-menu-btn" id="wsMenuBtn" title="Open modules" aria-label="Open modules">&#9776;</button>
        <a href="/Home.aspx" class="app-logo" aria-label="Ghazi Workstation Home">
            <img src="/images/gb-logo.png" alt="Ghazi Brothers" class="app-logo-img" />
        </a>
        <div class="app-title-block">
            <a href="/Home.aspx" class="brand-link">Ghazi Workstation</a>
            <div class="app-subtitle">Business management platform</div>
        </div>
        <div class="navbar-right">
            <span class="page-title"><%= PageTitleText %></span>
            <% if (ShowUserInfo) { %>
            <span class="user-info">
                <%= Username %>
                <% if (IsAdmin) { %><span class="badge badge-warning" style="font-size:.65rem;padding:1px 6px;">Admin</span><% } %>
            </span>
            <a href="/Inbox.aspx" class="inbox-bell" title="Notifications" aria-label="Notifications">
                &#128276;<% if (InboxUnread > 0) { %><span class="inbox-badge"><%= InboxUnread > 99 ? "99+" : InboxUnread.ToString() %></span><% } %>
            </a>
            <a href="/AccountSettings.aspx" class="btn btn-secondary" style="padding:3px 12px;font-size:.78rem;margin-right:.35rem;">Account</a>
            <a href="/Login.aspx?logout=1" class="btn btn-secondary" style="padding:3px 12px;font-size:.78rem;margin-right:.5rem;">Logout</a>
            <% } %>
            <span id="clock" class="clock"></span>
        </div>
    </div>
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
          <span><%= Server.HtmlEncode(latestNotif.DepartmentName) %> &middot; <%= latestNotif.StartDate.ToString("dd-MMM-yyyy") %> &ndash; <%= latestNotif.ValidTillDate.ToString("dd-MMM-yyyy") %></span>
        </a>
        <% if (PopupMemorandums.Count > 0) { %><hr style="margin:1rem 0; border:0; border-top:1px solid var(--border);" /><% } %>
      <% } %>

      <% if (PopupMemorandums.Count > 0) {
           var latestMemo = PopupMemorandums[0]; %>
        <h4 style="margin:0 0 .5rem; color:var(--gb-blue); font-size:.9rem;">Memorandums</h4>
        <p class="notif-popup-intro"><%= PopupMemorandums.Count %> active memorandum(s). Latest:</p>
        <a href="/Memorandums.aspx?id=<%= latestMemo.MemorandumID %>" class="notif-popup-link">
          <strong><%= Server.HtmlEncode(latestMemo.Name) %></strong>
          <span><%= Server.HtmlEncode(latestMemo.DepartmentName) %> &middot; <%= latestMemo.StartDate.ToString("dd-MMM-yyyy") %> &ndash; <%= latestMemo.ValidTillDate.ToString("dd-MMM-yyyy") %></span>
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
