import { Directive, ElementRef, inject, signal } from '@angular/core';
import { MessageService } from 'primeng/api';

/**
 * Drag and drop for a file field: put it on the element (usually a `<label>`) that holds an `<input type="file">`.
 * A dropped file goes into that input and fires its `change`, so the page handles it exactly like a chosen one;
 * the input's `accept` and `disabled` are honoured.
 */
@Directive({
  selector: '[appFileDrop]',
  host: {
    // While a file is over it: a dashed primary outline and a light tint, in both themes.
    '[style.outline]': "over() ? '2px dashed var(--p-primary-color)' : null",
    '[style.outline-offset]': "over() ? '2px' : null",
    '[style.background]': "over() ? 'color-mix(in srgb, var(--p-primary-color) 10%, transparent)' : null",
    '(dragenter)': 'enter($event)',
    '(dragover)': 'hover($event)',
    '(dragleave)': 'leave()',
    '(drop)': 'drop($event)',
  },
})
export class FileDrop {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly messages = inject(MessageService);

  protected readonly over = signal(false);
  // dragenter/dragleave also fire for every child the pointer crosses.
  private depth = 0;

  protected enter(event: DragEvent): void {
    if (!this.accepts(event)) return;
    event.preventDefault();
    this.depth++;
    this.over.set(true);
  }

  protected hover(event: DragEvent): void {
    if (!this.accepts(event)) return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
  }

  protected leave(): void {
    this.depth = Math.max(0, this.depth - 1);
    if (this.depth === 0) this.over.set(false);
  }

  protected drop(event: DragEvent): void {
    if (!this.accepts(event)) return;
    event.preventDefault();
    this.depth = 0;
    this.over.set(false);
    const input = this.input();
    const file = event.dataTransfer?.files[0];
    if (!input || !file) return;
    if (!matches(file, input.accept)) {
      this.messages.add({ severity: 'warn', summary: 'Не той файл', detail: `Потрібен файл ${input.accept.split(',').filter((type) => type.startsWith('.')).join(' або ')}` });
      return;
    }
    const files = new DataTransfer();
    files.items.add(file);
    input.files = files.files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
  }

  private accepts(event: DragEvent): boolean {
    const input = this.input();
    return !!input && !input.disabled && !!event.dataTransfer?.types.includes('Files');
  }

  private input(): HTMLInputElement | null {
    return this.host.nativeElement.querySelector('input[type="file"]');
  }
}

/** The same check the file picker makes: an extension (`.csv`) or a media type (`text/csv`) from `accept`. */
function matches(file: File, accept: string): boolean {
  const allowed = accept.split(',').map((type) => type.trim().toLowerCase()).filter(Boolean);
  if (!allowed.length) return true;
  const name = file.name.toLowerCase();
  return allowed.some((type) => (type.startsWith('.') ? name.endsWith(type) : file.type.toLowerCase() === type));
}
