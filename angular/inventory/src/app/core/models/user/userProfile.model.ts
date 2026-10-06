import { RoleModel } from "./role.model";

export class UserProfile {
    id!: string;
    email!: string;
    userName!: string;
    roles!: RoleModel[];
}