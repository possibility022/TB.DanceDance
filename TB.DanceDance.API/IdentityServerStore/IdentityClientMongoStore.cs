using IdentityServer4.Models;
using IdentityServer4.Stores;
using MongoDB.Driver;

namespace TB.DanceDance.API.IdentityServerStore
{
    public class IdentityClientMongoStore : IClientStore
    {
        private readonly IMongoCollection<Client> clientCollection;

        public IdentityClientMongoStore(IMongoCollection<Client> clientCollection)
        {
            this.clientCollection = clientCollection;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var builder = new FilterDefinitionBuilder<Client>();
            var filter = builder.Eq(c => c.ClientId, clientId);

            var res = await clientCollection.FindAsync(filter);
            return await res.FirstOrDefaultAsync();
        }
    }
}
