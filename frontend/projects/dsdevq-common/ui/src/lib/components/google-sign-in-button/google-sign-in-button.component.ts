/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/no-unsafe-call */
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  NgZone,
  OnDestroy,
  output,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'cmn-google-sign-in-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<div #btn></div>',
})
export class GoogleSignInButtonComponent implements AfterViewInit, OnDestroy {
  private readonly zone = inject(NgZone);
  private readonly btnRef = viewChild.required<ElementRef<HTMLElement>>('btn');

  public readonly clientId = input.required<string>();
  public readonly buttonConfiguration = input<google.accounts.id.GsiButtonConfiguration>({
    type: 'standard',
    shape: 'rectangular',
    theme: 'outline',
    text: 'continue_with',
    size: 'large',
    width: 368,
  });

  public readonly credential = output<string>();

  public ngAfterViewInit(): void {
    google.accounts.id.initialize({
      // eslint-disable-next-line @typescript-eslint/naming-convention
      client_id: this.clientId(),
      callback: (r: google.accounts.id.CredentialResponse) =>
        this.zone.run(() => this.credential.emit(r.credential as string)),
    });
    google.accounts.id.renderButton(this.btnRef().nativeElement, this.buttonConfiguration());
    google.accounts.id.prompt();
  }

  public ngOnDestroy(): void {
    google.accounts.id.cancel();
  }
}
