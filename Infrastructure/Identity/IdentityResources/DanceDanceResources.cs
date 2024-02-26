namespace Infrastructure.Identity.IdentityResources;

public static class DanceDanceResources
{
    public static class WestCoastSwing
    {
        public static class Scopes
        {
            public const string ReadScope = "tbdancedanceapi.read";
            public const string WriteScope = "tbdancedanceapi.write";
            public const string WriteConvert = "tbdancedanceapi.convert";
        }

        public static class Claims
        {
            public const string Groups = "wcsgroups";
        }

        public const string IdentityResource = "westcoastswing";

    }

    public class WcsDanceGroups : IdentityServer4.Models.IdentityResource
    {
        public WcsDanceGroups()
        {
            Description = "Information related to West Coast Swing";
            Name = WestCoastSwing.IdentityResource;
            Required = true;
            UserClaims.Add(WestCoastSwing.Claims.Groups);
        }
    }
}