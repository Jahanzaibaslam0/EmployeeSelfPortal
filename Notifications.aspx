<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.NotificationsPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card">
  <div class="card-header space-between">
    <h2>Notifications</h2>
    <a href="/NotificationSetup.aspx" class="btn btn-secondary">Notification Setup</a>
  </div>
  <div class="card-body" style="display:flex;gap:1.5rem;align-items:flex-start;">
    <div style="min-width:280px;max-width:320px;">
      <% if (ActiveNotifications.Count == 0) { %>
        <p class="text-muted">No active notifications.</p>
      <% } else {
           foreach (var n in ActiveNotifications) {
             var isSelected = Selected != null && Selected.NotificationID == n.NotificationID; %>
        <a href="/Notifications.aspx?id=<%= n.NotificationID %>"
           class="setup-link<%= isSelected ? " active" : "" %>"
           style="display:block;margin-bottom:.5rem;<%= isSelected ? "background:#e8f0fe;font-weight:600;" : "" %>">
          <%= Server.HtmlEncode(n.Name) %>
          <span style="display:block;font-size:.75rem;color:var(--text-muted);font-weight:400;margin-top:.15rem;">
            <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(n.DepartmentName) ? "All Departments" : n.DepartmentName) %>
            · <%= n.StartDate.ToString("dd-MMM-yyyy") %> – <%= n.ValidTillDate.ToString("dd-MMM-yyyy") %>
          </span>
        </a>
      <% } } %>
    </div>
    <div style="flex:1;min-width:0;">
      <% if (Selected != null) { %>
        <h3 style="margin-top:0;"><%= Server.HtmlEncode(Selected.Name) %></h3>
        <p style="color:var(--text-muted);font-size:.9rem;margin:.25rem 0 1rem;">
          <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(Selected.DepartmentName) ? "All Departments" : Selected.DepartmentName) %>
          · <%= Selected.StartDate.ToString("dd-MMM-yyyy") %> – <%= Selected.ValidTillDate.ToString("dd-MMM-yyyy") %>
        </p>
        <div style="white-space:pre-wrap;"><%= Server.HtmlEncode(Selected.Description) %></div>
      <% } else { %>
        <p>Select a notification.</p>
      <% } %>
    </div>
  </div>
</div>
</asp:Content>
