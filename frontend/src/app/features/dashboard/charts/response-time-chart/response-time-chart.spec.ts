import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResponseTimeChart } from './response-time-chart';

describe('ResponseTimeChart', () => {
  let component: ResponseTimeChart;
  let fixture: ComponentFixture<ResponseTimeChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResponseTimeChart],
    }).compileComponents();

    fixture = TestBed.createComponent(ResponseTimeChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
