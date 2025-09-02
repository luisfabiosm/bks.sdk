using bks.sdk.Observability.Logging;
using Domain.Core.Entities;
using Domain.Core.Ports.Domain;

namespace Domain.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IBKSLogger _logger;
        private static readonly List<NotificationEntry> _notifications = new();

        public NotificationService(IBKSLogger logger)
        {
            _logger = logger;
        }

        public async Task NotifyTransactionCompletedAsync(
            Conta conta,
            string transactionType,
            DateTime completedAt,
            CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken); // Simula envio de notificação

            var notification = new NotificationEntry
            {
                Id = Guid.NewGuid().ToString(),
                Type = "TRANSACTION_COMPLETED",
                Recipient = conta.Titular,
                AccountNumber = conta.Numero,
                TransactionType = transactionType,
                Timestamp = completedAt,
                Message = CreateCompletionMessage(transactionType, conta.Saldo),
                Channel = DetermineNotificationChannel(conta.Titular),
                Status = "SENT"
            };

            _notifications.Add(notification);

            _logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Information,
                "Notification sent",
                notification);

            _logger.Info($"📱 NOTIFICAÇÃO: {notification.Message} para {conta.Titular} ({notification.Channel})");
        }

        public async Task NotifyHighValueTransactionAsync(
            int accountNumber,
            string transactionType,
            decimal amount,
            CancellationToken cancellationToken)
        {
            await Task.Delay(15, cancellationToken);

            var notification = new NotificationEntry
            {
                Id = Guid.NewGuid().ToString(),
                Type = "HIGH_VALUE_ALERT",
                AccountNumber = accountNumber,
                TransactionType = transactionType,
                Timestamp = DateTime.UtcNow,
                Message = $"🚨 ALERTA: Transação de alto valor {transactionType} de {amount:C} processada na conta {accountNumber}",
                Channel = "SMS + EMAIL + PUSH",
                Status = "SENT",
                Priority = "HIGH"
            };

            _notifications.Add(notification);

            _logger.Warn(notification.Message);
        }

        public async Task NotifyLowBalanceAsync(
            Conta conta,
            decimal threshold,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            var notification = new NotificationEntry
            {
                Id = Guid.NewGuid().ToString(),
                Type = "LOW_BALANCE_WARNING",
                Recipient = conta.Titular,
                AccountNumber = conta.Numero,
                Timestamp = DateTime.UtcNow,
                Message = $"⚠️ Saldo baixo: Sua conta {conta.Numero} está com saldo de {conta.Saldo:C}, abaixo do limite de {threshold:C}",
                Channel = DetermineNotificationChannel(conta.Titular),
                Status = "SENT",
                Priority = "MEDIUM"
            };

            _notifications.Add(notification);

            _logger.Info(notification.Message);
        }

        private string CreateCompletionMessage(string transactionType, decimal newBalance)
        {
            var operacao = transactionType.ToLowerInvariant() switch
            {
                "creditotransaction" => "Crédito",
                "debitotransaction" => "Débito",
                _ => "Transação"
            };

            return $"✅ {operacao} processado com sucesso! Seu novo saldo é {newBalance:C}";
        }

        private string DetermineNotificationChannel(string titular)
        {
            // Lógica simples para determinar canal baseado no nome
            if (titular.Contains("Empresa", StringComparison.OrdinalIgnoreCase) ||
                titular.Contains("Ltda", StringComparison.OrdinalIgnoreCase))
            {
                return "EMAIL";
            }

            return "SMS + PUSH";
        }

        public async Task<IEnumerable<NotificationEntry>> GetNotificationHistoryAsync(
            int accountNumber,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            var notifications = _notifications.AsEnumerable();

            if (accountNumber==0)
            {
                notifications = notifications.Where(n => n.AccountNumber == accountNumber);
            }

            return notifications.OrderByDescending(n => n.Timestamp).Take(50).ToList();
        }

        public static void ClearNotifications()
        {
            _notifications.Clear();
        }
    }

}
