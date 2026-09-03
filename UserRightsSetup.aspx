<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UserRightsSetup.aspx.cs" Inherits="HRMS.UserRightsSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<style>
.rights-table th{text-align:center}.rights-table td.perm-col{text-align:center}
.cat-row td{background:#f3f4f6;font-weight:700;font-size:.8rem;text-transform:uppercase}
.admin-notice{background:#fffbeb;border:1px solid #fcd34d;border-radius:6px;padding:.75rem 1rem;color:#92400e;margin-bottom:1rem}
.scope-list{max-height:220px;overflow-y:auto;border:1px solid #e5e7eb;border-radius:6px;padding:.5rem}
.scope-list label{display:block;padding:.2rem 0;font-size:.88rem}
.scope-grid{display:grid;grid-template-columns:1fr 1fr;gap:1rem}
</style>
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />

<div class="card">
  <div class="card-header"><h2>Select User</h2></div>
  <div class="card-body" style="display:flex;gap:.75rem;align-items:flex-end;flex-wrap:wrap;">
    <div class="form-group" style="margin:0;min-width:280px;">
      <label>User</label>
      <select id="ddlUser" class="form-control" onchange="if(this.value) location.href='/UserRightsSetup.aspx?userId='+this.value;">
        <option value="">-- Select User --</option>
        <% foreach (var u in Users) { %>
        <option value="<%= u.UserID %>" <%= SelectedUserId==u.UserID?"selected":"" %>><%= Server.HtmlEncode(u.FullName) %> (<%= Server.HtmlEncode(u.Username) %>)</option>
        <% } %>
      </select>
    </div>
    <a href="/UserSetup.aspx" class="btn btn-secondary">+ Add New User</a>
  </div>
</div>

<% if (SelectedUserId > 0) { %>
<% if (SelectedIsAdmin) { %>
<div class="admin-notice"><strong><%= Server.HtmlEncode(SelectedFullName) %></strong> is an Administrator with full access. Settings below do not apply to admins.</div>
<% } %>
<input type="hidden" name="userId" value="<%= SelectedUserId %>" />
<input type="hidden" name="permissionsJson" id="permissionsJson" />
<input type="hidden" name="scopeJson" id="scopeJson" />

<% if (!SelectedIsAdmin) { %>
<div class="card mt-4">
  <div class="card-header"><h2>Data Access Scope — <%= Server.HtmlEncode(SelectedFullName) %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Scope Mode</label>
        <select id="scopeMode" class="form-control">
          <option value="OwnOnly" <%= DataScope.Mode==HRMS.Services.DataScopeMode.OwnOnly?"selected":"" %>>Own record only</option>
          <option value="Restricted" <%= DataScope.Mode==HRMS.Services.DataScopeMode.Restricted?"selected":"" %>>Restricted</option>
          <option value="All" <%= DataScope.Mode==HRMS.Services.DataScopeMode.All?"selected":"" %>>All employee data</option>
        </select>
      </div>
      <div class="form-group">
        <label class="checkbox-label"><input type="checkbox" id="incUnassignedDept" <%= DataScope.IncludeUnassignedDepartment?"checked":"" %> /> Include no department</label>
        <label class="checkbox-label"><input type="checkbox" id="incUnassignedLoc" <%= DataScope.IncludeUnassignedLocation?"checked":"" %> /> Include no location</label>
      </div>
    </div>
    <div class="scope-grid mt-4" id="restrictedScopePanel">
      <div><label><strong>Allowed Departments</strong></label>
        <div class="scope-list">
        <% foreach (var d in Departments) { %>
        <label><input type="checkbox" class="scope-dept" value="<%= d.Id %>" <%= DataScope.DepartmentIds.Contains(d.Id)?"checked":"" %> /> <%= Server.HtmlEncode(d.Name) %></label>
        <% } %>
        </div>
      </div>
      <div><label><strong>Allowed Locations</strong></label>
        <div class="scope-list">
        <% foreach (var l in Locations) { %>
        <label><input type="checkbox" class="scope-loc" value="<%= l.Id %>" <%= DataScope.LocationIds.Contains(l.Id)?"checked":"" %> /> <%= Server.HtmlEncode(l.Name) %></label>
        <% } %>
        </div>
      </div>
    </div>
  </div>
</div>
<% } %>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Form Rights — <%= Server.HtmlEncode(SelectedFullName) %></h2>
    <div>
      <button type="button" class="btn btn-secondary" onclick="checkAll('read',true)">All Read</button>
      <button type="button" class="btn btn-secondary" onclick="checkAll('write',true)">All Write</button>
      <button type="button" class="btn btn-secondary" onclick="clearAll()">Clear All</button>
    </div>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table rights-table" id="rightsTable">
      <thead><tr><th style="text-align:left;">Form</th><th>View</th><th>Add/Edit</th><th>Delete</th><th>Approve</th><th>Export</th></tr></thead>
      <tbody>
      <% string lastCat = null;
         foreach (var p in Permissions) {
           if (p.Category != lastCat) { lastCat = p.Category; %>
      <tr class="cat-row"><td colspan="6"><%= Server.HtmlEncode(p.Category) %></td></tr>
      <% } %>
      <tr data-form="<%= Server.HtmlEncode(p.FormKey) %>">
        <td><%= Server.HtmlEncode(p.FormName) %></td>
        <td class="perm-col"><input type="checkbox" class="perm-read" <%= p.CanRead?"checked":"" %> /></td>
        <td class="perm-col"><input type="checkbox" class="perm-write" <%= p.CanWrite?"checked":"" %> /></td>
        <td class="perm-col"><input type="checkbox" class="perm-delete" <%= p.CanDelete?"checked":"" %> /></td>
        <td class="perm-col"><input type="checkbox" class="perm-approve" <%= p.CanApprove?"checked":"" %> /></td>
        <td class="perm-col"><input type="checkbox" class="perm-export" <%= p.CanExport?"checked":"" %> /></td>
      </tr>
      <% } %>
      </tbody>
    </table>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="return prepareRightsPayload();">Save User Rights &amp; Data Scope</button>
    <a href="/UserSetup.aspx" class="btn btn-secondary">Back to Users</a>
  </div>
</div>
<script>
function toggleRestrictedPanel(){var m=document.getElementById('scopeMode'),p=document.getElementById('restrictedScopePanel');if(m&&p)p.style.display=m.value==='Restricted'?'':'none';}
var sm=document.getElementById('scopeMode');if(sm){sm.addEventListener('change',toggleRestrictedPanel);toggleRestrictedPanel();}
function checkAll(t,v){document.querySelectorAll('.perm-'+t).forEach(function(cb){cb.checked=v;});if(t==='write'&&v)document.querySelectorAll('.perm-read').forEach(function(cb){cb.checked=true;});}
function clearAll(){document.querySelectorAll('.perm-read,.perm-write,.perm-delete,.perm-approve,.perm-export').forEach(function(cb){cb.checked=false;});}
function prepareRightsPayload(){
  var rows=[];
  document.querySelectorAll('#rightsTable tbody tr[data-form]').forEach(function(tr){
    rows.push({formKey:tr.getAttribute('data-form'),canRead:tr.querySelector('.perm-read').checked,canWrite:tr.querySelector('.perm-write').checked,canDelete:tr.querySelector('.perm-delete').checked,canApprove:tr.querySelector('.perm-approve').checked,canExport:tr.querySelector('.perm-export').checked});
  });
  document.getElementById('permissionsJson').value=JSON.stringify(rows);
  var scopeEl=document.getElementById('scopeMode');
  if(scopeEl){
    var deptIds=[],locIds=[];
    document.querySelectorAll('.scope-dept:checked').forEach(function(cb){deptIds.push(parseInt(cb.value,10));});
    document.querySelectorAll('.scope-loc:checked').forEach(function(cb){locIds.push(parseInt(cb.value,10));});
    document.getElementById('scopeJson').value=JSON.stringify({mode:scopeEl.value,departmentIds:deptIds,locationIds:locIds,includeUnassignedDepartment:!!(document.getElementById('incUnassignedDept')&&document.getElementById('incUnassignedDept').checked),includeUnassignedLocation:!!(document.getElementById('incUnassignedLoc')&&document.getElementById('incUnassignedLoc').checked)});
  }
  document.getElementById('__handler').value='Save';
  return true;
}
</script>
<% } %>
</asp:Content>
