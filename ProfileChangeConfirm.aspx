<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProfileChangeConfirm.aspx.cs" Inherits="HRMS.ProfileChangeConfirmPage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="HRMS.Services" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<input type="hidden" name="__handler" id="__handler" value="" />
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<% if (Draft != null) { %>
<div class="card">
    <div class="card-header"><h2>Review changes before sending to HR</h2></div>
    <div class="card-body">
        <div class="alert alert-info">These values will <strong>not</strong> replace your official profile until an HR Manager approves the request. Internet or email is not used; HR is notified inside this application.</div>
        <div class="table-responsive">
            <table class="data-table">
                <thead><tr><th>Field</th><th>Current approved value</th><th>Proposed value</th></tr></thead>
                <tbody>
                <% foreach (var line in Draft.Lines.Where(l => !l.IsSnapshot)) { %>
                <tr>
                    <td><%= Server.HtmlEncode(line.FieldLabel) %></td>
                    <td><%= ProfileChangeRequestService.RenderChangeValue(line, false) %></td>
                    <td style="background:#fef3c7;"><%= ProfileChangeRequestService.RenderChangeValue(line, true) %></td>
                </tr>
                <% } %>
                </tbody>
            </table>
        </div>
    </div>
    <div class="card-footer" style="display:flex;gap:.75rem;">
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Submit';">Submit to HR</button>
        <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='Cancel';">Cancel</button>
    </div>
</div>
<% } %>
</asp:Content>
