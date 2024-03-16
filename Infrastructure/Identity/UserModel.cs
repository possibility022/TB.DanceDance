using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class User : IdentityUser
{

}

public class UserClaim : IdentityUserClaim<string>
{
}

public class UserLogin : IdentityUserLogin<string>
{

}

public class UserToken : IdentityUserToken<string> { }

public class Role : IdentityRole<string> { }

public class UserRole : IdentityUserRole<string> { }

public class RoleClaim : IdentityRoleClaim<string> { }
