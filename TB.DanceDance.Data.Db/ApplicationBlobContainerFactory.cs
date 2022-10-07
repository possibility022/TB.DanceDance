﻿using System;

namespace TB.DanceDance.Data.Db
{
    public class ApplicationBlobContainerFactory
    {
        public static string? TryGetConnectionStringFromEnvironmentVariables()
        {
            var connectionString = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString.Blob");
            return connectionString;
        }
    }
}
