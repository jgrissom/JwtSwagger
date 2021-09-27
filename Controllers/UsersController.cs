using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JWTSwagger.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace JWTSwagger.Controllers
{
  [Produces("application/json")]
  [Route("api/[controller]")]
  [ApiController]
  public class UsersController : ControllerBase
  {
    // access UserManager w/ dependecy injection
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    public UsersController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
      _userManager = userManager;
      _configuration = configuration;
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
    [Route("login")]
    [SwaggerOperation(summary: "User login", null)]
    [SwaggerResponse(200, "Success", typeof(UserTokenDTO))]
    public async Task<IActionResult> Login([FromBody] UserLogin model)
    {
      var user = await _userManager.FindByNameAsync(model.Username);
      if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
      {
        var userRoles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
          new Claim(JwtRegisteredClaimNames.Email, user.Email),
          new Claim(JwtRegisteredClaimNames.Jti, user.Id),
          new Claim("username", user.UserName),
        };

        foreach (var userRole in userRoles)
        {
          authClaims.Add(new Claim("roles", userRole));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
          issuer: _configuration["JWT:ValidIssuer"],
          audience: _configuration["JWT:ValidAudience"],
          expires: DateTime.Now.AddHours(3),
          claims: authClaims,
          signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new UserTokenDTO
        {
          Token = new JwtSecurityTokenHandler().WriteToken(token),
          Expiration = Convert.ToString(token.ValidTo)
        });
      }
      return Unauthorized(new { Message = "Login falied" });
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

    [HttpDelete("{username}")]
    [SwaggerOperation(summary: "Delete user", null)]
    [SwaggerResponse(204, "User deleted", null)]
    public async Task<IActionResult> Delete(string username)
    {
      // check for existence of user
      var user = await _userManager.FindByNameAsync(username);
      if (user == null)
        return NotFound(new { Message = "User Not Found"});

      var result = await _userManager.DeleteAsync(user);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "User not deleted" });

      return NoContent();
    }
  }
}
