namespace FloppyBird.DomainModels
{
    public class SessionScorecard
    {
        public SessionScorecard(List<User> users)
        {
            this.Users = users;
        }

        public List<User> Users { get; private set; }
        public List<User> Avengers() => this.Users?.Where(x => x.Group == Groups.Avengers).OrderByDescending(x => x.HighScore()).ToList();
        public int AvengersOverallScore() => Avengers().Sum(x => x.HighScore());

        public List<User> JusticeLeague() => this.Users?.Where(x => x.Group == Groups.JusticeLeague).OrderByDescending(x => x.HighScore()).ToList();
        public int JusticeLeagueOverallScore() => JusticeLeague().Sum(x => x.HighScore());
    }
}
