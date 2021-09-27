using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JWTSwagger.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

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
    public IActionResult Get() => Ok(_userManager.Users);
  }
}
