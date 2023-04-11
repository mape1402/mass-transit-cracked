﻿namespace MassTransit.Azure.Table.Tests.Audit
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AzureTable;
    using NUnit.Framework;
    using Shouldly;


    [TestFixture]
    public class Saving_audit_records_to_the_audit_store :
        AzureTableInMemoryTestFixture
    {
        [Test]
        public async Task Should_Have_Audit_Records()
        {
            _records = GetRecords<AuditRecord>();
            _records.ShouldNotBeEmpty();
            _records.Count().ShouldBe(4);
        }

        IEnumerable<AuditRecord> _records;

        Task<ConsumeContext<A>> _handledA;
        Task<ConsumeContext<B>> _handledB;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await InputQueueSendEndpoint.Send(new A());
            await _handledA;
            await InputQueueSendEndpoint.Send(new B());
            await _handledB;
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UseAzureTableAuditStore(TestCloudTable);
            base.ConfigureInMemoryBus(configurator);
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer<TestConsumer>();
            _handledA = Handled<A>(configurator);
            _handledB = Handled<B>(configurator);
        }


        class TestConsumer : IConsumer<A>,
            IConsumer<B>
        {
            public Task Consume(ConsumeContext<A> context)
            {
                return Task.CompletedTask;
            }

            public Task Consume(ConsumeContext<B> context)
            {
                return Task.CompletedTask;
            }
        }


        class A
        {
            public string Data { get; set; }
        }


        class B
        {
            public string Data { get; set; }
        }
    }
}
