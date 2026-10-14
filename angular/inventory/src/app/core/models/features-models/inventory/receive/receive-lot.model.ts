export class ReceiveLotModel {
    batchNumber: string = '';
    quantity!: number;
    unitCost!: number;
    manufactureDate?: string | null;
    expiryDate?: string | null;
    location?: string | null;
}