using System.ComponentModel.DataAnnotations;

namespace FloppyBird.Dtos
{
    public class CreateNewAccountParams
    {
        [Required]
        public string Username { get; set; }
    }
}
