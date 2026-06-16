using System.Net;

namespace TB.Auth.Web;

public class HtmlBuilder
{
    public string BuildDevLoginHtml(string returnUrl, string? error, string? message)
    {
        var errorHtml = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<p>{WebUtility.HtmlEncode(error)}</p>";

        var messageHtml = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : $"<p>{WebUtility.HtmlEncode(message)}</p>";

        var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);

        return $$"""
                 <!doctype html>
                 <html>
                 <body>
                 <h1>Dev Login</h1>
                 {{errorHtml}}
                 {{messageHtml}}
                 <form method="post" action="/dev/login">
                   <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
                   <label>Login</label>
                   <input name="login" autocomplete="username" />
                   <br />
                   <label>Password</label>
                   <input type="password" name="password" autocomplete="current-password" />
                   <br />
                   <button type="submit">Log in</button>
                 </form>
                 <p><a href="/dev/users/new">Create test user</a></p>
                 <p><a href="/dev/logout">Logout</a></p>
                 </body>
                 </html>
                 """;
    }
    
    public string BuildDevLogoutHtml()
    {
        return """
               <!doctype html>
               <html>
               <body>
               <h1>Dev Logout</h1>
               <form method="post" action="/dev/logout">
                 <button type="submit">Logout</button>
               </form>
               </body>
               </html>
               """;
    }
    
    public string BuildDevCreateUserHtml(string? error, string? message)
    {
        var errorHtml = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<p>{WebUtility.HtmlEncode(error)}</p>";

        var messageHtml = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : $"<p>{WebUtility.HtmlEncode(message)}</p>";

        return $$"""
                 <!doctype html>
                 <html>
                 <body>
                 <h1>Create Dev User</h1>
                 {{errorHtml}}
                 {{messageHtml}}
                 <form method="post" action="/dev/users/new">
                   <label>Login</label>
                   <input name="login" autocomplete="username" />
                   <br />
                   <label>Email (optional)</label>
                   <input name="email" autocomplete="email" />
                   <br />
                   <label>First name (optional)</label>
                   <input name="firstName" autocomplete="given-name" />
                   <br />
                   <label>Last name (optional)</label>
                   <input name="lastName" autocomplete="family-name" />
                   <br />
                   <label>Password</label>
                   <input type="password" name="password" autocomplete="new-password" />
                   <br />
                   <button type="submit">Create user</button>
                 </form>
                 <p><a href="/dev/login">Back to login</a></p>
                 </body>
                 </html>
                 """;
    }
}