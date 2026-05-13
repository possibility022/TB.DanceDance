using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Server.AspNetCore;
using TB.Auth.Web.Endpoints.Handlers;
using TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

namespace TB.Auth.Web.Endpoints;

public static class Endpoints
{
    extension(WebApplication app)
    {
        public void MapDevelopmentEndpoints()
        {
            app.MapGet("dev/login", GetDevLoginHandler.Handle);
            app.MapPost("dev/login", PostDevLoginHandler.HandleAsync);
            app.MapGet("dev/users/new", GetNewUserHandler.Handle);
            app.MapPost("dev/users/new", PostNewUserHandler.HandleAsync);
            app.MapGet("dev/logout", () => Results.Content(new HtmlBuilder().BuildDevLogoutHtml(), "text/html"));
            app.MapPost("dev/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect($"/dev/login?message={Uri.EscapeDataString("Logged out.")}");
            });
        }

        public void MapEndpoints(bool googleEnabled, bool allowPasswordLogin = false)
        {
            app.ConfigureConnectAuthorize(googleEnabled, allowPasswordLogin);
            app.MapPost("connect/token", TokenHandler.HandleAsync);
            app.MapGet("policy/dancedanceapp", () => Results.Content("""
                <!DOCTYPE html>
                <html lang="pl">
                <head><meta charset="UTF-8"><title>Polityka Prywatności</title>
                <style>body{font-family:sans-serif;max-width:800px;margin:40px auto;padding:0 20px;line-height:1.6}</style>
                </head>
                <body>
                <p><strong>Polityka prywatności dla aplikacji Dance Dance</strong></p>
                <p><strong>Ostatnia aktualizacja: Ostatnia aktualizacja: 01.01.2024</strong></p>
                <strong>Wprowadzenie</strong>
                <p>Dance Dance App (dalej nazywane &quot;Aplikacją&quot;) zobowiązuje się do ochrony prywatności użytkowników. Niniejsza Polityka Prywatności określa, w jaki sposób gromadzemy, używamy, ujawniamy i chronimy informacje osobiste. Korzystając z Aplikacji, zgadzasz się na postanowienia tej Polityki Prywatności.</p>
                <strong>Gromadzenie i Rodzaje Danych Osobowych</strong>
                <p>Aplikacja może gromadzić następujące rodzaje danych osobowych:</p>
                <ul>
                    <li>Dane identyfikacyjne (np. adres e-mail)</li>
                    <li><p>Dane logowania (np. adres IP, informacje o przeglądarce)</p></li>
                    <li><p><strong>Cel Gromadzenia Danych</strong></p></li>
                </ul>
                <p>Dane osobowe są gromadzone w celu:</p>
                <ul>
                    <li>Dostarczania i ulepszania usług Aplikacji</li>
                    <li>Personalizowania treści i doświadczenia użytkownika</li>
                    <li><p>Zarządzania kontem użytkownika</p></li>
                    <li><p><strong>Podstawa Prawna</strong></p></li>
                </ul>
                <p>Przetwarzanie danych identyfikujących jest niezbędne do wykonania świadczenia usługi oraz opiera się na zgodzie użytkownika.</p>
                <strong>Administrator Danych osobowych</strong>
                <p>Administratorem danych jest osoba prywatna Tomasz Bąk - Kontakt: &#116;&#111;&#109;&#97;&#115;&#122;&#98;&#97;&#107;&#107;&#64;&#112;&#114;&#111;&#116;&#111;&#110;&#109;&#97;&#105;&#108;&#46;&#99;&#111;&#109;</p>
                <strong>Okres przechowywania danych</strong>
                <p>Dane są przechowywane od momentu zalogowania się do aplikacji do momentu zgłoszenia ich usunięcia. Administrator ma czas do dwóch tygodni na usunięcie danych z systemu po otrzymaniu zgłoszenia.</p>
                <strong>Ujawnianie Danych Osobowych</strong>
                <p>Dane osobowe mogą być udostępniane tylko w zakresie niezbędnym do realizacji celów opisanych w punkcie 3. Mogą być udostępniane podmiotom trzecim zgodnie z obowiązującymi przepisami prawa.</p>
                <strong>Ochrona Danych Osobowych</strong>
                <p>Aplikacja podejmuje wszelkie niezbędne środki bezpieczeństwa, aby chronić dane osobowe przed nieuprawnionym dostępem, utratą lub zmianą.</p>
                <strong>Prawa Użytkowników</strong>
                <p>Użytkownicy mają prawo dostępu do swoich danych osobowych, ich poprawiania, usuwania lub ograniczania przetwarzania. Mają także prawo do wniesienia skargi do organu nadzorczego.</p>
                <strong>Cookies i Technologie Śledzenia</strong>
                <p>Aplikacja używa plików cookies w celu autoryzacji użytkownika i świadczenia usług.</p>
                <strong>Zmiany w Polityce Prywatności</strong>
                <p>Polityka Prywatności może być okresowo aktualizowana. W przypadku istotnych zmian, użytkownicy zostaną powiadomieni.</p>
                <strong>Lokalizacja</strong>
                <p>Dane są przechowywane na serwerach zlokalizowanych w europie. </p>
                <strong>Kontakt</strong>
                <strong>Aby usunąć wszystkie dane przechowywane na Twój temat, skontaktuj się z administratorem z maila który wykorzustujesz do logowania.</strong>
                <p>W przypadku pytań dotyczących Polityki Prywatności, prosimy o kontakt pod adresem &#116;&#111;&#109;&#97;&#115;&#122;&#98;&#97;&#107;&#107;&#64;&#112;&#114;&#111;&#116;&#111;&#110;&#109;&#97;&#105;&#108;&#46;&#99;&#111;&#109;.</p>
                </body></html>
                """, "text/html"));
            app.MapMethods("callback/login/google", 
                [HttpMethods.Get, HttpMethods.Post],
                GoogleLoginHandler.HandleAsync);
            app.MapMethods("connect/logout", [HttpMethods.Get, HttpMethods.Post], async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.SignOut(
                    properties: new AuthenticationProperties { RedirectUri = "/" },
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            });
        }
        
        private void ConfigureConnectAuthorize(bool googleEnabled, bool allowPasswordLogin)
        {
            const string endpoint = "connect/authorize";
            if (allowPasswordLogin)
            {
                app.MapMethods(endpoint, [HttpMethods.Get, HttpMethods.Post], ConnectAuthorizeHandler.HandleAsync);
                return;
            }
            
            if (!googleEnabled)
            {
                app.MapGet(endpoint,
                    () => Results.BadRequest(
                        "Google provider is not configured. Set Authentication:Google:ClientId and ClientSecret."));
            }
            else
            {
                app.MapMethods(endpoint, [HttpMethods.Get, HttpMethods.Post],
                    ConnectAuthorizeHandler.HandleAsync);
            }
        }
    }
}
