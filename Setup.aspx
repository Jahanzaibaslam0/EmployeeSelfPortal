<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="HRMS.SetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="setup-page-header">
    <h2>System Setup &amp; Configuration</h2>
    <p>Manage organization, employee, customer, and product setup options.</p>
</div>
<div class="setup-search">
    <input type="text" id="txtSetupSearch" class="form-control" placeholder="Search setup options..." />
</div>
<% foreach (var cat in Categories) { %>
<section class="setup-category" data-category="<%= cat.Category %>">
    <div class="setup-category-header">
        <div class="setup-category-icon"><%= cat.Icon %></div>
        <span class="setup-category-title"><%= cat.Category %></span>
    </div>
    <div class="setup-grid">
    <% foreach (var link in cat.Links) { %>
        <a href="<%= link.Url %>" class="setup-link" data-title="<%= link.Title.ToLowerInvariant() %>">
            <span class="setup-link-icon"><%= link.Icon %></span>
            <span><%= link.Title %></span>
        </a>
    <% } %>
    </div>
</section>
<% } %>
<script>
(function () {
    var search = document.getElementById('txtSetupSearch');
    if (!search) return;
    var categories = document.querySelectorAll('.setup-category');
    search.addEventListener('input', function () {
        var term = this.value.toLowerCase().trim();
        categories.forEach(function (cat) {
            var links = cat.querySelectorAll('.setup-link');
            var anyVisible = false;
            links.forEach(function (link) {
                var title = link.getAttribute('data-title') || '';
                var show = !term || title.indexOf(term) >= 0;
                link.style.display = show ? '' : 'none';
                if (show) anyVisible = true;
            });
            cat.style.display = anyVisible ? '' : 'none';
        });
    });
})();
</script>
</asp:Content>
