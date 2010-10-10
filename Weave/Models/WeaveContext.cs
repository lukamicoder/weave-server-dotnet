using System.Data.Objects;

namespace Weave.Models {
    class WeaveContext : ObjectContext {
        public ObjectSet<User> Users { get; private set; }
        public ObjectSet<Wbo> Wbos { get; private set; }

        public WeaveContext(string connectionString)
            : base(connectionString, "WeaveEntities") {
            Users = CreateObjectSet<User>();
            Wbos = CreateObjectSet<Wbo>(); 
            
            this.Connection.ConnectionString = connectionString;     
        }
    }
}