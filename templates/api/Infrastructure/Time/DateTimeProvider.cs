using {{SolutionName}}.Application.Abstractions;

namespace {{SolutionName}}.Infrastructure.Time;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
