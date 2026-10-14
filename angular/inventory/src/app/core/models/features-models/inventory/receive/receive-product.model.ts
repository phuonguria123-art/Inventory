import { ReceiveLotModel } from "./receive-lot.model";

export class ReceiveProductModel {
    productId!: string;
    lots: ReceiveLotModel[] = [];
}