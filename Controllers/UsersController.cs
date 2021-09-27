using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JWTSwagger.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace JWTSwagger.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UsersController : ControllerBase
  {
    // access UserManager w/ dependecy injection
    private readonly UserManager<ApplicationUser> _userManager;
    public UsersController(UserManager<ApplicationUser> userManager)
    {
      _userManager = userManager;
     }

    [HttpGet]
    public IActionResult Get() => Ok(_userManager.Users.Select(u => 
      new UserDTO { 
        Id = u.Id, 
        Email = u.Email, 
        UserName = u.UserName 
      }).OrderBy(u => u.UserName));

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] UserRegister model)
    {
      // check for existence of user
      var userExists = await _userManager.FindByEmailAsync(model.Email);
      if (userExists != null) // duplicate email
        return Conflict(new { message = $"{model.Email} is a duplicate Email" });
      userExists = await _userManager.FindByNameAsync(model.Username);
      if (userExists != null) // duplicate username
        return Conflict(new { message = $"{model.Username} is a duplicate Username" });

      ApplicationUser user = new ApplicationUser()
      {
        Email = model.Email,
        SecurityStamp = Guid.NewGuid().ToString(),
        UserName = model.Username
      };
      var result = await _userManager.CreateAsync(user, model.Password);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError);

      return NoContent();
    }
  }
}
