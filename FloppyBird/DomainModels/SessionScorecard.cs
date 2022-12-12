namespace FloppyBird.DomainModels
{
    public class SessionScorecard
    {
        public SessionScorecard(List<User> users)
        {
            if (users != null)
            {
                Users = users;
                Avengers = users.Where(x => x.Group == Groups.Avengers).OrderByDescending(x => x.HighScore).ToList();
                AvengersOverallScore = Avengers.Sum(x => x.HighScore);
                JusticeLeague = users.Where(x => x.Group == Groups.JusticeLeague).OrderByDescending(x => x.HighScore).ToList();
                JusticeLeagueOverallScore = JusticeLeague.Sum(x => x.HighScore);
            }
        }
        public List<User> Users { get; private set; }
        public List<User> Avengers { get; private set; }
        public int AvengersOverallScore { get; private set; }
        public List<User> JusticeLeague { get; private set; }
        public int JusticeLeagueOverallScore { get; private set; }
    }
}
