using System.ComponentModel.DataAnnotations;

namespace FloppyBird.Dtos
{
    public class ResetSessionParams
    {
        [Required]
        public int NumberOfMinutes { get; set; }

        [Required]
        public ScoreCountingType ScoreCountingType { get; set; }
  }
}
