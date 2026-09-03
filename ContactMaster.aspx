<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactMaster.aspx.cs" Inherits="HRMS.ContactMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Contact List</h2>
    <a href="/ContactMaster.aspx?newContact=1" class="btn btn-primary">+ New Contact</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Code</th><th>Customer</th><th>Name</th><th>Type</th><th>Status</th><th>Mobile</th><th>Email</th><th></th></tr></thead>
      <tbody>
      <% foreach (var c in Contacts) { %>
      <tr>
        <td><%= Server.HtmlEncode(c.ContactCode) %></td>
        <td><%= Server.HtmlEncode(c.CustomerCode) %> <%= Server.HtmlEncode(c.CustomerName) %></td>
        <td><%= Server.HtmlEncode(c.Name) %></td>
        <td><%= Server.HtmlEncode(c.ContactType) %></td>
        <td><%= Server.HtmlEncode(c.ContactStatus) %></td>
        <td><%= Server.HtmlEncode(c.Mobile) %></td>
        <td><%= Server.HtmlEncode(c.Email) %></td>
        <td>
          <a href="/ContactMaster.aspx?editId=<%= c.ContactID %>">Edit</a>
          <% if (c.ContactStatus == "Active") { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= c.ContactID %>';return confirm('Deactivate contact?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Contacts.Count == 0) { %><tr><td colspan="8">No contacts found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/ContactMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Contact" : "New Contact" %> <%= Server.HtmlEncode(Input.ContactCode) %></span>
</div>
<input type="hidden" name="ContactID" value="<%= Input.ContactID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Contact" : "Add Contact" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Contact Code</label><input type="text" class="form-control" value="<%= Server.HtmlEncode(Input.ContactCode) %>" readonly /></div>
      <div class="form-group"><label>Name *</label><input type="text" name="Name" class="form-control" value="<%= Server.HtmlEncode(Input.Name) %>" required /></div>
      <div class="form-group"><label>Search Name</label><input type="text" name="SearchName" class="form-control" value="<%= Server.HtmlEncode(Input.SearchName) %>" /></div>
      <div class="form-group"><label>Customer</label>
        <select name="CustomerID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var c in Customers) { %><option value="<%= c.Id %>" <%= Input.CustomerID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Contact For</label><input type="text" name="ContactFor" class="form-control" value="<%= Server.HtmlEncode(Input.ContactFor) %>" /></div>
      <div class="form-group"><label>Type</label>
        <select name="ContactType" class="form-control">
        <% foreach (var t in ContactTypes) { %><option value="<%= t %>" <%= Input.ContactType==t?"selected":"" %>><%= t %></option><% } %>
        </select></div>
      <div class="form-group"><label>Status</label>
        <select name="ContactStatus" class="form-control">
        <% foreach (var s in ContactStatuses) { %><option value="<%= s %>" <%= Input.ContactStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Gender</label>
        <select name="GenderID" class="form-control"><option value="0">--</option>
        <% foreach (var g in Genders) { %><option value="<%= g.Id %>" <%= Input.GenderID==g.Id?"selected":"" %>><%= Server.HtmlEncode(g.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Marital Status</label>
        <select name="MaritalStatus" class="form-control"><option value="">--</option>
        <% foreach (var m in MaritalStatuses) { %><option value="<%= m %>" <%= Input.MaritalStatus==m?"selected":"" %>><%= m %></option><% } %>
        </select></div>
      <div class="form-group"><label>Professional Title</label><input type="text" name="ProfessionalTitle" class="form-control" value="<%= Server.HtmlEncode(Input.ProfessionalTitle) %>" /></div>
      <div class="form-group"><label>Department</label><input type="text" name="Department" class="form-control" value="<%= Server.HtmlEncode(Input.Department) %>" /></div>
      <div class="form-group"><label>Office Location</label><input type="text" name="OfficeLocation" class="form-control" value="<%= Server.HtmlEncode(Input.OfficeLocation) %>" /></div>
      <div class="form-group"><label>Available From</label><input type="time" name="AvailableFrom" class="form-control" value="<%= Server.HtmlEncode(Input.AvailableFrom) %>" /></div>
      <div class="form-group"><label>Available To</label><input type="time" name="AvailableTo" class="form-control" value="<%= Server.HtmlEncode(Input.AvailableTo) %>" /></div>
      <div class="form-group"><label>Reports To</label><input type="text" name="ReportToManagerName" class="form-control" value="<%= Server.HtmlEncode(Input.ReportToManagerName) %>" /></div>
      <div class="form-group"><label>Phone</label><input type="text" name="Phone" class="form-control" value="<%= Server.HtmlEncode(Input.Phone) %>" /></div>
      <div class="form-group"><label>Mobile</label><input type="text" name="Mobile" class="form-control" value="<%= Server.HtmlEncode(Input.Mobile) %>" /></div>
      <div class="form-group"><label>Email</label><input type="text" name="Email" class="form-control" value="<%= Server.HtmlEncode(Input.Email) %>" /></div>
      <div class="form-group"><label>WhatsApp</label><input type="text" name="Whatsapp" class="form-control" value="<%= Server.HtmlEncode(Input.Whatsapp) %>" /></div>
      <div class="form-group"><label>URL</label><input type="text" name="URL" class="form-control" value="<%= Server.HtmlEncode(Input.URL) %>" /></div>
      <div class="form-group"><label>Fax</label><input type="text" name="Fax" class="form-control" value="<%= Server.HtmlEncode(Input.Fax) %>" /></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <a href="/ContactMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
