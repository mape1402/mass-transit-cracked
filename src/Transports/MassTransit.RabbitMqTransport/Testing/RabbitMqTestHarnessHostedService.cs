namespace MassTransit.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RabbitMQ.Client;
    using Serialization;


    public class RabbitMqTestHarnessHostedService :
        IHostedService
    {
        readonly ILogger<RabbitMqTestHarnessHostedService> _logger;
        readonly RabbitMqTestHarnessOptions _testOptions;
        readonly RabbitMqTransportOptions _transportOptions;

        public RabbitMqTestHarnessHostedService(IOptions<RabbitMqTransportOptions> transportOptions, IOptions<RabbitMqTestHarnessOptions> testOptions,
            ILogger<RabbitMqTestHarnessHostedService> logger)
        {
            _logger = logger;
            _transportOptions = transportOptions.Value;
            _testOptions = testOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_testOptions.CreateVirtualHostIfNotExists)
                await EnsureVirtualHostExists();

            if (_testOptions.CleanVirtualHost)
                await CleanVirtualHost();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        async Task EnsureVirtualHostExists()
        {
            var name = _transportOptions.VHost;

            if (string.IsNullOrWhiteSpace(name) || name == "/")
                return;

            using var client = GetHttpClient();

            var builder = GetUriBuilder($"api/vhosts/{name}");

            var responseMessage = await client.PutAsync(builder.Uri, new StringContent("{}", Encoding.UTF8, "application/json"));

            responseMessage.EnsureSuccessStatusCode();

            if (responseMessage.StatusCode == HttpStatusCode.Created)
                _logger.LogInformation("Created virtual host: {VirtualHost}", name);
        }

        async Task CleanVirtualHost()
        {
            var virtualHost = _transportOptions.VHost;

            if (string.IsNullOrWhiteSpace(virtualHost) || virtualHost == "/")
            {
                if (!_testOptions.ForceCleanRootVirtualHost)
                {
                    const string message = "CleanVirtualHost was specified on the root virtual host without ForceCleanRootVirtualHost";
                    _logger.LogError(message);
                    throw new InvalidOperationException(message);
                }
            }

            var factory = new ConnectionFactory
            {
                HostName = _transportOptions.Host,
                Port = _transportOptions.Port,
                VirtualHost = virtualHost ?? "/",
                UserName = _transportOptions.User,
                Password = _transportOptions.Pass
            };

            var connection = factory.CreateConnection();
            try
            {
                using var model = connection.CreateModel();
                model.ConfirmSelect();

                var exchangeCount = 0;
                var queueCount = 0;

                IList<string> exchanges = await GetVirtualHostEntities("exchanges");
                foreach (var exchange in exchanges)
                {
                    model.ExchangeDelete(exchange);
                    exchangeCount++;
                }

                IList<string> queues = await GetVirtualHostEntities("queues");
                foreach (var queue in queues)
                {
                    model.QueueDelete(queue);
                    queueCount++;
                }

                model.Close();

                if (exchangeCount > 0 || queueCount > 0)
                    _logger.LogInformation("Removed {QueueCount} queue(s), {ExchangeCount} exchange(s)", queueCount, exchangeCount);

                connection.Close(200, "Completed (Ok)");
            }
            catch (Exception ex)
            {
                if (connection.IsOpen)
                    connection.Close(500, $"Completed (not OK): {ex.Message}");
            }
        }

        async Task<IList<string>> GetVirtualHostEntities(string element)
        {
            using var client = GetHttpClient();

            var builder = GetUriBuilder($"api/{element}/{_transportOptions.VHost.Trim('/')}");

            var bytes = await client.GetByteArrayAsync(builder.Uri);

            var rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, SystemTextJsonMessageSerializer.Options);

            var entities = rootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();

            return entities.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("amq.")).ToList();
        }

        HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{_transportOptions.User}:{_transportOptions.Pass}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return client;
        }

        UriBuilder GetUriBuilder(string pathValue)
        {
            return new UriBuilder(_transportOptions.UseSsl ? "https" : "http", _transportOptions.Host, _transportOptions.ManagementPort, pathValue);
        }
    }
}
