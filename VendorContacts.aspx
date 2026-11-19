<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="VendorContacts.aspx.cs" Inherits="HRMS.VendorContactsPage" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p class="text-muted" style="margin-bottom:1rem;">Contacts linked to vendors only. Customer, employee, and other types are excluded.</p>
<hrms:ContactHubList runat="server" HubView="Vendor" ListPath="/VendorContacts.aspx" PageHeading="Vendor Contacts" LockTypeFilter="true" />
</asp:Content>
