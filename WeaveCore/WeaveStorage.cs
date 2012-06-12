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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using WeaveCore.Models;
using System.Data.SqlServerCe;

namespace WeaveCore {
    class WeaveStorage : WeaveLogEventBase {
        public int UserId { get; private set; }
        double _timeNow;

        public WeaveStorage() {
            Database.SetInitializer(new WeaveDbInitializer());

            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            _timeNow = Math.Round(ts.TotalSeconds, 2);
        }

        public bool AuthenticateUser(string userName, string password) {
            bool result = false;

            using (WeaveContext context = new WeaveContext()) {
                string hash = WeaveHelper.ConvertToHash(password);

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
                                     where wbos.UserId == UserId && wbos.Collection == coll && wbos.Ttl > _timeNow
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
                              where w.UserId == UserId && w.Ttl > _timeNow
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
                    var total = (from users in
                                     (from u in context.Users
                                      join w in context.Wbos on u.UserId equals w.UserId
                                      where u.UserId == UserId && w.Ttl > _timeNow
                                      select new { w.PayloadSize, u.UserId })
                                 group users by new { users.UserId } into g
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
                              where w.UserId == UserId && w.Ttl > _timeNow
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
                    Wbo modelWbo = wbo.ToModelWbo();
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
                        wboToUpdate.Ttl = modelWbo.Ttl;
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
                        foreach (WeaveBasicObject wbo in wboList) {
                            try {
                                Wbo modelWbo = wbo.ToModelWbo();
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
                                    wboToUpdate.Ttl = modelWbo.Ttl;
                                    wboToUpdate.SortIndex = modelWbo.SortIndex;
                                    wboToUpdate.Payload = modelWbo.Payload;
                                    wboToUpdate.PayloadSize = modelWbo.PayloadSize;
                                }
                                resultList.SuccessIds.Add(wbo.Id);
                            } catch (UpdateException ex) {
                                if (wbo.Id != null) {
                                    resultList.FailedIds[wbo.Id] = new Collection<string> { ex.Message };
                                }
                            }
                        }

                        context.SaveChanges();
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
                            wbosToDelete = wbosToDelete.OrderByDescending(wbo => wbo.SortIndex);
                            break;
                        case "newest":
                            wbosToDelete = wbosToDelete.OrderByDescending(wbo => wbo.Modified);
                            break;
                        case "oldest":
                            wbosToDelete = wbosToDelete.OrderBy(wbo => wbo.Modified);
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
                                    where w.UserId == UserId && w.Collection == coll && w.Id == id && w.Ttl > _timeNow
                                    select w).SingleOrDefault();

                    wbo.Id = id;
                    wbo.Collection = collection;
                    wbo.Modified = wboToGet.Modified;
                    wbo.SortIndex = wboToGet.SortIndex;
                    wbo.Payload = wboToGet.Payload;
                    wbo.Ttl = wboToGet.Ttl;
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
                                    where w.UserId == UserId && w.Collection == coll && w.Ttl > _timeNow
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
                            wbosToGet = wbosToGet.OrderByDescending(wbo => wbo.SortIndex);
                            break;
                        case "newest":
                            wbosToGet = wbosToGet.OrderByDescending(wbo => wbo.Modified);
                            break;
                        case "oldest":
                            wbosToGet = wbosToGet.OrderBy(wbo => wbo.Modified);
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
                            wbo.Ttl = wboToGet.Ttl;
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
    }
}