<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PositionHierarchy.aspx.cs" Inherits="HRMS.PositionHierarchyPage" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
    .stat-chips { display:flex; gap:.5rem; margin-bottom:.75rem; flex-wrap:wrap; }
    .stat-chip {
        display:inline-flex; align-items:center; padding:4px 12px;
        border-radius:20px; font-size:.78rem; font-weight:600;
    }
    .chip-total   { background:rgba(46,49,146,.12); color:var(--gb-blue); }
    .chip-active  { background:#dcfce7; color:#166534; }
    .chip-depth   { background:#fef3c7; color:#b45309; }
    .hierarchy-toolbar {
        display:flex; align-items:center; justify-content:space-between;
        flex-wrap:wrap; gap:.75rem; margin-bottom:1rem;
    }
    .filter-toggle {
        display:inline-flex; align-items:center; gap:.5rem;
        padding:.45rem .85rem; border-radius:8px; border:1px solid var(--border);
        background:#fff; font-size:.82rem; font-weight:600; text-decoration:none; color:var(--text-dark);
    }
    .filter-toggle.active { background:var(--gb-blue); color:#fff; border-color:var(--gb-blue); }
    .tree-section { font-size:.85rem; }
    .tree-node {
        display:flex; align-items:flex-start; gap:.75rem;
        padding:.55rem .75rem; margin-bottom:.35rem;
        border:1px solid var(--border); border-radius:8px; background:#fff;
    }
    .tree-node:hover { box-shadow:var(--shadow); }
    .tree-node-main { flex:1; min-width:0; }
    .tree-node-title { font-weight:700; color:var(--gb-blue); }
    .tree-node-meta { font-size:.78rem; color:var(--text-muted); margin-top:.15rem; }
    .tree-node-workers { font-size:.78rem; color:#475569; margin-top:.25rem; }
    .tree-connector { color:var(--text-muted); font-size:.75rem; margin-right:.25rem; }
    .orphan-banner {
        background:rgba(227,30,36,.08); border:1px solid rgba(227,30,36,.25);
        border-radius:8px; padding:.65rem .85rem; margin-bottom:1rem; font-size:.82rem;
    }
    .section-tabs { display:flex; gap:.35rem; margin-bottom:1rem; flex-wrap:wrap; }
    .section-tab {
        padding:.4rem .9rem; border-radius:8px; border:1px solid var(--border);
        background:#fff; font-weight:600; font-size:.82rem; cursor:pointer;
    }
    .section-tab.active { background:var(--gb-blue); color:#fff; border-color:var(--gb-blue); }
    .view-panel { display:none; }
    .view-panel.active { display:block; }
    .search-box { max-width:320px; margin-bottom:.75rem; }
    #flatTable { font-size:.82rem; }
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">

<div class="hierarchy-toolbar">
  <a href="/PositionMaster.aspx" class="btn btn-secondary">← Back to Position Master</a>
  <div style="display:flex; gap:.5rem; flex-wrap:wrap;">
    <% if (ActiveOnly) { %>
      <a href="/PositionHierarchy.aspx?activeOnly=false" class="filter-toggle">Show All Positions</a>
    <% } else { %>
      <a href="/PositionHierarchy.aspx?activeOnly=true" class="filter-toggle active">Active Only</a>
    <% } %>
  </div>
</div>

<div class="stat-chips">
  <span class="stat-chip chip-total">Total Positions: <%= TotalPositions %></span>
  <span class="stat-chip chip-active">Root Nodes: <%= RootCount %></span>
  <span class="stat-chip chip-depth">Max Depth: <%= MaxDepth %></span>
</div>

<div class="section-tabs">
  <button type="button" class="section-tab active" data-view="tree" onclick="showHierarchyView('tree')">Tree View</button>
  <button type="button" class="section-tab" data-view="flat" onclick="showHierarchyView('flat')">Flat List</button>
</div>

<div id="panel-tree" class="view-panel active">
  <div class="card">
    <div class="card-header space-between">
      <h2>Reporting Hierarchy</h2>
      <span style="font-size:.78rem; color:var(--text-muted);">
        <%= ActiveOnly ? "Active positions only" : "All positions" %>
      </span>
    </div>
    <div class="card-body tree-section">
      <% if (TreeRows.Count == 0 && OrphanNodes.Count == 0) { %>
        <p class="empty-row">No positions found.</p>
      <% } else {
           foreach (var row in TreeRows) {
             var d = row.Data;
             var indent = row.Depth * 24; %>
        <div class="tree-node" style="margin-left:<%= indent %>px">
          <% if (row.Depth > 0) { %><span class="tree-connector">└</span><% } %>
          <div class="tree-node-main">
            <div class="tree-node-title">
              <%= Server.HtmlEncode(d.PositionNo) %>
              <% if (!string.IsNullOrEmpty(d.Description)) { %>
              <span style="font-weight:500; color:var(--text-dark);"> — <%= Server.HtmlEncode(d.Description) %></span>
              <% } %>
            </div>
            <div class="tree-node-meta">
              <% if (!string.IsNullOrEmpty(d.JobTitle)) { %><span><%= Server.HtmlEncode(d.JobTitle) %></span><% } %>
              <% if (!string.IsNullOrEmpty(d.DepartmentName)) { %><span> · <%= Server.HtmlEncode(d.DepartmentName) %></span><% } %>
            </div>
            <% if (!string.IsNullOrEmpty(d.AssignedWorkers)) { %>
            <div class="tree-node-workers">Workers: <%= Server.HtmlEncode(d.AssignedWorkers) %></div>
            <% } %>
          </div>
          <span class="badge <%= d.IsActive ? "badge-success" : "badge-danger" %>">
            <%= d.IsActive ? "Active" : "Inactive" %>
          </span>
          <a class="btn-icon btn-edit" href="/PositionMaster.aspx?editId=<%= d.PositionID %>">Edit</a>
        </div>
      <% } } %>
    </div>
  </div>

  <% if (OrphanNodes.Count > 0) { %>
  <div class="card mt-4">
    <div class="card-header"><h2>Orphan Positions</h2></div>
    <div class="card-body">
      <div class="orphan-banner">
        <%= OrphanNodes.Count %> position(s) could not be placed in the tree (missing or circular reporting reference).
      </div>
      <div class="tree-section">
        <% foreach (var d in OrphanNodes) { %>
        <div class="tree-node">
          <div class="tree-node-main">
            <div class="tree-node-title">
              <%= Server.HtmlEncode(d.PositionNo) %>
              <% if (!string.IsNullOrEmpty(d.Description)) { %>
              <span style="font-weight:500; color:var(--text-dark);"> — <%= Server.HtmlEncode(d.Description) %></span>
              <% } %>
            </div>
            <div class="tree-node-meta">
              <% if (!string.IsNullOrEmpty(d.JobTitle)) { %><span><%= Server.HtmlEncode(d.JobTitle) %></span><% } %>
              <% if (!string.IsNullOrEmpty(d.DepartmentName)) { %><span> · <%= Server.HtmlEncode(d.DepartmentName) %></span><% } %>
              <% if (!string.IsNullOrEmpty(d.ReportsToPositionNo)) { %>
              <span> · Reports to: <%= Server.HtmlEncode(d.ReportsToPositionNo) %> (missing)</span>
              <% } %>
            </div>
            <% if (!string.IsNullOrEmpty(d.AssignedWorkers)) { %>
            <div class="tree-node-workers">Workers: <%= Server.HtmlEncode(d.AssignedWorkers) %></div>
            <% } %>
          </div>
          <span class="badge <%= d.IsActive ? "badge-success" : "badge-danger" %>">
            <%= d.IsActive ? "Active" : "Inactive" %>
          </span>
          <a class="btn-icon btn-edit" href="/PositionMaster.aspx?editId=<%= d.PositionID %>">Edit</a>
        </div>
        <% } %>
      </div>
    </div>
  </div>
  <% } %>
</div>

<div id="panel-flat" class="view-panel">
  <div class="card">
    <div class="card-header space-between">
      <h2>All Positions</h2>
      <span style="font-size:.78rem; color:var(--text-muted);"><%= AllPositions.Count %> record(s)</span>
    </div>
    <div class="card-body table-responsive">
      <input type="text" id="positionSearch" class="form-control search-box"
             placeholder="Search position no, job, department, workers…" oninput="filterPositionTable()" />
      <table class="data-table" id="flatTable">
        <thead class="grid-header">
          <tr>
            <th>Position No</th>
            <th>Job Title</th>
            <th>Department</th>
            <th>Reports To</th>
            <th>Assigned Workers</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
        <% if (AllPositions.Count == 0) { %>
          <tr class="empty-row"><td colspan="7">No positions found.</td></tr>
        <% } else {
             foreach (var p in AllPositions) { %>
          <tr>
            <td><strong><%= Server.HtmlEncode(p.PositionNo) %></strong></td>
            <td><%= Server.HtmlEncode(p.JobTitle) %></td>
            <td><%= Server.HtmlEncode(p.DepartmentName) %></td>
            <td><%= string.IsNullOrEmpty(p.ReportsToPositionNo) ? "—" : Server.HtmlEncode(p.ReportsToPositionNo) %></td>
            <td><%= string.IsNullOrEmpty(p.AssignedWorkers) ? "—" : Server.HtmlEncode(p.AssignedWorkers) %></td>
            <td>
              <span class="badge <%= p.IsActive ? "badge-success" : "badge-danger" %>">
                <%= p.IsActive ? "Active" : "Inactive" %>
              </span>
            </td>
            <td class="actions-col">
              <a class="btn-icon btn-edit" href="/PositionMaster.aspx?editId=<%= p.PositionID %>">Edit</a>
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
function showHierarchyView(view) {
    document.querySelectorAll('.section-tab').forEach(function (t) {
        t.classList.toggle('active', t.getAttribute('data-view') === view);
    });
    document.querySelectorAll('.view-panel').forEach(function (p) {
        p.classList.toggle('active', p.id === 'panel-' + view);
    });
}

function filterPositionTable() {
    var q = (document.getElementById('positionSearch').value || '').toLowerCase();
    document.querySelectorAll('#flatTable tbody tr').forEach(function (row) {
        if (row.classList.contains('empty-row')) return;
        row.style.display = row.textContent.toLowerCase().indexOf(q) >= 0 ? '' : 'none';
    });
}
</script>
</asp:Content>
