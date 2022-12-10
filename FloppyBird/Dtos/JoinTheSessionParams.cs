using System.ComponentModel.DataAnnotations;

namespace FloppyBird.Dtos
{
    public class JoinTheSessionParams
    {
        [Required]
        public string SessionToken { get; set; }
    }
}
