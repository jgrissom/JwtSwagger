using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class UserDTO
    {
      [Required]
      public string Id { get; set; }
      [Required, EmailAddress]
      public string Email { get; set; }
      [Required]
      public string UserName { get; set; }
    }
}
