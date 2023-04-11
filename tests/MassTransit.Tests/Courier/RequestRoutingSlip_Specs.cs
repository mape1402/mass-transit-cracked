﻿namespace MassTransit.Tests.Courier
{
    using System;
    using System.Threading.Tasks;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;
    using MassTransit.Testing;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Courier;


    [TestFixture]
    public class Using_a_routing_slip_for_a_request_response :
        InMemoryActivityTestFixture
    {
        [Test]
        public async Task Should_complete_the_request()
        {
            Response<Response> response = await _requestClient.GetResponse<Response>(new Request());
        }

        RequestProxy _requestProxy;
        ResponseProxy _responseProxy;
        Uri _requestAddress;
        IRequestClient<Request> _requestClient;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            var testActivity = GetActivityContext<TestActivity>();
            var secondActivity = GetActivityContext<SecondTestActivity>();

            _requestProxy = new RequestProxy(testActivity, secondActivity);
            _responseProxy = new ResponseProxy();

            configurator.Instance(_requestProxy);
            configurator.Instance(_responseProxy);

            _requestAddress = configurator.InputAddress;
        }

        protected override void SetupActivities(BusTestHarness testHarness)
        {
            AddActivityContext<TestActivity, TestArguments, TestLog>(() => new TestActivity());
            AddActivityContext<SecondTestActivity, TestArguments, TestLog>(() => new SecondTestActivity());
        }


        class RequestProxy :
            RoutingSlipRequestProxy<Request>
        {
            readonly ActivityTestContext _secondActivity;
            readonly ActivityTestContext _testActivity;

            public RequestProxy(ActivityTestContext testActivity, ActivityTestContext secondActivity)
            {
                _testActivity = testActivity;
                _secondActivity = secondActivity;
            }

            protected override async Task BuildRoutingSlip(RoutingSlipBuilder builder, ConsumeContext<Request> request)
            {
                builder.AddActivity(_testActivity.Name, _testActivity.ExecuteUri, new
                {
                    Value = "Hello",
                    NullValue = (string)null
                });

                builder.AddActivity(_secondActivity.Name, _secondActivity.ExecuteUri);
            }
        }


        class ResponseProxy :
            RoutingSlipResponseProxy<Request, Response>
        {
            protected override Task<Response> CreateResponseMessage(ConsumeContext<RoutingSlipCompleted> context, Request request)
            {
                return Task.FromResult(new Response());
            }
        }


        [OneTimeSetUp]
        public void Setup()
        {
            _requestClient = CreateRequestClient<Request>(_requestAddress);
        }


        public class Request
        {
        }


        public class Response
        {
        }
    }


    [TestFixture]
    public class Using_a_routing_slip_for_a_request_response_which_faults :
        InMemoryActivityTestFixture
    {
        [Test]
        public async Task Should_fault_the_request()
        {
            Assert.That(async () => await _requestClient.GetResponse<Response>(new Request()), Throws.TypeOf<RequestFaultException>());
        }

        RequestProxy _requestProxy;
        ResponseProxy _responseProxy;
        Uri _requestAddress;
        IRequestClient<Request> _requestClient;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            var testActivity = GetActivityContext<TestActivity>();
            var secondActivity = GetActivityContext<FaultyActivity>();

            _requestProxy = new RequestProxy(testActivity, secondActivity);
            _responseProxy = new ResponseProxy();

            configurator.Instance(_requestProxy);
            configurator.Instance(_responseProxy);

            _requestAddress = configurator.InputAddress;
        }

        protected override void SetupActivities(BusTestHarness testHarness)
        {
            AddActivityContext<TestActivity, TestArguments, TestLog>(() => new TestActivity());
            AddActivityContext<FaultyActivity, FaultyArguments, FaultyLog>(() => new FaultyActivity());
        }


        class RequestProxy :
            RoutingSlipRequestProxy<Request>
        {
            readonly ActivityTestContext _secondActivity;
            readonly ActivityTestContext _testActivity;

            public RequestProxy(ActivityTestContext testActivity, ActivityTestContext secondActivity)
            {
                _testActivity = testActivity;
                _secondActivity = secondActivity;
            }

            protected override async Task BuildRoutingSlip(RoutingSlipBuilder builder, ConsumeContext<Request> request)
            {
                builder.AddActivity(_testActivity.Name, _testActivity.ExecuteUri, new
                {
                    Value = "Hello",
                    NullValue = (string)null
                });

                builder.AddActivity(_secondActivity.Name, _secondActivity.ExecuteUri);
            }
        }


        class ResponseProxy :
            RoutingSlipResponseProxy<Request, Response>
        {
            protected override Task<Response> CreateResponseMessage(ConsumeContext<RoutingSlipCompleted> context, Request request)
            {
                return Task.FromResult(new Response());
            }
        }


        [OneTimeSetUp]
        public void Setup()
        {
            _requestClient = CreateRequestClient<Request>(_requestAddress);
        }


        public class Request
        {
        }


        public class Response
        {
        }
    }


    [TestFixture]
    public class Using_a_routing_slip_for_a_request_response_which_fault_with_fault_response :
        InMemoryActivityTestFixture
    {
        [Test]
        public async Task Should_fault_the_request()
        {
            (_, Task<Response<FaultResponse>> faulted) = await _requestClient.GetResponse<Response, FaultResponse>(new Request());
            await faulted;
        }

        RequestProxy _requestProxy;
        ResponseProxy _responseProxy;
        Uri _requestAddress;
        IRequestClient<Request> _requestClient;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            var testActivity = GetActivityContext<TestActivity>();
            var secondActivity = GetActivityContext<FaultyActivity>();

            _requestProxy = new RequestProxy(testActivity, secondActivity);
            _responseProxy = new ResponseProxy();

            configurator.Instance(_requestProxy);
            configurator.Instance(_responseProxy);

            _requestAddress = configurator.InputAddress;
        }

        protected override void SetupActivities(BusTestHarness testHarness)
        {
            AddActivityContext<TestActivity, TestArguments, TestLog>(() => new TestActivity());
            AddActivityContext<FaultyActivity, FaultyArguments, FaultyLog>(() => new FaultyActivity());
        }


        class RequestProxy :
            RoutingSlipRequestProxy<Request>
        {
            readonly ActivityTestContext _secondActivity;
            readonly ActivityTestContext _testActivity;

            public RequestProxy(ActivityTestContext testActivity, ActivityTestContext secondActivity)
            {
                _testActivity = testActivity;
                _secondActivity = secondActivity;
            }

            protected override async Task BuildRoutingSlip(RoutingSlipBuilder builder, ConsumeContext<Request> request)
            {
                builder.AddActivity(_testActivity.Name, _testActivity.ExecuteUri, new
                {
                    Value = "Hello",
                    NullValue = (string)null
                });

                builder.AddActivity(_secondActivity.Name, _secondActivity.ExecuteUri);
            }
        }


        class ResponseProxy :
            RoutingSlipResponseProxy<Request, Response, FaultResponse>
        {
            protected override Task<Response> CreateResponseMessage(ConsumeContext<RoutingSlipCompleted> context, Request request)
            {
                return Task.FromResult(new Response());
            }

            protected override Task<FaultResponse> CreateFaultedResponseMessage(ConsumeContext<RoutingSlipFaulted> context, Request request, Guid requestId)
            {
                return Task.FromResult(new FaultResponse());
            }
        }


        [OneTimeSetUp]
        public void Setup()
        {
            _requestClient = Bus.CreateRequestClient<Request>(_requestAddress, TimeSpan.FromSeconds(30));
        }


        public class Request
        {
        }


        public class Response
        {
        }


        public class FaultResponse
        {
        }
    }
}
