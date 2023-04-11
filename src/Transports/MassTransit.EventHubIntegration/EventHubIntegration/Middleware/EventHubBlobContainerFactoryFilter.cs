namespace MassTransit.EventHubIntegration.Middleware
{
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;


    public class EventHubBlobContainerFactoryFilter :
        IFilter<ProcessorContext>
    {
        readonly BlobContainerClient _blockClient;

        public EventHubBlobContainerFactoryFilter(BlobContainerClient blockClient)
        {
            _blockClient = blockClient;
        }

        public async Task Send(ProcessorContext context, IPipe<ProcessorContext> next)
        {
            await context.OneTimeSetup<ConfigureTopologyContext>(_ => CreateBlobIfNotExistsAsync(context.CancellationToken), () => new Context())
                .ConfigureAwait(false);

            await next.Send(context).ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("configureTopology");
            scope.Add("Uri", _blockClient.Uri);
            scope.Add("Name", _blockClient.Name);
        }

        async Task<bool> CreateBlobIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            Response<bool> exists = await _blockClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (exists.Value)
                return true;

            try
            {
                await _blockClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (RequestFailedException exception)
            {
                LogContext.Warning?.Log(exception, "Azure Blob Container does not exist: {Address}", _blockClient.Uri);
                return false;
            }
        }


        class Context :
            ConfigureTopologyContext
        {
        }
    }
}
