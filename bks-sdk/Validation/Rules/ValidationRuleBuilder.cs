using bks.sdk.Validation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Rules;

public class ValidationRuleBuilder<T>
{
    private readonly List<ISyncValidationRule<T>> _syncRules = new();
    private readonly List<IAsyncValidationRule<T>> _asyncRules = new();

    public PropertyValidationRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        where TProperty : IComparable<TProperty>
    {
        var propertyName = GetPropertyName(propertyExpression);
        var compiledExpression = propertyExpression.Compile();

        return new PropertyValidationRuleBuilder<T, TProperty>(propertyName, compiledExpression, this);
    }

    public ValidationRuleBuilder<T> Must(Func<T, bool> predicate, string errorMessage, string? ruleName = null)
    {
        var rule = new FuncValidationRule<T>(
            ruleName ?? $"Must_{Guid.NewGuid():N}",
            errorMessage,
            predicate);

        _syncRules.Add(rule);
        return this;
    }

    public ValidationRuleBuilder<T> MustAsync(Func<T, Task<bool>> asyncPredicate, string errorMessage, string? ruleName = null)
    {
        var rule = new AsyncFuncValidationRule<T>(
            ruleName ?? $"MustAsync_{Guid.NewGuid():N}",
            errorMessage,
            asyncPredicate);

        _asyncRules.Add(rule);
        return this;
    }

    internal void AddSyncRule(ISyncValidationRule<T> rule)
    {
        _syncRules.Add(rule);
    }

    internal void AddAsyncRule(IAsyncValidationRule<T> rule)
    {
        _asyncRules.Add(rule);
    }

    public (List<ISyncValidationRule<T>> SyncRules, List<IAsyncValidationRule<T>> AsyncRules) Build()
    {
        return (_syncRules.ToList(), _asyncRules.ToList());
    }

    private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Invalid property expression", nameof(propertyExpression));
    }
}

public class PropertyValidationRuleBuilder<T, TProperty> where TProperty : IComparable<TProperty>
{
    private readonly string _propertyName;
    private readonly Func<T, TProperty> _propertySelector;
    private readonly ValidationRuleBuilder<T> _parentBuilder;

    internal PropertyValidationRuleBuilder(string propertyName, Func<T, TProperty> propertySelector, ValidationRuleBuilder<T> parentBuilder)
    {
        _propertyName = propertyName;
        _propertySelector = propertySelector;
        _parentBuilder = parentBuilder;
    }

    public PropertyValidationRuleBuilder<T, TProperty> NotNull(string? customMessage = null)
    {
        var rule = new RequiredRule<T>(_propertyName, instance => _propertySelector(instance), customMessage);
        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> NotEmpty(string? customMessage = null)
    {
        var rule = new RequiredRule<T>(_propertyName, instance => _propertySelector(instance), customMessage);
        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> MinLength(int minLength, string? customMessage = null)
    {
        var rule = new MinLengthRule<T>(_propertyName, instance => _propertySelector(instance) as string, minLength, customMessage);
        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> MaxLength(int maxLength, string? customMessage = null)
    {
        var rule = new MaxLengthRule<T>(_propertyName, instance => _propertySelector(instance) as string, maxLength, customMessage);
        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> EmailAddress(string? customMessage = null)      
    {
        var rule = new EmailRule<T>(_propertyName, instance => _propertySelector(instance) as string, customMessage);
        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> GreaterThan(TProperty value, string? customMessage = null)
     
    {
        var rule = new FuncValidationRule<T>(
            $"GreaterThan_{_propertyName}",
            customMessage ?? $"{_propertyName} must be greater than {value}",
            instance => _propertySelector(instance).CompareTo(value) > 0);

        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string errorMessage)
    {
        var rule = new FuncValidationRule<T>(
            $"Must_{_propertyName}",
            errorMessage,
            instance => predicate(_propertySelector(instance)));

        _parentBuilder.AddSyncRule(rule);
        return this;
    }

    public PropertyValidationRuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> asyncPredicate, string errorMessage)
    {
        var rule = new AsyncFuncValidationRule<T>(
            $"MustAsync_{_propertyName}",
            errorMessage,
            async instance => await asyncPredicate(_propertySelector(instance)));

        _parentBuilder.AddAsyncRule(rule);
        return this;
    }

    public ValidationRuleBuilder<T> And()
    {
        return _parentBuilder;
    }
}
