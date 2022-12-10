namespace FloppyBird.DomainModels
{
    public class User
    {
        public Guid AccountToken { get; set; }
        public string Name { get; set; }
        public List<int> Scores { get; set; }
        public Groups Group { get; set; } = Groups.NoGroup;
    }
}
