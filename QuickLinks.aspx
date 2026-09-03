<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuickLinks.aspx.cs" Inherits="HRMS.QuickLinksPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<section class="quick-links-section">
    <div class="quick-links-section-header"><h2>HR Processes</h2></div>
    <div class="quick-links-grid">
    <% foreach (var link in HrProcessLinks) { %>
        <a href="<%= link.Url %>" class="quick-link-card"><div class="quick-link-icon"><%= link.Icon %></div>
        <div class="quick-link-body"><h3><%= link.Title %></h3><p><%= link.Description %></p></div></a>
    <% } %>
    </div>
</section>
<section class="quick-links-section">
    <div class="quick-links-section-header"><h2>Software Links</h2></div>
    <div class="quick-links-grid">
    <% foreach (var link in SoftwareLinks) { %>
        <a href="<%= link.Url %>" class="quick-link-card" <%= link.External ? "target=\"_blank\" rel=\"noopener\"" : "" %>>
        <div class="quick-link-icon"><%= link.Icon %></div>
        <div class="quick-link-body"><h3><%= link.Title %></h3><p><%= link.Description %></p></div></a>
    <% } %>
    <% if (SoftwareLinks.Count == 0) { %><p>No software links configured.</p><% } %>
    </div>
</section>
</asp:Content>
