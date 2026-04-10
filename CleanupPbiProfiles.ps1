# CleanupPbiProfiles.ps1
# Deletes ALL service principal profiles (and their owned workspaces) from the
# Power BI tenant, and clears the local AppOwnsDataDB Tenants table.
# Run this before a full "rebuild all" re-onboard.

$tenantId    = "24945eed-3a26-431f-8ccb-4f5b87881901"
$clientId    = "6c82a905-0cbe-4e08-ba39-d8cfedb70538"
$clientSecret= "ukw8Q~uTHRmQvjDHIVolBOliGgK9oFMXYVlq0boT"

# ── 1. Get access token ─────────────────────────────────────────────────────
$body = @{
    grant_type    = "client_credentials"
    client_id     = $clientId
    client_secret = $clientSecret
    scope         = "https://analysis.windows.net/powerbi/api/.default"
}
$tokenResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
    -Body $body
$token = $tokenResponse.access_token
$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
Write-Host "✅ Token acquired"

# ── 2. List all SP profiles ─────────────────────────────────────────────────
$profilesResp = Invoke-RestMethod `
    -Method Get `
    -Uri "https://api.powerbi.com/v1.0/myorg/profiles" `
    -Headers $headers
$profiles = $profilesResp.value
Write-Host "Found $($profiles.Count) profile(s):"
$profiles | ForEach-Object { Write-Host "  - $($_.displayName) | $($_.id)" }

if ($profiles.Count -eq 0) {
    Write-Host "Nothing to clean up."
} else {
    foreach ($profile in $profiles) {
        $profileId = $profile.id

        # ── 3. List workspaces visible to this profile ──────────────────
        $profileHeaders = @{
            Authorization          = "Bearer $token"
            "Content-Type"         = "application/json"
            "X-PowerBI-Profile-Id" = $profileId
        }
        try {
            $wsResp = Invoke-RestMethod `
                -Method Get `
                -Uri "https://api.powerbi.com/v1.0/myorg/groups?`$top=100" `
                -Headers $profileHeaders
            $workspaces = $wsResp.value
            Write-Host "`nProfile '$($profile.displayName)' owns $($workspaces.Count) workspace(s):"
            foreach ($ws in $workspaces) {
                Write-Host "  Deleting workspace '$($ws.name)' | $($ws.id) ..."
                try {
                    Invoke-RestMethod `
                        -Method Delete `
                        -Uri "https://api.powerbi.com/v1.0/myorg/groups/$($ws.id)" `
                        -Headers $profileHeaders | Out-Null
                    Write-Host "  ✅ Workspace deleted"
                } catch {
                    Write-Host "  ⚠️  Workspace delete failed (may already be gone): $($_.Exception.Message)"
                }
            }
        } catch {
            Write-Host "  ⚠️  Could not list workspaces for profile $profileId : $($_.Exception.Message)"
        }

        # ── 4. Delete the profile ────────────────────────────────────────
        Write-Host "Deleting profile '$($profile.displayName)' | $profileId ..."
        try {
            Invoke-RestMethod `
                -Method Delete `
                -Uri "https://api.powerbi.com/v1.0/myorg/profiles/$profileId" `
                -Headers $headers | Out-Null
            Write-Host "✅ Profile deleted"
        } catch {
            Write-Host "⚠️  Profile delete failed: $($_.Exception.Message)"
        }
    }
}

# ── 5. Clear Tenants table ───────────────────────────────────────────────────
Write-Host "`nClearing AppOwnsDataDB Tenants table..."
try {
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d AppOwnsDataDB -Q "DELETE FROM Tenants"
    Write-Host "✅ Tenants table cleared"
} catch {
    Write-Host "⚠️  sqlcmd failed: $($_.Exception.Message)"
}

Write-Host "`n🎉 Cleanup complete. You can now re-onboard all 4 tenants via the Admin app."
