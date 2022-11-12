using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TB.DanceDance.Core.IdentityServerStore
{

    public class ApiResourceRecord : ApiResource
    {
        private string? id;

        public ApiResourceRecord()
        {

        }

        public ApiResourceRecord(string name) : base(name)
        {

        }

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string? Id { get => id ?? Name; set => id = value; }
    }

    public class ApiScopeRecord : ApiScope
    {
        private string? id;

        public ApiScopeRecord()
        {

        }

        public ApiScopeRecord(string name, string displayName) : base(name, displayName)
        {

        }

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string? Id { get => id ?? Name; set => id = value; }
    }

    public class IdentityResourceRecord : IdentityResource
    {
        private string? id;

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string? Id { get => id ?? Name; set => id = value; }
    }

    public class IdentityResourceMongoStore : IResourceStore
    {
        private readonly IMongoCollection<ApiResourceRecord> apiResourceCollection;
        private readonly IMongoCollection<ApiScopeRecord> apiScopeCollection;
        private readonly IMongoCollection<IdentityResourceRecord> identityResourceCollection;

        public IdentityResourceMongoStore(IMongoCollection<ApiResourceRecord> apiResourceCollection,
            IMongoCollection<ApiScopeRecord> apiScopeCollection,
            IMongoCollection<IdentityResourceRecord> identityResourceCollection

            )
        {
            this.apiResourceCollection = apiResourceCollection;
            this.apiScopeCollection = apiScopeCollection;
            this.identityResourceCollection = identityResourceCollection;
        }

        public Task AddApiResource(ApiResourceRecord apiResource)
        {
            return apiResourceCollection.ReplaceOneAsync(r => r.Name == apiResource.Name, apiResource, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            var filter = new FilterDefinitionBuilder<ApiResourceRecord>()
                .In(r => r.Name, apiResourceNames);

            var res = await apiResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<ApiResourceRecord>()
                .AnyIn(r => r.Scopes, scopeNames);

            var res = await apiResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public Task AddApiScopeAsync(ApiScopeRecord apiScope)
        {
            return apiScopeCollection.ReplaceOneAsync(r => r.Name == apiScope.Name, apiScope, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }

        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<ApiScopeRecord>()
                .In(r => r.Name, scopeNames);

            var res = await apiScopeCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public Task AddIdentityResource(IdentityResourceRecord identityResource)
        {
            return identityResourceCollection.ReplaceOneAsync(r => r.Name == identityResource.Name, identityResource, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var filter = new FilterDefinitionBuilder<IdentityResourceRecord>()
                .In(r => r.Name, scopeNames);

            var res = await identityResourceCollection.FindAsync(filter);
            return await res.ToListAsync();
        }

        public async Task<Resources> GetAllResourcesAsync()
        {

            var t1 = apiResourceCollection.FindAsync(FilterDefinition<ApiResourceRecord>.Empty);
            var apis = GetRecords(t1);

            var t2 = apiScopeCollection.FindAsync(FilterDefinition<ApiScopeRecord>.Empty);
            var scopes = GetRecords(t2);

            var t3 = identityResourceCollection.FindAsync(FilterDefinition<IdentityResourceRecord>.Empty);
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
