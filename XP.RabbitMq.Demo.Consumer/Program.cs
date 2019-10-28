using System;
using System.Collections.Concurrent;
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
            const string RealTimePubExchange = "realtime_pub_exchange";
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };
            var concurrentDictionary = new ConcurrentDictionary<string, AvgCost>();

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            var tradesTransformBlock = new TransformBlock<Trade, AvgCost>(trade =>
            {
                if (concurrentDictionary.TryGetValue(trade.Key(), out var avgCost))
                {
                    //Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} {avgCost} {avgCost.Quantity}");
                    avgCost.Quantity += trade.Quantity;
                    if (avgCost.Quantity == 0)
                        avgCost.Price = 0;
                    else if (trade.Quantity > 0 && avgCost.Quantity > 0)
                        avgCost.Price = Math.Round(((avgCost.Price * avgCost.Quantity + trade.Price * trade.Quantity) / avgCost.Quantity), 6);
                    avgCost.History = new[] { trade };

                    concurrentDictionary.TryRemove(avgCost.Key(), out var avgCostOld);
                    concurrentDictionary.TryAdd(avgCost.Key(), avgCost);
                    return avgCost;
                }
                avgCost = new AvgCost
                {
                    Customer = trade.Client,
                    Quantity = trade.Quantity,
                    Symbol = trade.Symbol,
                    Price = trade.Price,
                    History = new[] { trade }
                };
                concurrentDictionary.TryAdd(avgCost.Key(), avgCost);
                return avgCost;
                

            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 10000, EnsureOrdered = true});

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            var broadcastBlock = new BroadcastBlock<AvgCost>(msg => msg);
            var pubActionBlock = new ActionBlock<AvgCost>(avgCost =>
            {
                var basicProps = channel.CreateBasicProperties();
                basicProps.Expiration = "10000";
                channel.BasicPublish(RealTimePubExchange, $"{avgCost.Customer}", body: Encoding.UTF8.GetBytes(avgCost.ToString()), basicProperties: basicProps);
            }, new ExecutionDataflowBlockOptions { EnsureOrdered = true });
            var batchBlock = new BatchBlock<AvgCost>(1000);
            var persistMongoActionBlock = new ActionBlock<IEnumerable<AvgCost>>(async avgCosts => await repository.SaveOrUpdateManyAsync(avgCosts));

            tradesTransformBlock.LinkTo(broadcastBlock, linkOptions);
            broadcastBlock.LinkTo(pubActionBlock, linkOptions);
            broadcastBlock.LinkTo(batchBlock, linkOptions);
            batchBlock.LinkTo(persistMongoActionBlock, linkOptions);

            channel.ExchangeDeclare(RealTimePubExchange, ExchangeType.Topic);
            channel.QueueDeclare(TradesQueueName, true, false, false);
            channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var trade = JsonSerializer.Deserialize<Trade>(Encoding.UTF8.GetString(body));

                tradesTransformBlock.Post(trade);
                channel.BasicAck(ea.DeliveryTag, false);
                Console.WriteLine("Processed {0}", trade);
            };

            channel.BasicConsume(TradesQueueName, false, consumer);

            await Task.WhenAll(tradesTransformBlock.Completion, batchBlock.Completion, pubActionBlock.Completion);
            tradesTransformBlock.Complete();
            Console.ReadLine();
        }
    }
}
