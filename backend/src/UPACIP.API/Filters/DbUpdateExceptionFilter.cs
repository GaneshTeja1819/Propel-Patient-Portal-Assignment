using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace UPACIP.API.Filters;

/// <summary>
/// Global exception filter that maps <see cref="DbUpdateException"/> to HTTP 422
/// when a ciphertext value exceeds the target column capacity (AC-001 edge case).
/// All other <see cref="DbUpdateException"/> causes are re-raised.
/// </summary>
public sealed class DbUpdateExceptionFilter : IExceptionFilter
{
    private const string ColumnTooLong = "22001"; // PostgreSQL SQLSTATE: string_data_right_truncation

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateException dbEx)
            return;

        // Walk inner exceptions looking for a PostgreSQL column-overflow error.
        var pgEx = FindPostgresException(dbEx);
        if (pgEx is null)
            return;

        // PostgreSQL SQLSTATE 22001 = string_data_right_truncation (value too long for column).
        if (pgEx.SqlState != ColumnTooLong)
            return;

        context.Result = new UnprocessableEntityObjectResult(new
        {
            title = "Field value too long after encryption.",
            detail = "One or more PHI fields produced a ciphertext that exceeds the column storage limit. " +
                     "Reduce the plaintext length and retry.",
            status = StatusCodes.Status422UnprocessableEntity
        });

        context.ExceptionHandled = true;
    }

    private static Npgsql.PostgresException? FindPostgresException(Exception ex)
    {
        var current = ex.InnerException;
        while (current is not null)
        {
            if (current is Npgsql.PostgresException pgEx)
                return pgEx;
            current = current.InnerException;
        }
        return null;
    }
}
