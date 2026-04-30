using Microsoft.AspNetCore.Identity;

namespace TB.Auth.Web.Identity;

public class User : IdentityUser
{

}

public class Role : IdentityRole<string> { }