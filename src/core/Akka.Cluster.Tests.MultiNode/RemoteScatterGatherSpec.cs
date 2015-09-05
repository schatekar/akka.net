//-----------------------------------------------------------------------
// <copyright file="ClusterDeathWatchSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote;
using Akka.Remote.TestKit;
using Akka.Routing;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Akka.TestKit.TestEvent;
using Xunit;

namespace Akka.Cluster.Tests.MultiNode
{
    public class RemoteScatterGatherSpecConfig : MultiNodeConfig
    {
        public sealed class Hit { }

        #region Internal Actor Classes

        public class SomeActor : UntypedActor
        {
            protected override void OnReceive(object message)
            {
                if(message is Hit) Sender.Tell(Self);
            }
        }

        #endregion

        readonly RoleName _first;
        public RoleName First { get { return _first; } }
        readonly RoleName _second;
        public RoleName Second { get { return _second; } }
        readonly RoleName _third;
        public RoleName Third { get { return _third; } }
        readonly RoleName _fourth;
        public RoleName Fourth { get { return _fourth; } }

        public RemoteScatterGatherSpecConfig()
        {
            _first = Role("first");
            _second = Role("second");
            _third = Role("third");
            _fourth = Role("fourth");

            CommonConfig = MultiNodeLoggingConfig.LoggingConfig.WithFallback(DebugConfig(false))
                .WithFallback(MultiNodeClusterSpec.ClusterConfig());

            DeployOnAll(@"
                /service-hello {
                    router = ""scatter-gather-pool""
                    nr-of-instances = 3
                    target.nodes = [""@first@"", ""@second@"", ""@third@""]
                    }");
        }
    }

    public class RemoteScatterGatherSpecNode1 : RemoteScatterGatherSpec { }
    public class RemoteScatterGatherSpecNode2 : RemoteScatterGatherSpec { }
    public class RemoteScatterGatherSpecNode3 : RemoteScatterGatherSpec { }
    public class RemoteScatterGatherSpecNode4 : RemoteScatterGatherSpec { }

    public abstract class RemoteScatterGatherSpec : MultiNodeSpec
    {
        private readonly RemoteScatterGatherSpecConfig _config;
        public RemoteScatterGatherSpec() : this(new RemoteScatterGatherSpecConfig())
        {

        }

        public RemoteScatterGatherSpec(RemoteScatterGatherSpecConfig config) : base(config)
        {
            _config = config;
        }

        protected override int InitialParticipantsValueFactory
        {
            get
            {
                return Roles.Count;
            }
        }

        [MultiNodeFact]
        public void RemoteScatterGatherSpecTests()
        {
            ARemoteScatterGatherFirstCompletePoolMustBeLocallyInstantiatedOnARemoteNodeAndBeAbleToCommunicateThroughItsRemoteActorRef();
        }

        public void ARemoteScatterGatherFirstCompletePoolMustBeLocallyInstantiatedOnARemoteNodeAndBeAbleToCommunicateThroughItsRemoteActorRef()
        {
            RunOn(() =>
            {
                EnterBarrier("start", "broadcast-end", "end", "done");
            }, _config.First, _config.Second, _config.Third);

            RunOn(() =>
            {
                Sys.EventStream.Publish(EventFilter.Warning(pattern: new Regex(".*received dead letter from.*")).Mute());

                EnterBarrier("start");
                var actor = Sys.ActorOf(Props.Create<RemoteScatterGatherSpecConfig.SomeActor>().WithRouter(new ScatterGatherFirstCompletedPool(1, TimeSpan.FromSeconds(10))), "service-hello");
                var foo = Assert.IsType<RoutedActorRef>(actor);

                var connectionCount = 3;
                var iterationCount = 10;

                for (int i = 0; i < connectionCount * iterationCount; i++)
                {
                    actor.Tell(new RemoteScatterGatherSpecConfig.Hit());
                }

                var replies = ReceiveWhile<IActorRef>(TimeSpan.FromSeconds(5), x => x as IActorRef, connectionCount * iterationCount)
                                .GroupBy(r => r.Path.Address)
                                .Select(g => new {
                                                     Address = g.Key,
                                                     Count = g.Count()
                });
                EnterBarrier("broadcast-end");

                actor.Tell(new Broadcast(PoisonPill.Instance));

                EnterBarrier("end");

                replies.Select(r => r.Count).Aggregate((a, b) => a + b).ShouldBe(connectionCount * iterationCount);
                replies.FirstOrDefault(r => r.Address == TestConductor.GetAddressFor(_config.Fourth).Result).ShouldBe(null);

                // shut down the actor before we let the other node(s) shut down so we don't try to send
                // "Terminate" to a shut down node
                Sys.Stop(actor);

                EnterBarrier("done");

            }, _config.Fourth);

            EnterBarrier("done");
        }
    }
}
