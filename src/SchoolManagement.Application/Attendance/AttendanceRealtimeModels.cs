namespace SchoolManagement.Application.Attendance;

public static class AttendanceRealtimeEventTypes
{
    public const string Created = "created";
    public const string Updated = "updated";
}

public sealed record AttendanceRealtimeEvent(
    string EventType,
    AttendanceResponse Attendance,
    DateTime OccurredAt);

public interface IAttendanceRealtimeNotifier
{
    Task BroadcastAttendanceChangedAsync(IEnumerable<Guid> recipientUserIds, AttendanceRealtimeEvent payload, CancellationToken cancellationToken);
}
