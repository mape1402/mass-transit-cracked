namespace MassTransit.Containers.Tests.Scenarios
{
    using System;


    public class SecondSagaMessage :
        CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }
}
