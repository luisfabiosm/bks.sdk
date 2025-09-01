using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Exceptions;

public abstract class BKSFrameworkException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Properties { get; }

    protected BKSFrameworkException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
        Properties = new Dictionary<string, object>();
    }

    protected BKSFrameworkException(string errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
        Properties = new Dictionary<string, object>();
    }

    public BKSFrameworkException AddProperty(string key, object value)
    {
        Properties[key] = value;
        return this;
    }

    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        return default;
    }
}

public class ValidationException : BKSFrameworkException
{
    public IEnumerable<string> ValidationErrors { get; }

    public ValidationException(IEnumerable<string> validationErrors)
        : base("VALIDATION_FAILED", "Validation failed")
    {
        ValidationErrors = validationErrors;
    }

    public ValidationException(string validationError)
        : base("VALIDATION_FAILED", validationError)
    {
        ValidationErrors = new[] { validationError };
    }
}

public class BusinessRuleException : BKSFrameworkException
{
    public BusinessRuleException(string message)
        : base("BUSINESS_RULE_VIOLATION", message)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base("BUSINESS_RULE_VIOLATION", message, innerException)
    {
    }
}

public class ConfigurationException : BKSFrameworkException
{
    public ConfigurationException(string message)
        : base("CONFIGURATION_ERROR", message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base("CONFIGURATION_ERROR", message, innerException)
    {
    }
}

public class ProcessingException : BKSFrameworkException
{
    public ProcessingException(string message)
        : base("PROCESSING_ERROR", message)
    {
    }

    public ProcessingException(string message, Exception innerException)
        : base("PROCESSING_ERROR", message, innerException)
    {
    }
}
