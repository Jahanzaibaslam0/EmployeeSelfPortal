<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Inbox.aspx.cs" Inherits="HRMS.InboxPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card">
    <div class="card-header"><h2>In-application notifications</h2></div>
    <div class="card-body">
        <p class="text-muted">Stored on this server. No email is sent.</p>
        <% if (Items.Count == 0) { %>
        <p>No notifications.</p>
        <% } else { %>
        <ul style="list-style:none;padding:0;">
        <% foreach (var n in Items) { %>
            <li style="padding:.75rem 0;border-bottom:1px solid var(--border);<%= n.IsRead ? "" : "background:#f8fafc;" %>">
                <a href="/Inbox.aspx?open=<%= n.InboxID %>" style="font-weight:<%= n.IsRead ? "500" : "700" %>;"><%= Server.HtmlEncode(n.Title) %></a>
                <div class="text-muted" style="font-size:.85rem;"><%= n.CreatedOn.ToString("dd-MMM-yyyy HH:mm") %></div>
                <div style="white-space:pre-wrap;"><%= Server.HtmlEncode(n.Message) %></div>
            </li>
        <% } %>
        </ul>
        <% } %>
    </div>
</div>
</asp:Content>
