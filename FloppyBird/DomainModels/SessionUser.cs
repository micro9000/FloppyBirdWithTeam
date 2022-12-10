namespace FloppyBird.DomainModels
{
    public class SessionUser
    {
        public Guid SessionToken { get; set; }
        public List<User> Users { get; set; }

        public List<User> Avengers() => this.Users?.Where(x => x.Group == Groups.Avengers).ToList();
        public List<User> JusticeLeague() => this.Users?.Where(x => x.Group == Groups.JusticeLeague).ToList();
    }
}
