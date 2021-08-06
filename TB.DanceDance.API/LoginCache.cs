using System;
using System.Collections.Generic;
using System.Linq;

namespace TB.DanceDance.API
{
    // todo delete and replace
    public static class LoginCache
    {
        private static Dictionary<string, DateTime> randomKeys = new Dictionary<string, DateTime>();

        private static object @lock = new object();

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateNewHash()
        {
            return RandomString(20);
        }

        public static void AddAsLoggedIn(string hash)
        {
            lock (@lock)
            {
                randomKeys.Add(hash, DateTime.Now.AddHours(5));

                var toRemove = new HashSet<string>();

                foreach (var dateTime in randomKeys)
                {
                    if (dateTime.Value < DateTime.Now)
                    {
                        toRemove.Add(dateTime.Key);
                    }
                }

                foreach (var val in toRemove)
                {
                    randomKeys.Remove(val);
                }
            }
        }

        public static bool CheckIfLoggedIn(string hash)
        {
            lock (@lock)
            {
                if (randomKeys.ContainsKey(hash))
                {
                    if (randomKeys[hash] > DateTime.Now)
                        return true;
                }
            }

            return false;
        }
    }
}
