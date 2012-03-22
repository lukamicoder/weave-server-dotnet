/* 
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2012 Karoly Lukacs

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
        columns[1] = ["User", "tdUser", "tdUser"];
        columns[2] = ["Payload", "tdLoad", "tdLoad"];
        columns[3] = ["First Sync", "tdDate", "tdDate"];
        columns[4] = ["Last Sync", "tdDate", "tdDate"];
        columns[5] = ["&nbsp;", "", "tdLink"];
        columns[6] = ["&nbsp;", "", "tdLink"];
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

function openDialog(type, value, value2) {
    $('#dialogContent')[0].style.color = '';

    switch (type) {
        case "del":
            $('#dialog').dialog("option", "title", "Delete User");
            $('#dialog').dialog("option", "width", 300);
            $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close"); }, "Delete": function () { deleteUser(value); } });

            $('#dialogContent')[0].innerHTML = "Are you sure you want to delete " + value2 + "?";
            break;
        case "details":
            $('#dialog').dialog("option", "title", value2);
            $('#dialog').dialog("option", "width", 250);
            $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });
            break;
        case "new":
            $('#dialog').dialog("option", "title", "Add New User");
            $('#dialog').dialog("option", "width", 340);
            $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close"); }, "Submit": function () { addUser(); } });

            $('#dialogContent')[0].innerHTML = "";
            $('#dialogContent').append("<div id='error'></div>");
            $('#dialogContent').append("<p><span>Username:</span><input id='login' type='text' class='textbox' /></p>");
            $('#dialogContent').append("<p><span>Password:</span><input id='password' type='password' class='textbox' /></p>");
            $('#dialogContent').append("<p><span>Re-type Password:</span><input id='password1' type='password' class='textbox' /></p>");
            break;
        case "error":
            $('#dialog').dialog("option", "title", "Error");
            $('#dialog').dialog("option", "width", 'auto');
            $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });

            $('#dialogContent')[0].style.color = 'Red';
            $('#dialogContent')[0].innerHTML = value;
            break;
        default:
            return;
    }

    $('#dialog').dialog("open");
}

function showDetails(userid, username) {
    var param = [{ name: 'userId', value: userid}];

    $.ajax({
        url: "/Admin/GetUserDetails",
        cache: false,
        data: param,
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
            openDialog('details', userid, username);
        },
        error: function (data) {
            openDialog("error", data.responseText);
        }
    });
}

function deleteUser(userid) {
    var param = [{ name: 'userid', value: userid}];

    $.ajax({
        url: "/Admin/DeleteUser",
        cache: false,
        data: param,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            loadUserTable();
            $('#dialog').dialog("close");
            user = null;
        },
        error: function (data) {
            openDialog("error", data.responseText);
        }
    });
}

function addUser() {
    var param = [{ name: 'login', value: $('#login')[0].value }, { name: 'password', value: $('#password')[0].value }, { name: 'password1', value: $('#password1')[0].value}];

    $.ajax({
        url: "/Admin/AddUser",
        cache: false,
        data: param,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            loadUserTable();
            $('#dialog').dialog("close");
        },
        error: function (data) {
            $('#error')[0].style.display = "block";
            $('#error')[0].innerHTML = data.responseText;
        }
    });
}

function loadUserTable() {
    $.ajax({
        url: "/Admin/GetUserList",
        cache: false,
        type: 'POST',
        dataType: 'json',
        success: function (users) {
            var rows = new Array();
            if (users !== undefined) {
                if (users.length == 0) {
                    row = ["&nbsp;", "", "", "", "", ""];
                    rows.push(row);
                } else {
                    for (var i = 0; i < users.length; i++) {
                        if (users[i] !== null) {
                            var username = users[i].UserName;
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

                            var detail = "";
                            if (datemax !== "") {
                                detail = document.createElement("a");
                                $(detail).attr('href', "javascript:showDetails('" + id + "', '" + username + "');");
                                $(detail).append(document.createTextNode('details'));
                            }

                            var del = document.createElement("a");
                            $(del).attr('href', "javascript:openDialog('del', '" + id + "', '" + username + "');");
                            $(del).append(document.createTextNode('delete'));

                            row = [username, size, datemin, datemax, detail, del];
                        } else {
                            var row = ["&nbsp;", "", "", "", "", ""];
                        }

                        rows.push(row);
                    }
                }
            }

            userTable.loadData(rows);
        },
        error: function (data) {
            openDialog("error", data.responseText);
        }
    });
}