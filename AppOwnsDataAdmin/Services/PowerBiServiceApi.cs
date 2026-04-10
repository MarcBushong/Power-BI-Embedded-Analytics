using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;
using Microsoft.Rest;
using AppOwnsDataShared.Models;
using Microsoft.Identity.Web;
using AppOwnsDataShared.Services;
using System.Text;

namespace AppOwnsDataAdmin.Services {

  public class EmbeddedReportViewModel {
    public string ReportId;
    public string Name;
    public string EmbedUrl;
    public string Token;
    public string TenantName;
  }

  public class PowerBiTenantDetails : PowerBiTenant {
    public IList<Report> Reports { get; set; }
    public IList<Dataset> Datasets { get; set; }
    public IList<GroupUser> Members { get; set; }
  }

  public class PowerBiServiceApi {

    private readonly AppOwnsDataDBService AppOwnsDataDBService;
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment Env;

    private ITokenAcquisition tokenAcquisition { get; }
    private string urlPowerBiServiceApiRoot { get; }
    private string targetCapacityId { get; }
    private PowerBIClient pbiClient { get; set; }

    public PowerBiServiceApi(IConfiguration configuration, ITokenAcquisition tokenAcquisition, AppOwnsDataDBService AppOwnsDataDBService, IWebHostEnvironment env) {
      this.Configuration = configuration;
      this.urlPowerBiServiceApiRoot = configuration["PowerBi:ServiceRootUrl"];
      this.targetCapacityId = configuration["PowerBi:TargetCapacityId"];
      this.tokenAcquisition = tokenAcquisition;
      this.AppOwnsDataDBService = AppOwnsDataDBService;
      this.Env = env;
      pbiClient = GetPowerBiClient();
    }

    public const string powerbiApiDefaultScope = "https://analysis.windows.net/powerbi/api/.default";

    public string GetAccessToken() {
      return this.tokenAcquisition.GetAccessTokenForAppAsync(powerbiApiDefaultScope).Result;
    }

    public PowerBIClient GetPowerBiClient() {
      var tokenCredentials = new TokenCredentials(GetAccessToken(), "Bearer");
      return new PowerBIClient(new Uri(urlPowerBiServiceApiRoot), tokenCredentials);
    }

    private PowerBIClient GetPowerBiClientForProfile(Guid ProfileId) {
      var tokenCredentials = new TokenCredentials(GetAccessToken(), "Bearer");
      return new PowerBIClient(new Uri(urlPowerBiServiceApiRoot), tokenCredentials, ProfileId);
    }

    private void SetCallingContext(string ProfileId = "") {
      if (ProfileId.Equals("")) {
        pbiClient = GetPowerBiClient();
      }
      else {
        pbiClient = GetPowerBiClientForProfile(new Guid(ProfileId));
      }
    }

    public async Task<EmbeddedReportViewModel> GetReport(Guid WorkspaceId, Guid ReportId) {

      // call to Power BI Service API to get embedding data
      var report = await pbiClient.Reports.GetReportInGroupAsync(WorkspaceId, ReportId);

      // generate read-only embed token for the report
      var datasetId = report.DatasetId;
      var tokenRequest = new GenerateTokenRequest(TokenAccessLevel.View, datasetId);
      var embedTokenResponse = await pbiClient.Reports.GenerateTokenAsync(WorkspaceId, ReportId, tokenRequest);
      var embedToken = embedTokenResponse.Token;

      // return report embedding data to caller
      return new EmbeddedReportViewModel {
        ReportId = report.Id.ToString(),
        EmbedUrl = report.EmbedUrl,
        Name = report.Name,
        Token = embedToken
      };
    }

    public Dataset GetDataset(Guid WorkspaceId, string DatasetName) {
      var datasets = pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
      foreach (var dataset in datasets) {
        if (dataset.Name.Equals(DatasetName)) {
          return dataset;
        }
      }
      return null;
    }

    public async Task<IList<Group>> GetTenantWorkspaces() {
      var workspaces = (await pbiClient.Groups.GetGroupsAsync()).Value;
      return workspaces;
    }

        /*    public PowerBiTenant OnboardNewTenant(PowerBiTenant tenant) {

              this.SetCallingContext();

              var createRequest = new CreateOrUpdateProfileRequest(tenant.Name + " Profile");
              var profile = pbiClient.Profiles.CreateProfile(createRequest);

              string profileId = profile.Id.ToString();
              tenant.ProfileId = profileId;

              SetCallingContext(profileId);
        */
        public PowerBiTenant OnboardNewTenant(PowerBiTenant tenant)
        {
            this.SetCallingContext();

            ServicePrincipalProfile profile;
            Group workspace;

            try
            {
                // ─── STEP 1: Get or Create Profile ──────────────────────
                string profileName = tenant.Name + " Profile";

                var existingProfiles = pbiClient.Profiles.GetProfiles();
                var existingProfile = existingProfiles.Value
                    .FirstOrDefault(p => p.DisplayName == profileName);

                if (existingProfile != null)
                {
                    profile = existingProfile;
                    Console.WriteLine($"✅ Reusing profile: {profile.DisplayName} | {profile.Id}");
                }
                else
                {
                    var profileRequest = new CreateOrUpdateProfileRequest(profileName);
                    profile = pbiClient.Profiles.CreateProfile(profileRequest);
                    Console.WriteLine($"✅ Created profile: {profile.DisplayName} | {profile.Id}");
                }

                string profileId = profile.Id.ToString();
                tenant.ProfileId = profileId;

                // ─── STEP 2: Get or Create Workspace ────────────────────
                string workspaceName = "AODSK: " + tenant.Name;
                bool workspaceFoundInProfileContext = false;

                // ✅ Search profile context first
                SetCallingContext(profileId);
                var allProfileWorkspaces = pbiClient.Groups.GetGroups(top: 100);

                // ✅ Search SP root context
                this.SetCallingContext();
                var allRootWorkspaces = pbiClient.Groups.GetGroups(top: 100);

                // ✅ Check profile context first
                workspace = allProfileWorkspaces.Value
                    .FirstOrDefault(w =>
                        w.Name.Trim().ToLower() == workspaceName.Trim().ToLower() ||
                        w.Name.Trim().ToLower() == tenant.Name.Trim().ToLower()
                    );

                if (workspace != null)
                {
                    workspaceFoundInProfileContext = true;
                    Console.WriteLine($"✅ Found in profile context: '{workspace.Name}' | {workspace.Id}");
                }
                else
                {
                    // ✅ Check SP root context
                    workspace = allRootWorkspaces.Value
                        .FirstOrDefault(w =>
                            w.Name.Trim().ToLower() == workspaceName.Trim().ToLower() ||
                            w.Name.Trim().ToLower() == tenant.Name.Trim().ToLower()
                        );

                    if (workspace != null)
                    {
                        workspaceFoundInProfileContext = false;
                        Console.WriteLine($"✅ Found in SP root context: '{workspace.Name}' | {workspace.Id}");
                    }
                    else
                    {
                        // Create new workspace under profile context so the profile owns it
                        SetCallingContext(profileId);
                        var workspaceRequest = new GroupCreationRequest(workspaceName);
                        workspace = pbiClient.Groups.CreateGroup(
                            workspaceRequest,
                            workspaceV2: true
                        );
                        workspaceFoundInProfileContext = true;
                        Console.WriteLine($"✅ Created workspace under profile context: '{workspace.Name}' | {workspace.Id}");
                    }
                }

                // If the workspace was found in SP root context, add the profile as Admin
                // so all subsequent steps can operate under the profile context
                if (!workspaceFoundInProfileContext)
                {
                    Console.WriteLine($"Migrating workspace to profile context — adding profile as Admin...");
                    this.SetCallingContext();
                    pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                    {
                        Identifier = profileId,
                        PrincipalType = PrincipalType.App,
                        GroupUserAccessRight = "Admin"
                    });
                    workspaceFoundInProfileContext = true;
                    Console.WriteLine($"✅ Profile added as Admin — switching to profile context");
                }

                // All remaining steps use the profile context
                SetCallingContext(profileId);

                tenant.WorkspaceId = workspace.Id.ToString();
                tenant.WorkspaceUrl = "https://app.powerbi.com/groups/" + workspace.Id + "/";

                // ─── STEP 3: Assign to Capacity ─────────────────────────
                if (!string.IsNullOrEmpty(targetCapacityId))
                {
                    if (workspace.IsOnDedicatedCapacity == true)
                    {
                        Console.WriteLine($"✅ Already on capacity — skipping");
                    }
                    else
                    {
                        SetCallingContext(profileId);
                        Console.WriteLine($"Assigning to capacity (profile context)...");
                        pbiClient.Groups.AssignToCapacity(
                            workspace.Id,
                            new AssignToCapacityRequest
                            {
                                CapacityId = new Guid(targetCapacityId)
                            }
                        );
                        Console.WriteLine($"✅ AssignToCapacity succeeded");
                    }
                }

                // ─── STEP 4: Add Service Principal as Contributor ───────
                SetCallingContext(profileId);

                // ✅ Wait for workspace to fully provision
                System.Threading.Thread.Sleep(3000);

                IList<GroupUser> workspaceUsers;
                try
                {
                    workspaceUsers = pbiClient.Groups.GetGroupUsers(workspace.Id).Value;
                    Console.WriteLine($"Workspace users: {workspaceUsers.Count}");
                    foreach (var u in workspaceUsers)
                    {
                        Console.WriteLine($"  → {u.Identifier} | {u.PrincipalType} | {u.GroupUserAccessRight}");
                    }
                }
                catch (HttpOperationException)
                {
                    Console.WriteLine($"⚠️ GetGroupUsers failed — treating as empty");
                    workspaceUsers = new List<GroupUser>();
                }

                // ✅ Get SP object ID from config
                string servicePrincipalObjectId = Configuration["PowerBi:ServicePrincipalObjectId"];

                if (!string.IsNullOrEmpty(servicePrincipalObjectId))
                {
                    bool spAlreadyAdded = workspaceUsers.Any(u =>
                        u.Identifier == servicePrincipalObjectId &&
                        u.PrincipalType == PrincipalType.App
                    );

                    if (!spAlreadyAdded)
                    {
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            Identifier = servicePrincipalObjectId,
                            PrincipalType = PrincipalType.App,
                            GroupUserAccessRight = "Contributor"
                        });
                        Console.WriteLine($"✅ Added SP as Contributor");
                    }
                    else
                    {
                        Console.WriteLine($"✅ SP already a member — skipping");
                    }
                }

                // ─── STEP 5: Add Admin User ──────────────────────────────
                string adminUserEmail = Configuration["DemoSettings:AdminUser"];
                if (!string.IsNullOrEmpty(adminUserEmail))
                {
                    bool adminAlreadyAdded = workspaceUsers.Any(u =>
                        u.EmailAddress == adminUserEmail
                    );

                    if (!adminAlreadyAdded)
                    {
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            Identifier = adminUserEmail,
                            PrincipalType = PrincipalType.User,
                            EmailAddress = adminUserEmail,
                            GroupUserAccessRight = "Admin"
                        });
                        Console.WriteLine($"✅ Added admin user: {adminUserEmail}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Admin already a member — skipping");
                    }


                 
                }                // ─── STEP 6: Publish PBIX ────────────────────────────────

                if (workspaceFoundInProfileContext)
                {
                    SetCallingContext(profileId);
                }
                else
                {
                    this.SetCallingContext();
                }
                string importName = "Sales";

                Dataset dataset = GetDataset(workspace.Id, importName);

                if (dataset != null)
                {
                    // Check if the existing dataset's DatabaseName parameter matches
                    // what the tenant is configured for — if not, delete and republish
                    var currentParams = pbiClient.Datasets.GetParametersInGroup(workspace.Id, dataset.Id).Value;
                    var currentDb = currentParams.FirstOrDefault(p => p.Name == "DatabaseName")?.CurrentValue ?? "";

                    if (!string.Equals(currentDb, tenant.DatabaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"⚠️ Dataset database mismatch: '{currentDb}' → '{tenant.DatabaseName}' — republishing PBIX");
                        pbiClient.Datasets.DeleteDatasetInGroup(workspace.Id, dataset.Id);
                        dataset = null;
                    }
                    else
                    {
                        Console.WriteLine($"✅ Dataset already exists and database matches — skipping publish");
                    }
                }

                if (dataset == null)
                {
                    Console.WriteLine($"Publishing PBIX: {importName}");
                    PublishPBIX(workspace.Id, importName, tenant.DatabaseName);
                    dataset = GetDataset(workspace.Id, importName);
                    Console.WriteLine($"✅ PBIX published: {importName}");
                }

                // ─── STEP 7: Update Parameters ───────────────────────────
               
                
                
                var req = new UpdateMashupParametersRequest(
                    new List<UpdateMashupParameterDetails>() {
                new UpdateMashupParameterDetails {
                    Name = "DatabaseServer",
                    NewValue = tenant.DatabaseServer
                },
                new UpdateMashupParameterDetails {
                    Name = "DatabaseName",
                    NewValue = tenant.DatabaseName
                }
                    }
                );

                pbiClient.Datasets.UpdateParametersInGroup(workspace.Id, dataset.Id, req);
                Console.WriteLine($"✅ Parameters updated");

                // Wait for Power BI to rebind the datasource after the parameter change
                // before patching credentials — if we patch too soon we hit the old
                // template datasource (devcamp) instead of the new target server.
                System.Threading.Thread.Sleep(6000);

                // ─── STEP 8: Patch Credentials + Refresh ────────────────
                // GetDatasourcesInGroup must use profile context (dataset is profile-owned).
                // Gateways.UpdateDatasource must use SP root context (virtual gateway admin).
                // profileId is passed so PatchSqlDatasourceCredentials can manage both.
                PatchSqlDatasourceCredentials(
                    workspace.Id,
                    dataset.Id,
                    tenant.DatabaseUserName,
                    tenant.DatabaseUserPassword,
                    profileId
                );
                Console.WriteLine($"✅ Credentials patched");

                SetCallingContext(profileId); // ensure profile context for refresh
                pbiClient.Datasets.RefreshDatasetInGroup(workspace.Id, dataset.Id);
                Console.WriteLine($"✅ Dataset refresh triggered");

                // ─── STEP 9: Publish Paginated Report ───────────────────
                if (!string.IsNullOrEmpty(targetCapacityId))
                {
                    string paginatedReportName = "Sales Summary";

                    var existingReports = pbiClient.Reports.GetReports(workspace.Id).Value;
                    bool reportExists = existingReports.Any(r => r.Name == paginatedReportName);

                    if (!reportExists)
                    {
                        PublishRDL(workspace, paginatedReportName, dataset);
                        Console.WriteLine($"✅ Paginated report published");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Paginated report already exists — skipping");
                    }
                }
            }
            catch (HttpOperationException ex)
            {
                var errorBody = ex.Response?.Content ?? "No response body";
                throw new Exception($"Power BI API Error: {errorBody}", ex);
            }

            return tenant;
        }
        public PowerBiTenantDetails GetTenantDetails(PowerBiTenant tenant) {

      SetCallingContext(tenant.ProfileId);

      return new PowerBiTenantDetails {
        Name = tenant.Name,
        DatabaseName = tenant.DatabaseName,
        DatabaseServer = tenant.DatabaseServer,
        DatabaseUserName = tenant.DatabaseUserName,
        DatabaseUserPassword = tenant.DatabaseUserPassword,
        ProfileId = tenant.ProfileId,
        Created = tenant.Created,
        WorkspaceId = tenant.WorkspaceId,
        WorkspaceUrl = tenant.WorkspaceUrl,
        Members = pbiClient.Groups.GetGroupUsers(new Guid(tenant.WorkspaceId)).Value,
        Datasets = pbiClient.Datasets.GetDatasetsInGroup(new Guid(tenant.WorkspaceId)).Value,
        Reports = pbiClient.Reports.GetReportsInGroup(new Guid(tenant.WorkspaceId)).Value
      };

    }

    public void DeleteTenant(PowerBiTenant tenant) {

      Guid workspaceIdGuid = new Guid(tenant.WorkspaceId);

      // try profile context first, then SP root; if workspace is already gone, skip gracefully
      try {
        SetCallingContext(tenant.ProfileId);
        pbiClient.Groups.DeleteGroup(workspaceIdGuid);
      }
      catch (HttpOperationException ex) when (
          ex.Response?.StatusCode == System.Net.HttpStatusCode.BadRequest ||
          ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound) {
        Console.WriteLine($"⚠️ Workspace already deleted — skipping DeleteGroup");
      }
      catch (HttpOperationException) {
        try {
          SetCallingContext();
          pbiClient.Groups.DeleteGroup(workspaceIdGuid);
        }
        catch (HttpOperationException ex2) when (
            ex2.Response?.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            ex2.Response?.StatusCode == System.Net.HttpStatusCode.NotFound) {
          Console.WriteLine($"⚠️ Workspace already deleted — skipping DeleteGroup");
        }
      }

      // delete the service principal profile
      try {
        SetCallingContext();
        pbiClient.Profiles.DeleteProfile(new Guid(tenant.ProfileId));
      }
      catch (HttpOperationException) {
        Console.WriteLine($"⚠️ Profile already deleted — skipping DeleteProfile");
      }

    }

    public void PublishPBIX(Guid WorkspaceId, string ImportName, string DatabaseName = "") {

      string template = DatabaseName.ToLower() switch {
        "contososales"   => "SalesReportTemplate_EU.pbix",
        _                => "SalesReportTemplate.pbix"   // US default (WingtipSales, MegaCorpSales, AcmeCorpSales)
      };

      string PbixFilePath = System.IO.Path.Combine(this.Env.WebRootPath, "PBIX", template);

      // fall back to base template if the region-specific file hasn't been created yet
      if (!System.IO.File.Exists(PbixFilePath)) {
        PbixFilePath = System.IO.Path.Combine(this.Env.WebRootPath, "PBIX", "SalesReportTemplate.pbix");
      }

      FileStream stream = new FileStream(PbixFilePath, FileMode.Open, FileAccess.Read);

      var import = pbiClient.Imports.PostImportWithFileInGroup(WorkspaceId, stream, ImportName);

      while (import.ImportState != "Succeeded") {
        System.Threading.Thread.Sleep(1000);
        import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id);
      }

      // Give Power BI a moment after import completes before the caller
      // tries to query or update the freshly-created dataset.
      System.Threading.Thread.Sleep(3000);

    }

    public void PatchSqlDatasourceCredentials(Guid WorkspaceId, string DatasetId, string SqlUserName, string SqlUserPassword, string ProfileId = "") {

      // GetDatasourcesInGroup requires profile context — datasets in profile-owned
      // workspaces are invisible to the SP root even when it has workspace membership.
      if (!ProfileId.Equals("")) {
        SetCallingContext(ProfileId);
      }
      var datasources = (pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId)).Value;

      // Gateways.UpdateDatasource requires SP root context — the virtual gateway
      // admin is the service principal, not the profile.
      SetCallingContext();

      // find the target SQL datasource
      foreach (var datasource in datasources) {
        if (datasource.DatasourceType.ToLower() == "sql") {
          // get the datasourceId and the gatewayId
          var datasourceId = datasource.DatasourceId;
          var gatewayId = datasource.GatewayId;
          // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
          UpdateDatasourceRequest req = new UpdateDatasourceRequest {
            CredentialDetails = new CredentialDetails(
              new BasicCredentials(SqlUserName, SqlUserPassword),
              PrivacyLevel.None,
              EncryptedConnection.NotEncrypted)
          };
          // Execute Patch command to update Azure SQL datasource credentials
          pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
        }
      };

    }

    public void PublishRDL(Group Workspace, string ImportName, Dataset TargetDataset) {

      string rdlFilePath = this.Env.WebRootPath + @"/PBIX/SalesSummaryPaginated.rdl";

      FileStream stream = new FileStream(rdlFilePath, FileMode.Open, FileAccess.Read);
      StreamReader reader = new StreamReader(stream);
      string rdlFileContent = reader.ReadToEnd();
      reader.Close();
      stream.Close();

      rdlFileContent = rdlFileContent.Replace("{{TargetDatasetId}}", TargetDataset.Id.ToString())
                                     .Replace("{{PowerBIWorkspaceName}}", Workspace.Name)
                                     .Replace("{{PowerBIDatasetName}}", TargetDataset.Name);

      MemoryStream contentSteam = new MemoryStream(Encoding.ASCII.GetBytes(rdlFileContent));

      string rdlImportName = ImportName + ".rdl";

      var import = pbiClient.Imports.PostImportWithFileInGroup(Workspace.Id, contentSteam, rdlImportName, ImportConflictHandlerMode.Abort);

      // poll to determine when import operation has complete
      do { import = pbiClient.Imports.GetImportInGroup(Workspace.Id, import.Id); }
      while (import.ImportState.Equals("Publishing"));

      Guid reportId = import.Reports[0].Id;

    }

    public async Task<EmbeddedReportViewModel> GetReportEmbeddingData(PowerBiTenant Tenant) {

      SetCallingContext(Tenant.ProfileId);

      Guid workspaceId = new Guid(Tenant.WorkspaceId);
      var reports = (await pbiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;

      var report = reports.Where(report => report.Name.Equals("Sales")).First();

      GenerateTokenRequest generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View");

      // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
      string embedToken = pbiClient.Reports.GenerateTokenInGroup(workspaceId, report.Id,
                                                                 generateTokenRequestParameters).Token;

      return new EmbeddedReportViewModel {
        ReportId = report.Id.ToString(),
        Name = report.Name,
        EmbedUrl = report.EmbedUrl,
        Token = embedToken,
        TenantName = Tenant.Name
      };

    }

  }

}