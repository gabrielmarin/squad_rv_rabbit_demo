using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using XP.RabbitMq.Demo.CrossCutting.IoC;
using XP.RabbitMq.Demo.Domain;
using XP.RabbitMq.Demo.Domain.Interfaces;


namespace XP.RabbitMq.Demo.Consumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var container = DependencyInjectionConfig.InitializeContainer();
            var repository = container.GetInstance<IAvgCostRepository>();
            const string TradesQueueName = "trades_queue";
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(TradesQueueName, true, false, false);
                channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body;
                    var trade = JsonSerializer.Deserialize<Trade>(Encoding.UTF8.GetString(body));

                    var avgCost = await repository.FindAsync(trade.Client, trade.Symbol);
                    if (avgCost == null)
                        await repository.SaveOrUpdateAsync(new AvgCost
                        {
                            Customer = trade.Client,
                            Quantity = trade.Quantity,
                            Symbol = trade.Symbol,
                            Price = trade.Price,
                            History = new[] { trade }
                        });
                    else
                    {
                        avgCost.Quantity += trade.Quantity;
                        if(trade.Quantity > 0)
                            avgCost.Price = Math.Round(((avgCost.Price * avgCost.Quantity + trade.Price * trade.Quantity) / avgCost.Quantity), 6);
                        if (avgCost.Quantity == 0)
                            avgCost.Price = 0;
                        avgCost.History = new[] { trade };
                        await repository.SaveOrUpdateAsync(avgCost);
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                    Console.WriteLine("Processed {0}", trade);
                };

                channel.BasicConsume(TradesQueueName, false, consumer);

                Console.ReadLine();
            }
        }
    }
}
