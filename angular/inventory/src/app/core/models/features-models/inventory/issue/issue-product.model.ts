import { IssueLotModel } from "./issue-lot.model";

export class IssueProductModel {
    productId!: string
    lots!: IssueLotModel[];
}