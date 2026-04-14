import * as msal from "@azure/msal-browser";
import AppSettings from './../appSettings';

export default class SpaAuthService {

  private static clientId: string = AppSettings.clientId;
  private static authority: string = "https://login.microsoftonline.com/" + AppSettings.tenant;

  public static userIsAuthenticated: boolean = false;
  public static userDisplayName: string = "";
  public static userName: string = "";
  public static uiUpdateCallback: any;

  private static msalConfig: msal.Configuration = {
    auth: {
      clientId: SpaAuthService.clientId,
      authority: SpaAuthService.authority,
    },
    cache: {
      cacheLocation: "localStorage",
      storeAuthStateInCookie: false
    }
  };

  private static publicApplication: msal.PublicClientApplication =
                 new msal.PublicClientApplication(SpaAuthService.msalConfig);

  // MSAL-browser 2.28+ requires explicit initialization before any auth operation
  private static initPromise: Promise<void> | null = null;

  private static ensureInitialized = (): Promise<void> => {
    if (!SpaAuthService.initPromise) {
      SpaAuthService.initPromise = SpaAuthService.publicApplication.initialize();
    }
    return SpaAuthService.initPromise;
  };

  static attemptSillentLogin = async () => {
    await SpaAuthService.ensureInitialized();
    var userInfo: msal.AccountInfo = SpaAuthService.publicApplication.getAllAccounts()[0];
    if (userInfo) {
      SpaAuthService.userName = userInfo.username;
      SpaAuthService.userDisplayName = userInfo.name;
      SpaAuthService.userIsAuthenticated = true;
      SpaAuthService.uiUpdateCallback();
    }
  }

  static login = async () => {
    try {
      await SpaAuthService.ensureInitialized();
      var loginRequest: msal.PopupRequest = { scopes: AppSettings.apiScopes }
      var loginResult: msal.AuthenticationResult = await SpaAuthService.publicApplication.loginPopup(loginRequest);
      var userInfo: msal.AccountInfo = loginResult.account;
      SpaAuthService.userName = userInfo.username;
      SpaAuthService.userDisplayName = userInfo.name;
      SpaAuthService.userIsAuthenticated = true;
      SpaAuthService.uiUpdateCallback();
    }
    catch (error: any) {
      console.error("Login failed:", error);
      if (error instanceof msal.BrowserAuthError && error.errorCode === "interaction_in_progress") {
        localStorage.clear();
        sessionStorage.clear();
        document.cookie = 'msal.interaction.status=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/';
        location.reload();
      } else if (error instanceof msal.BrowserAuthError && error.errorCode === "popup_window_error") {
        alert("The login popup was blocked. Please allow popups for this site in your browser settings and try again.");
      } else if (!(error instanceof msal.BrowserAuthError && error.errorCode === "user_cancelled")) {
        alert("Login failed: " + (error.message || "Unknown error. Check the browser console for details."));
      }
    }
  }

  static logout = () => {
    SpaAuthService.userName = "";
    SpaAuthService.userDisplayName = "";
    SpaAuthService.userIsAuthenticated = false;
    sessionStorage.clear();
    localStorage.clear();
    document.cookie = 'msal.interaction.status=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/';
    location.reload();
  }

  static async getAccessToken(): Promise<string> {

    var tokenRequest: msal.SilentRequest = {
      scopes: AppSettings.apiScopes,
      account: SpaAuthService.publicApplication.getAccountByUsername(SpaAuthService.userName)
    };

    var tokenReponse: msal.AuthenticationResult;
    try {
      tokenReponse = <msal.AuthenticationResult>(await SpaAuthService.publicApplication.acquireTokenSilent(tokenRequest));
    }
    catch (error) {
      if (error instanceof msal.InteractionRequiredAuthError) {
        tokenReponse = await SpaAuthService.publicApplication.acquireTokenPopup(tokenRequest);
      }
      else {
        throw error;
      }
    }
    // return access token to caller 
    var accessToken: string = tokenReponse.accessToken;
    return accessToken;
  }

}
