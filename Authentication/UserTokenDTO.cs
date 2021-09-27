using System.ComponentModel.DataAnnotations;

namespace JWTSwagger.Authentication
{
    public class UserTokenDTO
    {
      [Required]
      public string Token { get; set; }
      [Required]
      public string Expiration { get; set; }
    }
}
