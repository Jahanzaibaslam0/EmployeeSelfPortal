<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProfileChangeHistory.aspx.cs" Inherits="HRMS.ProfileChangeHistoryPage" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<div class="card">
    <div class="card-header"><h2>Profile update history</h2></div>
    <div class="card-body table-responsive">
        <p class="text-muted">Official profile data always shows the last HR-approved values. Pending and rejected requests never overwrite that data.</p>
        <table class="data-table">
            <thead><tr><th>Request</th><th>Status</th><th>Submitted</th><th>Decision</th><th></th></tr></thead>
            <tbody>
            <% if (Requests.Count == 0) { %>
            <tr><td colspan="5" class="empty-cell">No profile update requests yet.</td></tr>
            <% } foreach (var r in Requests) { %>
            <tr>
                <td><%= Server.HtmlEncode(r.RequestCode) %></td>
                <td><span class="<%= r.BadgeClass %>"><%= Server.HtmlEncode(r.StatusDisplay) %></span></td>
                <td><%= r.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %></td>
                <td><%= r.DecidedOn.HasValue ? r.DecidedOn.Value.ToString("dd-MMM-yyyy HH:mm") : "\u2014" %></td>
                <td><a href="/ProfileChangeHistory.aspx?id=<%= r.RequestID %>">View</a></td>
            </tr>
            <% } %>
            </tbody>
        </table>
    </div>
</div>
<% if (Detail != null) { %>
<div class="card mt-4">
    <div class="card-header"><h2><%= Server.HtmlEncode(Detail.RequestCode) %> <span class="<%= Detail.BadgeClass %>"><%= Server.HtmlEncode(Detail.StatusDisplay) %></span></h2></div>
    <div class="card-body">
        <% if (!string.IsNullOrEmpty(Detail.DecisionComments)) { %>
        <div class="alert alert-<%= Detail.Status == "Rejected" ? "error" : "info" %>"><strong>HR comments:</strong> <%= Server.HtmlEncode(Detail.DecisionComments) %></div>
        <% } %>
        <table class="data-table">
            <thead><tr><th>Field</th><th>Approved value at submit</th><th>Proposed value</th></tr></thead>
            <tbody>
            <% foreach (var line in Detail.VisibleLines) { %>
            <tr>
                <td><%= Server.HtmlEncode(line.FieldLabel) %></td>
                <td><%= ProfileChangeRequestService.RenderChangeValue(line, false) %></td>
                <td><%= ProfileChangeRequestService.RenderChangeValue(line, true) %></td>
            </tr>
            <% } %>
            </tbody>
        </table>
        <h3 class="form-section-title" style="margin-top:1rem;">Workflow history</h3>
        <ul>
        <% foreach (var h in Detail.History) { %>
            <li><%= h.ActionOn.ToString("dd-MMM-yyyy HH:mm") %> &mdash; <%= Server.HtmlEncode(HRMS.Services.ProfileChangeStatuses.Display(h.Status)) %>
                by <%= Server.HtmlEncode(h.ActorName) %>
                <% if (!string.IsNullOrEmpty(h.Comments)) { %> &middot; <%= Server.HtmlEncode(h.Comments) %><% } %></li>
        <% } %>
        </ul>
    </div>
</div>
<% } %>
</asp:Content>
