<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="HRMS.HomePage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (ShowProfileSyncWarning) { %>
    <div class="alert alert-warning"><%= ProfileSyncMessage %></div>
<% } %>
<% if (Slides.Count == 0) { %>
    <section class="home-welcome-banner">
        <div class="home-welcome-text">
            <h2>Welcome to Ghazi Workstation</h2>
            <p>Open a business module to reach the same processes you already use. Nothing in the workflows has changed &mdash; only how you find them.</p>
        </div>
    </section>
<% } else { %>
    <section class="home-gallery" aria-label="Image gallery">
        <div class="home-gallery-slider" id="homeGallerySlider">
        <% for (var i = 0; i < Slides.Count; i++) { var slide = Slides[i]; %>
            <div class="home-gallery-slide <%= i == 0 ? "active" : "" %>">
                <img src="<%= ResolveUrl(slide.DisplayUrl) %>" alt="<%= Server.HtmlEncode(slide.Title) %>" />
                <% if (!string.IsNullOrWhiteSpace(slide.Title)) { %>
                <div class="home-gallery-caption"><strong><%= Server.HtmlEncode(slide.Title) %></strong>
                <% if (!string.IsNullOrWhiteSpace(slide.Description)) { %><span><%= Server.HtmlEncode(slide.Description) %></span><% } %>
                </div><% } %>
            </div>
        <% } %>
        </div>
    </section>
<% } %>
<div class="quick-links-intro"><p>Choose a module, then open an existing process you are authorized to use.</p></div>
<section class="ws-module-board">
<% foreach (var mod in Modules) { %>
    <article class="ws-module-card" id="mod-<%= mod.Id %>">
        <div class="ws-module-card-head">
            <span class="ws-mod-icon"><%= Server.HtmlEncode(mod.Icon) %></span>
            <div>
                <h2><%= Server.HtmlEncode(mod.Title) %></h2>
                <p><%= Server.HtmlEncode(mod.Subtitle) %></p>
            </div>
            <span class="quick-links-count"><%= mod.VisibleCount %> process<%= mod.VisibleCount == 1 ? "" : "es" %></span>
        </div>
        <div class="ws-module-card-body">
            <% var any = false; foreach (var item in mod.Items) { if (!item.Visible) continue; any = true; %>
            <a href="<%= item.Url %>" class="ws-process-link">
                <strong><%= Server.HtmlEncode(item.Title) %></strong>
                <span><%= Server.HtmlEncode(item.Description) %></span>
            </a>
            <% } %>
            <% if (!any) { %>
            <p class="ws-mod-empty">No processes are available in this module yet, or none are granted to your account.</p>
            <% } %>
        </div>
    </article>
<% } %>
</section>
</asp:Content>
<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
(function () {
    var slider = document.getElementById('homeGallerySlider');
    if (!slider) return;
    var slides = slider.querySelectorAll('.home-gallery-slide');
    if (slides.length < 2) return;
    var index = 0;
    setInterval(function () {
        slides[index].classList.remove('active');
        index = (index + 1) % slides.length;
        slides[index].classList.add('active');
    }, 6000);
})();
</script>
</asp:Content>
