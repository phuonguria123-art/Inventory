import { RoleModel } from "./role.model";

export class PermissionModel {
    id!: string;
    code!: string;
    description!: string;
    roles!: RoleModel[];
}