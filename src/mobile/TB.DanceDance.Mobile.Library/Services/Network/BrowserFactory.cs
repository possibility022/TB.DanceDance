using Duende.IdentityModel.OidcClient.Browser;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class BrowserFactory : IBrowserFactory
{
    Func<IBrowser>? factory;

    public void SetFactory(Func<IBrowser> factory)
    {
        this.factory = factory;
    }

    public IBrowser CreateBrowser()
    {
        if (factory == null)
            throw new InvalidOperationException("Browser factory is not set.");
        return factory();
    }
}