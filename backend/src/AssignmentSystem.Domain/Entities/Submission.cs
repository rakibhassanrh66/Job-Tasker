// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A student's answer to an assignment. Unique on (AssignmentId, StudentId) — one row per
/// student per assignment (business rule 6); revisions overwrite this row rather than
/// inserting another.
/// </summary>
public class Submission
{
    public Guid Id { get; set; }

    public Guid AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = null!;

    public Guid StudentId { get; set; }

    /// <summary>Owner of this submission. Business rule 8 checks against this.</summary>
    public User Student { get; set; } = null!;

    public string AnswerText { get; set; } = null!;

    public string? AttachmentUrl { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public DateTime SubmittedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Null until graded. Bounded by [0, Assignment.MaxMarks] — business rule 9.</summary>
    public int? Marks { get; set; }

    public string? Feedback { get; set; }

    public Guid? GradedByTeacherId { get; set; }

    public User? GradedByTeacher { get; set; }

    public DateTime? GradedAt { get; set; }
}
