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
            _collection = db.GetCollection<AvgCost>("avg_cost");
        }
        public async Task<bool> SaveOrUpdateAsync(AvgCost avgCost)
        {
            var updOpts = new UpdateOptions { IsUpsert = true };
            var updDef = new UpdateDefinitionBuilder<AvgCost>()
                .SetOnInsert(x => x.Customer, avgCost.Customer)
                .Set(x => x.Price, avgCost.Price)
                .Set(x => x.Quantity, avgCost.Quantity)
                .PushEach(x => x.History, avgCost.History);

            var result = await _collection.UpdateOneAsync(x => x.Customer == avgCost.Customer, updDef, updOpts);
            return result.IsAcknowledged;
        }
    }
}
