using System;
using MongoDB.Driver;
using SimpleInjector;
using XP.RabbitMq.Demo.Domain.Interfaces;
using XP.RabbitMq.Demo.Infra.Data;


namespace XP.RabbitMq.Demo.CrossCutting.IoC
{
    public class DependencyInjectionConfig
    {
        public static Container InitializeContainer()
        {
            var container = new Container();

            container.RegisterSingleton(() => new MongoClient("mongodb://localhost:27017"));

            container.Register<IAvgCostRepository, AvgCostRepository>();

            container.Verify();
            return container;
        }
    }
}
