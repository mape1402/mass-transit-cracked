namespace MassTransit.Middleware.InMemoryOutbox
{
    using System;
    using Courier.Contracts;


    public abstract class InMemoryOutboxCourierContextProxy :
        InMemoryOutboxConsumeContext<RoutingSlip>,
        CourierContext
    {
        readonly CourierContext _courierContext;

        protected InMemoryOutboxCourierContextProxy(CourierContext courierContext)
            : base(courierContext)
        {
            _courierContext = courierContext;
        }

        DateTime CourierContext.Timestamp => _courierContext.Timestamp;
        TimeSpan CourierContext.Elapsed => _courierContext.Elapsed;
        Guid CourierContext.TrackingNumber => _courierContext.TrackingNumber;
        Guid CourierContext.ExecutionId => _courierContext.ExecutionId;
        string CourierContext.ActivityName => _courierContext.ActivityName;
    }
}
