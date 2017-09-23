using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Newtonsoft.Json;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Individual.Interfaces;

namespace Dispatcher
{
    internal class EventProcessor : IEventProcessor
    {

        private readonly IReliableStateManager stateManager;
        private IReliableDictionary<string, string> dictionary;

        public EventProcessor(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.CompletedTask;
        }

        public async Task OpenAsync(PartitionContext context)
        {
            dictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("DeviceToIndividualDictionary");
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                await ProcessMessageAsync(data);

                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
            }

            await context.CheckpointAsync();
        }

        private async Task ProcessMessageAsync(string jsonMessage)
        {
            dynamic message = JsonConvert.DeserializeObject(jsonMessage);

            string deviceId = message.devicemac;
            string userId = null;

            // Look up the device in the dictionary
            using (var tx = this.stateManager.CreateTransaction())
            {
                var result = await this.dictionary.TryGetValueAsync(tx, deviceId);
                if (result.HasValue)
                    userId = result.Value;
            }

            if (userId != null)
            {
                await InvokeActorAsync(userId, jsonMessage);
            }

        }

        private async Task InvokeActorAsync(string userId, string jsonMessage)
        {
            ActorId actorId = new ActorId(userId);

            var individualActor = ActorProxy.Create<IIndividual>(actorId, new Uri("fabric:/TestReliableActors/IndividualActorService"));

            await individualActor.ProcessMessage(userId, jsonMessage);
        }
    }
}
