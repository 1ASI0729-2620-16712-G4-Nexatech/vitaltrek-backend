using Microsoft.EntityFrameworkCore;

namespace VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

/// <summary>
///     Provides helpers to interpret provider-specific database update failures.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    private const int MySqlDuplicateEntryErrorCode = 1062;

    /// <summary>
    ///     Determines whether the exception was caused by a unique key violation in MySQL.
    /// </summary>
    /// <param name="exception">The database update exception.</param>
    /// <returns><see langword="true"/> when a duplicate key violation is found in the exception chain.</returns>
    public static bool IsDuplicateKeyViolation(this DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (!string.Equals(current.GetType().Name, "MySqlException", StringComparison.Ordinal)) continue;
            var numberProperty = current.GetType().GetProperty("Number");
            if (numberProperty?.PropertyType == typeof(int) &&
                numberProperty.GetValue(current) is int errorCode && errorCode == MySqlDuplicateEntryErrorCode) return true;
        }
        return false;
    }
}
