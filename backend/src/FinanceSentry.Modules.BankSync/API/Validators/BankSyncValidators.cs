namespace FinanceSentry.Modules.BankSync.API.Validators;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data annotation validators for BankSync API request models.
/// </summary>

public class ValidPublicTokenAttribute : ValidationAttribute
{
    public ValidPublicTokenAttribute()
        : base("publicToken is required and must be at most 100 characters.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string s) return false;
        return !string.IsNullOrWhiteSpace(s) && s.Length <= 100;
    }
}

public class ValidDateNotFutureAttribute : ValidationAttribute
{
    public ValidDateNotFutureAttribute()
        : base("{0} must be a valid date not in the future.") { }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // optional field
        if (value is not DateTime dt) return false;
        return dt.Date <= DateTime.UtcNow.Date;
    }
}

public class ValidEndDateAttribute : ValidationAttribute
{
    private readonly string _startDateProperty;

    public ValidEndDateAttribute(string startDateProperty)
        : base("endDate must be >= startDate.")
    {
        _startDateProperty = startDateProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;

        var startProp = context.ObjectType.GetProperty(_startDateProperty);
        if (startProp is null) return ValidationResult.Success;

        var startValue = startProp.GetValue(context.ObjectInstance) as DateTime?;
        if (startValue is null) return ValidationResult.Success;

        if (value is DateTime endDate && endDate < startValue.Value)
            return new ValidationResult(ErrorMessage);

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validated request model for transaction queries.
/// </summary>
public class TransactionQueryParams : IValidatableObject
{
    [Required]
    public Guid UserId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "offset must be >= 0")]
    public int Offset { get; set; } = 0;

    [Range(1, 100, ErrorMessage = "limit must be between 1 and 100")]
    public int Limit { get; set; } = 50;

    [ValidDateNotFuture]
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("endDate must be >= startDate", [nameof(EndDate)]);
    }
}
