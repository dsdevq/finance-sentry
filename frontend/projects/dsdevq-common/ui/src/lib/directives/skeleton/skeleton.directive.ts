import {Directive, effect, inject, input, TemplateRef, ViewContainerRef} from '@angular/core';

import {SkeletonComponent} from '../../components/skeleton/skeleton.component';

export interface SkeletonConfig {
  loading: boolean;
  count?: number;
  width?: string;
  height?: string;
  className?: string;
}

@Directive({selector: '[cmnSkeleton]'})
export class SkeletonDirective {
  private readonly vcr = inject(ViewContainerRef);
  private readonly templateRef = inject(TemplateRef<unknown>);

  public readonly cmnSkeleton = input<SkeletonConfig>({loading: false});

  constructor() {
    effect(() => {
      const config = this.cmnSkeleton();
      this.vcr.clear();
      if (config.loading) {
        const count = config.count ?? 1;
        for (let i = 0; i < count; i++) {
          const ref = this.vcr.createComponent(SkeletonComponent);
          ref.setInput('width', config.width ?? '100%');
          ref.setInput('height', config.height ?? '1.25rem');
          ref.setInput('className', (config.className ?? '') + (i < count - 1 ? ' mb-cmn-2' : ''));
        }
      } else {
        this.vcr.createEmbeddedView(this.templateRef);
      }
    });
  }
}
