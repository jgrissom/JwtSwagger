using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class RoleUsersDTO
    {
      [Required]
      public string Id { get; set; }
      [Required]
      public string Name { get; set; }
      public List<string> Users { get; set; }
    }
}
