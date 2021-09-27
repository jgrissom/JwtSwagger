using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using JWTSwagger.Authentication;
using Swashbuckle.AspNetCore.Annotations;

namespace JWTExample.Controllers
{
  [Produces("application/json")]
  [Route("api/[controller]")]
  [ApiController]
  public class RolesController : Controller
  {
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
    {
      _roleManager = roleManager;
    }

    [HttpGet]
    [SwaggerOperation(summary: "Return all roles", null)]
    [SwaggerResponse(200, "Success", typeof(RoleDTO))]
    public IActionResult Get() => Ok(_roleManager.Roles.OrderBy(r => r.Name).Select(r => new RoleDTO { Id = r.Id, Name = r.Name }));
  }
}
