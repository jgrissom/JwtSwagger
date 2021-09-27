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
using Microsoft.AspNetCore.Authorization;

namespace JWTSwagger.Controllers
{
  [Authorize(Roles = "admin")]
  [Produces("application/json")]
  [Route("api/[controller]")]
  [ApiController]
  public class UsersController : ControllerBase
  {
    // access UserManager w/ dependecy injection
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
      _userManager = userManager;
      _roleManager = roleManager;
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
    
    [HttpGet("{username}")]
    [SwaggerOperation(summary: "Return specific user & associated roles", null)]
    [SwaggerResponse(200, "Success", typeof(UserRolesDTO))]
    public async Task<IActionResult> Get(String username)
    {
      // check for existence of user
      var user = await _userManager.FindByNameAsync(username);
      if (user == null) 
        return StatusCode(StatusCodes.Status404NotFound);

      var userRoles = (await _userManager.GetRolesAsync(user)).ToList();

      return Ok(new UserRolesDTO {Id = user.Id, UserName = user.UserName, Email = user.Email, Roles = userRoles});
    }

    [HttpPost]
    [Route("register")]
    [AllowAnonymous]
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
    [SwaggerOperation(summary: "Assign user to role", null)]
    [SwaggerResponse(204, "User assigned to role", null)]
    [Route("assignrole")]
    public async Task<IActionResult> AssignUserToRole([FromBody] UserRole model)
    {
      // check for existence of user
      var user = await _userManager.FindByNameAsync(model.Username);
      if (user == null) 
        return StatusCode(StatusCodes.Status404NotFound, new { Status = "Error", Message = "User Not Found."});

      // check for existence of role
      var role = await _roleManager.FindByNameAsync(model.RoleName);
      if (role == null) 
        return StatusCode(StatusCodes.Status404NotFound, new { Status = "Error", Message = "Role Not Found."});

      var result = await _userManager.AddToRoleAsync(user, model.RoleName);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to assign user to role" });

      return NoContent();
    }

    [HttpPost]
    [Route("removerole")]
    [SwaggerOperation(summary: "Remove user from role", null)]
    [SwaggerResponse(204, "User removed from role", null)]
    public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRole model)
    {
      // check for existence of user
      var user = await _userManager.FindByNameAsync(model.Username);
      if (user == null) 
        return StatusCode(StatusCodes.Status404NotFound, new { Status = "Error", Message = "User Not Found."});

      // check for existence of role
      var role = await _roleManager.FindByNameAsync(model.RoleName);
      if (role == null) 
        return StatusCode(StatusCodes.Status404NotFound, new { Status = "Error", Message = "Role Not Found."});

      var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to remove user from role" });

      return NoContent();
    }

    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
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
