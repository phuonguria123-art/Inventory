import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InventoryTransaction } from './inventory-transaction';

describe('InventoryTransaction', () => {
  let component: InventoryTransaction;
  let fixture: ComponentFixture<InventoryTransaction>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryTransaction],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryTransaction);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
