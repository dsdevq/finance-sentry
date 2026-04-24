const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const MS_PER_MINUTE = SECONDS_PER_MINUTE * MS_PER_SECOND;
const MINUTES_PER_HOUR = 60;

export function getRelativeTime(timestamp: string | null): string {
  if (!timestamp) {
    return 'Never';
  }
  const diffMs = Date.now() - new Date(timestamp).getTime();
  const minutes = Math.floor(diffMs / MS_PER_MINUTE);
  if (minutes < 1) {
    return 'Just now';
  }
  if (minutes < MINUTES_PER_HOUR) {
    return `${minutes}m ago`;
  }
  return `${Math.floor(minutes / MINUTES_PER_HOUR)}h ago`;
}
