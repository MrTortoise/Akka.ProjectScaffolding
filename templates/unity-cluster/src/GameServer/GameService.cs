﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Configuration;
using Domain.Interface;

namespace GameServer
{
    public class GameService
    {
        public async Task RunAsync(string[] args, CancellationToken cancellationToken)
        {
            // force interface assembly to be loaded before creating ProtobufSerializer

            var type = typeof(IUser);
            if (type == null)
                throw new InvalidProgramException("!");

            // run cluster nodes

            var clusterRunner = CreateClusterRunner();

            var standAlone = args.Length > 0 && args[0] == "standalone";
            if (standAlone)
            {
                clusterRunner.LaunchNode(3001, 9001, "user-table", "user");
            }
            else
            {
                clusterRunner.LaunchNode(3001, 0, "user-table");
                clusterRunner.LaunchNode(3011, 9001, "user");
                clusterRunner.LaunchNode(3012, 9002, "user");
            }

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation exception
            }
        }

        private ClusterRunner CreateClusterRunner()
        {
            var commonConfig = ConfigurationFactory.ParseString(@"
                akka {
                  actor {
                    provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    serializers {
                      wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
                      proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced.ProtobufSerializer""
                    }
                    serialization-bindings {
                      ""Akka.Interfaced.NotificationMessage, Akka.Interfaced"" = proto
                      ""Akka.Interfaced.RequestMessage, Akka.Interfaced"" = proto
                      ""Akka.Interfaced.ResponseMessage, Akka.Interfaced"" = proto
                      ""System.Object"" = wire
                    }
                  }
                  remote {
                    helios.tcp {
                      hostname = ""127.0.0.1""
                    }
                  }
                  cluster {
                    seed-nodes = [""akka.tcp://GameCluster@127.0.0.1:3001""]
                    auto-down-unreachable-after = 30s
                  }
                }");

            return new ClusterRunner(commonConfig);
        }
    }
}