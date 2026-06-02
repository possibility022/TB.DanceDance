namespace Domain.Entities;

/// <summary>
/// Generates short, URL-safe, Base62-encoded link identifiers.
/// Base62 uses characters: 0-9, A-Z, a-z (62 total characters).
/// With 8 characters: 62^8 = ~218 trillion possible combinations.
/// </summary>
public static class ShortLinkGenerator
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int LinkIdLength = 8;

    /// <summary>
    /// Generates a random 8-character Base62 string suitable for use as a short link ID.
    /// </summary>
    /// <returns>An 8-character Base62 string (e.g., "aB3xK9zQ")</returns>
    public static string GenerateShortLinkId()
    {
        var chars = new char[LinkIdLength];
        var random = Random.Shared;

        for (int i = 0; i < LinkIdLength; i++)
        {
            chars[i] = Base62Chars[random.Next(Base62Chars.Length)];
        }

        return new string(chars);
    }
}
