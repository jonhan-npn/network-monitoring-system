import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResourceHistory } from './resource-history';

describe('ResourceHistory', () => {
  let component: ResourceHistory;
  let fixture: ComponentFixture<ResourceHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResourceHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourceHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
