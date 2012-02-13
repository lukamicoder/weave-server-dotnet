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
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using WeaveCore.Models;
using System.Data.SqlServerCe;

namespace WeaveCore {
    class WeaveAdminStorage : WeaveLogEventBase {
        public WeaveAdminStorage() {
            Database.SetInitializer(new WeaveDbInitializer());
        }

        public int AuthenticateUser(string userName, string password) {
            int id;

            using (WeaveContext context = new WeaveContext()) {
                string hash = WeaveHelper.ConvertToHash(password);

                if (userName.Contains("@")) {
                    id = (from u in context.Users
                          where u.Email == userName && u.Md5 == hash
                          select u.UserId).SingleOrDefault();
                } else {
                    id = (from u in context.Users
                          where u.UserName == userName && u.Md5 == hash
                          select u.UserId).SingleOrDefault();
                }
            }

            return id;
        }

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
                                        u.Email,
                                        Payload = (Double?)g.Sum(p => p.PayloadSize),
                                        DateMin = g.Min(p => p.Modified),
                                        DateMax = g.Max(p => p.Modified)
                                    }).ToList();

                    foreach (var user in userList) {
                        long userId = user.UserId;
                        string userName = user.UserName;
                        string email = user.Email;
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

                        list.Add(new { UserId = userId, UserName = String.IsNullOrEmpty(email) ? userName : email, Payload = payload, DateMin = dateMin, DateMax = dateMax });
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public List<object> GetUserSummary(Int32 userId) {
            List<object> list = new List<object>();

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var userList = (from u in context.Users
                                    where u.UserId == userId
                                    join w in context.Wbos on u.UserId equals w.UserId 
                                    into g 
                                    select new {
                                        u.UserId,
                                        u.UserName,
                                        u.Email,
                                        Payload = (Double?)g.Sum(p => p.PayloadSize),
                                        DateMin = g.Min(p => p.Modified),
                                        DateMax = g.Max(p => p.Modified)
                                    }).ToList();

                    foreach (var user in userList) {
                        string userName = user.UserName;
                        string email = user.Email;
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

                        list.Add(new { UserId = userId, UserName = String.IsNullOrEmpty(email) ? userName : email, Payload = payload, DateMin = dateMin, DateMax = dateMax });
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public List<object> GetUserDetails(Int32 userId) {
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

        public bool CreateUser(string userName, string password, string email) {
            bool result = false;

            if (!String.IsNullOrEmpty(userName)) {
                using (WeaveContext context = new WeaveContext()) {
                    try {
                        string hash = WeaveHelper.ConvertToHash(password);
                        User user = new User { Email = email, UserName = userName, Md5 = hash };
                        context.Users.Add(user);

                        int x = context.SaveChanges();

                        if (x != 0) {
                            result = true;
                            RaiseLogEvent(this, String.Format("{0} user account has been created.", String.IsNullOrEmpty(email) ? userName : email), LogType.Information);
                        }
                    } catch (EntityException x) {
                        RaiseLogEvent(this, x.Message, LogType.Error);
                        throw new WeaveException("Database unavailable.", 503);
                    }
                }
            }

            return result;
        }

        public bool IsUserNameUnique(string userName) {
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

        public bool DeleteUser(Int32 userId) {
            string userName = "";
            bool result = false;

            using (WeaveContext context = new WeaveContext()) {
                try {
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
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return result;
        }

        public void ChangePassword(Int32 userId, string password) {
            if (String.IsNullOrEmpty(password)) {
                throw new WeaveException("3", 404);
            }

            using (WeaveContext context = new WeaveContext()) {
                string hash = WeaveHelper.ConvertToHash(password);
                try {
                    var userToGet = (from u in context.Users
                                     where u.UserId == userId
                                     select u).SingleOrDefault();

                    if (userToGet != null) {
                        userToGet.Md5 = hash;
                        context.SaveChanges();
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
        }

        public void ClearUserData(Int32 userId) {
            using (WeaveContext context = new WeaveContext()) {
                try {
                    const string sql = "DELETE FROM Wbos WHERE UserID = @userid";
                    object param;
                    if (context.Database.Connection is SqlCeConnection) {
                        param = new SqlCeParameter("userid", userId);
                    } else {
                        param = new SqlParameter("userid", userId);
                    }

                    context.Database.ExecuteSqlCommand(sql, param);
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
        }
    }
}