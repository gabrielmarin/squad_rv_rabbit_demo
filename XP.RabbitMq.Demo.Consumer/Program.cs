using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using XP.RabbitMq.Demo.Domain;


namespace XP.RabbitMq.Demo.Consumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "trades_queue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var trade = JsonSerializer.Deserialize<Trade>(Encoding.UTF8.GetString(body));
                    Console.WriteLine(" [x] Received {0}", trade);
                };

                channel.BasicConsume("trades_queue", true, consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();


            }
        }
    }
}
