import {type Budget} from '../../models/budget/budget.model';

export const BUDGET_MOCK_DATA: Budget[] = [
  {category: 'Housing', limit: 2000, spent: 1850, color: '#4f46e5'},
  {category: 'Food & Drink', limit: 1200, spent: 1060, color: '#818cf8'},
  {category: 'Transport', limit: 600, spent: 795, color: '#10b981'},
  {category: 'Shopping', limit: 800, spent: 636, color: '#f59e0b'},
  {category: 'Entertainment', limit: 400, spent: 530, color: '#ef4444'},
  {category: 'Health & Fitness', limit: 150, spent: 55, color: '#06b6d4'},
  {category: 'Utilities', limit: 200, spent: 142, color: '#8b5cf6'},
  {category: 'Travel', limit: 500, spent: 420, color: '#f97316'},
];
