import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeviceStatusChart } from './device-status-chart';

describe('DeviceStatusChart', () => {
  let component: DeviceStatusChart;
  let fixture: ComponentFixture<DeviceStatusChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeviceStatusChart],
    }).compileComponents();

    fixture = TestBed.createComponent(DeviceStatusChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
