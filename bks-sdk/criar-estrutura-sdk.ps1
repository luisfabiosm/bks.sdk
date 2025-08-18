# criar-estrutura-sdk.ps1
$root = "bks-sdk-v99"

$folders = @(
    "$root/Core/Configuration",
    "$root/Core/Initialization",
    "$root/Authentication/Implementations",
    "$root/Transactions",
    "$root/Common/Results",
    "$root/Observability/Logging",
    "$root/Observability/Tracing",
    "$root/Cache/Implementations",
    "$root/Events/Implementations",
    "$root/Mediator/Implementations"
)

$files = @(
    # Core
    "$root/Core/Configuration/SDKSettings.cs",
    "$root/Core/Configuration/SDKConfiguration.cs",
    "$root/Core/Initialization/SDKInitializer.cs",
    # Authentication
    "$root/Authentication/ILicenseValidator.cs",
    "$root/Authentication/IJwtTokenProvider.cs",
    "$root/Authentication/Implementations/LicenseValidator.cs",
    "$root/Authentication/Implementations/JwtTokenProvider.cs",
    # Transactions
    "$root/Transactions/BaseTransaction.cs",
    "$root/Transactions/ITransactionProcessor.cs",
    "$root/Transactions/TransactionProcessor.cs",
    # Common
    "$root/Common/Results/Result.cs",
    # Observability
    "$root/Observability/Logging/ILogger.cs",
    "$root/Observability/Logging/SerilogLogger.cs",
    "$root/Observability/Tracing/ITracer.cs",
    "$root/Observability/Tracing/OpenTelemetryTracer.cs",
    # Cache
    "$root/Cache/ICacheProvider.cs",
    "$root/Cache/Implementations/RedisCacheProvider.cs",
    # Events
    "$root/Events/EventBrokerType.cs",
    "$root/Events/IDomainEvent.cs",
    "$root/Events/IEventBroker.cs",
    "$root/Events/DomainEvent.cs",
    "$root/Events/Implementations/RabbitMQEventBroker.cs",
    "$root/Events/Implementations/KafkaEventBroker.cs",
    "$root/Events/Implementations/GooglePubSubEventBroker.cs",
    # Mediator
    "$root/Mediator/IRequest.cs",
    "$root/Mediator/IRequestHandler.cs",
    "$root/Mediator/IMediator.cs",
    "$root/Mediator/Implementations/Mediator.cs"
)

# Criar pastas
foreach ($folder in $folders) {
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder | Out-Null
    }
}

# Criar arquivos vazios
foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        New-Item -ItemType File -Path $file | Out-Null
    }
}

Write-Host "Estrutura do SDK criada com sucesso!"