<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProfileChangeReview.aspx.cs" Inherits="HRMS.ProfileChangeReviewPage" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<input type="hidden" name="__handler" id="__handler" value="" />
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>

<div class="card">
    <div class="card-header"><h2>Pending profile updates (<%= Pending.Count %>)</h2></div>
    <div class="card-body table-responsive">
        <table class="data-table">
            <thead><tr><th>Request</th><th>Employee</th><th>Submitted</th><th>By</th><th></th></tr></thead>
            <tbody>
            <% if (Pending.Count == 0) { %>
            <tr><td colspan="5" class="empty-cell">No pending profile update requests.</td></tr>
            <% } foreach (var r in Pending) { %>
            <tr>
                <td><%= Server.HtmlEncode(r.RequestCode) %></td>
                <td><%= Server.HtmlEncode(r.EmployeeCode) %> <%= Server.HtmlEncode(r.EmployeeName) %></td>
                <td><%= r.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %></td>
                <td><%= Server.HtmlEncode(r.SubmittedByName) %></td>
                <td><a href="/ProfileChangeReview.aspx?id=<%= r.RequestID %>">Review</a></td>
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
        <p><strong>Employee:</strong> <%= Server.HtmlEncode(Detail.EmployeeCode) %> <%= Server.HtmlEncode(Detail.EmployeeName) %>
           &middot; <a href="/EmployeeMaster.aspx?editId=<%= Detail.EmployeeID %>">Open approved profile</a></p>
        <p><strong>Submitted:</strong> <%= Detail.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %> by <%= Server.HtmlEncode(Detail.SubmittedByName) %></p>
        <% if (Detail.DecidedOn.HasValue) { %>
        <p><strong>Decision:</strong> <%= Detail.DecidedOn.Value.ToString("dd-MMM-yyyy HH:mm") %> by <%= Server.HtmlEncode(Detail.DecidedByName) %></p>
        <% } %>
        <div class="table-responsive">
            <table class="data-table">
                <thead><tr><th>Field</th><th>Current approved value</th><th>Proposed value</th></tr></thead>
                <tbody>
                <% foreach (var line in Detail.VisibleLines) { %>
                <tr>
                    <td><%= Server.HtmlEncode(line.FieldLabel) %></td>
                    <td><%= ProfileChangeRequestService.RenderChangeValue(line, false) %></td>
                    <td style="background:#fef3c7;"><%= ProfileChangeRequestService.RenderChangeValue(line, true) %></td>
                </tr>
                <% } %>
                </tbody>
            </table>
        </div>
        <% if (!string.IsNullOrEmpty(Detail.DecisionComments)) { %>
        <p style="margin-top:1rem;"><strong>HR comments:</strong> <%= Server.HtmlEncode(Detail.DecisionComments) %></p>
        <% } %>
        <h3 class="form-section-title" style="margin-top:1rem;">Workflow history</h3>
        <ul>
        <% foreach (var h in Detail.History) { %>
            <li><%= h.ActionOn.ToString("dd-MMM-yyyy HH:mm") %> &mdash; <%= Server.HtmlEncode(HRMS.Services.ProfileChangeStatuses.Display(h.Status)) %>
                by <%= Server.HtmlEncode(h.ActorName) %>
                <% if (!string.IsNullOrEmpty(h.Comments)) { %> &middot; <%= Server.HtmlEncode(h.Comments) %><% } %></li>
        <% } %>
        </ul>
        <% if (CanDecide) { %>
        <input type="hidden" name="requestId" value="<%= Detail.RequestID %>" />
        <div class="form-group" style="margin-top:1rem;">
            <label>Comments (required to reject)</label>
            <textarea name="comments" class="form-control" rows="3"></textarea>
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Approve';">Approve</button>
        <button type="submit" class="btn btn-secondary" onclick="if(!confirm('Reject this request? Official profile will stay unchanged.')) return false; document.getElementById('__handler').value='Reject';">Reject</button>
        <% } else if (Detail.Status == "Pending") { %>
        <p class="text-muted" style="margin-top:1rem;">You cannot decide this request (including your own submission).</p>
        <% } %>
    </div>
</div>
<% } %>
</asp:Content>
