namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// What kind of lifecycle subject a <see cref="ThesisEvent"/> is about. <c>Candidate</c> paths
/// are modeled now (per data-model.md) but only exercisable once the 019 Candidate concept ships.
/// </summary>
public enum ThesisSubjectType
{
    Thesis,
    Candidate,
}
