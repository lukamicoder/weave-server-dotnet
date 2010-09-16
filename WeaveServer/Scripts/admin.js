/* Copyright (C) 2010 Karoly Lukacs <lukamicoder@gmail.com>
*
* This is free software; you can redistribute it and/or
* modify it under the terms of the GNU Library General Public
* License as published by the Free Software Foundation; either
* version 3 of the License, or (at your option) any later version.
*
* This library is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
* Library General Public License for more details.
*
* You should have received a copy of the GNU Library General Public
* License along with this software; if not, write to the
* Free Software Foundation, Inc., 59 Temple Place - Suite 330,
* Boston, MA 02111-1307, USA.
*/

$(document).ready(function () {
    $('#dialog').dialog({
        modal: true,
        autoOpen: false,
        height: 'auto',
        minHeight: 33,
        closeText: ''
    });

    if ("undefined" == typeof (userTable)) {
        var columns = new Array;
        columns[1] = new Array("User", "tdCell", "tdCell");
        columns[2] = new Array("Payload", "tdLoad", "tdLoad");
        columns[3] = new Array("Last Sync Date", "tdCell", "tdCell");
        columns[4] = new Array("&nbsp;", "", "tdLink");
        columns[5] = new Array("&nbsp;", "", "tdLink");
        userTable = new JSONTable('tableContainer', 'userTable', columns);
    }

    if ("undefined" == typeof (detailsTable)) {
        var columns = new Array;
        columns[1] = new Array("Collection", "tdCell", "tdCell");
        columns[2] = new Array("Count", "tdLoad", "tdLoad");
        detailsTable = new JSONTable('dialogContent', 'detailsTable', columns);
    }

    loadUserTable();
});

function openDialog(type, value) {
    $('#dialogContent')[0].style.color = '';
    if (type == "del") {
        $('#dialog').dialog("option", "title", "Delete User");
        $('#dialog').dialog("option", "width", 300);
        $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close") }, "Delete": function () { deleteUser(value); } });

        $('#dialogContent')[0].innerHTML = "Are you sure you want to delete " + value + "?";
    } else if (type == "details") {
        $('#dialog').dialog("option", "title", "User Details - " + value);
        $('#dialog').dialog("option", "width", 250);
        $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });
    } else if (type == "new") {
        $('#dialog').dialog("option", "title", "Add New User");
        $('#dialog').dialog("option", "width", 290);
        $('#dialog').dialog("option", "buttons", { "Cancel": function () { $(this).dialog("close") }, "Submit": function () { addUser(); } });

        $('#dialogContent')[0].innerHTML = "";
        $('#dialogContent').append("<div id='error'></div>");
        $('#dialogContent').append("<p><span>Username:</span><input name='login' id='login' type='text' /></p>");
        $('#dialogContent').append("<p><span>Password:</span><input name='password' id='password' type='password' /></p>");
    } else if (type == "error") {
        $('#dialog').dialog("option", "title", "Error");
        $('#dialog').dialog("option", "width", 300);
        $('#dialog').dialog("option", "buttons", { "OK": function () { $(this).dialog("close"); } });

        $('#dialogContent')[0].style.color = 'Red';
        $('#dialogContent')[0].innerHTML = value;
    } else {
        return;
    }

    $('#dialog').dialog("open");
}

function showDetails(user) {
    var param = [{ name: 'user', value: user}];

    $.ajax({
        url: "/Admin/GetUserDetails",
        cache: false,
        data: param,
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            var rows = new Array;
            if (data != undefined) {
                for (var d in data) {
                    if (d != null) {
                        var row = new Array(d, data[d]);
                    } else {
                        var row = new Array("", "");
                    }

                    rows.push(row);
                }
            }

            $('#dialogContent')[0].innerHTML = "";
            detailsTable.loadData(rows);
            openDialog('details', user);
        },
        error: function (data) {
            openDialog("error", data.responseText);
        }
    });
}

function deleteUser(user) {
    var param = [{ name: 'user', value: user}];

    $.ajax({
        url: "/Admin/RemoveUser",
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
    var param = [{ name: 'login', value: $('#login')[0].value }, { name: 'password', value: $('#password')[0].value}];

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
            var rows = new Array;
            if (users != undefined) {
                for (var i = 0; i < users.length; i++) {
                    if (users[i] != null) {
                        var username = users[i].User;
                        var size = users[i].FormattedPayload;
                        if (users[i].Date > 0) {
                            var date = new Date(users[i].Date);
                            date = date.format();
                        } else {
                            var date = "";
                        }

                        if (date != "") {
                            var del = document.createElement("a");
                            $(del).attr('href', "javascript:showDetails('" + username + "');");
                            $(del).append(document.createTextNode('details'));
                        } else {
                            var del = "";
                        }

                        var edit = document.createElement("a");
                        $(edit).attr('href', "javascript:openDialog('del', '" + username + "');");
                        $(edit).append(document.createTextNode('delete'));

                        var row = new Array(username, size, date, del, edit);
                    } else {
                        var row = new Array("", "", "", "", "");
                    }

                    rows.push(row);
                }
            }

            userTable.loadData(rows);
        },
        error: function (data) {
            openDialog("error", data.responseText);
        }
    });
}