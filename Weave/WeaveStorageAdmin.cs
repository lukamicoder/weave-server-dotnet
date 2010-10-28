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
            List<object> list = new List<object>();

            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var userList = (from u in context.Users
                                    join w in context.Wbos on u.UserId equals w.UserId
                                    into g
                                    select new {
                                        u.UserId,
                                        u.UserName,
                                        Payload = (Double?)g.Sum(p => p.PayloadSize),
                                        Date = (Double?)g.Max(p => p.Modified)
                                    }).ToList();

                    foreach (var user in userList) {
                        long userId = user.UserId;
                        string userName = user.UserName;
                        double total;
                        string payload = "";
                        if (user.Payload != null) {
                            total = (user.Payload.Value * 1000) / 1024 / 1024;
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

                        list.Add(new { UserId = userId, UserName = userName, Payload = payload, Date = date });
                    }
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public List<object> GetUserDetails(Int64 userId) {
            List<object> list = new List<object>();
            using (WeaveContext context = new WeaveContext(ConnectionString)) {
                try {
                    var cts = from w in context.Wbos
                              where w.UserId == userId
                              group w by new { w.Collection } into g
                              select new { g.Key.Collection, Count = (Int64)g.Count(), Payload = (double?)g.Sum(p => p.PayloadSize) };

                    foreach (var p in cts) {
                        double total;
                        string payload = "";
                        if (p.Payload != null) {
                            total = (p.Payload.Value * 1000) / 1024 / 1024;
                            if (total >= 1024) {
                                payload = Math.Round((total / 1024), 1) + "MB";
                            } else if (total > 0) {
                                payload = Math.Round(total, 1) + "KB";
                            }
                        }

                        list.Add(new { Collection = WeaveCollectionDictionary.GetValue(p.Collection), p.Count, Payload = payload });
                    }
                } catch (EntityException x) {
                    WeaveLogger.WriteMessage(x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
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

        public int Cleanup(int days) {
            if (days > 0) {
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                int dayinsecond = 60 * 60 * 24;
                double deleteTime = ts.TotalSeconds - (dayinsecond * days);

                using (WeaveContext context = new WeaveContext(ConnectionString)) {
                    try {
                        var wbosToDelete = from wbo in context.Wbos
                                           where wbo.Modified < deleteTime &&
                                                 (wbo.Collection == 3 ||
                                                  wbo.Collection == 4 ||
                                                  wbo.Collection == 9 ||
                                                  wbo.Payload == null)
                                           select wbo;

                        int total = wbosToDelete.ToList().Count();

                        foreach (var wboToDelete in wbosToDelete) {
                            context.DeleteObject(wboToDelete);
                        }

                        context.SaveChanges();

                        return total;
                    } catch (EntityException x) {
                        WeaveLogger.WriteMessage(x.Message, LogType.Error);

                        return -1;
                    }
                }
            } else {
                return -1;
            }
        }
    }
}