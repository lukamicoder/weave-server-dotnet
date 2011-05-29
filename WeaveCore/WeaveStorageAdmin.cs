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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WeaveCore.Models;

namespace WeaveCore {
    class WeaveStorageAdmin : WeaveStorageBase {
        public List<object> GetUserList() {
            List<object> list = new List<object>();

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var userList = (from u in context.Users
                                    join w in context.Wbos on u.UserId equals w.UserId
                                    into g
                                    select new {
                                        u.UserId,
                                        u.UserName,
                                        Payload = (Double?)g.Sum(p => p.PayloadSize),
                                        DateMin = (Double?)g.Min(p => p.Modified),
                                        DateMax = (Double?)g.Max(p => p.Modified)
                                    }).ToList();

                    foreach (var user in userList) {
                        long userId = user.UserId;
                        string userName = user.UserName;
                        string payload = "";
                        if (user.Payload != null) {
                            double total = (user.Payload.Value * 1000) / 1024 / 1024;
                            if (total >= 1024) {
                                payload = Math.Round((total / 1024), 1) + "MB";
                            } else if (total > 0) {
                                payload = Math.Round(total, 1) + "KB";
                            }
                        }

                        double dateMin = 0;
                        if (user.DateMin != null) {
                            dateMin = 1000 * user.DateMin.Value;
                        }

                        double dateMax = 0;
                        if (user.DateMax != null) {
                            dateMax = 1000 * user.DateMax.Value;
                        }

                        list.Add(new { UserId = userId, UserName = userName, Payload = payload, DateMin = dateMin, DateMax = dateMax });
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public List<object> GetUserDetails(Int64 userId) {
            List<object> list = new List<object>();
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var cts = from w in context.Wbos
                              where w.UserId == userId
                              group w by new { w.Collection } into g
                              select new { g.Key.Collection, Count = (Int64)g.Count(), Payload = (double?)g.Sum(p => p.PayloadSize) };

                    foreach (var p in cts) {
                        //crypto, keys, meta
                        if (p.Collection != 2 && p.Collection != 5 && p.Collection != 6) {
                            string payload = "";
                            if (p.Payload != null) {
                                double total = (p.Payload.Value * 1000) / 1024 / 1024;
                                if (total >= 1024) {
                                    payload = Math.Round((total / 1024), 0) + "MB";
                                } else if (total > 0) {
                                    payload = Math.Round(total, 0) + "KB";
                                }
                            }

                            list.Add(new { Collection = WeaveCollectionDictionary.GetValue(p.Collection), p.Count, Payload = payload });
                        }
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public bool CreateUser(string userName, string password) {
            bool result = false;

            if (!String.IsNullOrEmpty(userName)) {
                using (WeaveContext context = new WeaveContext()) {
                    try {
                        string hash = HashString(password);
                        User user = new User { UserName = userName, Md5 = hash };
                        context.Users.Add(user);

                        int x = context.SaveChanges();

                        if (x != 0) {
                            result = true;
                            RaiseLogEvent(this, String.Format("{0} user account has been created.", userName), LogType.Information);
                        }
                    } catch (EntityException x) {
                        RaiseLogEvent(this, x.Message, LogType.Error);
                        throw new WeaveException("Database unavailable.", 503);
                    }
                }
            }

            return result;
        }

        public bool IsUniqueUserName(string userName) {
            bool result = false;

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var id = (from u in context.Users
                              where u.UserName == userName
                              select u.UserId).SingleOrDefault();

                    if (id == 0) {
                        result = true;
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }

        public int Cleanup(int days) {
            if (days > 0) {
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                const int dayInSecond = 60 * 60 * 24;
                double deleteTime = ts.TotalSeconds - (dayInSecond * days);

                using (WeaveContext context = new WeaveContext()) {
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
                            context.Wbos.Remove(wboToDelete);
                        }

                        context.SaveChanges();

                        RaiseLogEvent(this, String.Format("Cleanup: {0} records have been deleted.", total), LogType.Information);
                        return total;
                    } catch (EntityException x) {
                        RaiseLogEvent(this, x.Message, LogType.Error);

                        return -1;
                    }
                }
            }

            return -1;
        }
    }
}