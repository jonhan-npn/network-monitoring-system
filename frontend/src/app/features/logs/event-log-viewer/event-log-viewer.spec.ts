import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventLogViewer } from './event-log-viewer';

describe('EventLogViewer', () => {
  let component: EventLogViewer;
  let fixture: ComponentFixture<EventLogViewer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventLogViewer],
    }).compileComponents();

    fixture = TestBed.createComponent(EventLogViewer);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
