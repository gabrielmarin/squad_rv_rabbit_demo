using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Faker.Extensions;
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;
using Serilog;
using XP.RabbitMq.Demo.Domain;

namespace XP.RabbitMq.Demo.Producer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Publishing trades...");
            const string ExchangeName = "trades_exchange";
            const string TradesQueueName = "trades_queue";
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest"};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);
                channel.QueueDeclare(TradesQueueName, true, false, false);
                channel.QueueBind(TradesQueueName, ExchangeName, TradesQueueName);

                foreach (var (buying, selling) in GenerateRandomTradePairs())
                {
                    channel.BasicPublish(ExchangeName, TradesQueueName, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(buying)));
                    Log.Information(buying.ToString());
                    channel.BasicPublish(ExchangeName, TradesQueueName, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(selling)));
                    Log.Information(selling.ToString());
                }
            }

            Console.Read();
        }

        private static IEnumerable<Tuple<Trade, Trade>> GenerateRandomTradePairs()
        {
            var symbols = new[] { "PETR3", "MGLU3", "VALE3", "ITUB3", "BTOW3" };
            var prices = new[] { 25.50m, 45.35m, 19.88m, 20m, 32.23m, 24m, 26.66m };
            var names = Enumerable.Range(1, 10).Select(x => Faker.Name.First()).ToArray();
            var random = new Random();

            while (true)
            {
                var buyingTrade = new Trade
                {
                    Client = names.Random(),
                    Symbol = symbols.Random(),
                    Price = prices.RandomItem(),
                    Quantity = random.Next(10, 1001)
                };

                var sellingTrade = new Trade
                {
                    Client = buyingTrade.Client,
                    Symbol = buyingTrade.Symbol,
                    Price = prices.RandomItem(),
                    Quantity = random.Next(Convert.ToInt32(buyingTrade.Quantity * 0.8), buyingTrade.Quantity + 1) * -1
                };
                
                yield return Tuple.Create(buyingTrade, sellingTrade);
            }
        }
    }
}
