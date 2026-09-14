using Fast.Workshops.Api.Models;

namespace Fast.Workshops.Api.Repositories;

public interface IAttendanceRecordRepository
{
    /// <summary>Atomically creates one record per workshop; e.g. Create(1).</summary>
    AttendanceRecord Create(int workshopId);
    /// <summary>Returns a detached record or null; e.g. Find(1).</summary>
    AttendanceRecord? Find(int id);
    /// <summary>Returns detached attendance records; e.g. for filtering.</summary>
    IReadOnlyList<AttendanceRecord> List();
    /// <summary>Atomically adds a participant once; e.g. AddCollaborator(1, 2).</summary>
    void AddCollaborator(int attendanceId, int collaboratorId);
    /// <summary>Removes an association or throws if absent; e.g. RemoveCollaborator(1, 2).</summary>
    void RemoveCollaborator(int attendanceId, int collaboratorId);
}
