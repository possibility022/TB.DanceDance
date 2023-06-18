namespace TB.DanceDance.Identity.IdentityResources;

public static class DanceDanceResources
{
    public static class WestCoastSwing
    {
        public static class Scopes
        {
            public const string ReadScope = "tbdancedanceapi.read";
            public const string WriteScope = "tbdancedanceapi.write";
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
            this.Description = "Information related to West Coast Swing";
            this.Name = WestCoastSwing.IdentityResource;
            this.Required = true;
            this.UserClaims.Add(WestCoastSwing.Claims.Groups);
        }
    }
}