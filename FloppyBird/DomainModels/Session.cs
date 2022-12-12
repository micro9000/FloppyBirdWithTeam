namespace FloppyBird.DomainModels
{
    public class Session
    {
        public Guid SessionToken { get; set; }
        public string Name { get; set; }
        public DateTime StartedAt { get; set; }
        public Guid GameMasterAccountToken { get; set; }
        public List<User> Users { get; set; } = new List<User>();
    }
}
