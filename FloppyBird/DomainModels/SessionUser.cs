namespace FloppyBird.DomainModels
{
    public class SessionUser
    {
        public Guid SessionToken { get; set; }
        public List<User> Users { get; set; }
    }
}
