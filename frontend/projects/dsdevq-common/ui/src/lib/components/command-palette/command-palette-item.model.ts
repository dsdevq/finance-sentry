export interface CommandPaletteItem {
  id: string;
  label: string;
  icon: string;
  group: string;
}

export interface PaletteResult {
  type: 'navigate' | 'action';
  id: string;
}
