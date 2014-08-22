/*
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2014 Karoly Lukacs

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

namespace Weave.Core.Models {
    public enum LogType {
        Error = 0,
        Info = 1,
        Warning = 2,
        Debug = 3
    }

    public enum RequestMethod { GET, PUT, POST, DELETE }

    public enum DatabaseType { NA, SQLite, MySQL, SQLServer }

    public enum RequestFunction { NotSupported, Info, Storage, Node, Password }

    public enum WeaveErrorCodes {
        InvalidProtocol = 1,
        IncorrectCaptcha = 2,
        InvalidUsername = 3,
        NoOverwrite = 4,
        UseridPathMismatch = 5,
        JsonParse = 6,
        MissingPassword = 7,
        InvalidWbo = 8,
        BadPasswordStrength = 9,
        InvalidResetCode = 10,
        FunctionNotSupported = 11,
        NoEmail = 12,
        InvalidCollection = 13
    }
}