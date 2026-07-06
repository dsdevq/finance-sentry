/**
 * A canonical spending category, resolved server-side from the reference tables.
 * The single source of truth for category keys + display labels on the frontend.
 */
export interface CategoryModel {
  readonly key: string;
  readonly label: string;
  readonly sortOrder: number;
}
