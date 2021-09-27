using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class UserRolesDTO
    {
      [Required]
      public string Id { get; set; }
      [Required]
      public string Email { get; set; }
      [Required]
      public string UserName { get; set; }
      [Required]
      public List<string> Roles { get; set; }
    }
}
