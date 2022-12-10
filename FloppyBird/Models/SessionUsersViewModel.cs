using FloppyBird.DomainModels;

namespace FloppyBird.Models
{
    public class SessionUsersViewModel
    {
        public List<User> Avengers { get; set; }
        public int? AvengersOverallScore { get; set; }
        public List<User> JusticeLeague { get; set; }
        public int? JusticeLeagueOverallScore { get; set; }
    }
}
