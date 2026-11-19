<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactChangeReview.aspx.cs" Inherits="HRMS.ContactChangeReviewPage" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<input type="hidden" name="__handler" id="__handler" value="" />
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>

<div class="card">
    <div class="card-header"><h2>Contact approvals in process (<%= Pending.Count %>)</h2></div>
    <div class="card-body table-responsive">
        <table class="data-table">
            <thead><tr><th>Request</th><th>Contact</th><th>Record</th><th>Action</th><th>Submitted</th><th>By</th><th></th></tr></thead>
            <tbody>
            <% if (Pending.Count == 0) { %>
            <tr><td colspan="7" class="empty-cell">No contact or address requests are waiting for approval.</td></tr>
            <% } foreach (var r in Pending) { %>
            <tr>
                <td><%= Server.HtmlEncode(r.RequestCode) %></td>
                <td><%= Server.HtmlEncode(r.ContactCode) %> <%= Server.HtmlEncode(r.ContactName) %></td>
                <td><%= Server.HtmlEncode(r.EntityDisplay) %></td>
                <td><%= Server.HtmlEncode(r.ActionType) %></td>
                <td><%= r.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %></td>
                <td><%= Server.HtmlEncode(r.SubmittedByName) %></td>
                <td><a href="/ContactChangeReview.aspx?id=<%= r.RequestID %>">Review</a></td>
            </tr>
            <% } %>
            </tbody>
        </table>
    </div>
</div>

<div class="card mt-4">
    <div class="card-header"><h2>Completed contact and address requests (<%= Completed.Count %>)</h2></div>
    <div class="card-body table-responsive">
        <table class="data-table">
            <thead><tr><th>Request</th><th>Contact</th><th>Record</th><th>Action</th><th>Status</th><th>Decision</th><th>By</th><th></th></tr></thead>
            <tbody>
            <% if (Completed.Count == 0) { %>
            <tr><td colspan="8" class="empty-cell">No approved or rejected requests yet.</td></tr>
            <% } foreach (var r in Completed) { %>
            <tr>
                <td><%= Server.HtmlEncode(r.RequestCode) %></td>
                <td><%= Server.HtmlEncode(r.ContactCode) %> <%= Server.HtmlEncode(r.ContactName) %></td>
                <td><%= Server.HtmlEncode(r.EntityDisplay) %></td>
                <td><%= Server.HtmlEncode(r.ActionType) %></td>
                <td><span class="<%= r.BadgeClass %>"><%= Server.HtmlEncode(r.StatusDisplay) %></span></td>
                <td><%= r.DecidedOn.HasValue ? r.DecidedOn.Value.ToString("dd-MMM-yyyy HH:mm") : "\u2014" %></td>
                <td><%= Server.HtmlEncode(r.DecidedByName) %></td>
                <td><a href="/ContactChangeReview.aspx?id=<%= r.RequestID %>">View history</a></td>
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
        <p><strong>Contact:</strong> <%= Server.HtmlEncode(Detail.ContactCode) %> <%= Server.HtmlEncode(Detail.ContactName) %>
           &middot; <a href="/ContactMaster.aspx?editId=<%= Detail.ContactID %>">Open contact</a></p>
        <p><strong>Record:</strong> <%= Server.HtmlEncode(Detail.EntityDisplay) %><% if (Detail.AddressID > 0) { %> #<%= Detail.AddressID %><% } %>
           &middot; <strong>Action:</strong> <%= Server.HtmlEncode(Detail.ActionType) %>
           &middot; <strong>Submitted:</strong> <%= Detail.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %> by <%= Server.HtmlEncode(Detail.SubmittedByName) %></p>
        <% if (Detail.DecidedOn.HasValue) { %>
        <p><strong>Decision:</strong> <%= Detail.DecidedOn.Value.ToString("dd-MMM-yyyy HH:mm") %> by <%= Server.HtmlEncode(Detail.DecidedByName) %></p>
        <% } %>
        <div class="table-responsive">
            <table class="data-table">
                <thead><tr><th>Field</th><th>Previous value</th><th>Proposed value</th></tr></thead>
                <tbody>
                <% foreach (var line in Detail.Lines) { %>
                <tr>
                    <td><%= Server.HtmlEncode(line.FieldLabel) %></td>
                    <td><%= Server.HtmlEncode(line.OldValue) %></td>
                    <td style="background:#fef3c7;"><%= Server.HtmlEncode(line.NewValue) %></td>
                </tr>
                <% } %>
                <% if (Detail.Lines.Count == 0) { %><tr><td colspan="3">No field differences captured.</td></tr><% } %>
                </tbody>
            </table>
        </div>
        <% if (!string.IsNullOrEmpty(Detail.DecisionComments)) { %>
        <p style="margin-top:1rem;"><strong>Remarks:</strong> <%= Server.HtmlEncode(Detail.DecisionComments) %></p>
        <% } %>
        <h3 class="form-section-title" style="margin-top:1rem;">Workflow history</h3>
        <ul>
        <% foreach (var h in Detail.History) { %>
            <li><%= h.ActionOn.ToString("dd-MMM-yyyy HH:mm") %> &mdash; <%= Server.HtmlEncode(h.StatusDisplay) %>
                by <%= Server.HtmlEncode(h.ActorName) %>
                <% if (!string.IsNullOrEmpty(h.Comments)) { %> &middot; <%= Server.HtmlEncode(h.Comments) %><% } %></li>
        <% } %>
        </ul>
        <% if (CanDecide) { %>
        <input type="hidden" name="requestId" value="<%= Detail.RequestID %>" />
        <div class="form-group" style="margin-top:1rem;">
            <label>Remarks (required to reject)</label>
            <textarea name="comments" class="form-control" rows="3"></textarea>
        </div>
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Approve';">Approve</button>
        <button type="submit" class="btn btn-secondary" onclick="if(!confirm('Reject this request? Proposed values will not be applied and the record will not be marked Verified.')) return false; document.getElementById('__handler').value='Reject';">Reject</button>
        <% } else if (Detail.Status == ContactWorkflowStatuses.InProcess) { %>
        <p class="text-muted" style="margin-top:1rem;">You cannot decide this request (including your own submission, unless you are an administrator).</p>
        <% } %>
    </div>
</div>
<% } %>
</asp:Content>
