import { Component, inject } from '@angular/core';
import { PagesRoutingModule } from '../../pages/pages-routing-module';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PermissionService } from '../../core/services/permission.service';

@Component({
  imports: [RouterLink, RouterLinkActive],
  selector: 'app-sidebar',
  styleUrl: './sidebar.scss',
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly permissionService = inject(PermissionService);

  hasPermission(permission: string): boolean {
    return this.permissionService.getGrantedPolicy(permission);
  }
}
