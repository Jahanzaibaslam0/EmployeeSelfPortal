<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MyContacts.aspx.cs" Inherits="HRMS.MyContactsPage" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<p class="text-muted" style="margin-bottom:1rem;">Contacts you created. Other users' records are not shown here.</p>
<hrms:ContactHubList runat="server" HubView="Mine" ListPath="/MyContacts.aspx" PageHeading="My Contacts" />
</asp:Content>
