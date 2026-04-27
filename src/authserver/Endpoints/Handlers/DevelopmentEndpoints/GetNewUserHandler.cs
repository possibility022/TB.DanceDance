namespace TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

public static class GetNewUserHandler
{
    public static IResult Handle(HttpContext context)
    {
        var error = context.Request.Query["error"].ToString();
        var message = context.Request.Query["message"].ToString();
        
        var htmlBuilder = new HtmlBuilder();

        return Results.Content(htmlBuilder.BuildDevCreateUserHtml(error, message), "text/html");
    }
}