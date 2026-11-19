<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.ImageGallerySetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="galleryImageID" value="<%= Input.GalleryImageID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.GalleryImageID > 0 ? "Edit Gallery Image" : "Add Gallery Image" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Title <span class="required">*</span></label>
        <input type="text" name="title" class="form-control" value="<%= Server.HtmlEncode(Input.Title) %>" maxlength="150" />
      </div>
      <div class="form-group">
        <label>Sort Order</label>
        <input type="number" name="sortOrder" class="form-control" value="<%= Input.SortOrder %>" min="0" />
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> />
          Active
        </label>
      </div>
      <div class="form-group full-width">
        <label>Description / Caption</label>
        <textarea name="description" class="form-control" rows="3" maxlength="500"><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
      <div class="form-group full-width">
        <label>Image <%= Input.GalleryImageID == 0 ? "(required)" : "(leave empty to keep current)" %></label>
        <input type="file" name="imageFile" class="form-control" accept=".png,.jpg,.jpeg,.webp,.gif" />
        <small style="color:var(--text-muted);">PNG, JPG, JPEG, WEBP, or GIF &mdash; shown on the home page gallery</small>
        <% if (Input.GalleryImageID > 0 && !string.IsNullOrWhiteSpace(Input.ImagePath)) { %>
        <div style="margin-top:.75rem;">
          <img src="<%= Input.PreviewUrl %>" alt="<%= Server.HtmlEncode(Input.Title) %>"
               style="max-height:140px;max-width:100%;border-radius:8px;border:1px solid var(--border);background:#111;" />
        </div>
        <% } %>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.GalleryImageID > 0 ? "Update Image" : "Save Image" %></button>
    <a href="<%= ResolveUrl("~/ImageGallerySetup.aspx") %>" class="btn btn-secondary">Clear</a>
    <a href="<%= ResolveUrl("~/Home.aspx") %>" class="btn btn-secondary">View Home Page</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header"><h2>Gallery Images</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead class="grid-header">
        <tr>
          <th>Preview</th>
          <th>Title</th>
          <th>Sort</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="5">No gallery images found. Add images to display them on the home page.</td></tr>
        <% } else { foreach (var item in Records) { %>
        <tr>
          <td>
            <% if (item.GalleryImageID > 0) { %>
            <img src="<%= item.PreviewUrl %>" alt="<%= Server.HtmlEncode(item.Title) %>"
                 style="height:48px;width:80px;object-fit:cover;border-radius:4px;background:#111;" />
            <% } %>
          </td>
          <td><%= Server.HtmlEncode(item.Title) %></td>
          <td><%= item.SortOrder %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/ImageGallerySetup.aspx") %>?editId=<%= item.GalleryImageID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.GalleryImageID %>';return confirm('Deactivate this gallery image?');">X</button>
          </td>
        </tr>
        <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
