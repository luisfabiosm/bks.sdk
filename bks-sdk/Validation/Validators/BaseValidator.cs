using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Validation.Abstractions;
using bks.sdk.Validation.Rules;


namespace bks.sdk.Validation.Validators;

public abstract class BaseValidator<T> : IValidator<T>
{
    protected readonly IBKSLogger Logger;
    private readonly List<IValidationRule<T>> _rules = new();
    private readonly List<IAsyncValidationRule<T>> _asyncRules = new();
    private readonly List<ISyncValidationRule<T>> _syncRules = new();

    protected BaseValidator(IBKSLogger logger)
    {
        Logger = logger;
        ConfigureRules();
    }

    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        if (instance == null)
        {
            return ValidationResult.Failure("Instance cannot be null");
        }

        var errors = new List<string>();

        try
        {
            Logger.Trace($"Iniciando validação assíncrona para {typeof(T).Name}");

            // Executar regras síncronas
            foreach (var rule in _syncRules)
            {
                if (!rule.IsValid(instance))
                {
                    errors.Add($"{rule.RuleName}: {rule.ErrorMessage}");
                }
            }

            // Executar regras assíncronas
            foreach (var rule in _asyncRules)
            {
                if (!await rule.IsValidAsync(instance, cancellationToken))
                {
                    errors.Add($"{rule.RuleName}: {rule.ErrorMessage}");
                }
            }

            // Executar regras híbridas
            foreach (var rule in _rules)
            {
                var isValid = rule is IAsyncValidationRule<T> asyncRule
                    ? await asyncRule.IsValidAsync(instance, cancellationToken)
                    : rule.IsValid(instance);

                if (!isValid)
                {
                    errors.Add($"{rule.RuleName}: {rule.ErrorMessage}");
                }
            }

            // Executar validações customizadas
            var customErrors = await ExecuteCustomValidationsAsync(instance, cancellationToken);
            errors.AddRange(customErrors);

            var result = errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(errors);

            Logger.Trace($"Validação concluída para {typeof(T).Name} - Válido: {result.IsValid}, Erros: {errors.Count}");

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Erro durante validação de {typeof(T).Name}");
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public ValidationResult Validate(T instance)
    {
        if (instance == null)
        {
            return ValidationResult.Failure("Instance cannot be null");
        }

        var errors = new List<string>();

        try
        {
            Logger.Trace($"Iniciando validação síncrona para {typeof(T).Name}");

            // Executar apenas regras síncronas
            foreach (var rule in _syncRules)
            {
                if (!rule.IsValid(instance))
                {
                    errors.Add($"{rule.RuleName}: {rule.ErrorMessage}");
                }
            }

            // Executar regras híbridas síncronamente
            foreach (var rule in _rules.Where(r => r is ISyncValidationRule<T>))
            {
                if (!rule.IsValid(instance))
                {
                    errors.Add($"{rule.RuleName}: {rule.ErrorMessage}");
                }
            }

            // Executar validações customizadas síncronas
            var customErrors = ExecuteCustomValidations(instance);
            errors.AddRange(customErrors);

            var result = errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(errors);

            Logger.Trace($"Validação síncrona concluída para {typeof(T).Name} - Válido: {result.IsValid}, Erros: {errors.Count}");

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Erro durante validação síncrona de {typeof(T).Name}");
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    protected abstract void ConfigureRules();

    protected virtual Task<List<string>> ExecuteCustomValidationsAsync(T instance, CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<string>());
    }

    protected virtual List<string> ExecuteCustomValidations(T instance)
    {
        return new List<string>();
    }

    protected void AddRule(IValidationRule<T> rule)
    {
        _rules.Add(rule);
    }

    protected void AddAsyncRule(IAsyncValidationRule<T> rule)
    {
        _asyncRules.Add(rule);
    }

    protected void AddSyncRule(ISyncValidationRule<T> rule)
    {
        _syncRules.Add(rule);
    }

    protected void AddRule<TProperty>(
        Func<T, TProperty> propertySelector,
        Func<TProperty, bool> predicate,
        string errorMessage,
        string? ruleName = null)
    {
        var rule = new FuncValidationRule<T>(
            ruleName ?? $"Custom_{Guid.NewGuid():N}",
            errorMessage,
            instance => predicate(propertySelector(instance)));

        AddSyncRule(rule);
    }

    protected void AddAsyncRule<TProperty>(
        Func<T, TProperty> propertySelector,
        Func<TProperty, Task<bool>> asyncPredicate,
        string errorMessage,
        string? ruleName = null)
    {
        var rule = new AsyncFuncValidationRule<T>(
            ruleName ?? $"AsyncCustom_{Guid.NewGuid():N}",
            errorMessage,
            async instance => await asyncPredicate(propertySelector(instance)));

        AddAsyncRule(rule);
    }
}
