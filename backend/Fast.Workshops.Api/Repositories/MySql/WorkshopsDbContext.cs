using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Fast.Workshops.Api.Repositories.MySql;

/// <summary>Maps the existing MySQL schema; e.g. held_at retains its original offset.</summary>
public sealed class WorkshopsDbContext(DbContextOptions<WorkshopsDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWorkshops(modelBuilder);
        ConfigureCollaborators(modelBuilder);
        ConfigureAttendance(modelBuilder);
        ConfigureParticipants(modelBuilder);
    }

    private static void ConfigureWorkshops(ModelBuilder modelBuilder)
    {
        var workshop = modelBuilder.Entity<WorkshopRow>();
        workshop.ToTable("workshops");
        workshop.HasKey(row => row.Id);
        workshop.Property(row => row.Id).HasColumnName("id").ValueGeneratedOnAdd();
        workshop.Property(row => row.Name).HasColumnName("name").HasColumnType("longtext");
        workshop.Property(row => row.Description).HasColumnName("description").HasColumnType("longtext");
        // Preserve the original offset and all seven fractional digits for calendar-date filters.
        workshop.Property(row => row.HeldAt).HasColumnName("held_at").HasColumnType("varchar(33)")
            .HasConversion(value => value.ToString("O", CultureInfo.InvariantCulture),
                value => DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture));
    }

    private static void ConfigureCollaborators(ModelBuilder modelBuilder)
    {
        var collaborator = modelBuilder.Entity<CollaboratorRow>();
        collaborator.ToTable("collaborators");
        collaborator.HasKey(row => row.Id);
        collaborator.Property(row => row.Id).HasColumnName("id").ValueGeneratedOnAdd();
        collaborator.Property(row => row.Name).HasColumnName("name").HasColumnType("longtext");
    }

    private static void ConfigureAttendance(ModelBuilder modelBuilder)
    {
        var attendance = modelBuilder.Entity<AttendanceRow>();
        attendance.ToTable("attendance_records");
        attendance.HasKey(row => row.Id);
        attendance.Property(row => row.Id).HasColumnName("id").ValueGeneratedOnAdd();
        attendance.Property(row => row.WorkshopId).HasColumnName("workshop_id");
        attendance.HasIndex(row => row.WorkshopId).IsUnique();
        attendance.HasOne<WorkshopRow>().WithOne().HasForeignKey<AttendanceRow>(row => row.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureParticipants(ModelBuilder modelBuilder)
    {
        var participant = modelBuilder.Entity<ParticipantRow>();
        participant.ToTable("attendance_participants");
        participant.HasKey(row => new { row.AttendanceId, row.CollaboratorId });
        participant.Property(row => row.AttendanceId).HasColumnName("attendance_id");
        participant.Property(row => row.CollaboratorId).HasColumnName("collaborator_id");
        participant.HasOne<AttendanceRow>().WithMany(row => row.Participants)
            .HasForeignKey(row => row.AttendanceId).OnDelete(DeleteBehavior.Restrict);
        participant.HasOne<CollaboratorRow>().WithMany()
            .HasForeignKey(row => row.CollaboratorId).OnDelete(DeleteBehavior.Restrict);
    }
}
