namespace FinanceSentry.Modules.Research.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class ThesisNotFoundException()
    : ApiException(404, "THESIS_NOT_FOUND", "Investment thesis not found.");
