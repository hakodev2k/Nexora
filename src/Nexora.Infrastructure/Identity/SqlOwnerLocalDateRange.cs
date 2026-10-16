using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using NodaTime;

namespace Nexora.Infrastructure.Identity;

internal sealed record OwnerLocalDateRange(string? TimeZoneId, string? TimeZoneVersion, DateTime? FromUtc, DateTime? ToUtc);

/// <summary>Internal own-profile day boundaries. Callers retain their source action and projection guards.</summary>
internal static class SqlOwnerLocalDateRange
{
    public static IdentityOperationResult<OwnerLocalDateRange> Read(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null)
            return IdentityOperationResult<OwnerLocalDateRange>.Success(new(null, null, null, null));
        if (from is not null && to is not null && to < from)
            return IdentityOperationResult<OwnerLocalDateRange>.Failure("ValidationFailed", 422, "End date cannot be before start date.");
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
            return IdentityOperationResult<OwnerLocalDateRange>.Failure("PermissionDenied", 403, "Date filtering is unavailable.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT u.[TimeZoneId] FROM [identity].[User] u
            INNER JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
            WHERE u.[Id] = @UserId AND ps.[Id] = @OwnerId;
            """;
        command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@OwnerId", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        var zoneId = command.ExecuteScalar() as string;
        var zone = string.IsNullOrWhiteSpace(zoneId) ? null : DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId);
        if (zone is null)
            return IdentityOperationResult<OwnerLocalDateRange>.Failure("TimeZoneUnavailable", 503,
                "The configured profile IANA timezone is unavailable. Correct the profile timezone before filtering by date.");
        DateTime? fromUtc = null, toUtc = null;
        try
        {
            if (from is { } first)
                fromUtc = zone.AtStartOfDay(new LocalDate(first.Year, first.Month, first.Day)).ToInstant().ToDateTimeUtc();
            if (to is { } last)
            {
                var end = last.AddDays(1);
                toUtc = zone.AtStartOfDay(new LocalDate(end.Year, end.Month, end.Day)).ToInstant().ToDateTimeUtc();
            }
        }
        catch (Exception error) when (error is SkippedTimeException or ArgumentOutOfRangeException or InvalidOperationException or OverflowException)
        {
            return IdentityOperationResult<OwnerLocalDateRange>.Failure("DateRangeUnavailable", 422,
                "The requested local-day boundary does not exist or is outside the supported date range.");
        }
        return IdentityOperationResult<OwnerLocalDateRange>.Success(new(zoneId, DateTimeZoneProviders.Tzdb.VersionId, fromUtc, toUtc));
    }
}
