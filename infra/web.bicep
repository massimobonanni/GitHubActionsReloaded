param location string
param tags object
param environmentName string
param deploymentStage string
param githubRepository string

@description('Numeric GitHub ID of the repository owner (user or organization), not the workflow actor.')
@minLength(1)
param githubOwnerId string

@description('Numeric GitHub repository ID.')
@minLength(1)
param githubRepositoryId string

@secure()
param demoApiKey string

var suffix = uniqueString(subscription().id, resourceGroup().id)
var appName = 'excuseops-${deploymentStage}-${suffix}'
var githubRepositoryParts = split(githubRepository, '/')
var websiteContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'de139f84-1756-47ae-9be6-808fbbe84772')

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-${environmentName}'
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appCommandLine: 'dotnet ExcuseOps.Web.dll'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'DeploymentStage'
          value: deploymentStage
        }
        {
          name: 'Demo__ApiKey'
          value: demoApiKey
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
      ]
    }
  }
}

resource scmBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: app
  name: 'scm'
  properties: {
    allow: false
  }
}

resource ftpBasicAuth 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: app
  name: 'ftp'
  properties: {
    allow: false
  }
}

resource deployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${environmentName}-github'
  location: location
  tags: tags
}

resource githubFederation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: deployIdentity
  name: 'github-${deploymentStage}'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepositoryParts[0]}@${githubOwnerId}/${githubRepositoryParts[1]}@${githubRepositoryId}:environment:${deploymentStage}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

// Includes sites/publish/action and config/list/action for Entra-authenticated Kudu deployment.
// Supplying resource-group-name to webapps-deploy avoids subscription-wide resource discovery.
resource appDeploymentRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(app.id, deployIdentity.id, websiteContributor)
  scope: app
  properties: {
    roleDefinitionId: websiteContributor
    principalId: deployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output appName string = app.name
output endpoint string = 'https://${app.properties.defaultHostName}'
output clientId string = deployIdentity.properties.clientId
