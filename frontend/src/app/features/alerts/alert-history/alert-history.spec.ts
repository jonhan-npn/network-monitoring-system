import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertHistory } from './alert-history';

describe('AlertHistory', () => {
  let component: AlertHistory;
  let fixture: ComponentFixture<AlertHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AlertHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(AlertHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
