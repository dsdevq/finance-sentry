import {CdkTableModule} from '@angular/cdk/table';
import {ChangeDetectionStrategy, Component, input, output, TrackByFunction} from '@angular/core';

export interface TableColumn<T = Record<string, unknown>> {
  key: string;
  header: string;
  align?: 'left' | 'right' | 'center';
  cell: (row: T) => string;
}

@Component({
  selector: 'cmn-data-table',
  imports: [CdkTableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overflow-x-auto rounded-cmn-lg border border-border-default bg-surface-card">
      <table [dataSource]="rows()" cdk-table class="w-full text-cmn-sm">
        @for (col of columns(); track col.key) {
          <ng-container [cdkColumnDef]="col.key">
            <th *cdkHeaderCellDef [class]="headerCellClass(col)" cdk-header-cell>
              {{ col.header }}
            </th>
            <td *cdkCellDef="let row" [class]="dataCellClass(col)" cdk-cell>
              {{ col.cell(row) }}
            </td>
          </ng-container>
        }

        <tr
          *cdkHeaderRowDef="columnKeys()"
          cdk-header-row
          class="border-b border-border-default"
        ></tr>
        <tr
          *cdkRowDef="let row; columns: columnKeys()"
          (click)="rowClick.emit(row)"
          cdk-row
          class="border-b border-border-default last:border-0 transition-colors hover:bg-surface-raised"
        ></tr>

        <tr *cdkNoDataRow class="cdk-row">
          <td
            [attr.colspan]="columns().length"
            class="py-cmn-6 text-center text-cmn-sm text-text-secondary"
          >
            {{ emptyMessage() }}
          </td>
        </tr>
      </table>
    </div>
  `,
})
export class DataTableComponent<T = Record<string, unknown>> {
  public readonly columns = input<TableColumn<T>[]>([]);
  public readonly rows = input<T[]>([]);
  public readonly emptyMessage = input<string>('No data');
  public readonly trackBy = input<TrackByFunction<T> | null>(null);

  public readonly rowClick = output<T>();

  public columnKeys(): string[] {
    return this.columns().map(c => c.key);
  }

  public headerCellClass(col: TableColumn<T>): string {
    const align =
      col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left';
    return `px-cmn-4 py-cmn-3 font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary ${align}`;
  }

  public dataCellClass(col: TableColumn<T>): string {
    const align =
      col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left';
    return `px-cmn-4 py-cmn-3 text-text-primary ${align}`;
  }
}
