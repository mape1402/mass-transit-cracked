﻿namespace MassTransit.Tests.SagaStateMachineTests.Dynamic_Modify
{
    using System;
    using NUnit.Framework;
    using SagaStateMachine;


    [TestFixture(Category = "Dynamic Modify")]
    public class When_an_event_is_declared
    {
        [Test]
        public void It_should_capture_a_simple_event_name()
        {
            Assert.AreEqual("Hello", _hello.Name);
        }

        [Test]
        public void It_should_capture_the_data_event_name()
        {
            Assert.AreEqual("EventA", _eventA.Name);
        }

        [Test]
        public void It_should_create_configured_events()
        {
            Assert.IsInstanceOf<TriggerEvent>(_eventB);
        }

        [Test]
        public void It_should_create_the_proper_event_type_for_data_events()
        {
            Assert.IsInstanceOf<MessageEvent<A>>(_eventA);
        }

        [Test]
        public void It_should_create_the_proper_event_type_for_simple_events()
        {
            Assert.IsInstanceOf<TriggerEvent>(_hello);
        }

        Event _hello;
        Event<A> _eventA;
        Event<B> _eventB;


        class Instance :
            SagaStateMachineInstance
        {
            public State CurrentState { get; set; }
            public Guid CorrelationId { get; set; }
        }


        [OneTimeSetUp]
        public void A_state_is_declared()
        {
            MassTransitStateMachine<Instance>
                .New(builder => builder
                    .Event("Hello", out _hello)
                    .Event("EventA", out _eventA)
                    .Event("EventB", x => x.CorrelateById(ctx => ctx.Message.Id), out _eventB)
                );
        }


        class A
        {
        }


        class B
        {
            public Guid Id { get; set; }
        }
    }
}
