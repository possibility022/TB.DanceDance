using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class User : IdentityUser
{

}

public class Role : IdentityRole<string> { }