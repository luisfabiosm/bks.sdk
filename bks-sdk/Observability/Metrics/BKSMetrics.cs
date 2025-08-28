using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Metrics
{
    public static class BKSMetrics
    {
        public static readonly Meter Meter = new("bks.sdk", "1.0.6");

        // Contadores para transações
        public static readonly Counter<long> TransactionStarted = Meter.CreateCounter<long>(
            "bks_transactions_started_total");

        public static readonly Counter<long> TransactionCompleted = Meter.CreateCounter<long>(
            "bks_transactions_completed_total");

        public static readonly Counter<long> TransactionFailed = Meter.CreateCounter<long>(
            "bks_transactions_failed_total");

        // Histogram para duração das transações
        public static readonly Histogram<double> TransactionDuration = Meter.CreateHistogram<double>(
            "bks_transaction_duration_seconds");

        // Gauge para transações ativas
        public static readonly UpDownCounter<long> ActiveTransactions = Meter.CreateUpDownCounter<long>(
            "bks_active_transactions");
    }
}
