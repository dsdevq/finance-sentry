namespace FinanceSentry.Modules.Research.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class WatchlistItemNotFoundException()
    : ApiException(404, "WATCHLIST_ITEM_NOT_FOUND", "Watchlist item not found.");
