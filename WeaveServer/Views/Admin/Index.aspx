<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <title>Weave Server Administration</title>
    <script type="text/javascript" src="../Scripts/admin.js"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">    
    <a href="javascript:openDialog('new');" class="addnew">Add new</a>
    <div id="tableContainer"></div>
    
    <div id="dialog">
        <div id="dialogContent"></div>
    </div>
</asp:Content>
