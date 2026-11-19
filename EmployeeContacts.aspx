<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EmployeeContacts.aspx.cs" Inherits="HRMS.EmployeeContactsPage" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p class="text-muted" style="margin-bottom:1rem;">Contacts linked to employees / staff only. Customer, vendor, and guest types are excluded.</p>
<hrms:ContactHubList runat="server" HubView="Employee" ListPath="/EmployeeContacts.aspx" PageHeading="Employee / Staff Contacts" LockTypeFilter="true" />
</asp:Content>
