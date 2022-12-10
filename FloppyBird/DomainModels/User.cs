namespace FloppyBird.DomainModels
{
    public class User
    {
        public Guid AccountToken { get; set; }
        public string Name { get; set; }
        public Guid GroupToken { get; set; }
        public List<UserScore> Scores { get; set; }
        public Groups Group { get; set; }
    }
}
