export type SubscriptionStatus = 'active' | 'paused';
export type SubscriptionSort = 'date' | 'amount' | 'name';

export interface Subscription {
  id: string;
  name: string;
  category: string;
  amount: number;
  frequency: string;
  nextDate: string;
  status: SubscriptionStatus;
  logo: string;
  color: string;
}
