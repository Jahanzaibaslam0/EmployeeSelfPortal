<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="HRMS.HomePage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>HRMS - Home</title>
    <link rel="stylesheet" href="/css/style.css?v=10" />
</head>
<body>
<form id="form1" runat="server">
<%@ Register Src="~/Controls/AppHeader.ascx" TagPrefix="hrms" TagName="AppHeader" %>
<hrms:AppHeader runat="server" PageTitleText="Home" />
<main class="container">
<% if (ShowProfileSyncWarning) { %>
    <div class="alert alert-warning"><%= ProfileSyncMessage %></div>
<% } %>
<% if (Slides.Count == 0) { %>
    <section class="home-welcome-banner">
        <div class="home-welcome-text">
            <h2>Welcome to HRMS</h2>
            <p>Human Resource Management System — access HR processes and employee services from one place.</p>
        </div>
    </section>
<% } else { %>
    <section class="home-gallery" aria-label="Image gallery">
        <div class="home-gallery-slider" id="homeGallerySlider">
        <% for (var i = 0; i < Slides.Count; i++) { var slide = Slides[i]; %>
            <div class="home-gallery-slide <%= i == 0 ? "active" : "" %>">
                <img src="<%= slide.ImagePath %>" alt="<%= slide.Title %>" />
                <% if (!string.IsNullOrWhiteSpace(slide.Title)) { %>
                <div class="home-gallery-caption"><strong><%= slide.Title %></strong>
                <% if (!string.IsNullOrWhiteSpace(slide.Description)) { %><span><%= slide.Description %></span><% } %>
                </div><% } %>
            </div>
        <% } %>
        </div>
    </section>
<% } %>
<div class="quick-links-intro"><p>Browse HR processes, announcements, and employee services.</p></div>
<section class="quick-links-section">
    <div class="quick-links-section-header">
        <h2>HR Processes &amp; Services</h2>
        <span class="quick-links-count"><%= ProcessLinks.Count %> links</span>
    </div>
    <div class="quick-links-grid">
    <% foreach (var link in ProcessLinks) { %>
        <a href="<%= link.Url %>" class="quick-link-card quick-link-card--process">
            <div class="quick-link-icon"><%= link.Icon %></div>
            <div class="quick-link-body">
                <h3><%= link.Title %></h3>
                <p><%= link.Description %></p>
                <span class="quick-link-badge"><%= link.Category %></span>
            </div>
            <span class="quick-link-arrow">&#8594;</span>
        </a>
    <% } %>
    </div>
</section>
</main>
<%@ Register Src="~/Controls/AppFooter.ascx" TagPrefix="hrms" TagName="AppFooter" %>
<hrms:AppFooter runat="server" />
</form>
<script src="/js/app.js"></script>
</body>
</html>
