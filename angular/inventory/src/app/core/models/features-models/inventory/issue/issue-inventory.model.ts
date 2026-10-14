import { IssueProductModel } from "./issue-product.model";

export class IssueInventoryModel {
    warehouseId!: string;
    reference?: string;
    notes?: string;
    products!: IssueProductModel[]
}