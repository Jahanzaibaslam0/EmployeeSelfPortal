<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="NoBusinessLinkContacts.aspx.cs" Inherits="HRMS.NoBusinessLinkContactsPage" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p class="text-muted" style="margin-bottom:1rem;">Guest / other contacts with no customer, vendor, or employee business link.</p>
<hrms:ContactHubList runat="server" HubView="NoBusinessLink" ListPath="/NoBusinessLinkContacts.aspx" PageHeading="No Business Link" LockTypeFilter="true" />
</asp:Content>
