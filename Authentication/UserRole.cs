using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class UserRole
    {
      [Required(ErrorMessage = "Username is required")]
      public string Username { get; set; }

      [Required(ErrorMessage = "Role name is required")]
      public string RoleName { get; set; }
    }
}
