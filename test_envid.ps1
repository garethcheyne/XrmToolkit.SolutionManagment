# Test script to check EnvironmentId resolution
# Run from PowerShell: .\test_envid.ps1
# It will prompt for your Dataverse org URL

param(
    [string]$OrgUrl
)

if (-not $OrgUrl) {
    $OrgUrl = Read-Host "Enter Dataverse org URL (e.g. https://yourorg.crm.dynamics.com)"
}

$xtbRoot = "C:\APPS\XRMTOOLBOX"

# Load required assemblies
Add-Type -Path "$xtbRoot\Microsoft.Xrm.Sdk.dll"
Add-Type -Path "$xtbRoot\Microsoft.Xrm.Tooling.Connector.dll"
Add-Type -Path "$xtbRoot\Microsoft.Crm.Sdk.Proxy.dll"

Write-Host "`n=== Connecting to $OrgUrl ===" -ForegroundColor Cyan
Write-Host "(Will open a browser login window if needed)" -ForegroundColor Gray

try {
    $connStr = "AuthType=OAuth;Url=$OrgUrl;LoginPrompt=Auto;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d"
    $svc = New-Object Microsoft.Xrm.Tooling.Connector.CrmServiceClient($connStr)

    if (-not $svc.IsReady) {
        Write-Host "ERROR: Connection failed - $($svc.LastCrmError)" -ForegroundColor Red
        exit 1
    }

    Write-Host "Connected OK!`n" -ForegroundColor Green

    # 1. CrmServiceClient.EnvironmentId
    Write-Host "1. CrmServiceClient.EnvironmentId  = " -NoNewline
    $val = $svc.EnvironmentId
    if ($val) { Write-Host $val -ForegroundColor Green } else { Write-Host "(null)" -ForegroundColor Red }

    # 2. OrganizationDetail.EnvironmentId
    Write-Host "2. OrganizationDetail.EnvironmentId = " -NoNewline
    $val2 = $svc.OrganizationDetail?.EnvironmentId
    if ($val2) { Write-Host $val2 -ForegroundColor Green } else { Write-Host "(null)" -ForegroundColor Red }

    # 3. ConnectedOrgId (this is the Dataverse org ID, NOT the environment ID)
    Write-Host "3. ConnectedOrgId (Dataverse)       = " -NoNewline
    Write-Host $svc.ConnectedOrgId -ForegroundColor Yellow

    # 4. TenantId
    Write-Host "4. TenantId                         = " -NoNewline
    Write-Host $svc.TenantId -ForegroundColor Yellow

    # 5. ConnectedOrgFriendlyName
    Write-Host "5. ConnectedOrgFriendlyName         = " -NoNewline
    Write-Host $svc.ConnectedOrgFriendlyName -ForegroundColor Yellow

    # 6. CrmConnectOrgUriActual
    Write-Host "6. CrmConnectOrgUriActual           = " -NoNewline
    Write-Host $svc.CrmConnectOrgUriActual -ForegroundColor Yellow

    # 7. OrganizationDetail full dump
    Write-Host "`n=== OrganizationDetail properties ===" -ForegroundColor Cyan
    $od = $svc.OrganizationDetail
    if ($od) {
        Write-Host "  EnvironmentId    = $($od.EnvironmentId)"
        Write-Host "  OrganizationId   = $($od.OrganizationId)"
        Write-Host "  FriendlyName     = $($od.FriendlyName)"
        Write-Host "  UniqueName       = $($od.UniqueName)"
        Write-Host "  UrlName          = $($od.UrlName)"
        Write-Host "  Geo              = $($od.Geo)"
        Write-Host "  State            = $($od.State)"
        Write-Host "  OrganizationVersion = $($od.OrganizationVersion)"
        Write-Host "  Endpoints:"
        foreach ($ep in $od.Endpoints.GetEnumerator()) {
            Write-Host "    $($ep.Key) = $($ep.Value)"
        }
    } else {
        Write-Host "  (OrganizationDetail is null)" -ForegroundColor Red
    }

    Write-Host "`n=== Expected Power Automate maker URL ===" -ForegroundColor Cyan
    $envId = $svc.EnvironmentId
    if (-not $envId) { $envId = $od?.EnvironmentId }
    if ($envId) {
        Write-Host "https://make.powerapps.com/environments/$envId/solutions/fd140aaf-4df4-11dd-bd17-0019b9312238" -ForegroundColor Green
    } else {
        Write-Host "Could not resolve EnvironmentId from any source!" -ForegroundColor Red
    }
}
catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
}
finally {
    if ($svc) { $svc.Dispose() }
}
