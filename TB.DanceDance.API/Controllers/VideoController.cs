using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static IdentityServer4.IdentityServerConstants;

namespace TB.DanceDance.API.Controllers;

[Authorize(LocalApi.PolicyName)]
public class VideoController : Controller
{
    [Route("api/video/getsomething")]
    [HttpGet]
    public IEnumerable<string> GetSomething()
    {
        return new[] { "abc", "xyz" };
    }
}
