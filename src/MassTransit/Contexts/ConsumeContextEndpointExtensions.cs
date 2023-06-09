namespace MassTransit
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    public static class ConsumeContextEndpointExtensions
    {
        /// <summary>
        /// Returns the endpoint for a fault, either directly to the requester, or published
        /// </summary>
        /// <param name="context"></param>
        /// <typeparam name="T">The message type</typeparam>
        /// <returns></returns>
        public static Task<ISendEndpoint> GetFaultEndpoint<T>(this ConsumeContext context)
            where T : class
        {
            var destinationAddress = context.FaultAddress ?? context.ResponseAddress;

            return GetEndpoint<Fault<T>>(context.ReceiveContext, context, destinationAddress, context.RequestId);
        }

        /// <summary>
        /// Returns the endpoint for a fault, either directly to the requester, or published
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faultAddress"></param>
        /// <param name="requestId"></param>
        /// <typeparam name="T">The response type</typeparam>
        /// <returns></returns>
        public static Task<ISendEndpoint> GetFaultEndpoint<T>(this ConsumeContext context, Uri faultAddress, Guid? requestId = default)
            where T : class
        {
            var destinationAddress = faultAddress ?? context.FaultAddress ?? context.ResponseAddress;

            return GetEndpoint<T>(context.ReceiveContext, context, destinationAddress, requestId ?? context.RequestId);
        }

        /// <summary>
        /// Returns the endpoint for a receive fault, either directly to the requester, or published
        /// </summary>
        /// <param name="context"></param>
        /// <param name="consumeContext"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public static Task<ISendEndpoint> GetReceiveFaultEndpoint(this ReceiveContext context, ConsumeContext consumeContext, Guid? requestId)
        {
            var destinationAddress = consumeContext?.FaultAddress ?? consumeContext?.ResponseAddress;

            return GetEndpoint<ReceiveFault>(context, consumeContext, destinationAddress, requestId);
        }

        /// <summary>
        /// Returns the endpoint for a response, either directly to the requester, or published
        /// </summary>
        /// <param name="context"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<ISendEndpoint> GetResponseEndpoint<T>(this ConsumeContext context)
            where T : class
        {
            return GetEndpoint<T>(context.ReceiveContext, context, context.ResponseAddress, context.RequestId);
        }

        /// <summary>
        /// Returns the endpoint for a response, either directly to the requester, or published
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseAddress"></param>
        /// <param name="requestId"></param>
        /// <typeparam name="T">The response type</typeparam>
        /// <returns></returns>
        public static Task<ISendEndpoint> GetResponseEndpoint<T>(this ConsumeContext context, Uri responseAddress, Guid? requestId = default)
            where T : class
        {
            return GetEndpoint<T>(context.ReceiveContext, context, responseAddress ?? context.ResponseAddress, requestId ?? context.RequestId);
        }

        /// <summary>
        /// Returns the endpoint for a response, either directly to the requester, or published
        /// </summary>
        /// <param name="receiveContext"></param>
        /// <param name="consumeContext"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="requestId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static Task<ISendEndpoint> GetEndpoint<T>(ReceiveContext receiveContext, ConsumeContext consumeContext, Uri destinationAddress, Guid? requestId)
            where T : class
        {
            if (destinationAddress != null && consumeContext != null)
                return GetSendEndpoint(receiveContext.SendEndpointProvider, consumeContext, destinationAddress, requestId);

            return GetPublishEndpoint<T>(receiveContext.PublishEndpointProvider, consumeContext, requestId);
        }

        internal static Task<ISendEndpoint> GetPublishEndpoint<T>(this IPublishEndpointProvider publishEndpointProvider, ConsumeContext consumeContext,
            Guid? requestId)
            where T : class
        {
            Task<ISendEndpoint> publishSendEndpointTask = publishEndpointProvider.GetPublishSendEndpoint<T>();
            if (publishSendEndpointTask.Status == TaskStatus.RanToCompletion)
            {
                return consumeContext != null
                    ? Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(publishSendEndpointTask.Result, consumeContext, requestId))
                    : publishSendEndpointTask;
            }

            async Task<ISendEndpoint> GetPublishSendEndpointAsync()
            {
                var publishSendEndpoint = await publishSendEndpointTask.ConfigureAwait(false);

                return consumeContext != null
                    ? new ConsumeSendEndpoint(publishSendEndpoint, consumeContext, requestId)
                    : publishSendEndpoint;
            }

            return GetPublishSendEndpointAsync();
        }

        internal static Task<ISendEndpoint> GetSendEndpoint(this ISendEndpointProvider sendEndpointProvider, ConsumeContext consumeContext,
            Uri destinationAddress, Guid? requestId)
        {
            Task<ISendEndpoint> sendEndpointTask = sendEndpointProvider.GetSendEndpoint(destinationAddress);
            if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
                return Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(sendEndpointTask.Result, consumeContext, requestId));

            async Task<ISendEndpoint> GetResponseEndpointAsync()
            {
                var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

                return new ConsumeSendEndpoint(sendEndpoint, consumeContext, requestId);
            }

            return GetResponseEndpointAsync();
        }
    }
}
