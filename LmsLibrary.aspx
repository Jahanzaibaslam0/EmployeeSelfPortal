<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.LmsLibraryPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>

<p style="margin-bottom:.75rem;">
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/Home.aspx") %>">Back to Home</a>
  <% if (CanManage) { %>
  <a class="btn btn-primary" href="<%= ResolveUrl("~/LmsDocumentSetup.aspx") %>">Manage LMS Documents</a>
  <% } %>
</p>

<div class="card">
  <div class="card-header space-between">
    <h2><%= PageTitle %></h2>
    <% if (CanManage) { %><span class="badge badge-warning">Admin / Setup access</span><% } %>
  </div>
  <div class="card-body">
    <p class="text-muted" style="margin-top:0;"><%= Server.HtmlEncode(ScopeNote) %></p>

    <div class="doc-toolbar" style="margin-bottom:1rem;">
      <div class="search-box">
        <input type="text" id="lmsQ" class="form-control" placeholder="Search manuals, SOPs, policies..." value="<%= Server.HtmlEncode(SearchTerm) %>" />
        <select id="lmsCategory" class="form-control">
          <option value="">All Categories</option>
          <% foreach (var c in HRMS.Services.LmsCategories.All) { %>
          <option value="<%= c %>" <%= SelectedCategory==c?"selected":"" %>><%= HRMS.Services.LmsCategories.DisplayName(c) %></option>
          <% } %>
        </select>
        <button type="button" class="btn btn-primary" id="lmsFilterBtn">Filter</button>
      </div>
      <span class="doc-count"><%= Documents.Count %> document(s)</span>
    </div>

    <% if (Documents.Count == 0) { %>
    <div class="empty-state">
      <span class="icon">&#128218;</span>
      <p>No authorized documents found.</p>
    </div>
    <% } else { %>
    <div style="overflow-x:auto;">
      <table class="doc-table" id="lmsTable">
        <thead>
          <tr>
            <th>#</th>
            <th>Title</th>
            <th>Category</th>
            <th>Department</th>
            <th>Version</th>
            <th>Effective</th>
            <th>File</th>
          </tr>
        </thead>
        <tbody>
          <% for (var i = 0; i < Documents.Count; i++) {
               var doc = Documents[i];
               var href = FileHref(doc);
          %>
          <tr>
            <td><%= i + 1 %></td>
            <td>
              <strong><%= Server.HtmlEncode(doc.Title) %></strong>
              <% if (!string.IsNullOrWhiteSpace(doc.Description)) { %>
              <div class="text-muted" style="font-size:.8rem;"><%= Server.HtmlEncode(doc.Description) %></div>
              <% } %>
            </td>
            <td><%= Server.HtmlEncode(doc.CategoryDisplay) %></td>
            <td><%= Server.HtmlEncode(doc.DepartmentName) %></td>
            <td><%= Server.HtmlEncode(doc.VersionLabel) %></td>
            <td><%= Server.HtmlEncode(doc.EffectiveDate) %></td>
            <td>
              <% if (!string.IsNullOrEmpty(href)) { %>
              <a class="doc-link" href="<%= Server.HtmlEncode(href) %>" target="_blank" rel="noopener noreferrer"
                 title="<%= Server.HtmlEncode(doc.OriginalFileName) %>">
                <span class="icon">&#128206;</span> Open
              </a>
              <% } else { %>—<% } %>
            </td>
          </tr>
          <% } %>
        </tbody>
      </table>
    </div>
    <% } %>
  </div>
</div>
<script>
(function () {
  var btn = document.getElementById('lmsFilterBtn');
  if (!btn) return;
  btn.addEventListener('click', function () {
    var q = (document.getElementById('lmsQ') || {}).value || '';
    var cat = (document.getElementById('lmsCategory') || {}).value || '';
    var url = '<%= ResolveUrl("~/LmsLibrary.aspx") %>?q=' + encodeURIComponent(q) + '&category=' + encodeURIComponent(cat);
    window.location.href = url;
  });
})();
</script>
</asp:Content>
