import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Adjust } from './adjust';

describe('Adjust', () => {
  let component: Adjust;
  let fixture: ComponentFixture<Adjust>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Adjust],
    }).compileComponents();

    fixture = TestBed.createComponent(Adjust);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
