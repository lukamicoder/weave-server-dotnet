<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
	<title>Weave Server Administration</title>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<form method="post">
		<div class="loginform">
			<div class="header">Please Log In:</div>
			<div class="row"><label class="field">Username:</label><input name="login" type="text" class="textbox" /></div>
			<div class="row"><label class="field">Password:</label><input name="password" type="password" class="textbox" /></div>
			<div class="buttonbar"><label class="field">&nbsp;</label><input type="submit" id="loginButton" value="Submit" class="button" /></div>
			<div class="error" style="display:<%= Html.Encode(ViewData["errorDisplay"])%>;"><label id="errorLabel"><%= Html.Encode(ViewData["errorMessage"])%></label></div>
		</div>
	</form>
</asp:Content>
