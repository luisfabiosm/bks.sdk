
using bks.sdk.Core.Authentication;
using bks.sdk.Core.Cryptography;
using bks.sdk.Core.Mediator;
using bks.sdk.Transactions.Base;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public abstract class BaseTransactionHandler<TTransaction, TResponse> : ITransactionHandler<TTransaction, TransactionResult<TResponse>>
        where TTransaction : BaseTransaction<TransactionResult<TResponse>>
    {
        protected readonly IBksLogger Logger;
        protected readonly IBksAuthenticationService AuthenticationService;
        protected readonly ISecureTokenGenerator TokenGenerator;
        protected readonly IEventPublisher EventPublisher;
        protected readonly ActivitySource ActivitySource;

        private static readonly ActivitySource HandlerActivitySource = new("BKS.SDK.TransactionHandler");

        protected BaseTransactionHandler(
            IBksLogger logger,
            IBksAuthenticationService authenticationService,
            ISecureTokenGenerator tokenGenerator,
            IEventPublisher eventPublisher)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AuthenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            TokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
            EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            ActivitySource = HandlerActivitySource;
        }

        /// <summary>
        /// Processa a transação com todas as etapas do pipeline
        /// </summary>
        public async ValueTask<TransactionResult<TResponse>> Handle(TTransaction transaction, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var context = CreateTransactionContext();

            using var activity = ActivitySource.StartActivity($"Handle_{typeof(TTransaction).Name}");
            activity?.SetTag("transaction.id", transaction.TransactionId);
            activity?.SetTag("transaction.correlation_id", transaction.CorrelationId);
            activity?.SetTag("transaction.type", typeof(TTransaction).FullName);

            Logger.LogInformation("Starting transaction processing: {TransactionType} [{TransactionId}]",
                typeof(TTransaction).Name, transaction.TransactionId);

            try
            {
                // 1. Pré-processamento
                await PreProcessing(transaction, context, cancellationToken);

                // 2. Processamento principal
                var result = await Processing(transaction, context, cancellationToken);

                // 3. Pós-processamento
                await PostProcessing(transaction, context, result, cancellationToken);

                stopwatch.Stop();
                result = result with { ProcessingDuration = stopwatch.Elapsed };

                activity?.SetTag("transaction.success", result.Success);
                activity?.SetTag("transaction.duration_ms", stopwatch.ElapsedMilliseconds);

                Logger.LogInformation("Transaction processed successfully: {TransactionType} [{TransactionId}] in {Duration}ms",
                    typeof(TTransaction).Name, transaction.TransactionId, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("transaction.success", false);
                activity?.SetTag("transaction.error", ex.Message);
                activity?.SetTag("transaction.duration_ms", stopwatch.ElapsedMilliseconds);

                Logger.LogError(ex, "Transaction processing failed: {TransactionType} [{TransactionId}]",
                    typeof(TTransaction).Name, transaction.TransactionId);

                var errorResult = await HandleError(transaction, context, ex, stopwatch.Elapsed, cancellationToken);

                // Publicar evento de erro
                await PublishErrorEvent(transaction, context, ex, cancellationToken);

                return errorResult;
            }
        }

        protected virtual async ValueTask PreProcessing(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("PreProcessing");

            Logger.LogDebug("Starting pre-processing for transaction {TransactionId}", transaction.TransactionId);

            // 1. Validação da transação
            await ValidateTransaction(transaction, cancellationToken);

            // 2. Autenticação (se necessária)
            if (transaction.RequiresAuthentication)
            {
                await ValidateAuthentication(transaction, context, cancellationToken);
            }

            // 3. Autorização (verificar permissões)
            await ValidateAuthorization(transaction, context, cancellationToken);

            // 4. Validações de negócio específicas
            await ValidateBusinessRules(transaction, context, cancellationToken);

            // 5. Publicar evento de início
            await PublishStartEvent(transaction, context, cancellationToken);

            // 6. Pré-processamento específico
            await SpecificPreProcessing(transaction, context, cancellationToken);

            Logger.LogDebug("Pre-processing completed for transaction {TransactionId}", transaction.TransactionId);
        }

        protected virtual async ValueTask<TransactionResult<TResponse>> Processing(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("Processing");

            Logger.LogDebug("Starting main processing for transaction {TransactionId}", transaction.TransactionId);

            try
            {
                // Executar lógica específica da transação
                var result = await ExecuteTransaction(transaction, context, cancellationToken);

                // Gerar token seguro se necessário
                string? secureToken = null;
                if (ShouldGenerateSecureToken(transaction, result))
                {
                    var tokenResult = await TokenGenerator.GenerateTokenAsync(transaction);
                    secureToken = tokenResult.Token;
                }

                var successResult = TransactionResult<TResponse>.Success(
                    result,
                    transaction.TransactionId,
                    transaction.CorrelationId,
                    GetSuccessMessage(transaction, result),
                    secureToken: secureToken
                );

                Logger.LogDebug("Main processing completed for transaction {TransactionId}", transaction.TransactionId);

                return successResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during main processing for transaction {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

  
        protected virtual async ValueTask PostProcessing(TTransaction transaction, TransactionContext context, TransactionResult<TResponse> result, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("PostProcessing");

            Logger.LogDebug("Starting post-processing for transaction {TransactionId}", transaction.TransactionId);

            try
            {
                // 1. Auditoria
                await CreateAuditTrail(transaction, context, result, cancellationToken);

                // 2. Publicar eventos de domínio
                await PublishDomainEvents(transaction, context, result, cancellationToken);

                // 3. Pós-processamento específico
                await SpecificPostProcessing(transaction, context, result, cancellationToken);

                // 4. Confirmação de operações
                await ConfirmOperations(transaction, context, result, cancellationToken);

                Logger.LogDebug("Post-processing completed for transaction {TransactionId}", transaction.TransactionId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error during post-processing for transaction {TransactionId}. Transaction was successful but post-processing failed.",
                    transaction.TransactionId);
                // Não relançar a exceção para não afetar o resultado da transação principal
            }
        }

    
        protected virtual async ValueTask<TransactionResult<TResponse>> HandleError(TTransaction transaction, TransactionContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
        {
            using var activity = ActivitySource.StartActivity("HandleError");
            activity?.SetTag("error.type", exception.GetType().Name);
            activity?.SetTag("error.message", exception.Message);

            Logger.LogError(exception, "Handling error for transaction {TransactionId}", transaction.TransactionId);

            try
            {
                // Permitir tratamento específico de erro
                var specificErrorResult = await HandleSpecificError(transaction, context, exception, cancellationToken);
                if (specificErrorResult != null)
                {
                    return specificErrorResult with { ProcessingDuration = duration };
                }

                // Tratamento padrão baseado no tipo de exceção
                var errorResult = exception switch
                {
                    UnauthorizedAccessException => TransactionResult<TResponse>.Error(
                        transaction.TransactionId,
                        transaction.CorrelationId,
                        "Access denied",
                        "UNAUTHORIZED",
                        exception.Message,
                        duration),

                    ArgumentException => TransactionResult<TResponse>.Error(
                        transaction.TransactionId,
                        transaction.CorrelationId,
                        "Invalid argument",
                        "INVALID_ARGUMENT",
                        exception.Message,
                        duration),

                    TimeoutException => TransactionResult<TResponse>.Error(
                        transaction.TransactionId,
                        transaction.CorrelationId,
                        "Operation timeout",
                        "TIMEOUT",
                        exception.Message,
                        duration),

                    OperationCanceledException => TransactionResult<TResponse>.Error(
                        transaction.TransactionId,
                        transaction.CorrelationId,
                        "Operation cancelled",
                        "CANCELLED",
                        exception.Message,
                        duration),

                    _ => TransactionResult<TResponse>.Error(
                        transaction.TransactionId,
                        transaction.CorrelationId,
                        "Internal error",
                        "INTERNAL_ERROR",
                        exception.Message,
                        duration)
                };

                // Criar auditoria do erro
                await CreateErrorAuditTrail(transaction, context, exception, duration, cancellationToken);

                return errorResult;
            }
            catch (Exception handlerException)
            {
                Logger.LogError(handlerException, "Error in error handler for transaction {TransactionId}", transaction.TransactionId);

                return TransactionResult<TResponse>.Error(
                    transaction.TransactionId,
                    transaction.CorrelationId,
                    "Critical error in error handling",
                    "CRITICAL_ERROR",
                    $"Original: {exception.Message}; Handler: {handlerException.Message}",
                    duration);
            }
        }

        #region Abstract and Virtual Methods

        /// <summary>
        /// Executa a lógica específica da transação (deve ser implementado pelas classes derivadas)
        /// </summary>
        protected abstract ValueTask<TResponse> ExecuteTransaction(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Validações de negócio específicas (pode ser sobrescrito pelas classes derivadas)
        /// </summary>
        protected virtual ValueTask ValidateBusinessRules(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Pré-processamento específico (pode ser sobrescrito pelas classes derivadas)
        /// </summary>
        protected virtual ValueTask SpecificPreProcessing(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Pós-processamento específico (pode ser sobrescrito pelas classes derivadas)
        /// </summary>
        protected virtual ValueTask SpecificPostProcessing(TTransaction transaction, TransactionContext context, TransactionResult<TResponse> result, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Tratamento específico de erro (pode ser sobrescrito pelas classes derivadas)
        /// </summary>
        protected virtual ValueTask<TransactionResult<TResponse>?> HandleSpecificError(TTransaction transaction, TransactionContext context, Exception exception, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<TransactionResult<TResponse>?>(null);
        }

        /// <summary>
        /// Determina se deve gerar token seguro para a transação
        /// </summary>
        protected virtual bool ShouldGenerateSecureToken(TTransaction transaction, TResponse result)
        {
            return true; // Por padrão, sempre gera token
        }

        /// <summary>
        /// Obtém a mensagem de sucesso para a transação
        /// </summary>
        protected virtual string GetSuccessMessage(TTransaction transaction, TResponse result)
        {
            return "Transaction completed successfully";
        }

        #endregion

        #region Private Helper Methods

        private async ValueTask ValidateTransaction(TTransaction transaction, CancellationToken cancellationToken)
        {
            var validationResult = transaction.Validate();
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                throw new ArgumentException($"Transaction validation failed: {errors}");
            }

            // Verificar timeout
            var elapsed = DateTimeOffset.UtcNow - transaction.CreatedAt;
            if (elapsed > transaction.Timeout)
            {
                throw new TimeoutException($"Transaction has expired. Created at {transaction.CreatedAt}, timeout is {transaction.Timeout}");
            }
        }

        private async ValueTask ValidateAuthentication(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            var authContext = AuthenticationService.GetCurrentContext();
            if (authContext == null || authContext.ApplicationId != context.ApplicationId)
            {
                throw new UnauthorizedAccessException("Invalid or missing authentication");
            }
        }

        private async ValueTask ValidateAuthorization(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            var requiredPermissions = transaction.RequiredPermissions.ToList();
            if (requiredPermissions.Any() && !context.HasAllPermissions(requiredPermissions))
            {
                var missing = requiredPermissions.Where(p => !context.HasPermission(p));
                throw new UnauthorizedAccessException($"Missing required permissions: {string.Join(", ", missing)}");
            }
        }

        private TransactionContext CreateTransactionContext()
        {
            var authContext = AuthenticationService.GetCurrentContext();
            if (authContext == null)
            {
                throw new UnauthorizedAccessException("No authentication context available");
            }

            return new TransactionContext
            {
                ApplicationId = authContext.ApplicationId,
                ApplicationName = authContext.ApplicationName,
                UserId = null, // Pode ser definido por classes derivadas
                Permissions = authContext.Permissions,
                SessionId = authContext.SessionId,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };
        }

        private async ValueTask PublishStartEvent(TTransaction transaction, TransactionContext context, CancellationToken cancellationToken)
        {
            var startEvent = new TransactionStartedEvent
            {
                TransactionId = transaction.TransactionId,
                CorrelationId = transaction.CorrelationId,
                TransactionType = typeof(TTransaction).FullName!,
                ApplicationId = context.ApplicationId,
                UserId = context.UserId,
                StartedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>(transaction.Metadata)
            };

            await EventPublisher.PublishAsync(startEvent, cancellationToken);
        }

        private async ValueTask PublishDomainEvents(TTransaction transaction, TransactionContext context, TransactionResult<TResponse> result, CancellationToken cancellationToken)
        {
            var completedEvent = new TransactionCompletedEvent
            {
                TransactionId = transaction.TransactionId,
                CorrelationId = transaction.CorrelationId,
                TransactionType = typeof(TTransaction).FullName!,
                ApplicationId = context.ApplicationId,
                UserId = context.UserId,
                CompletedAt = DateTimeOffset.UtcNow,
                Success = result.Success,
                Duration = result.ProcessingDuration,
                SecureToken = result.SecureToken,
                Metadata = new Dictionary<string, object>(result.Metadata)
            };

            await EventPublisher.PublishAsync(completedEvent, cancellationToken);
        }

        private async ValueTask PublishErrorEvent(TTransaction transaction, TransactionContext context, Exception exception, CancellationToken cancellationToken)
        {
            var errorEvent = new TransactionErrorEvent
            {
                TransactionId = transaction.TransactionId,
                CorrelationId = transaction.CorrelationId,
                TransactionType = typeof(TTransaction).FullName!,
                ApplicationId = context.ApplicationId,
                UserId = context.UserId,
                ErroredAt = DateTimeOffset.UtcNow,
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message,
                ErrorDetail = exception.ToString(),
                Metadata = new Dictionary<string, object>(transaction.Metadata)
            };

            await EventPublisher.PublishAsync(errorEvent, cancellationToken);
        }

        private async ValueTask CreateAuditTrail(TTransaction transaction, TransactionContext context, TransactionResult<TResponse> result, CancellationToken cancellationToken)
        {
            var auditInfo = TransactionAuditInfo.ForCompletion(transaction, context, result, result.ProcessingDuration);

            var auditEvent = new TransactionAuditEvent
            {
                AuditInfo = auditInfo,
                EventType = "TransactionCompleted"
            };

            await EventPublisher.PublishAsync(auditEvent, cancellationToken);
        }

        private async ValueTask CreateErrorAuditTrail(TTransaction transaction, TransactionContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
        {
            var errorResult = TransactionResult<TResponse>.FromException(exception, transaction.TransactionId, transaction.CorrelationId, duration);
            var auditInfo = TransactionAuditInfo.ForCompletion(transaction, context, errorResult, duration);

            var auditEvent = new TransactionAuditEvent
            {
                AuditInfo = auditInfo,
                EventType = "TransactionError"
            };

            await EventPublisher.PublishAsync(auditEvent, cancellationToken);
        }

        private async ValueTask ConfirmOperations(TTransaction transaction, TransactionContext context, TransactionResult<TResponse> result, CancellationToken cancellationToken)
        {
            // Implementação específica para confirmação de operações
            // Pode incluir confirmação de operações bancárias, envio de emails, etc.
            Logger.LogDebug("Confirming operations for transaction {TransactionId}", transaction.TransactionId);
        }

        #endregion
    }

}
