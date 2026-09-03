<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.LanguageSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="languageID" value="<%= Input.LanguageID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.LanguageID > 0 ? "Edit Language" : "Add Language" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Code <span class="required">*</span></label>
        <input type="text" name="languageCode" class="form-control"
               value="<%= Server.HtmlEncode(Input.LanguageCode) %>" maxlength="10"
               placeholder="e.g. en, ur" />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="languageName" class="form-control"
               value="<%= Server.HtmlEncode(Input.LanguageName) %>" maxlength="100"
               placeholder="e.g. English" />
      </div>
      <div class="form-group">
        <label>Native Name</label>
        <input type="text" name="nativeName" class="form-control"
               value="<%= Server.HtmlEncode(Input.NativeName) %>" maxlength="100"
               placeholder="e.g. English, اردو" />
      </div>
      <div class="form-group">
        <label>Region</label>
        <input type="text" name="region" class="form-control"
               value="<%= Server.HtmlEncode(Input.Region) %>" maxlength="100"
               placeholder="e.g. Pakistan, Global" />
      </div>
      <div class="form-group">
        <label>Source</label>
        <input type="text" name="source" class="form-control"
               value="<%= Server.HtmlEncode(Input.Source) %>" maxlength="100"
               placeholder="e.g. ISO 639-1" />
      </div>
      <div class="form-group">
        <label>Priority</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isPriority" value="true" <%= Input.IsPriority ? "checked" : "" %> /> Priority Language
        </label>
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.LanguageID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/LanguageSetup.aspx") %>" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Language List</h2>
    <div style="display:flex;gap:.5rem;align-items:center;">
      <a href="<%= ResolveUrl("~/LanguageSetup.aspx") %>?handler=DownloadExcel" class="btn btn-secondary">Download Excel</a>
      <input type="text" id="txtSearch" class="form-control" style="width:220px"
             placeholder="Search…" onkeyup="searchTable(this.value)" />
    </div>
  </div>
  <div class="card-body">
    <div style="display:flex;gap:.75rem;align-items:end;margin-bottom:1.25rem;flex-wrap:wrap;">
      <div class="form-group" style="margin:0;">
        <label>Upload Excel</label>
        <input type="file" name="languageFile" class="form-control" accept=".xlsx,.xls" />
      </div>
      <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='UploadExcel';">Upload</button>
    </div>

    <div class="table-responsive">
      <table class="data-table" id="dataTable">
        <thead class="grid-header">
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Native Name</th>
            <th>Region</th>
            <th>Source</th>
            <th>Priority</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <% if (Languages.Count == 0) { %>
          <tr class="empty-row"><td colspan="8">No records found.</td></tr>
          <% } else {
               foreach (var item in Languages) { %>
          <tr>
            <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(item.LanguageCode) %></code></td>
            <td><%= Server.HtmlEncode(item.LanguageName) %></td>
            <td><%= Server.HtmlEncode(item.NativeName) %></td>
            <td><%= Server.HtmlEncode(item.Region) %></td>
            <td><%= Server.HtmlEncode(item.Source) %></td>
            <td><span class="badge <%= item.IsPriority ? "badge-success" : "badge-danger" %>"><%= item.IsPriority ? "Yes" : "No" %></span></td>
            <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
            <td class="actions-col">
              <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/LanguageSetup.aspx") %>?editId=<%= item.LanguageID %>">Edit</a>
              <button type="submit" class="btn-icon btn-delete"
                      onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.LanguageID %>';return confirm('Remove this language?');">X</button>
            </td>
          </tr>
          <% } } %>
        </tbody>
      </table>
    </div>
  </div>
</div>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
function searchTable(q) {
  q = (q || '').toLowerCase();
  document.querySelectorAll('#dataTable tbody tr').forEach(function (r) {
    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
  });
}
</script>
</asp:Content>
