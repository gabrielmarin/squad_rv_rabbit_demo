using System;
using System.Collections.Generic;
using System.Text;

namespace XP.RabbitMq.Demo.Domain
{
    public class AvgCost
    {
        public string Customer { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public IEnumerable<Trade> History { get; set; }
    }
}
