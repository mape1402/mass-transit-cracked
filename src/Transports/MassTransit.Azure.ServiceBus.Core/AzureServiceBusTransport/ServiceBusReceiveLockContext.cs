namespace MassTransit.AzureServiceBusTransport
{
    using System;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Transports;


    public class ServiceBusReceiveLockContext :
        ReceiveLockContext
    {
        readonly MessageLockContext _lockContext;
        readonly ServiceBusReceivedMessage _message;
        readonly ReceiveContext _receiveContext;

        public ServiceBusReceiveLockContext(ReceiveContext receiveContext, MessageLockContext lockContext, ServiceBusReceivedMessage message)
        {
            _receiveContext = receiveContext;
            _lockContext = lockContext;
            _message = message;
        }

        public Task Complete()
        {
            return _lockContext.Complete();
        }

        public async Task Faulted(Exception exception)
        {
            switch (exception)
            {
                case MessageLockExpiredException _:
                case MessageTimeToLiveExpiredException _:
                case ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost }:
                case ServiceBusException { Reason: ServiceBusFailureReason.SessionLockLost }:
                case ServiceBusException { Reason: ServiceBusFailureReason.ServiceCommunicationProblem }:
                    return;

                default:
                    try
                    {
                        await _lockContext.Abandon(exception).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LogContext.Warning?.Log(exception, "Abandon message faulted: {MessageId} - {Exception}", _message.MessageId, ex);
                    }

                    break;
            }
        }

        public Task ValidateLockStatus()
        {
            if (_message.LockedUntil <= DateTime.UtcNow)
                throw new MessageLockExpiredException(_receiveContext.InputAddress, $"The message lock expired: {_message.MessageId}");

            if (_message.ExpiresAt < DateTime.UtcNow)
                throw new MessageTimeToLiveExpiredException(_receiveContext.InputAddress, $"The message expired: {_message.MessageId}");

            return Task.CompletedTask;
        }
    }
}
