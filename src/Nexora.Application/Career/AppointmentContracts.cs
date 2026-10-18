using System.Text.Json.Serialization;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;

namespace Nexora.Application.Career;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CareerAppointmentCommand(Guid? EventId, EventCommand? Event);
public sealed record CareerAppointment(Guid Id, Guid JobId, Guid EventId, string ETag, EventRecord? Event, string State);
public sealed record CareerAppointmentPage(IReadOnlyList<CareerAppointment> Items, Guid? NextCursor);
public interface ICareerAppointmentService
{
    IdentityOperationResult<CareerAppointmentPage> List(IdentityPrincipal actor, Guid jobId, Guid? cursor = null);
    IdentityOperationResult<object?> Link(IdentityPrincipal actor, Guid jobId, string? jobETag, CareerAppointmentCommand command, string? key = null, string? traceId = null);
    IdentityOperationResult<object?> Unlink(IdentityPrincipal actor, Guid jobId, Guid linkId, string? linkETag, string? key = null, string? traceId = null);
}
