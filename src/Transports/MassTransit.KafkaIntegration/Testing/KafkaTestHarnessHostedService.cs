namespace MassTransit.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Confluent.Kafka.Admin;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;


    public class KafkaTestHarnessHostedService :
        IHostedService
    {
        readonly IOptions<KafkaTestHarnessOptions> _testHarnessOptions;
        readonly ILogger<KafkaTestHarnessHostedService> _logger;
        readonly IAdminClient _adminClient;

        public KafkaTestHarnessHostedService(IAdminClient adminClient, IOptions<KafkaTestHarnessOptions> testHarnessOptions,
            ILogger<KafkaTestHarnessHostedService> logger)
        {
            _adminClient = adminClient;
            _testHarnessOptions = testHarnessOptions;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_testHarnessOptions.Value.TopicNames.Any())
                return;

            var options = _testHarnessOptions.Value;

            if (options.CleanTopicsOnStart)
                await DeleteTopics(options, cancellationToken).ConfigureAwait(false);

            if (options.CreateTopicsIfNotExists)
                await CreateTopics(options.TopicNames, options, cancellationToken).ConfigureAwait(false);
        }

        async Task DeleteTopics(KafkaTestHarnessOptions options, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting topics: {TopicNames}", string.Join(", ", options.TopicNames));
                await _adminClient.DeleteTopicsAsync(options.TopicNames).ConfigureAwait(false);
                // Adding some delays for kafka to settle
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            }
            catch (DeleteTopicsException e)
            {
                if (!e.Results.All(x => x.Error.Reason.EndsWith("[Broker: Unknown topic or partition].", StringComparison.OrdinalIgnoreCase)))
                    _logger.LogWarning(e, "Topic deletion failed");
            }
        }

        async Task CreateTopics(IReadOnlyList<string> topicNames, KafkaTestHarnessOptions options, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating topics: {TopicNames} with {NumPartitions} partitions and {ReplicationFactor} replicas",
                    string.Join(", ", topicNames), options.Partitions, options.Replicas);
                await _adminClient.CreateTopicsAsync(topicNames.Select(x => new TopicSpecification
                {
                    Name = x,
                    NumPartitions = options.Partitions,
                    ReplicationFactor = options.Replicas
                })).ConfigureAwait(false);
            }
            catch (CreateTopicsException e)
            {
                List<string> topics = e.Results.Where(result => result.Error.Reason.Contains($"Topic '{result.Topic}' is marked for deletion."))
                    .Select(result => result.Topic).ToList();

                if (!e.Results.All(x => x.Error.Reason.EndsWith("already exists.", StringComparison.OrdinalIgnoreCase)) && !topics.Any())
                    _logger.LogWarning(e, "Topic creation failed");

                if (topics.Any())
                {
                    // Retry failing topics creation
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    await CreateTopics(topics, options, cancellationToken).ConfigureAwait(false);
                }
            }

            // Adding some delays for kafka to settle
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
