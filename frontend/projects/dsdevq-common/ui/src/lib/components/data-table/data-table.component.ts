import {CdkTableModule} from '@angular/cdk/table';
import {NgTemplateOutlet} from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  input,
  output,
  TrackByFunction,
} from '@angular/core';

import {ButtonComponent} from '../button/button.component';
import {SkeletonComponent} from '../skeleton/skeleton.component';

import {CmnColumnAlign, CmnColumnComponent} from './data-table-column.component';
import {type CmnTablePagination} from './data-table-pagination.model';

const SKELETON_ROWS = 5;

@Component({
  selector: 'cmn-data-table',
  imports: [ButtonComponent, CdkTableModule, NgTemplateOutlet, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overflow-x-auto rounded-cmn-lg border border-border-default bg-surface-card">
      @if (loading()) {
        <div class="p-cmn-4 space-y-cmn-3">
          @for (_ of skeletonRows; track $index) {
            <div class="flex gap-cmn-4">
              <cmn-skeleton height="1rem" width="20%" />
              <cmn-skeleton height="1rem" width="35%" />
              <cmn-skeleton height="1rem" width="25%" />
              <cmn-skeleton height="1rem" width="20%" />
            </div>
          }
        </div>
      } @else {
        <table [dataSource]="rows()" cdk-table class="w-full text-cmn-sm">
          @for (col of columns(); track col.key()) {
            <ng-container [cdkColumnDef]="col.key()">
              <th *cdkHeaderCellDef [class]="headerCellClass(col.align())" cdk-header-cell>
                @if (col.headerCell(); as h) {
                  <ng-container *ngTemplateOutlet="h.template" />
                } @else {
                  {{ col.header() }}
                }
              </th>
              <td
                *cdkCellDef="let row; let i = index"
                [class]="dataCellClass(col.align())"
                cdk-cell
              >
                @if (col.cell(); as c) {
                  <ng-container
                    *ngTemplateOutlet="c.template; context: {$implicit: row, index: i}"
                  />
                } @else {
                  {{ defaultCell(row, col.key()) }}
                }
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
      }
    </div>

    @if (!loading() && pagination(); as p) {
      @if (p.totalCount > 0) {
      <div
        class="flex items-center justify-between mt-cmn-4"
        role="navigation"
        aria-label="Pagination"
      >
        <cmn-button
          [disabled]="p.offset <= 0"
          (clicked)="previousPage.emit()"
          variant="secondary"
          size="sm"
          aria-label="Previous page"
        >
          Previous
        </cmn-button>
        <span class="text-cmn-sm text-text-secondary">
          Page {{ currentPage(p) }} of {{ totalPages(p) }}
        </span>
        <cmn-button
          [disabled]="!p.hasMore"
          (clicked)="nextPage.emit()"
          variant="secondary"
          size="sm"
          aria-label="Next page"
        >
          Next
        </cmn-button>
      </div>
      }
    }
  `,
})
export class DataTableComponent<T = Record<string, unknown>> {
  public readonly rows = input<T[]>([]);
  public readonly emptyMessage = input<string>('No data');
  public readonly loading = input<boolean>(false);
  public readonly trackBy = input<TrackByFunction<T> | null>(null);
  public readonly pagination = input<CmnTablePagination | null>(null);

  public readonly rowClick = output<T>();
  public readonly previousPage = output<void>();
  public readonly nextPage = output<void>();

  public readonly columns = contentChildren(CmnColumnComponent<T>);
  public readonly columnKeys = computed(() => this.columns().map(c => c.key()));

  protected readonly skeletonRows = Array.from({length: SKELETON_ROWS});

  public headerCellClass(align: CmnColumnAlign): string {
    return `px-cmn-4 py-cmn-3 font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary ${this.alignClass(align)}`;
  }

  public dataCellClass(align: CmnColumnAlign): string {
    return `px-cmn-4 py-cmn-3 text-text-primary ${this.alignClass(align)}`;
  }

  protected currentPage(p: CmnTablePagination): number {
    return p.limit > 0 ? Math.floor(p.offset / p.limit) + 1 : 1;
  }

  protected totalPages(p: CmnTablePagination): number {
    return p.limit > 0 ? Math.max(1, Math.ceil(p.totalCount / p.limit)) : 1;
  }

  protected defaultCell(row: T, key: string): string {
    const value = (row as Record<string, unknown>)[key];
    return value === null || value === undefined ? '' : String(value);
  }

  private alignClass(align: CmnColumnAlign): string {
    return `text-${align}`;
  }
}
