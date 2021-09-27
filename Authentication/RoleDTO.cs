using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class RoleDTO
    {
      [Required]
      public string Id { get; set; }
      [Required]
      public string Name { get; set; }
    }
}
