import { Configuration, PopupRequest } from "@azure/msal-browser";

// Config Azure AD app setting to be passed to Msal on creation
const TenantId = "24945eed-3a26-431f-8ccb-4f5b87881901";
const ClientId = "e83d71a3-6b80-48bd-9e2c-fa6b04dc40ba";

export const msalConfig: Configuration = {
    auth: {
        clientId: ClientId,
        authority: "https://login.microsoftonline.com/" + TenantId,
        redirectUri: "/",
        postLogoutRedirectUri: "/"        
    }
};

export const userPermissionScopes: string[] = [
  "api://" + ClientId + "/Reports.Embed"
]

export const PowerBiLoginRequest: PopupRequest = {
  scopes: userPermissionScopes
};