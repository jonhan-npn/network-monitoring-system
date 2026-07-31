import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PingHistory } from './ping-history';

describe('PingHistory', () => {
  let component: PingHistory;
  let fixture: ComponentFixture<PingHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PingHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(PingHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
