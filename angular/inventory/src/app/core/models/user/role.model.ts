import { PermissionModel } from "./permission.model";

export class RoleModel {
    id!: string;
    name!: string;
    description!: string;
    permissions!: PermissionModel[];
}