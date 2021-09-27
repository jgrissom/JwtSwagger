using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JWTSwagger.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Swashbuckle.AspNetCore.Annotations;

namespace JWTSwagger.Controllers
{
  [Produces("application/json")]
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
    [SwaggerOperation(summary: "Return all users", null)]
    [SwaggerResponse(200, "Success", typeof(UserDTO))]
    public IActionResult Get() => Ok(_userManager.Users.Select(u => 
      new UserDTO { 
        Id = u.Id, 
        Email = u.Email, 
        UserName = u.UserName 
      }).OrderBy(u => u.UserName));

    [HttpPost]
    [Route("register")]
    [SwaggerOperation(summary: "User registration", null)]
    [SwaggerResponse(204, "User registered", null)]
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

    [HttpPost]
    [Route("resetpassword")]
    [SwaggerOperation(summary: "Reset user password", null)]
    [SwaggerResponse(204, "User password reset", null)]
    public async Task<IActionResult> ResetPassword([FromBody] UserLogin model)
    {
      // check for existence of user
      var user = await _userManager.FindByNameAsync(model.Username);
      if (user == null) 
        return NotFound(new { Message = "User Not Found"});

      string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
      var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Password not reset" });

      return NoContent();
    }
  }
}
