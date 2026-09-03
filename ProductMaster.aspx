<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProductMaster.aspx.cs" Inherits="HRMS.ProductMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Product List</h2>
    <a href="/ProductMaster.aspx?newProduct=1" class="btn btn-primary">+ New Product</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Code</th><th>Name</th><th>Item ID</th><th>Inventory Type</th><th>Product Group</th><th>Brand</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var p in Products) { %>
      <tr>
        <td><%= Server.HtmlEncode(p.ProductCode) %></td>
        <td><%= Server.HtmlEncode(p.ProductName) %></td>
        <td><%= Server.HtmlEncode(p.ItemID) %></td>
        <td><%= Server.HtmlEncode(p.InventoryType) %></td>
        <td><%= Server.HtmlEncode(p.ProductGroupName) %></td>
        <td><%= Server.HtmlEncode(p.BrandCode) %></td>
        <td><%= p.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/ProductMaster.aspx?editId=<%= p.ProductID %>">Edit</a>
          <% if (p.IsActive) { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= p.ProductID %>';return confirm('Remove this product?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Products.Count == 0) { %><tr><td colspan="8">No products found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/ProductMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Edit Product" : "New Product" %> <%= Server.HtmlEncode(Input.ProductCode) %></span>
</div>
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Product" : "Add Product" %></h2></div>
  <div class="card-body">
    <input type="hidden" name="ProductID" value="<%= Input.ProductID %>" />
    <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
    <div class="form-grid">
      <div class="form-group"><label>Product Code</label>
        <input type="text" class="form-control" value="<%= Server.HtmlEncode(Input.ProductCode) %>" readonly /></div>
      <div class="form-group"><label>Product Name *</label>
        <input type="text" name="ProductName" class="form-control" value="<%= Server.HtmlEncode(Input.ProductName) %>" required /></div>
      <div class="form-group"><label>Item ID</label>
        <input type="text" name="ItemID" class="form-control" value="<%= Server.HtmlEncode(Input.ItemID) %>" /></div>
      <div class="form-group"><label>Default Selling Price</label>
        <input type="text" name="SellingPrice" class="form-control" value="<%= Server.HtmlEncode(Input.SellingPrice) %>" /></div>

      <div class="form-group"><label>Inventory Type</label>
        <select name="InventoryTypeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in InventoryTypes) { %><option value="<%= item.Id %>" <%= Input.InventoryTypeID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>SU (Stock Unit)</label>
        <select name="SUUnitOfMeasureID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in UnitOfMeasures) { %><option value="<%= item.Id %>" <%= Input.SUUnitOfMeasureID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>PU (Purchase Unit)</label>
        <select name="PUUnitOfMeasureID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in UnitOfMeasures) { %><option value="<%= item.Id %>" <%= Input.PUUnitOfMeasureID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>IU (Issue Unit)</label>
        <select name="IUUnitOfMeasureID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in UnitOfMeasures) { %><option value="<%= item.Id %>" <%= Input.IUUnitOfMeasureID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>

      <div class="form-group"><label>Product Nature</label>
        <select name="ProductNatureID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in ProductNatures) { %><option value="<%= item.Id %>" <%= Input.ProductNatureID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Item Registered</label>
        <select name="ItemRegisteredID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in ItemRegisteredList) { %><option value="<%= item.Id %>" <%= Input.ItemRegisteredID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Brand Code</label>
        <select name="BrandCodeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in BrandCodes) { %><option value="<%= item.Id %>" <%= Input.BrandCodeID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Brand Group</label>
        <select name="BrandGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in BrandGroups) { %><option value="<%= item.Id %>" <%= Input.BrandGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Product Group</label>
        <select name="ProductGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in ProductGroups) { %><option value="<%= item.Id %>" <%= Input.ProductGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Sales Group</label>
        <select name="ProductSalesGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in SalesGroups) { %><option value="<%= item.Id %>" <%= Input.ProductSalesGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Item Group</label>
        <select name="ItemGroupID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in ItemGroups) { %><option value="<%= item.Id %>" <%= Input.ItemGroupID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Sales Category</label>
        <select name="SalesCategoryID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in SalesCategories) { %><option value="<%= item.Id %>" <%= Input.SalesCategoryID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Division</label>
        <select name="ProductDivisionID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Divisions) { %><option value="<%= item.Id %>" <%= Input.ProductDivisionID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Team</label>
        <select name="ProductTeamID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in Teams) { %><option value="<%= item.Id %>" <%= Input.ProductTeamID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>HS Code</label>
        <select name="HSCodeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var item in HSCodes) { %><option value="<%= item.Id %>" <%= Input.HSCodeID==item.Id?"selected":"" %>><%= Server.HtmlEncode(item.Name) %></option><% } %>
        </select></div>

      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <a href="/ProductMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
