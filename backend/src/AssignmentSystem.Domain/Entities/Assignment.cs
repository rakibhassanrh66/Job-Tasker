// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A piece of work set by a teacher for one subject within one class.
/// </summary>
public class Assignment
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    /// <summary>Stored as timestamptz. Always UTC.</summary>
    public DateTime Deadline { get; set; }

    public int MaxMarks { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    public Guid ClassCourseId { get; set; }

    public ClassCourse ClassCourse { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    /// <summary>The teacher who created this. Business rule 4 checks ownership against it.</summary>
    public Guid CreatedByTeacherId { get; set; }

    public User CreatedByTeacher { get; set; } = null!;

    /// <summary>When true, submissions after the deadline are accepted and flagged Late
    /// (business rule 5) rather than rejected.</summary>
    public bool AllowLateSubmission { get; set; }

    /// <summary>When true, a student may revise their submission up until the deadline
    /// (business rule 7). After the deadline, updates are refused regardless.</summary>
    public bool AllowUpdateBeforeDeadline { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    /// <summary>
    /// Deadline check kept on the entity so the rule has one definition and can be unit
    /// tested without a database. The clock is passed in rather than read from
    /// DateTime.UtcNow, so tests can place "now" on either side of the deadline.
    /// </summary>
    public bool IsPastDeadline(DateTime utcNow) => utcNow > Deadline;
}
