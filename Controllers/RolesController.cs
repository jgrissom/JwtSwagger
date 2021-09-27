using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using JWTSwagger.Authentication;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace JWTExample.Controllers
{
  [Authorize(Roles = "admin")]
  [Produces("application/json")]
  [Route("api/[controller]")]
  [ApiController]
  public class RolesController : Controller
  {
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
      _userManager = userManager;
      _roleManager = roleManager;
    }

    [HttpGet]
    [SwaggerOperation(summary: "Return all roles", null)]
    [SwaggerResponse(200, "Success", typeof(RoleDTO))]
    public IActionResult Get() => Ok(_roleManager.Roles.OrderBy(r => r.Name).Select(r => new RoleDTO { Id = r.Id, Name = r.Name }));

    [HttpGet("{rolename}")]
    [SwaggerOperation(summary: "Return specific role & associated users", null)]
    [SwaggerResponse(200, "Success", typeof(RoleUsersDTO))]
    public async Task<IActionResult> Get([FromRoute] string rolename)
    {
      // Check if role exists
      var role = await _roleManager.FindByNameAsync(rolename);
      if (role == null)
        return NotFound(new { Message = "Role Not Found"});

      // determine list of users assigned to role
      List<string> UsersAssigned = new List<string>();
      foreach(ApplicationUser user in _userManager.Users.ToList())
      {
        if (await _userManager.IsInRoleAsync(user, role.Name))
        {
          UsersAssigned.Add(user.UserName);
        }
      }
      return Ok(new RoleUsersDTO { Id = role.Id, Name = role.Name , Users = UsersAssigned });
    }

    [HttpPost]
    [Route("create")]
    [SwaggerOperation(summary: "Create role", null)]
    [SwaggerResponse(204, "Role created", null)]
    public async Task<IActionResult> Create([FromBody] CreateRole model)
    {
      // Check if role already exists
      var role = await _roleManager.FindByNameAsync(model.RoleName);
      if (role != null)
        return Conflict(new { Message = $"{model.RoleName} is a duplicate Role" });

      var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to create role" });
      return NoContent();
    }

    [HttpDelete("{rolename}")]
    [SwaggerOperation(summary: "Delete role", null)]
    [SwaggerResponse(204, "Role deleted", null)]
    public async Task<IActionResult> Delete([FromRoute] string rolename)
    {
      // check for existence of role
      var role = await _roleManager.FindByNameAsync(rolename);
      if (role == null)
        return NotFound(new { Message = "Role Not Found"});

      var result = await _roleManager.DeleteAsync(role);
      if (!result.Succeeded)
        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to delete role" });

      return NoContent();
    }
  }
}
