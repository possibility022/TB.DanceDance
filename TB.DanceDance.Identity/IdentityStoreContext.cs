﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Identity
{
    public class IdentityStoreContext : IdentityDbContext<User, Role, string>
    {

        public IdentityStoreContext(DbContextOptions<IdentityStoreContext> options) : base(options)
        {
            
        }

    }
}