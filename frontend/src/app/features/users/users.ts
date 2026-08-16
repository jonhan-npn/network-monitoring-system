import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-users',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class Users {
  readonly query = signal('');
  readonly users = [
    { initials: 'JM', name: 'Johan M.', email: 'johan@northstar.io', role: 'Administrator', lastActive: 'Active now', status: 'Active', color: 'orange' },
    { initials: 'SL', name: 'Sokha Lim', email: 'sokha@northstar.io', role: 'Network operator', lastActive: '12 minutes ago', status: 'Active', color: 'violet' },
    { initials: 'DP', name: 'Dara Pich', email: 'dara@northstar.io', role: 'Viewer', lastActive: 'Yesterday', status: 'Active', color: 'blue' },
    { initials: 'KS', name: 'Kosal S.', email: 'kosal@northstar.io', role: 'Network operator', lastActive: 'Aug 1, 2026', status: 'Invited', color: 'green' },
  ];
  readonly filtered = computed(() => this.users.filter(user => Object.values(user).some(value => value.toLowerCase().includes(this.query().toLowerCase()))));
}
