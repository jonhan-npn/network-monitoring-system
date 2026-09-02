import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResourceUsageChart } from './resource-usage-chart';

describe('ResourceUsageChart', () => {
  let component: ResourceUsageChart;
  let fixture: ComponentFixture<ResourceUsageChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResourceUsageChart],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourceUsageChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
