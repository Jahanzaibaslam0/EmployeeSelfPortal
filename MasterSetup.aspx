<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MasterSetup.aspx.cs" Inherits="HRMS.MasterSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="setup-page-header">
    <div>
        <h2>Master Setup</h2>
        <p>Open any configuration page you are authorized to use. Search or jump to a section, then view, add, or update setup values from the page you open.</p>
    </div>
    <div class="setup-page-meta">
        <span><strong><%= VisibleLinkCount %></strong> setup page<%= VisibleLinkCount == 1 ? "" : "s" %></span>
        <span><%= Categories.Count %> section<%= Categories.Count == 1 ? "" : "s" %></span>
    </div>
</div>

<% if (Categories.Count == 0) { %>
<div class="setup-empty">
    You do not have permission to any setup pages. Ask an administrator to grant read access on User Rights.
</div>
<% } else { %>
<div class="setup-toolbar">
    <div class="setup-search">
        <label for="txtSetupSearch" class="sr-only">Search setup pages</label>
        <input type="text" id="txtSetupSearch" class="form-control" placeholder="Search setup pages (name or section)..." autocomplete="off" />
    </div>
    <nav class="setup-jump" aria-label="Setup sections">
        <% foreach (var cat in Categories) { %>
        <a href="#<%= CategoryAnchor(cat.Category) %>" class="setup-jump-chip"><%= Server.HtmlEncode(cat.Category) %> <span><%= cat.Links.Count %></span></a>
        <% } %>
    </nav>
</div>
<p id="setupNoResults" class="setup-empty" hidden>No setup pages match your search.</p>
<% foreach (var cat in Categories) { %>
<section class="setup-category" id="<%= CategoryAnchor(cat.Category) %>" data-category="<%= Server.HtmlEncode(cat.Category) %>">
    <div class="setup-category-header">
        <div class="setup-category-icon"><%= Server.HtmlEncode(cat.Icon) %></div>
        <span class="setup-category-title"><%= Server.HtmlEncode(cat.Category) %></span>
        <span class="setup-category-count"><%= cat.Links.Count %></span>
    </div>
    <div class="setup-grid">
    <% foreach (var link in cat.Links) { %>
        <a href="<%= link.Url %>" class="setup-link<%= link.CanWrite ? "" : " setup-link--readonly" %>"
           data-title="<%= Server.HtmlEncode((link.Title + " " + cat.Category).ToLowerInvariant()) %>">
            <span class="setup-link-icon"><%= Server.HtmlEncode(link.Icon) %></span>
            <span class="setup-link-body">
                <span class="setup-link-title"><%= Server.HtmlEncode(link.Title) %></span>
                <span class="setup-link-access"><%= Server.HtmlEncode(link.AccessLabel) %></span>
            </span>
        </a>
    <% } %>
    </div>
</section>
<% } %>
<% } %>
<script>
(function () {
    var search = document.getElementById('txtSetupSearch');
    if (!search) return;
    var categories = document.querySelectorAll('.setup-category');
    var empty = document.getElementById('setupNoResults');
    var chips = document.querySelectorAll('.setup-jump-chip');
    search.addEventListener('input', function () {
        var term = this.value.toLowerCase().trim();
        var anyVisible = false;
        categories.forEach(function (cat) {
            var links = cat.querySelectorAll('.setup-link');
            var catVisible = false;
            links.forEach(function (link) {
                var title = link.getAttribute('data-title') || '';
                var show = !term || title.indexOf(term) >= 0;
                link.style.display = show ? '' : 'none';
                if (show) catVisible = true;
            });
            cat.style.display = catVisible ? '' : 'none';
            if (catVisible) anyVisible = true;
        });
        chips.forEach(function (chip) {
            var href = chip.getAttribute('href') || '';
            var id = href.charAt(0) === '#' ? href.substring(1) : '';
            var section = id ? document.getElementById(id) : null;
            chip.style.display = !section || section.style.display === 'none' ? 'none' : '';
        });
        if (empty) empty.hidden = anyVisible;
    });
})();
</script>
</asp:Content>
