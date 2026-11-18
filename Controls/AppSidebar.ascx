<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AppSidebar.ascx.cs" Inherits="HRMS.Controls.AppSidebar" %>
<% if (ShowSidebar) { %>
<aside class="ws-sidebar" id="wsSidebar" aria-label="Ghazi Workstation modules">
    <div class="ws-sidebar-head">
        <span class="ws-sidebar-kicker">Modules</span>
        <button type="button" class="ws-sidebar-pin" id="wsSidebarCollapse" title="Collapse menu" aria-label="Collapse menu">&#171;</button>
    </div>
    <nav class="ws-sidenav">
        <% foreach (var mod in Modules) { %>
        <div class="ws-mod<%= mod.Expanded ? " is-open" : "" %>" data-module="<%= mod.Id %>">
            <button type="button" class="ws-mod-toggle" aria-expanded="<%= mod.Expanded ? "true" : "false" %>">
                <span class="ws-mod-icon"><%= Server.HtmlEncode(mod.Icon) %></span>
                <span class="ws-mod-copy">
                    <span class="ws-mod-title"><%= Server.HtmlEncode(mod.Title) %></span>
                    <span class="ws-mod-sub"><%= Server.HtmlEncode(mod.Subtitle) %></span>
                </span>
                <span class="ws-mod-caret">&#9662;</span>
            </button>
            <div class="ws-mod-items">
                <% var any = false; foreach (var item in mod.Items) { if (!item.Visible) continue; any = true; %>
                <a href="<%= item.Url %>" class="ws-mod-link<%= item.Active ? " is-active" : "" %>" title="<%= Server.HtmlEncode(item.Description) %>"><%= Server.HtmlEncode(item.Title) %></a>
                <% } %>
                <% if (!any) { %>
                <p class="ws-mod-empty">No processes available yet.</p>
                <% } %>
            </div>
        </div>
        <% } %>
    </nav>
</aside>
<% } %>
