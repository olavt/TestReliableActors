using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.EventHubs;

namespace Dispatcher
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Dispatcher : StatefulService
    {
        private const string EhConnectionString = "<Replace with connection string to your Event Hub, for IoT Hub find the Event Hub-compatible endpoint>";
        private const string EhEntityPath = "<Replace with the Event Hub name, for IoT Hub find the Event Hub-compatible name>";
        private const string StorageConnectionString = "<Replace with your storage account connection string>";
        private const string StorageContainerName = "eventprocessor";

        public Dispatcher(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await CreateDeviceToIndividualDictionary();

            var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("DeviceToIndividualDictionary");

            var eventProcessorHost = new EventProcessorHost(
                EhEntityPath,
                PartitionReceiver.DefaultConsumerGroupName,
                EhConnectionString,
                StorageConnectionString,
                StorageContainerName);

            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(this.StateManager));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task CreateDeviceToIndividualDictionary()
        {
            var dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("DeviceToIndividualDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                await dictionary.AddAsync(tx, "80:ea:ca:00:00:bf", "1");
                await dictionary.AddAsync(tx, "80:ea:ca:00:01:c0", "2");

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }
        }
    }
}
