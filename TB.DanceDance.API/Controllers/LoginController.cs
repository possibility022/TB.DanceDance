using System;
using Microsoft.AspNetCore.Mvc;

namespace TB.DanceDance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] UserInfo info)
        {
            if (info.Email != null && info.Password != null
                                   && info.Email.Equals("dancedance@email.com", StringComparison.CurrentCultureIgnoreCase)
                                   && info.Password.Equals("123salsafever123"))
            {
                var hashValue = LoginCache.GenerateNewHash();
                LoginCache.AddAsLoggedIn(hashValue);
                return new OkObjectResult(hashValue);
            }

            return Unauthorized();
        }

        public class UserInfo
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }
    }
}
