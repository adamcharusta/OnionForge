namespace {{SolutionName}}.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
