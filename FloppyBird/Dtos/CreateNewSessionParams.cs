using System.ComponentModel.DataAnnotations;

namespace FloppyBird.Dtos
{
    public class CreateNewSessionParams
    {
        [Required]
        public string Name { get; set; }
    }
}
