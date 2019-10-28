using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XP.RabbitMq.Demo.Domain.Interfaces
{
    public interface IAvgCostRepository
    {
        Task<bool> SaveOrUpdateAsync(AvgCost avgCost);
        Task<bool> SaveOrUpdateManyAsync(IEnumerable<AvgCost> avgCosts);
        Task<AvgCost> FindAsync(string customer, string symbol);
    }
}
