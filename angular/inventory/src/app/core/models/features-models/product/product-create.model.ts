export class ProductCreateModel {
    name!: string;
    code!: string;
    imgUrl!: string | null;
    origin!: string | null;
    weight!: number;
    unitPrice!: number;
    supplierId!: string | null;
}
