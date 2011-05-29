/* Copyright (C) 2011 Karoly Lukacs <lukamicoder@gmail.com>
 *
 * Based on code created by Mozilla Labs.
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

using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using WeaveCore.Models;

namespace WeaveCore {
    abstract class WeaveStorageBase : WeaveLogEventBase {
        public int UserId { get; private set; }

        public bool AuthenticateUser(string userName, string password) {
            bool result = false;

            using (WeaveContext context = new WeaveContext()) {
                string hash = HashString(password);

                var id = (from u in context.Users
                          where u.UserName == userName && u.Md5 == hash
                          select u.UserId).SingleOrDefault();

                if (id != 0) {
                    UserId = id;
                    result = true;
                }
            }

            return result;
        }

        public string HashString(string value) {
            StringBuilder hashedString = new StringBuilder();
            using (MD5CryptoServiceProvider serviceProvider = new MD5CryptoServiceProvider()) {
                byte[] data = serviceProvider.ComputeHash(Encoding.ASCII.GetBytes(value));
                for (int i = 0; i < data.Length; i++) {
                    hashedString.Append(data[i].ToString("x2"));
                }
            }

            return hashedString.ToString();
        }

        public bool DeleteUser(Int64 userId) {
            string userName = "";
            bool result = false;

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var wboList = (from wbos in context.Wbos
                                   join users in context.Users on wbos.UserId equals users.UserId
                                   where users.UserId == userId
                                   select wbos).ToList();

                    foreach (var del in wboList) {
                        context.Wbos.Remove(del);
                    }

                    var user = (from u in context.Users
                                where u.UserId == userId
                                select u).SingleOrDefault();

                    if (user != null) {
                        userName = user.UserName;
                        context.Users.Remove(user);
                    }

                    int x = context.SaveChanges();

                    if (x != 0) {
                        result = true;
                        if (!String.IsNullOrEmpty(userName)) {
                            RaiseLogEvent(this, String.Format("{0} user account has been deleted.", userName), LogType.Information);                
                        }
                    }
                } catch (EntityException x) {
                    OnLogEvent(this, new WeaveLogEventArgs(x.Message, LogType.Error));
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }
    }
}