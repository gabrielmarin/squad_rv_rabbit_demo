using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace XP.RabbitMq.Demo.Domain
{
    public class AvgCost
    {
        public ObjectId Id { get; set; }
        public string Customer { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public IEnumerable<Trade> History { get; set; }


        public string Key() => $"{Customer}:{Symbol}";
        public override string ToString()
            => $"{Customer} Avg Cost is {Price:C} for {Symbol}";
    }
}
