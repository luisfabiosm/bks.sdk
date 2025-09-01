using bks.sdk.Observability.Logging;
using bks.sdk.Validation.Rules;
using bks.sdk.Validation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Examples;

public record CreateUserRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int Age { get; init; }
    public string? Document { get; init; }
}

// Exemplo de validador usando BaseValidator
public class CreateUserRequestValidator : BaseValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IBKSLogger logger) : base(logger)
    {
    }

    protected override void ConfigureRules()
    {
        // Nome obrigatório e com tamanho mínimo
        AddSyncRule(new RequiredRule<CreateUserRequest>("Name", x => x.Name));
        AddSyncRule(new MinLengthRule<CreateUserRequest>("Name", x => x.Name, 2));
        AddSyncRule(new MaxLengthRule<CreateUserRequest>("Name", x => x.Name, 100));

        // Email obrigatório e formato válido
        AddSyncRule(new RequiredRule<CreateUserRequest>("Email", x => x.Email));
        AddSyncRule(new EmailRule<CreateUserRequest>("Email", x => x.Email));

        // Idade deve ser maior que 0 e menor que 150
        AddSyncRule(new RangeRule<CreateUserRequest, int>("Age", x => x.Age, 1, 150));

        // Telefone opcional, mas se informado deve ter formato válido
        AddSyncRule(new RegexRule<CreateUserRequest>("Phone",
            x => x.Phone,
            @"^\(\d{2}\)\s\d{4,5}-\d{4}$",
            "Phone must be in format (XX) XXXXX-XXXX"));

        // CPF opcional, mas se informado deve ser válido
        AddSyncRule(new CPFRule<CreateUserRequest>("Document", x => x.Document));

        // Validação customizada: nome não pode ser "Admin"
        AddRule(x => x.Name,
            name => !name.Equals("Admin", StringComparison.OrdinalIgnoreCase),
            "Name cannot be 'Admin'",
            "NoAdminName");

        // Validação assíncrona customizada: email não pode já existir
        AddAsyncRule(x => x.Email,
            async email => await CheckEmailNotExistsAsync(email),
            "Email already exists",
            "UniqueEmail");
    }

    protected override async Task<List<string>> ExecuteCustomValidationsAsync(CreateUserRequest instance, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        // Validação complexa que envolve múltiplas propriedades
        if (instance.Age < 18 && string.IsNullOrWhiteSpace(instance.Phone))
        {
            errors.Add("Phone is required for users under 18 years old");
        }

        // Simulação de validação que requer acesso a dados externos
        if (!string.IsNullOrWhiteSpace(instance.Document))
        {
            var isValidDocument = await ValidateDocumentWithExternalServiceAsync(instance.Document, cancellationToken);
            if (!isValidDocument)
            {
                errors.Add("Document validation failed with external service");
            }
        }

        return errors;
    }

    private async Task<bool> CheckEmailNotExistsAsync(string email)
    {
        // Simulação de consulta assíncrona ao banco de dados
        await Task.Delay(50); // Simula latência

        // Lista de emails já existentes (em um caso real, seria uma consulta ao banco)
        var existingEmails = new[] { "admin@test.com", "test@test.com", "user@test.com" };

        return !existingEmails.Contains(email.ToLowerInvariant());
    }

    private async Task<bool> ValidateDocumentWithExternalServiceAsync(string document, CancellationToken cancellationToken)
    {
        // Simulação de validação com serviço externo
        await Task.Delay(100, cancellationToken); // Simula chamada HTTP

        // Simular que documentos que começam com "000" são inválidos
        return !document.StartsWith("000");
    }
}

// Exemplo usando o builder pattern
public class AlternativeCreateUserRequestValidator : BaseValidator<CreateUserRequest>
{
    public AlternativeCreateUserRequestValidator(IBKSLogger logger) : base(logger)
    {
    }

    protected override void ConfigureRules()
    {
        // Usando o padrão builder para configurar regras
        // (Implementação simplificada - em uma versão completa, o builder seria mais robusto)

        // Nome
        AddRule(x => x.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name is required");

        AddRule(x => x.Name,
            name => name?.Length >= 2,
            "Name must have at least 2 characters");

        // Email
        AddRule(x => x.Email,
            email => !string.IsNullOrWhiteSpace(email),
            "Email is required");

        AddRule(x => x.Email,
            email => email?.Contains("@") == true,
            "Email must be valid");

        // Idade
        AddRule(x => x.Age,
            age => age > 0 && age < 150,
            "Age must be between 1 and 150");
    }
}

// Exemplo de validador simples para transações
public record TransactionRequest
{
    public string AccountId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
}

public class TransactionRequestValidator : BaseValidator<TransactionRequest>
{
    public TransactionRequestValidator(IBKSLogger logger) : base(logger)
    {
    }

    protected override void ConfigureRules()
    {
        // Account ID obrigatório e formato UUID
        AddSyncRule(new RequiredRule<TransactionRequest>("AccountId", x => x.AccountId));
        AddSyncRule(new RegexRule<TransactionRequest>("AccountId",
            x => x.AccountId,
            @"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$",
            "AccountId must be a valid UUID"));

        // Amount deve ser positivo
        AddSyncRule(new PositiveNumberRule<TransactionRequest>("Amount", x => x.Amount));

        // Description obrigatória com tamanho mínimo
        AddSyncRule(new RequiredRule<TransactionRequest>("Description", x => x.Description));
        AddSyncRule(new MinLengthRule<TransactionRequest>("Description", x => x.Description, 5));

        // TransactionType deve ser um valor válido
        AddRule(x => x.TransactionType,
            type => new[] { "DEBIT", "CREDIT", "TRANSFER" }.Contains(type?.ToUpperInvariant()),
            "TransactionType must be DEBIT, CREDIT, or TRANSFER");
    }

    protected override async Task<List<string>> ExecuteCustomValidationsAsync(TransactionRequest instance, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        // Validação de regras de negócio específicas
        if (instance.TransactionType?.ToUpperInvariant() == "TRANSFER" && instance.Amount > 10000)
        {
            errors.Add("Transfer amount cannot exceed R$ 10,000.00");
        }

        // Validação assíncrona de conta existente
        var accountExists = await CheckAccountExistsAsync(instance.AccountId, cancellationToken);
        if (!accountExists)
        {
            errors.Add("Account not found");
        }

        return errors;
    }

    private async Task<bool> CheckAccountExistsAsync(string accountId, CancellationToken cancellationToken)
    {
        // Simulação de consulta ao repositório
        await Task.Delay(50, cancellationToken);

        // Em um cenário real, consultaria o repositório de contas
        return !string.IsNullOrWhiteSpace(accountId);
    }
}