namespace TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

public class GetDevLoginHandler
{
    public static IResult Handle(HttpContext context)
    {
        var routeBuilder = new RouteBuilder(context);
        var returnUrl = routeBuilder.GetValidatedReturnUrl(context.Request.Query["returnUrl"].ToString());
        var error = context.Request.Query["error"].ToString();
        var message = context.Request.Query["message"].ToString();

        var htmlBuilder = new HtmlBuilder();
        
        var html = htmlBuilder.BuildDevLoginHtml(returnUrl, error, message);
        
        return Results.Content(html, "text/html");
    }
}