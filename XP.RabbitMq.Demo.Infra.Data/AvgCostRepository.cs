using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using XP.RabbitMq.Demo.Domain;
using XP.RabbitMq.Demo.Domain.Interfaces;

namespace XP.RabbitMq.Demo.Infra.Data
{
    public class AvgCostRepository : IAvgCostRepository
    {
        private readonly IMongoCollection<AvgCost> _collection;

        public AvgCostRepository(IMongoClient mongoClient)
        {
            var db = mongoClient.GetDatabase("RabbitMqDemo");

            if(db.ListCollectionNames().FirstOrDefaultAsync().Result == null)
                db.CreateCollection("avg_cost");
            _collection = db.GetCollection<AvgCost>("avg_cost");

            var indexBuilder = Builders<AvgCost>.IndexKeys;
            var indexKeysDefinition = indexBuilder.Combine(indexBuilder.Ascending(x => x.Customer), indexBuilder.Ascending(x => x.Symbol));
            _collection.Indexes.CreateOneAsync(new CreateIndexModel<AvgCost>(indexKeysDefinition)).Wait();
        }
        public async Task<bool> SaveOrUpdateAsync(AvgCost avgCost)
        {
            var updOpts = new UpdateOptions { IsUpsert = true };
            var updDef = new UpdateDefinitionBuilder<AvgCost>()
                .SetOnInsert(x => x.Customer, avgCost.Customer)
                .SetOnInsert(x => x.Symbol, avgCost.Symbol)
                .Set(x => x.Price, avgCost.Price)
                .Set(x => x.Quantity, avgCost.Quantity)
                .PushEach(x => x.History, avgCost.History);

            var result = await _collection.UpdateOneAsync(x => x.Customer == avgCost.Customer && x.Symbol == avgCost.Symbol, updDef, updOpts);
            return result.IsAcknowledged;
        }

        public async Task<AvgCost> FindAsync(string customer, string symbol)
        {
            var projectionDef = new ProjectionDefinitionBuilder<AvgCost>().Exclude(x => x.History);
            var findOptions = new FindOptions<AvgCost, AvgCost>(){Projection = projectionDef}; 
            var cursor = await _collection.FindAsync(x => x.Customer == customer && x.Symbol == symbol, findOptions);
            
            return await cursor.FirstOrDefaultAsync();
        }
    }
}
