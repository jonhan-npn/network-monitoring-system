import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

type NavItem = { label: string; path: string; icon: string; exact?: boolean; badge?: string };

@Component({
  selector: 'app-admin-layout',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css',
})
export class AdminLayout {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly sidebarOpen = signal(false);
  readonly userMenuOpen = signal(false);
  readonly pageTitle = signal('Overview');
  readonly user = this.auth.currentUser;
  readonly authenticationEnabled = true;

  readonly initials = computed(() => {
    const username = this.user()?.username ?? '';
    const parts = username.split(/[\s._-]+/).filter(Boolean);
    if (!parts.length) return 'NW';
    return parts
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('');
  });

  readonly displayName = computed(() => this.user()?.username ?? 'Signed in');
  readonly role = computed(() => this.user()?.role ?? 'Viewer');

  readonly nav: NavItem[] = [
    { label: 'Overview', path: '/', icon: 'grid', exact: true },
    { label: 'Devices', path: '/devices', icon: 'server' },
    { label: 'Alert rules', path: '/alerts/rules', icon: 'bell' },
    { label: 'Alert history', path: '/alerts/history', icon: 'clock' },
    { label: 'Event log', path: '/logs', icon: 'activity' },
  ];

  readonly secondaryNav: NavItem[] = [{ label: 'Settings', path: '/settings', icon: 'settings' }];

  constructor() {
    this.updateTitle(this.router.url);

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.updateTitle(event.urlAfterRedirects);
        this.sidebarOpen.set(false);
        this.userMenuOpen.set(false);
      });
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  toggleUserMenu(event?: Event): void {
    event?.stopPropagation();
    this.userMenuOpen.update((open) => !open);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
  }

  private updateTitle(url: string): void {
    const path = url.split('?')[0];
    if (path === '/' || path.startsWith('/dashboard')) return this.pageTitle.set('Overview');
    if (path.startsWith('/devices/new')) return this.pageTitle.set('Add device');
    if (/^\/devices\/[^/]+\/edit/.test(path)) return this.pageTitle.set('Edit device');
    if (path.includes('/ping-history')) return this.pageTitle.set('Ping history');
    if (path.includes('/resource-history')) return this.pageTitle.set('Resource history');
    if (/^\/devices\/[^/]+/.test(path)) return this.pageTitle.set('Device details');
    if (path.startsWith('/devices')) return this.pageTitle.set('Devices');
    if (path.startsWith('/alerts/rules')) return this.pageTitle.set('Alert rules');
    if (path.startsWith('/alerts/history')) return this.pageTitle.set('Alert history');
    if (path.startsWith('/logs')) return this.pageTitle.set('Event log');
    if (path.startsWith('/settings')) return this.pageTitle.set('Settings');
    this.pageTitle.set('Overview');
  }
}
