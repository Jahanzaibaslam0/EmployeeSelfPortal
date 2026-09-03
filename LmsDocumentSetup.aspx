<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.LmsDocumentSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<p style="margin-bottom:.75rem;">
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/LmsLibrary.aspx") %>">Open Knowledge Library</a>
  <a class="btn btn-secondary" href="<%= ResolveUrl("~/Home.aspx") %>">Back to Home</a>
</p>

<div class="card">
  <div class="card-header"><h2><%= Input.DocumentID > 0 ? "Edit LMS Document" : "Add LMS Document" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="documentID" value="<%= Input.DocumentID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Title *</label>
        <input type="text" name="title" class="form-control" maxlength="200" value="<%= Server.HtmlEncode(Input.Title) %>" /></div>
      <div class="form-group"><label>Category *</label>
        <select name="category" class="form-control">
          <% foreach (var c in HRMS.Services.LmsCategories.All) { %>
          <option value="<%= c %>" <%= Input.Category==c?"selected":"" %>><%= HRMS.Services.LmsCategories.DisplayName(c) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Access Scope *</label>
        <select name="accessScope" class="form-control" id="accessScope">
          <% foreach (var s in HRMS.Services.LmsAccessScopes.All) { %>
          <option value="<%= s %>" <%= Input.AccessScope==s?"selected":"" %>><%= HRMS.Services.LmsAccessScopes.DisplayName(s) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Primary Department</label>
        <select name="departmentID" class="form-control">
          <option value="0">— None —</option>
          <% foreach (var d in Departments) { %>
          <option value="<%= d.Id %>" <%= Input.DepartmentID==d.Id?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Primary Job / Role</label>
        <select name="jobID" class="form-control">
          <option value="0">— None —</option>
          <% foreach (var j in Jobs) { %>
          <option value="<%= j.Id %>" <%= Input.JobID==j.Id?"selected":"" %>><%= Server.HtmlEncode(j.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Version</label>
        <input type="text" name="versionLabel" class="form-control" maxlength="50" value="<%= Server.HtmlEncode(Input.VersionLabel) %>" /></div>
      <div class="form-group"><label>Effective Date</label>
        <input type="date" name="effectiveDate" class="form-control" value="<%= Server.HtmlEncode(Input.EffectiveDate) %>" /></div>
      <div class="form-group"><label>Expiry Date</label>
        <input type="date" name="expiryDate" class="form-control" value="<%= Server.HtmlEncode(Input.ExpiryDate) %>" /></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Description</label>
        <textarea name="description" class="form-control" rows="3"><%= Server.HtmlEncode(Input.Description) %></textarea></div>
      <div class="form-group"><label>Document File</label>
        <input type="file" name="documentFile" class="form-control" />
        <% if (!string.IsNullOrWhiteSpace(Input.DocumentPath)) { %>
        <small>Current: <a href="<%= Server.HtmlEncode(Input.DocumentPath) %>" target="_blank" rel="noopener noreferrer"><%= Server.HtmlEncode(Input.OriginalFileName) %></a></small>
        <% } %>
      </div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>

    <h3 style="margin:1.25rem 0 .5rem;font-size:1rem;">Additional Access Grants</h3>
    <p class="text-muted" style="margin-top:0;">Use for Restricted documents, or to grant extra access beyond the primary scope.</p>
    <div class="form-grid">
      <div class="form-group"><label>Employees</label>
        <select name="grantEmployeeIDs" class="form-control" multiple size="6">
          <% foreach (var e in Employees) { %>
          <option value="<%= e.Id %>" <%= IsGrantSelected("Employee", e.Id)?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Departments</label>
        <select name="grantDepartmentIDs" class="form-control" multiple size="6">
          <% foreach (var d in Departments) { %>
          <option value="<%= d.Id %>" <%= IsGrantSelected("Department", d.Id)?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Jobs / Roles</label>
        <select name="grantJobIDs" class="form-control" multiple size="6">
          <% foreach (var j in Jobs) { %>
          <option value="<%= j.Id %>" <%= IsGrantSelected("Job", j.Id)?"selected":"" %>><%= Server.HtmlEncode(j.Name) %></option>
          <% } %>
        </select></div>
    </div>

    <div class="form-actions" style="margin-top:1rem;">
      <button type="submit" class="btn btn-primary"><%= Input.DocumentID > 0 ? "Update Document" : "Save Document" %></button>
      <% if (Input.DocumentID > 0) { %><a class="btn btn-secondary" href="<%= ResolveUrl("~/LmsDocumentSetup.aspx") %>">Cancel</a><% } %>
    </div>
  </div>
</div>

<div class="card" style="margin-top:1rem;">
  <div class="card-header"><h2>All LMS Documents</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead>
        <tr>
          <th>Title</th><th>Category</th><th>Scope</th><th>Department</th><th>Job</th><th>Status</th><th>File</th><th></th>
        </tr>
      </thead>
      <tbody>
        <% foreach (var r in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(r.Title) %></td>
          <td><%= Server.HtmlEncode(r.CategoryDisplay) %></td>
          <td><%= Server.HtmlEncode(r.AccessScope) %></td>
          <td><%= Server.HtmlEncode(r.DepartmentName) %></td>
          <td><%= Server.HtmlEncode(r.JobTitle) %></td>
          <td><%= r.IsActive ? "Active" : "Inactive" %></td>
          <td><% if (r.HasFile) { %><a href="<%= Server.HtmlEncode(r.DocumentPath) %>" target="_blank" rel="noopener noreferrer">View</a><% } %></td>
          <td style="white-space:nowrap;">
            <a class="btn btn-secondary" style="padding:2px 8px;font-size:.75rem;" href="<%= ResolveUrl("~/LmsDocumentSetup.aspx?editId=" + r.DocumentID) %>">Edit</a>
            <% if (r.IsActive) { %>
            <button type="button" class="btn btn-secondary" style="padding:2px 8px;font-size:.75rem;"
                    onclick="document.getElementById('deleteId').value='<%= r.DocumentID %>';document.getElementById('__handler').value='Delete';if(confirm('Deactivate this document?'))this.form.submit();">Deactivate</button>
            <% } %>
          </td>
        </tr>
        <% } %>
        <% if (Records.Count == 0) { %><tr><td colspan="8">No LMS documents yet.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
