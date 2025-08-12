using BKS.SDK.Core.Observability;
using BKS.SDK.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Mediator
{
    internal sealed class BksMediator : IBksMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBksLogger _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly ActivitySource _activitySource;
        private readonly ConcurrentDictionary<Type, HandlerInfo> _handlerCache;

        private static readonly ActivitySource ActivitySource = new("BKS.SDK.Mediator");

        public BksMediator(
            IServiceProvider serviceProvider,
            IBksLogger logger,
            IEventPublisher eventPublisher)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _activitySource = ActivitySource;
            _handlerCache = new ConcurrentDictionary<Type, HandlerInfo>();
        }

        public async ValueTask<TResponse> Send<TResponse>(ITransaction<TResponse> transaction, CancellationToken cancellationToken = default)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var transactionType = transaction.GetType();
            using var activity = _activitySource.StartActivity($"Process_{transactionType.Name}");

            activity?.SetTag("transaction.id", transaction.TransactionId);
            activity?.SetTag("transaction.correlation_id", transaction.CorrelationId);
            activity?.SetTag("transaction.type", transactionType.FullName);

            _logger.LogInformation("Processing transaction {TransactionType} with ID {TransactionId}",
                transactionType.Name, transaction.TransactionId);

            try
            {
                var handlerInfo = GetOrCacheHandlerInfo(transactionType, typeof(TResponse));
                if (handlerInfo == null)
                {
                    throw new InvalidOperationException($"No handler registered for transaction type {transactionType.FullName}");
                }

                var handler = _serviceProvider.GetService(handlerInfo.HandlerType);
                if (handler == null)
                {
                    throw new InvalidOperationException($"Handler {handlerInfo.HandlerType.FullName} not registered in DI container");
                }

                // Usar reflexão para chamar o método Handle
                var handleMethod = handlerInfo.HandlerType.GetMethod("Handle");
                if (handleMethod == null)
                {
                    throw new InvalidOperationException($"Handle method not found in {handlerInfo.HandlerType.FullName}");
                }

                var result = handleMethod.Invoke(handler, new object[] { transaction, cancellationToken });

                if (result is ValueTask<TResponse> valueTask)
                {
                    var response = await valueTask;

                    activity?.SetTag("transaction.success", true);
                    _logger.LogInformation("Transaction {TransactionType} processed successfully", transactionType.Name);

                    // Publicar evento de transação processada
                    await PublishTransactionProcessedEvent(transaction, response, cancellationToken);

                    return response;
                }
                else if (result is Task<TResponse> task)
                {
                    var response = await task;

                    activity?.SetTag("transaction.success", true);
                    _logger.LogInformation("Transaction {TransactionType} processed successfully", transactionType.Name);

                    await PublishTransactionProcessedEvent(transaction, response, cancellationToken);

                    return response;
                }
                else
                {
                    throw new InvalidOperationException($"Handler must return ValueTask<{typeof(TResponse).Name}> or Task<{typeof(TResponse).Name}>");
                }
            }
            catch (Exception ex)
            {
                activity?.SetTag("transaction.success", false);
                activity?.SetTag("transaction.error", ex.Message);

                _logger.LogError(ex, "Failed to process transaction {TransactionType} with ID {TransactionId}",
                    transactionType.Name, transaction.TransactionId);

                // Publicar evento de erro
                await PublishTransactionErrorEvent(transaction, ex, cancellationToken);

                throw;
            }
        }

        public async ValueTask Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            await _eventPublisher.PublishAsync(@event, cancellationToken);
        }

        public bool HasHandler<TResponse>(ITransaction<TResponse> transaction)
        {
            if (transaction == null)
                return false;

            var transactionType = transaction.GetType();
            var handlerInfo = GetOrCacheHandlerInfo(transactionType, typeof(TResponse));

            return handlerInfo != null && _serviceProvider.GetService(handlerInfo.HandlerType) != null;
        }

        public IEnumerable<HandlerInfo> GetRegisteredHandlers()
        {
            return _handlerCache.Values.ToList();
        }

        private HandlerInfo? GetOrCacheHandlerInfo(Type transactionType, Type responseType)
        {
            return _handlerCache.GetOrAdd(transactionType, _ => FindHandlerInfo(transactionType, responseType));
        }

        private HandlerInfo? FindHandlerInfo(Type transactionType, Type responseType)
        {
            // Procurar por ITransactionHandler<TTransaction, TResponse>
            var handlerInterfaceType = typeof(ITransactionHandler<,>).MakeGenericType(transactionType, responseType);

            // Verificar se existe um handler registrado no DI
            var handlerType = FindImplementationType(handlerInterfaceType);
            if (handlerType == null)
                return null;

            return new HandlerInfo
            {
                TransactionType = transactionType,
                ResponseType = responseType,
                HandlerType = handlerType,
                HandlerName = handlerType.Name
            };
        }

        private Type? FindImplementationType(Type interfaceType)
        {
            // Esta é uma implementação simplificada
            // Em uma implementação completa, você poderia usar reflection para encontrar
            // todas as implementações registradas no DI container

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
                        .ToList();

                    if (types.Any())
                    {
                        return types.First();
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Ignorar assemblies que não podem ser carregados
                    continue;
                }
            }

            return null;
        }

        private async ValueTask PublishTransactionProcessedEvent<TResponse>(
            ITransaction<TResponse> transaction,
            TResponse response,
            CancellationToken cancellationToken)
        {
            try
            {
                var @event = new TransactionProcessedEvent
                {
                    TransactionId = transaction.TransactionId,
                    CorrelationId = transaction.CorrelationId,
                    TransactionType = transaction.GetType().FullName!,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Success = true,
                    Metadata = new Dictionary<string, object>(transaction.Metadata)
                };

                await _eventPublisher.PublishAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish transaction processed event");
            }
        }

        private async ValueTask PublishTransactionErrorEvent<TResponse>(
            ITransaction<TResponse> transaction,
            Exception exception,
            CancellationToken cancellationToken)
        {
            try
            {
                var @event = new TransactionProcessedEvent
                {
                    TransactionId = transaction.TransactionId,
                    CorrelationId = transaction.CorrelationId,
                    TransactionType = transaction.GetType().FullName!,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Success = false,
                    ErrorMessage = exception.Message,
                    Metadata = new Dictionary<string, object>(transaction.Metadata)
                };

                await _eventPublisher.PublishAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish transaction error event");
            }
        }
    }

}
