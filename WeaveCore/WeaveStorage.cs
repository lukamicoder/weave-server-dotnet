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
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using WeaveCore.Models;

namespace WeaveCore {
    class WeaveStorage : WeaveLogEventBase {
        public int UserId { get; private set; }

        #region Collection
        public double GetMaxTimestamp(string collection) {
            double result = 0;

            if (String.IsNullOrEmpty(collection)) {
                return 0;
            }

            using (WeaveContext context = new WeaveContext()) {
                try {
                    int coll = WeaveCollectionDictionary.GetKey(collection);
                    var time = (from wbos in
                                    (from wbos in context.Wbos
                                     where wbos.UserId == UserId && wbos.Collection == coll
                                     select new { wbos.Modified })
                                group wbos by new { wbos.Modified }
                                    into g
                                    select new { max = g.Max(p => p.Modified) }).SingleOrDefault();

                    if (time.max != null) {
                        result = time.max.Value;
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return Math.Round(result, 2);
        }

        public IList<string> GetCollectionList() {
            IList<string> list = new List<string>();

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var coll = (from wbos in context.Wbos
                                where wbos.UserId == UserId
                                select new { wbos.Collection }).Distinct();

                    foreach (var c in coll) {
                        list.Add(WeaveCollectionDictionary.GetValue(c.Collection));
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return list;
        }

        public Dictionary<string, double> GetCollectionListWithTimestamps() {
            var dic = new Dictionary<string, double>();

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var coll = from wbos in context.Wbos
                               where wbos.UserId == UserId
                               group wbos by new { wbos.Collection }
                                   into g
                                   select new {
                                       Collection = (Int16?)g.Key.Collection,
                                       Timestamp = g.Max(p => p.Modified)
                                   };

                    foreach (var c in coll) {
                        dic.Add(WeaveCollectionDictionary.GetValue(c.Collection.Value), c.Timestamp.Value);
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return dic;
        }

        public Dictionary<string, long> GetCollectionListWithCounts() {
            var dic = new Dictionary<string, long>();
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var cts = from w in context.Wbos
                              where w.UserId == UserId
                              group w by new { w.Collection } into g
                              select new { g.Key.Collection, ct = (Int64)g.Count() };

                    foreach (var p in cts) {
                        dic.Add(WeaveCollectionDictionary.GetValue(p.Collection), p.ct);
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return dic;
        }

        public double GetStorageTotal() {
            double result;

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var total = (from u in context.Users
                                 where u.UserId == UserId
                                 join w in context.Wbos on u.UserId equals w.UserId
                                 into g
                                 select new {
                                     Payload = (double?)g.Sum(p => p.PayloadSize)
                                 }).SingleOrDefault();

                    result = total.Payload.Value / 1024;
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
            return result;
        }

        public Dictionary<string, int> GetCollectionStorageTotals() {
            var dic = new Dictionary<string, int>();
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var cts = from w in context.Wbos
                              where w.UserId == UserId
                              group w by new { w.Collection } into g
                              select new { g.Key.Collection, Payload = (int?)g.Sum(p => p.PayloadSize) };

                    foreach (var p in cts) {
                        dic.Add(WeaveCollectionDictionary.GetValue(p.Collection), p.Payload.Value / 1024);
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return dic;
        }
        #endregion

        #region Wbo
        public void SaveWbo(WeaveBasicObject wbo) {
            using (WeaveContext context = new WeaveContext()) {
                try {
                    Wbo modelWbo = wbo.GetModelWbo();
                    modelWbo.UserId = UserId;

                    var wboToUpdate = (from w in context.Wbos
                                       where w.UserId == UserId &&
                                             w.Collection == modelWbo.Collection &&
                                             w.Id == modelWbo.Id
                                       select w).SingleOrDefault();

                    if (wboToUpdate == null) {
                        context.Wbos.Add(modelWbo);
                    } else {
                        wboToUpdate.Modified = modelWbo.Modified;
                        wboToUpdate.SortIndex = modelWbo.SortIndex;
                        wboToUpdate.Payload = modelWbo.Payload;
                        wboToUpdate.PayloadSize = modelWbo.PayloadSize;
                    }

                    context.SaveChanges();
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
        }

        public void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList) {
            if (wboList != null && wboList.Count > 0) {
                using (WeaveContext context = new WeaveContext()) {
                    try {
                        //using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted })) {
                            foreach (WeaveBasicObject wbo in wboList) {
                                try {
                                    Wbo modelWbo = wbo.GetModelWbo();
                                    modelWbo.UserId = UserId;

                                    var wboToUpdate = (from wbos in context.Wbos
                                                        where wbos.UserId == UserId &&
                                                        wbos.Collection == modelWbo.Collection &&
                                                        wbos.Id == modelWbo.Id
                                                        select wbos).SingleOrDefault();

                                    if (wboToUpdate == null) {
                                        context.Wbos.Add(modelWbo);
                                    } else {
                                        wboToUpdate.Modified = modelWbo.Modified;
                                        wboToUpdate.SortIndex = modelWbo.SortIndex;
                                        wboToUpdate.Payload = modelWbo.Payload;
                                        wboToUpdate.PayloadSize = modelWbo.PayloadSize;
                                    }

                                    context.SaveChanges();
                                    resultList.SuccessIds.Add(wbo.Id);
                                } catch (UpdateException ex) {
                                    if (wbo.Id != null) {
                                        resultList.FailedIds[wbo.Id] = new Collection<string> { ex.Message };
                                    }
                                }
                            }

                        //    transaction.Complete();
                        //}

                    } catch (EntityException x) {
                        RaiseLogEvent(this, x.Message, LogType.Error);
                        throw new WeaveException("Database unavailable.", 503);
                    }
                }
            }
        }

        public bool DeleteWbo(string id, string collection) {
            bool result = false;
            int coll = WeaveCollectionDictionary.GetKey(collection);
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var wboToDelete = (from wbo in context.Wbos
                                       where wbo.UserId == UserId &&
                                             wbo.Collection == coll &&
                                             wbo.Id == id
                                       select wbo).SingleOrDefault();

                    if (wboToDelete != null) {
                        context.Wbos.Remove(wboToDelete);

                        int x = context.SaveChanges();

                        if (x != 0) {
                            result = true;
                        }
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }

                return result;
            }
        }

        public void DeleteWboList(string collection, string id, string newer, string older, string sort,
                                  string limit, string offset, string ids, string indexAbove, string indexBelow) {

            int coll = WeaveCollectionDictionary.GetKey(collection);
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var wbosToDelete = from wbo in context.Wbos
                                       where wbo.UserId == UserId && wbo.Collection == coll
                                       select wbo;

                    if (id != null) {
                        wbosToDelete = wbosToDelete.Where(p => p.Id == id);
                    }

                    if (ids != null) {
                        string[] idArray = ids.Split(new[] { ',' });
                        if (idArray.Length > 0) {
                            wbosToDelete = wbosToDelete.Where(p => idArray.Contains(p.Id));
                        }
                    }

                    if (indexAbove != null) {
                        int iabove = Convert.ToInt32(indexAbove);
                        wbosToDelete = wbosToDelete.Where(p => p.SortIndex > iabove);
                    }

                    if (indexBelow != null) {
                        int ibelow = Convert.ToInt32(indexBelow);
                        wbosToDelete = wbosToDelete.Where(p => p.SortIndex < ibelow);
                    }

                    if (newer != null) {
                        double dnewer = Convert.ToDouble(newer);
                        wbosToDelete = wbosToDelete.Where(p => p.Modified > dnewer);
                    }

                    if (older != null) {
                        double dolder = Convert.ToDouble(older);
                        wbosToDelete = wbosToDelete.Where(p => p.Modified < dolder);
                    }

                    switch (sort) {
                        case "index":
                            wbosToDelete.OrderByDescending(wbo => wbo.SortIndex);
                            break;
                        case "newest":
                            wbosToDelete.OrderByDescending(wbo => wbo.Modified);
                            break;
                        case "oldest":
                            wbosToDelete.OrderBy(wbo => wbo.Modified);
                            break;
                    }

                    int lim;
                    if (limit != null && Int32.TryParse(limit, out lim) && lim > 0) {
                        int off;
                        if (offset != null && Int32.TryParse(limit, out off) && off > 0) {
                            wbosToDelete = wbosToDelete.Take(lim).Skip(off);
                        } else {
                            wbosToDelete = wbosToDelete.Take(lim);
                        }
                    }

                    foreach (var wboToDelete in wbosToDelete) {
                        context.Wbos.Remove(wboToDelete);
                    }

                    context.SaveChanges();
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
        }

        public WeaveBasicObject RetrieveWbo(string collection, string id) {
            WeaveBasicObject wbo = new WeaveBasicObject();

            int coll = WeaveCollectionDictionary.GetKey(collection);
            using (WeaveContext context = new WeaveContext()) {
                try {
                    var wboToGet = (from w in context.Wbos
                                    where w.UserId == UserId && w.Collection == coll && w.Id == id
                                    select w).SingleOrDefault();

                    wbo.Id = id;
                    wbo.Collection = collection;
                    wbo.Modified = wboToGet.Modified;
                    wbo.SortIndex = wboToGet.SortIndex;
                    wbo.Payload = wboToGet.Payload;
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return wbo;
        }

        public IList<WeaveBasicObject> RetrieveWboList(string collection, string id, bool full, string newer, string older, string sort, string limit, string offset,
                                                       string ids, string indexAbove, string indexBelow) {
            IList<WeaveBasicObject> wboList = new List<WeaveBasicObject>();
            int coll = WeaveCollectionDictionary.GetKey(collection);

            using (WeaveContext context = new WeaveContext()) {
                try {
                    var wbosToGet = from w in context.Wbos
                                    where w.UserId == UserId && w.Collection == coll
                                    select w;

                    if (id != null) {
                        wbosToGet = wbosToGet.Where(p => p.Id == id);
                    }

                    if (ids != null) {
                        string[] idArray = ids.Split(new[] { ',' });
                        if (idArray.Length > 0) {
                            wbosToGet = wbosToGet.Where(p => idArray.Contains(p.Id));
                        }
                    }

                    if (indexAbove != null) {
                        int iabove = Convert.ToInt32(indexAbove);
                        wbosToGet = wbosToGet.Where(p => p.SortIndex > iabove);
                    }

                    if (indexBelow != null) {
                        int ibelow = Convert.ToInt32(indexBelow);
                        wbosToGet = wbosToGet.Where(p => p.SortIndex < ibelow);
                    }

                    if (newer != null) {
                        double dnewer = Convert.ToDouble(newer);
                        wbosToGet = wbosToGet.Where(p => p.Modified > dnewer);
                    }

                    if (older != null) {
                        double dolder = Convert.ToDouble(older);
                        wbosToGet = wbosToGet.Where(p => p.Modified < dolder);
                    }

                    switch (sort) {
                        case "index":
                            wbosToGet.OrderByDescending(wbo => wbo.SortIndex);
                            break;
                        case "newest":
                            wbosToGet.OrderByDescending(wbo => wbo.Modified);
                            break;
                        case "oldest":
                            wbosToGet.OrderBy(wbo => wbo.Modified);
                            break;
                    }

                    int lim;
                    if (limit != null && Int32.TryParse(limit, out lim) && lim > 0) {
                        int off;
                        if (offset != null && Int32.TryParse(limit, out off) && off > 0) {
                            wbosToGet = wbosToGet.Take(lim).Skip(off);
                        } else {
                            wbosToGet = wbosToGet.Take(lim);
                        }
                    }

                    foreach (Wbo wboToGet in wbosToGet) {
                        WeaveBasicObject wbo = new WeaveBasicObject();
                        if (full) {
                            wbo.Id = id;
                            wbo.Collection = collection;
                            wbo.Id = wboToGet.Id;
                            wbo.Modified = wboToGet.Modified;
                            wbo.SortIndex = wboToGet.SortIndex;
                            wbo.Payload = wboToGet.Payload;
                        } else {
                            wbo.Id = id;
                        }

                        wboList.Add(wbo);
                    }
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }

            return wboList;
        }
        #endregion

        #region Admin
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

        public void ChangePassword(string password) {
            if (String.IsNullOrEmpty(password)) {
                throw new WeaveException("3", 404);
            }

            using (WeaveContext context = new WeaveContext()) {
                string hash = HashString(password);
                try {
                    var userToGet = (from u in context.Users
                                     where u.UserId == UserId
                                     select u).SingleOrDefault();

                    userToGet.Md5 = hash;

                    context.SaveChanges();
                } catch (EntityException x) {
                    RaiseLogEvent(this, x.Message, LogType.Error);
                    throw new WeaveException("Database unavailable.", 503);
                }
            }
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
        #endregion
    }
}