/* 
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2013 Karoly Lukacs

Based on code created by Mozilla Labs.
 
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

$(document).ready(function () {
    var columns;

    $('#dialog').dialog({
        modal: true,
        autoOpen: false,
        height: 'auto',
        minHeight: 33,
        closeText: ''
    });

    if ("undefined" == typeof (userTable)) {
        columns = [];
        columns[1] = ["Payload", "tdLoad", "tdLoad"];
        columns[2] = ["First Sync", "tdDate", "tdDate"];
        columns[3] = ["Last Sync", "tdDate", "tdDate"];
        columns[4] = ["&nbsp;", "", "tdLink"];
        columns[5] = ["&nbsp;", "", "tdLink"];
        userTable = new JSONTable('tableContainer', 'userTable', columns);
    }

    if ("undefined" == typeof (detailsTable)) {
        columns = [];
        columns[1] = ["Collection", "tdCell", "tdCell"];
        columns[2] = ["Count", "tdLoad", "tdLoad"];
        columns[3] = ["Payload", "tdLoad", "tdLoad"];
        detailsTable = new JSONTable('dialogContent', 'detailsTable', columns);
    }

    loadUserTable();
});

function openDialog(type) {
    $('#dialogContent')[0].style.color = '';
    switch(type) {
        case "del":
            $('#dialog').dialog("option", "title", "Delete Account");
            $('#dialog').dialog("option", "width", 340);
            $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close"); }, "Delete": function () { deleteUser(); } });

            $('#dialogContent')[0].innerHTML = "Are you sure you want to delete your account?<br />This cannot be undone.";
            break;
        case "details":
            $('#dialog').dialog("option", "title", "Account Details");
            $('#dialog').dialog("option", "width", 250);
            $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });
            break;
        case "clear":
            $('#dialog').dialog("option", "title", "Clear Sync Data");
            $('#dialog').dialog("option", "width", 340);
            $('#dialog').dialog("option", "buttons", { "Cancel": function () { loadUserTable(); $(this).dialog("close"); }, "Clear": function () { clearUserData(); } });

            $('#dialogContent')[0].innerHTML = "Are you sure you want to clear your sync data?<br />This cannot be undone.";
            break;
        case "pass":
            $('#dialog').dialog("option", "title", "Change Password");
            $('#dialog').dialog("option", "width", 360);
            $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close"); }, "Submit": function () { changePassword(); } });

            $('#dialogContent')[0].innerHTML = "";
            $('#dialogContent').append("<div id='error'></div>");
            $('#dialogContent').append("<p><span class='longspan'>New Password:</span><input id='password' type='password' class='textbox' /></p>");
            $('#dialogContent').append("<p><span class='longspan'>Re-type New Password:</span><input id='password1' type='password' class='textbox' /></p>");
            break;
        default:   
            return;
    }

    $('#dialog').dialog("open");
}

function openErrorDialog(text) {
    $('#dialog').dialog("option", "title", "Error");
    $('#dialog').dialog("option", "width", 'auto');
    $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });

    $('#dialogContent')[0].innerHTML = "";
    $('#dialogContent')[0].style.color = 'Red';
    $('#dialogContent')[0].innerHTML = text;

    $('#dialog').dialog("open");
}

function showDetails() {
    $.ajax({
        url: "/Account/GetUserDetails",
        cache: false,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            var rows = new Array();
            if (data !== undefined) {
                for (var i = 0; i < data.length; i++) {
                    var row;
                    if (data[i] !== null) {
                        row = [data[i].Collection, data[i].Count, data[i].Payload];
                    } else {
                        row = ["", "", ""];
                    }

                    rows.push(row);
                }
            }

            $('#dialogContent')[0].innerHTML = "";
            detailsTable.loadData(rows);
            openDialog('details');
        },
        error: function (data) {
            openErrorDialog(data.responseText);
        }
    });
}

function changePassword() {
    var pass = $('#password')[0].value;
    var pass1 = $('#password1')[0].value;
    if (pass == '' && pass1 == '') {
        return;
    } else if (pass != pass1) {
        $('#error')[0].style.display = "block";
        $('#error')[0].innerHTML = "Passwords do not match.";
        return;
    }

    $.ajax({
        url: "/Account/ChangePassword",
        cache: false,
        data: [{ name: 'password', value: pass}],
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            $('#dialog').dialog("close");
        },
        error: function (data) {
            openErrorDialog(data.responseText);
        }
    });
}

function deleteUser() {
    $.ajax({
        url: "/Account/DeleteUser",
        cache: false,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            $('#dialog').dialog("close");
            window.location = '/Account/Logout';
        },
        error: function (data) {
            openErrorDialog(data.responseText);
        }
    });
}

function clearUserData() {
    $.ajax({
        url: "/Account/ClearUserData",
        cache: false,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            loadUserTable();
            $('#dialog').dialog("close");
        },
        error: function (data) {
            openErrorDialog(data.responseText);
        }
    });
}

function loadUserTable() {
    $.ajax({
        url: "/Account/GetUserSummary",
        cache: false,
        type: 'POST',
        dataType: 'json',
        success: function (users) {
            var rows = new Array();
            if (users !== undefined) {
                if (users.length == 0) {
                    row = ["&nbsp;", "", "", "", ""];
                    rows.push(row);
                } else {
                    for (var i = 0; i < users.length; i++) {
                        if (users[i] !== null) {
                            var id = users[i].UserId;
                            var size = users[i].Payload;

                            var datemin = "";
                            if (users[i].DateMin > 0) {
                                datemin = new Date(users[i].DateMin);
                                datemin = datemin.format();
                            }

                            var datemax = "";
                            if (users[i].DateMax > 0) {
                                datemax = new Date(users[i].DateMax);
                                datemax = datemax.format();
                            }

                            var detail = ""; ;
                            if (datemax !== "") {
                                detail = document.createElement("a");
                                $(detail).attr('href', "javascript:showDetails()");
                                $(detail).append(document.createTextNode('details'));
                            }

                            var clear = "&nbsp;";
                            if (datemax !== "") {
                                clear = document.createElement("a");
                                $(clear).attr('href', "javascript:openDialog('clear');");
                                $(clear).append(document.createTextNode('clear'));
                            }

                            row = [size, datemin, datemax, detail, clear];
                        } else {
                            var row = ["&nbsp;", "", "", "", ""];
                        }

                        rows.push(row);
                    }
                }
            }

            userTable.loadData(rows);
        },
        error: function (data) {
            openErrorDialog(data.responseText);
        }
    });
}