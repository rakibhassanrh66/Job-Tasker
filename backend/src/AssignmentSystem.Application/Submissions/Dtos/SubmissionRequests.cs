// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Submissions.Dtos;

/// <summary>Marks are bounded by the parent assignment's MaxMarks, which only the service
/// can check — see business rule 9.</summary>
public record GradeSubmissionRequest(int Marks, string? Feedback);

public record ChangeSubmissionStatusRequest(SubmissionStatus Status);

public record CreateSubmissionRequest(string AnswerText, string? AttachmentUrl);

public record UpdateSubmissionRequest(string AnswerText, string? AttachmentUrl);
