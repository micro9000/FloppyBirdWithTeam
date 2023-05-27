namespace FloppyBird.DomainModels
{
    public class SessionScorecard
    {
        public SessionScorecard(List<User> users, ScoreCountingType scoreCountingType)
		{
			SessionScoreCountingType = (int)scoreCountingType;
			if (users != null)
			{
				Users = users;
				if (scoreCountingType == ScoreCountingType.CountEveryScore)
					CountEveryScore();
				else
					CountHighestScoreOnly();
			}
        }

		public int SessionScoreCountingType { get; set; }
		public List<User> Users { get; private set; }
        public List<User> Avengers { get; private set; }
        public int AvengersOverallScore { get; private set; }
        public List<User> JusticeLeague { get; private set; }
        public int JusticeLeagueOverallScore { get; private set; }

        private void CountEveryScore ()
		{
				Avengers = Users.Where(x => x.Group == Groups.Avengers).OrderByDescending(x => x.TotalScore).ToList();
				JusticeLeague = Users.Where(x => x.Group == Groups.JusticeLeague).OrderByDescending(x => x.TotalScore).ToList();
				AvengersOverallScore = Avengers.Sum(x => x.TotalScore);
				JusticeLeagueOverallScore = JusticeLeague.Sum(x => x.TotalScore);
		}

		private void CountHighestScoreOnly ()
		{
				Avengers = Users.Where(x => x.Group == Groups.Avengers).OrderByDescending(x => x.HighScore).ToList();
				JusticeLeague = Users.Where(x => x.Group == Groups.JusticeLeague).OrderByDescending(x => x.HighScore).ToList();
				AvengersOverallScore = Avengers.Sum(x => x.HighScore);
				JusticeLeagueOverallScore = JusticeLeague.Sum(x => x.HighScore);
		}
	}
}
