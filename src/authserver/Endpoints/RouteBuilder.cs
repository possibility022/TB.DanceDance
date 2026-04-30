namespace TB.Auth.Web;

public class RouteBuilder
{
    private readonly HttpContext context;

    public RouteBuilder(HttpContext context)
    {
        this.context = context;
    }
    
    public string GetValidatedReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Relative, out var relativeUri) && !relativeUri.IsAbsoluteUri)
        {
            if (returnUrl.StartsWith('/'))
            {
                return returnUrl;
            }

            return "/";
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
        {
            return "/";
        }

        var requestHost = context.Request.Host;
        var sameHost = string.Equals(absoluteUri.Host, requestHost.Host, StringComparison.OrdinalIgnoreCase);
        var samePort = absoluteUri.Port == (requestHost.Port ?? (context.Request.IsHttps ? 443 : 80));
        var sameScheme = string.Equals(absoluteUri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase);

        return sameHost && samePort && sameScheme
            ? absoluteUri.ToString()
            : "/";
    }
}