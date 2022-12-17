using System.ComponentModel.DataAnnotations;

namespace FloppyBird.Dtos
{
    public class CreateNewSessionParams
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public int NumberOfMinutes { get; set; }

        [Required]
        public ScoreCountingType ScoreCountingType { get; set; }
  }
}
