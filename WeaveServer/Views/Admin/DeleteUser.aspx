<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
	<title>Delete Weave User</title>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<form method="post">
		<div class="loginform">
			<div class="header">Delete Your Weave Account:</div>
			<div class="row"><label class="field">Username:</label><input name="login" type="text" class="textbox" /></div>
			<div class="row"><label class="field">Password:</label><input name="password" type="password" class="textbox" /></div>
			<div class="buttonbar"><label class="field">&nbsp;</label><input type="submit" id="loginButton" value="Delete" class="button" /></div>
			<div class="error"><label style="<%= Html.Encode(ViewData["resultStyle"])%>;"><%= Html.Encode(ViewData["resultMessage"])%></label></div>
		</div>
	</form>
</asp:Content>
