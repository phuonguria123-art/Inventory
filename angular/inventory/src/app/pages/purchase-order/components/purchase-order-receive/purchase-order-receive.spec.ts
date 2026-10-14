import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PurchaseOrderReceive } from './purchase-order-receive';

describe('PurchaseOrderReceive', () => {
  let component: PurchaseOrderReceive;
  let fixture: ComponentFixture<PurchaseOrderReceive>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseOrderReceive],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseOrderReceive);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
