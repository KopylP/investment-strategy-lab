import { translate } from '@jsverse/transloco';
import { Location } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, NavigationStart, Router } from '@angular/router';

interface VisitedPage {
  id: number;
  url: string;
  title: string;
}

@Injectable({ providedIn: 'root' })
export class NavigationHistory {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  private readonly pages = signal<VisitedPage[]>([]);
  private readonly position = signal(-1);

  readonly previous = computed(() => this.pages()[this.position() - 1] ?? null);

  constructor() {
    let restoredId: number | undefined;

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) restoredId = event.restoredState?.navigationId;
      else if (event instanceof NavigationEnd) this.record(event, restoredId);
    });
  }

  back(parentUrl: string): void {
    if (this.previous()) this.location.back();
    else void this.router.navigateByUrl(parentUrl);
  }

  private record(event: NavigationEnd, restoredId: number | undefined): void {
    const page: VisitedPage = {
      id: restoredId ?? event.id,
      url: event.urlAfterRedirects,
      title: this.titleOf(this.router.routerState.snapshot.root),
    };
    if (page.url.startsWith('/auth')) return this.restart([]);

    const pages = this.pages();
    const position = this.position();
    if (restoredId !== undefined) {
      const known = pages.findIndex((visited) => visited.id === restoredId);
      if (known >= 0) this.position.set(known);
      else this.restart([page]);
    } else if (this.router.currentNavigation()?.extras.replaceUrl && position >= 0) {
      this.restart([...pages.slice(0, position), page]);
    } else {
      this.restart([...pages.slice(0, position + 1), page]);
    }
  }

  private restart(pages: VisitedPage[]): void {
    this.pages.set(pages);
    this.position.set(pages.length - 1);
  }

  private titleOf(route: ActivatedRouteSnapshot): string {
    if (route.firstChild) return this.titleOf(route.firstChild);

    return route.title ? translate(route.title) : '';
  }
}
