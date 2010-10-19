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

function StringBuilder(value) {
	this.strings = [""];

	this.append = function (value) {
	    if (value) {
	        this.strings[this.strings.length] = value;
	    }
	    return this;
	};

	this.clear = function () {
	    this.strings.length = 1;
	};

	this.removeLast = function () {
	    if (this.strings.length > 1) {
	        this.strings.splice(this.strings.length - 1, 1);
	    }
	};

	this.toString = function () {
	    return this.strings.join("");
	};

	return this;
}

var dateFormat = function () {
	return function (date) {
		var currDate = date.getDate();
		var currMonth = date.getMonth();
		currMonth++;
		var currYear = date.getFullYear();

		var currHour = date.getHours();
		var ap = "";
		if (currHour < 12) {
			ap = "AM";
		} else {
			ap = "PM";
		}
		if (currHour === 0) {
			currHour = 12;
		} else if (currHour > 12) {
			currHour = currHour - 12;
		}

		var currMin = date.getMinutes();
		if (currMin < 10) {
			currMin = "0" + currMin;
		}

		return currMonth + "/" + currDate + "/" + currYear + " " + currHour + ":" + currMin + " " + ap;
	};
}();

Date.prototype.format = function () {
	return dateFormat(this);
};

function JSONTable(containerID, tableID, columns) {
	this._rowNumber = 0;
	this.tableClass = 'JSONTable';

	this.loadData = function (records) {
	    this._rowNumber = records.length;
	    this._init();
	    var rows = $('#' + tableID).find('tbody > tr').get();
	    for (var i in rows) {
	        var record = records[i];
	        var col = 0;
	        $(rows[i]).find('td').each(function () {
	            $(this).empty();
	            if (record !== null) {
	                $(this).append(record[col]);
	            } else {
	                $(this).append('&nbsp;');
	            }

	            col++;
	        });
	    }
	};

	this._init = function () {
	    var sb = new StringBuilder();
	    var rows = $('#' + tableID).find('tbody > tr').get();
	    if (rows.length > 0) {
	        var currentRowNumber = rows.length;
	        var diff = currentRowNumber - this._rowNumber;
	        var z;
	        if (diff > 0) {
	            for (z = 1; z < diff; z++) {
	                $('#' + tableID).find('tbody > tr:eq(' + (currentRowNumber - z) + ')').remove();
	            }
	            if (this._rowNumber > 0) {
	                $('#' + tableID).find('tbody > tr:eq(' + this._rowNumber + ')').remove();
	            }
	        } else if (diff < 0) {
	            diff = Math.abs(diff);
	            for (z = 0; z < diff; z++) {
	                this._createRow(sb);
	            }
	            $('#' + tableID).find('tbody').append(sb.toString());
	        }

	        $('#' + tableID).find('tfoot > tr > td:eq(1)').empty().append(this.total);
	    } else {
	        var $table = $('<table/>');
	        var table = $table.attr("class", this.tableClass)[0];
	        table = $table.attr("id", tableID)[0];
	        sb.append("<thead><tr>");
	        for (var i in columns) {
	            sb.append("<th class='").append(columns[i][1]).append("'>").append(columns[i][0]).append("</th>");
	        }
	        sb.append("</tr></thead>");

	        sb.append("<tbody>");
	        this._createRow(sb);
	        for (var x = 0; x < this._rowNumber - 1; x++) {
	            this._createRow(sb);
	        }
	        sb.append("</tbody>");

	        $table.append(sb.toString());
	        $('#' + containerID)[0].appendChild(table);
	    }

	    $('#' + tableID + ' tr:odd').addClass('oddRow');
	    $('#' + tableID + ' tr:even').addClass('evenRow');
	};

	this._createRow = function (sb) {
	    sb.append("<tr>");
	    for (var i in columns) {
	        sb.append("<td class='").append(columns[i][2]).append("'></td>");
	    }
	    sb.append("</tr>");
	};
}

