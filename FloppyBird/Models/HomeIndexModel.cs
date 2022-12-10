using FloppyBird.DomainModels;

namespace FloppyBird.Models
{
    public class HomeIndexModel
    {
        public bool CurrentUserIsTheGameMaster { get; set; }
        public User User { get; set; }
        public Session CurrentSession { get; set; }
        public SessionUser SessionUser { get; set; }
        public string BaseUrl { get; set; }
    }
}
