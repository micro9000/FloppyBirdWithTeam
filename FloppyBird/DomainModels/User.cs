namespace FloppyBird.DomainModels
{
    public class User
    {
        public Guid AccountToken { get; set; }
        public string Name { get; set; }
        public List<int> Scores { get; set; }
        public int HighScore()
        {
            var isValid = this.Scores != null && this.Scores.Count > 0;
            return isValid ? this.Scores.Max() : 0;
        }
        public Groups Group { get; set; } = Groups.NoGroup;
    }
}
