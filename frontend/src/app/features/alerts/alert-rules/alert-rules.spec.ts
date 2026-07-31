import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AlertRules } from './alert-rules';

describe('AlertRules', () => {
  let component: AlertRules;
  let fixture: ComponentFixture<AlertRules>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AlertRules],
    }).compileComponents();

    fixture = TestBed.createComponent(AlertRules);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
