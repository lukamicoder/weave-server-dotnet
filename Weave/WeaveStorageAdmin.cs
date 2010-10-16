/* Copyright (C) 2010 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Weave.Models;

namespace Weave {
    class WeaveStorageAdmin : WeaveStorage {
        public List<object> GetUserList() {
            List<object> result = new List<object>();

            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var userList = (from u in context.Users
                                    join w in context.Wbos on u.UserId equals w.UserId
                                    into g
                                    select new {
                                        u.UserId,
                                        u.UserName,
                                        Total = (Double?)g.Sum(p => p.PayloadSize),
                                        Date = (Double?)g.Max(p => p.Modified)
                                    }).ToList();

                    foreach (var user in userList) {
                        long userId = user.UserId;
                        string userName = user.UserName;
                        double total;
                        string payload = "";
                        if (user.Total != null) {
                            total = (user.Total.Value * 1000) / 1024 / 1024;
                            if (total >= 1024) {
                                payload = Math.Round((total / 1024), 1) + "MB";
                            } else if (total > 0) {
                                payload = Math.Round(total, 1) + "KB";
                            }
                        }
                        double date;
                        if (user.Date != null) {
                            date = 1000 * user.Date.Value;
                        } else {
                            date = 0;
                        }

                        result.Add(new { UserId = userId, UserName = userName, Payload = payload, Date = date });
                    }
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }

        public bool DeleteUser(Int64 userId) {
            bool result = false;

            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var wboList = (from wbos in context.Wbos
                                   join users in context.Users on wbos.UserId equals users.UserId
                                   where users.UserId == userId
                                   select wbos).ToList();

                    foreach (var del in wboList) {
                        context.DeleteObject(del);
                    }

                    var user = (from u in context.Users
                                where u.UserId == userId
                                select u).SingleOrDefault();

                    if (user != null) {
                        context.DeleteObject(user);
                    }

                    int x = context.SaveChanges();

                    if (x != 0) {
                        result = true;
                    }
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }

        public bool CreateUser(string userName, string password) {
            bool result = false;

            if (!String.IsNullOrEmpty(userName)) {
                using (WeaveContext context = new WeaveContext(ConnectionString)) {
                    try {
                        string hash = HashString(password);
                        User user = new User { UserName = userName, Md5 = hash };
                        context.Users.AddObject(user);

                        int x = context.SaveChanges();

                        if (x != 0) {
                            result = true;
                        }
                    } catch (EntityException x) {
                        WeaveLogger.WriteMessage(x.Message, LogType.Error);
                        throw new WeaveException("Database unavailable.", 503);
                    }
                }
            }

            return result;
        }

        public bool IsUniqueUserName(string userName) {
            bool result = false;

            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var id = (from u in context.Users
                              where u.UserName == userName
                              select u.UserId).SingleOrDefault();

                    if (id == 0) {
                        result = true;
                    }
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }

        public void Cleanup() {
            //14 days
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            double deleteTime = ts.TotalSeconds - (60 * 60 * 24 * 14) * 100;

            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var wbosToDelete = from wbo in context.Wbos
                                       where wbo.Modified < deleteTime &&
                                             (wbo.Collection == 3 ||
                                              wbo.Collection == 4 ||
                                              wbo.Collection == 9 ||
                                              wbo.Payload == null)
                                       select wbo;

                    foreach (var wboToDelete in wbosToDelete) {
                        context.DeleteObject(wboToDelete);
                    }

                    context.SaveChanges();
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
        }
    }
}