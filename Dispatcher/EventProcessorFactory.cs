using Microsoft.Azure.EventHubs.Processor;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dispatcher
{
    internal class EventProcessorFactory : IEventProcessorFactory
    {
        private readonly IReliableStateManager stateManager;

        public EventProcessorFactory(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(stateManager);
        }

    }
}
