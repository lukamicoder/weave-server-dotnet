using System;

namespace Weave.Models {
    public partial class Wbo {
        public Int64 UserId { get; set; }
        public string Id { get; set; }
        public Int16 Collection { get; set; }
        public string ParentId { get; set; }
        public string PredecessorId { get; set; }
        public Double? Modified { get; set; }
        public Int32? SortIndex { get; set; }
        public string Payload { get; set; }
        public Int32 PayloadSize { get; set; }

        public User User { get; set; }
    }
}