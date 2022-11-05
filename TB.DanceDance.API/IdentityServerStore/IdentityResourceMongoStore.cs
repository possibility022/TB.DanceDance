using IdentityServer4.Models;
using IdentityServer4.Stores;
using MongoDB.Driver;

namespace TB.DanceDance.API.IdentityServerStore
{
    public class IdentityResourceMongoStore : IResourceStore
    {
        private readonly IMongoCollection<ApiResource> apiResourceCollection;
        private readonly IMongoCollection<ApiScope> apiScopeCollection;
        private readonly IMongoCollection<IdentityResource> identityResourceCollection;

        public IdentityResourceMongoStore(IMongoCollection<ApiResource> apiResourceCollection,
            IMongoCollection<ApiScope> apiScopeCollection,
            IMongoCollection<IdentityResource> identityResourceCollection

            )
        {
            this.apiResourceCollection = apiResourceCollection;
            this.apiScopeCollection = apiScopeCollection;
            this.identityResourceCollection = identityResourceCollection;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            var filter = new FilterDefinitionBuilder<ApiResource>()
                .In(r => r.Name, apiResourceNames);

            var res = await apiResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<ApiResource>()
                .AnyIn(r => r.Scopes, scopeNames);

            var res = await apiResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<ApiScope>()
                .In(r => r.Name, scopeNames);

            var res = await apiScopeCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<IdentityResource>()
                .In(r => r.Name, scopeNames);

            var res = await identityResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<Resources> GetAllResourcesAsync()
        {

            var t1 = apiResourceCollection.FindAsync(FilterDefinition<ApiResource>.Empty);
            var apis = GetRecords(t1);

            var t2 = apiScopeCollection.FindAsync(FilterDefinition<ApiScope>.Empty);
            var scopes = GetRecords(t2);

            var t3 = identityResourceCollection.FindAsync(FilterDefinition<IdentityResource>.Empty);
            var identities = GetRecords(t3);

            await Task.WhenAll(t1, t2, t3);

            return new Resources(identities.Result, apis.Result, scopes.Result);
        }

        private async Task<List<T>> GetRecords<T>(Task<IAsyncCursor<T>> findTask)
        {
            var res = await findTask;
            return await res.ToListAsync();
        }
    }
}
