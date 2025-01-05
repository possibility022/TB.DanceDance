import ConfigProvider from "./ConfigProvider";
import AppApiClient from "./HttpApiClient";



class AppClient {
    public async warmupRequest() {
        // Our app and identity is hosted on the same environment/instance.
        // We can call identity endpoint to warm up whole app.
        const identityConfig = ConfigProvider.getMetadataConfig();
        await AppApiClient.get(identityConfig.jwks_uri)
    }
}

const appClient = new AppClient()

export default appClient