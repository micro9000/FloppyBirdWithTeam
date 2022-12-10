namespace FloppyBird.DomainModels
{
    public class SessionUser
    {
        public Guid SessionToken { get; set; }
        public List<User> Users { get; set; }

        public List<User> Avengers() => this.Users?.Where(x => x.Group == Groups.Avengers).OrderByDescending(x => x.HighScore()).ToList();
        public int AvengersOverallScore() => Avengers().Sum(x => x.HighScore());

        public List<User> JusticeLeague() => this.Users?.Where(x => x.Group == Groups.JusticeLeague).OrderByDescending(x => x.HighScore()).ToList();
        public int JusticeLeagueOverallScore() => JusticeLeague().Sum(x => x.HighScore());
    }
}
