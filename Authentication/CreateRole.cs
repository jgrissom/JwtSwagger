using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class CreateRole
    {
      [Required(ErrorMessage = "Role name is required")]
      public string RoleName { get; set; }
    }
}
