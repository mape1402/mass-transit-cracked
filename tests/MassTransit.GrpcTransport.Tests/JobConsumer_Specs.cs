namespace MassTransit.GrpcTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using JobConsumerTests;
    using MassTransit.Contracts.JobService;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    namespace JobConsumerTests
    {
        public interface OddJob
        {
            TimeSpan Duration { get; }
        }


        public class OddJobConsumer :
            IJobConsumer<OddJob>
        {
            public async Task Run(JobContext<OddJob> context)
            {
                if (context.RetryAttempt == 0)
                    await Task.Delay(context.Job.Duration, context.CancellationToken);
            }
        }


        public class OddJobCompletedConsumer :
            IConsumer<JobCompleted<OddJob>>
        {
            public Task Consume(ConsumeContext<JobCompleted<OddJob>> context)
            {
                return Task.CompletedTask;
            }
        }
    }


    [TestFixture]
    public class JobConsumer_Specs
    {
        [Test]
        public async Task Should_complete_the_job()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(5);

            await harness.Start();

            var jobId = NewId.NextGuid();

            IRequestClient<SubmitJob<OddJob>> client = harness.GetRequestClient<SubmitJob<OddJob>>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(jobId));

            Assert.That(await harness.Published.Any<JobSubmitted>(), Is.True);
            Assert.That(await harness.Published.Any<JobStarted>(), Is.True);

            Assert.That(await harness.Published.Any<JobCompleted>(), Is.True);
            Assert.That(await harness.Published.Any<JobCompleted<OddJob>>(), Is.True);

            await harness.Stop();
        }

        [Test]
        public async Task Should_create_a_unique_job_id()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(5);

            await harness.Start();

            await harness.Bus.Publish<OddJob>( new { Duration = TimeSpan.FromSeconds(1) });

            Assert.That(await harness.Published.Any<JobSubmitted>(), Is.True);
            Assert.That(await harness.Published.Any<JobStarted>(), Is.True);

            Assert.That(await harness.Published.Any<JobCompleted>(), Is.True);
            Assert.That(await harness.Published.Any<JobCompleted<OddJob>>(), Is.True);

            IPublishedMessage<JobCompleted> publishedMessage = await harness.Published.SelectAsync<JobCompleted>().First();
            Assert.That(publishedMessage.Context.Message.JobId, Is.Not.EqualTo(Guid.Empty));

            await harness.Stop();
        }

        [Test]
        public async Task Should_cancel_the_job()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();

            await harness.Start();

            var jobId = NewId.NextGuid();

            IRequestClient<SubmitJob<OddJob>> client = harness.GetRequestClient<SubmitJob<OddJob>>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = jobId,
                Job = new { Duration = TimeSpan.FromSeconds(10) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(jobId));

            Assert.That(await harness.Published.Any<JobSubmitted>(), Is.True);
            Assert.That(await harness.Published.Any<JobStarted>(), Is.True);

            await harness.Bus.Publish<CancelJob>(new { JobId = jobId });

            Assert.That(await harness.Published.Any<JobCanceled>(), Is.True);

            await harness.Stop();

        }

        [Test]
        public async Task Should_cancel_the_job_and_retry_it()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();

            await harness.Start();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(5);

            var jobId = NewId.NextGuid();

            IRequestClient<SubmitJob<OddJob>> client = harness.GetRequestClient<SubmitJob<OddJob>>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = jobId,
                Job = new { Duration = TimeSpan.FromSeconds(10) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(jobId));

            Assert.That(await harness.Published.Any<JobSubmitted>(), Is.True);
            Assert.That(await harness.Published.Any<JobStarted>(), Is.True);

            await harness.Bus.Publish<CancelJob>(new { JobId = jobId });

            Assert.That(await harness.Published.Any<JobCanceled>(), Is.True);
            Assert.That(await harness.Sent.Any<JobSlotReleased>(), Is.True);

            await harness.Bus.Publish<RetryJob>(new { JobId = jobId });
            Assert.That(await harness.Published.Any<JobCompleted>(), Is.True);
            Assert.That(await harness.Published.Any<JobCompleted<OddJob>>(), Is.True);

            await harness.Stop();
        }

        [Test]
        public async Task Should_cancel_the_job_and_get_the_status()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();

            await harness.Start();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(10);

            var jobId = NewId.NextGuid();

            IRequestClient<SubmitJob<OddJob>> client = harness.GetRequestClient<SubmitJob<OddJob>>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = jobId,
                Job = new { Duration = TimeSpan.FromSeconds(10) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(jobId));

            Assert.That(await harness.Published.Any<JobSubmitted>(), Is.True);
            Assert.That(await harness.Published.Any<JobStarted>(), Is.True);

            IRequestClient<GetJobState> stateClient = harness.GetRequestClient<GetJobState>();

            Response<JobState> jobState = await stateClient.GetResponse<JobState>(new { JobId = jobId });

            Assert.That(jobState.Message.CurrentState, Is.EqualTo("Started"));

            await harness.Bus.Publish<CancelJob>(new { JobId = jobId });

            Assert.That(await harness.Published.Any<JobCanceled>(), Is.True);
            Assert.That(await harness.Sent.Any<JobSlotReleased>(), Is.True);

            jobState = await stateClient.GetResponse<JobState>(new { JobId = jobId });

            Assert.That(jobState.Message.CurrentState, Is.EqualTo("Canceled"));

            await harness.Stop();
        }

        [Test]
        public async Task Should_return_not_found()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();

            await harness.Start();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(10);

            var jobId = NewId.NextGuid();

            IRequestClient<GetJobState> stateClient = harness.GetRequestClient<GetJobState>();

            var jobState = await stateClient.GetJobState(jobId);

            Assert.That(jobState.CurrentState, Is.EqualTo("NotFound"));

            await harness.Stop();
        }

        [Test]
        public async Task Should_cancel_the_job_while_waiting()
        {
            await using var provider = SetupServiceCollection();

            var harness = provider.GetTestHarness();
            harness.TestInactivityTimeout = TimeSpan.FromSeconds(10);

            await harness.Start();

            var previousJobId = NewId.NextGuid();

            var jobId = NewId.NextGuid();

            IRequestClient<SubmitJob<OddJob>> client = harness.GetRequestClient<SubmitJob<OddJob>>();

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = previousJobId,
                Job = new { Duration = TimeSpan.FromSeconds(10) }
            });

            Assert.That(await harness.Published.Any<JobStarted>(x => x.Context.Message.JobId == previousJobId), Is.True);

            response = await client.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = jobId,
                Job = new { Duration = TimeSpan.FromSeconds(10) }
            });

            Assert.That(await harness.Published.Any<JobSubmitted>(x => x.Context.Message.JobId == jobId), Is.True);

            Assert.That(response.Message.JobId, Is.EqualTo(jobId));

            Assert.That(await harness.Sent.Any<JobSlotWaitElapsed>(x => x.Context.Message.JobId == jobId), Is.True);

            await harness.Bus.Publish<CancelJob>(new { JobId = jobId });

            Assert.That(await harness.Published.Any<JobCanceled>(x => x.Context.Message.JobId == jobId), Is.True);

            await harness.Bus.Publish<CancelJob>(new { JobId = previousJobId });
            Assert.That(await harness.Published.Any<JobCanceled>(x => x.Context.Message.JobId == previousJobId), Is.True);

            await harness.Stop();
        }

        static ServiceProvider SetupServiceCollection()
        {
            var provider = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();

                    x.AddConsumer<OddJobConsumer>();
                    x.AddConsumer<OddJobCompletedConsumer>()
                        .Endpoint(e => e.ConcurrentMessageLimit = 1);

                    x.UsingGrpc((context, cfg) =>
                    {
                        cfg.UseDelayedMessageScheduler();

                        var options = new ServiceInstanceOptions()
                            .SetEndpointNameFormatter(context.GetService<IEndpointNameFormatter>() ??
                                DefaultEndpointNameFormatter.Instance);

                        cfg.ServiceInstance(options, instance =>
                        {
                            instance.ConfigureJobServiceEndpoints();

                            instance.ConfigureEndpoints(context, f => f.Include<OddJobConsumer>());
                        });

                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            harness.TestInactivityTimeout = TimeSpan.FromSeconds(10);

            return provider;
        }
    }
}
