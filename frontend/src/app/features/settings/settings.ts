import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, RouterLink],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  readonly saved = signal(false);
  monitoringInterval = 30;
  timeout = 5;
  emailAlerts = true;
  telegramAlerts = false;
  maintenanceMode = false;
  save() { this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); }
}
