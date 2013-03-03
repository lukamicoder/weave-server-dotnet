namespace WeaveCore.Models {
    public class User {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Payload { get; set; }
        public double DateMin { get; set; }
        public double DateMax { get; set; }
    }
}