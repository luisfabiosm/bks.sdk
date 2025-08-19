using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Implementations
{

    public class InMemoryEventBroker : IEventBroker
    {
        private readonly DomainEventDispatcher _dispatcher;

        public InMemoryEventBroker(DomainEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            await _dispatcher.DispatchAsync(domainEvent);
        }

        public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
        {
            _dispatcher.RegisterHandler(async (@event) =>
            {
                if (@event is TEvent typedEvent)
                {
                    await handler(typedEvent);
                }
            });

            return Task.CompletedTask;
        }
    }


}
