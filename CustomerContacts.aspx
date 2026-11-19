<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CustomerContacts.aspx.cs" Inherits="HRMS.CustomerContactsPage" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p class="text-muted" style="margin-bottom:1rem;">Contacts linked to customers only. Vendor, employee, and other types are excluded.</p>
<hrms:ContactHubList runat="server" HubView="Customer" ListPath="/CustomerContacts.aspx" PageHeading="Customer Contacts" LockTypeFilter="true" />
</asp:Content>
