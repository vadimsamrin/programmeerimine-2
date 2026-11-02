namespace KooliProjekt.Application.Infrastructure;

/// <summary>
/// A response returned by commands and queries. The shape mirrors the course
/// examples, so a UI can show global and field-specific validation messages.
/// </summary>
public class OperationResult
{
    public List<string> Errors { get; } = [];
    public Dictionary<string, List<string>> PropertyErrors { get; } = new();
    public bool HasErrors => Errors.Count > 0 || PropertyErrors.Count > 0;

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error)) Errors.Add(error);
    }

    public void AddPropertyError(string propertyName, string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return;
        var key = string.IsNullOrWhiteSpace(propertyName) ? string.Empty : propertyName;
        if (!PropertyErrors.TryGetValue(key, out var errors))
        {
            errors = [];
            PropertyErrors[key] = errors;
        }

        errors.Add(error);
    }

    public static OperationResult Success() => new();

    public static OperationResult Failure(params string[] errors)
    {
        var result = new OperationResult();
        foreach (var error in errors) result.AddError(error);
        return result;
    }
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; init; }

    public static OperationResult<T> Success(T? value) => new() { Value = value };

    public new static OperationResult<T> Failure(params string[] errors)
    {
        var result = new OperationResult<T>();
        foreach (var error in errors) result.AddError(error);
        return result;
    }
}
