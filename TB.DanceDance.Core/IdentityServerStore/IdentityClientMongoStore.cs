using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TB.DanceDance.Core.IdentityServerStore
{

    public class ClientRecord : Client
    {
        private string? id;

        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string? Id { get => id ?? ClientId; set => id = value; }
    }

    public class IdentityClientMongoStore : IClientStore
    {
        private readonly IMongoCollection<ClientRecord> clientCollection;

        public IdentityClientMongoStore(IMongoCollection<ClientRecord> clientCollection)
        {
            this.clientCollection = clientCollection;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var builder = new FilterDefinitionBuilder<ClientRecord>();
            var filter = builder.Eq(c => c.ClientId, clientId);

            var res = await clientCollection.FindAsync(filter);
            return await res.FirstOrDefaultAsync();
        }

        public Task AddClientAsync(ClientRecord client)
        {
            return clientCollection.ReplaceOneAsync(r => r.ClientId == client.ClientId, client, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }
    }
}
