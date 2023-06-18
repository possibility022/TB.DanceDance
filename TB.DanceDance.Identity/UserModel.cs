using Microsoft.AspNetCore.Identity;

namespace TB.DanceDance.Identity;

public class User : Microsoft.AspNetCore.Identity.IdentityUser
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
