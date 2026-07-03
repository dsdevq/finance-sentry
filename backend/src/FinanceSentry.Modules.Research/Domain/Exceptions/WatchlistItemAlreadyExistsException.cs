namespace FinanceSentry.Modules.Research.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class WatchlistItemAlreadyExistsException()
    : ApiException(409, "WATCHLIST_ITEM_ALREADY_EXISTS", "Ticker is already on the watchlist.");
