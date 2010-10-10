using System;
using System.Collections.Generic;

namespace Weave.Models {
    public partial class User {
        public Int64 UserId { get; set; }
        public string UserName { get; set; }
        public string Md5 { get; set; }

        public ICollection<Wbo> Wbos { get; set; }
    }
}