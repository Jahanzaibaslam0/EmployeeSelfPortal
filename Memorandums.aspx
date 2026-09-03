<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.MemorandumsPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<div class="card">
  <div class="card-header space-between">
    <h2>Memorandums</h2>
    <a href="/MemorandumSetup.aspx" class="btn btn-secondary">Memorandum Setup</a>
  </div>
  <div class="card-body" style="display:flex;gap:1.5rem;align-items:flex-start;">
    <div style="min-width:280px;max-width:320px;">
      <% if (ActiveMemorandums.Count == 0) { %>
        <p class="text-muted">No active memorandums.</p>
      <% } else {
           foreach (var m in ActiveMemorandums) {
             var isSelected = Selected != null && Selected.MemorandumID == m.MemorandumID; %>
        <a href="/Memorandums.aspx?id=<%= m.MemorandumID %>"
           class="setup-link<%= isSelected ? " active" : "" %>"
           style="display:block;margin-bottom:.5rem;<%= isSelected ? "background:#e8f0fe;font-weight:600;" : "" %>">
          <%= Server.HtmlEncode(m.Name) %>
          <span style="display:block;font-size:.75rem;color:var(--text-muted);font-weight:400;margin-top:.15rem;">
            <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(m.DepartmentName) ? "All Departments" : m.DepartmentName) %>
            · <%= m.StartDate.ToString("dd-MMM-yyyy") %> – <%= m.ValidTillDate.ToString("dd-MMM-yyyy") %>
          </span>
        </a>
      <% } } %>
    </div>
    <div style="flex:1;min-width:0;">
      <% if (Selected != null) { %>
        <h3 style="margin-top:0;"><%= Server.HtmlEncode(Selected.Name) %></h3>
        <p style="color:var(--text-muted);font-size:.9rem;margin:.25rem 0 1rem;">
          <%= Server.HtmlEncode(string.IsNullOrWhiteSpace(Selected.DepartmentName) ? "All Departments" : Selected.DepartmentName) %>
          · <%= Selected.StartDate.ToString("dd-MMM-yyyy") %> – <%= Selected.ValidTillDate.ToString("dd-MMM-yyyy") %>
        </p>
        <div style="white-space:pre-wrap;"><%= Server.HtmlEncode(Selected.Description) %></div>
        <% if (Selected.HasDocument) { %>
        <p style="margin-top:1rem;">
          <a href="<%= Server.HtmlEncode(Selected.DocumentPath) %>" target="_blank" rel="noopener noreferrer" class="btn btn-secondary">
            <%= string.IsNullOrWhiteSpace(Selected.OriginalFileName) ? "View Attached Document" : Server.HtmlEncode(Selected.OriginalFileName) %>
          </a>
        </p>
        <% } %>
      <% } else { %>
        <p>Select a memorandum.</p>
      <% } %>
    </div>
  </div>
</div>
</asp:Content>
