using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Domain.Entities;

public sealed class CalendarEvent : BaseEntity
{
    public long FamilyId { get; set; }

    public long CreatedByUserId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public EventType EventType { get; set; }

    public string? Description { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public string? ColorHex { get; set; }

    public bool IsRecurring { get; set; }

    public string? RecurrenceRule { get; set; }

    public string VisibilityScope { get; set; } = "Family";

    public long? LinkedChildProfileId { get; set; }

    public bool IsActive { get; set; } = true;

    public Family? Family { get; set; }

    public User? CreatedByUser { get; set; }

    public ChildProfile? LinkedChildProfile { get; set; }

    public ICollection<EventReminder> Reminders { get; } = new List<EventReminder>();
}
