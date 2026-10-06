const frontendUrl = 'http://localhost:4200';
const apiUrl = 'https://localhost:7081';

export const environment = {
  production: false,
  frontendUrl,
  apiUrl,
  baseUrl: frontendUrl,
  oAuthConfig: {
    issuer: apiUrl,
    redirectUri: frontendUrl,
    clientId: 'Inventory_App',
    responseType: 'code',
    scope: 'offline_access Inventory_App',
    requireHttps: true,
  },
  apis: {
    default: {
      name: 'Inventory',
      url: apiUrl,
    },
  },
  clientTitle: 'Inventory Management',
};
