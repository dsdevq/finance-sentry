namespace FinanceSentry.Modules.Alerts.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class AlertNotFoundException() : ApiException(404, "ALERT_NOT_FOUND", "Alert not found.");
