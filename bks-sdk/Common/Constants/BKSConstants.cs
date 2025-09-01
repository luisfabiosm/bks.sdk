using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Constants;


public static class BKSConstants
{
    public const string FrameworkName = "BKS Framework";
    public const string FrameworkVersion = "1.0.0";

    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-ID";
        public const string RequestId = "X-Request-ID";
        public const string TraceId = "X-Trace-ID";
        public const string SpanId = "X-Span-ID";
        public const string FrameworkVersion = "X-BKS-Framework-Version";
        public const string ServerName = "X-Server-Name";
        public const string RequestTime = "X-Request-Time";
    }

    public static class ConfigurationSections
    {
        public const string Root = "BKSFramework";
        public const string Processing = "BKSFramework:Processing";
        public const string Security = "BKSFramework:Security";
        public const string Events = "BKSFramework:Events";
        public const string Observability = "BKSFramework:Observability";
    }

    public static class EventTypes
    {
        public const string TransactionStarted = "pipeline.transaction.started";
        public const string TransactionProcessing = "pipeline.transaction.processing";
        public const string TransactionCompleted = "pipeline.transaction.completed";
        public const string TransactionFailed = "pipeline.transaction.failed";
        public const string TransactionCancelled = "pipeline.transaction.cancelled";
    }

    public static class MetricNames
    {
        public const string TransactionDuration = "bks_transaction_duration_seconds";
        public const string TransactionCount = "bks_transaction_count_total";
        public const string PipelineStepDuration = "bks_pipeline_step_duration_seconds";
        public const string ValidationDuration = "bks_validation_duration_seconds";
    }

    public static class ActivityNames
    {
        public const string Pipeline = "BKS.Pipeline";
        public const string Validation = "BKS.Validation";
        public const string Processing = "BKS.Processing";
        public const string Events = "BKS.Events";
        public const string Security = "BKS.Security";
    }

    public static class ClaimTypes
    {
        public const string CorrelationId = "correlation_id";
        public const string ApplicationName = "app_name";
        public const string ProcessorType = "processor_type";
    }

    public static class Timeouts
    {
        public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DefaultValidationTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan DefaultProcessingTimeout = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan DefaultEventPublishTimeout = TimeSpan.FromSeconds(10);
    }
}