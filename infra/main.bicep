targetScope = 'subscription'

param environmentName string
param location string

@allowed(['test', 'production'])
param deploymentStage string

@description('Exact, case-sensitive GitHub OWNER/REPO, with no URL prefix.')
param githubRepository string

@description('Numeric GitHub ID of the repository owner (user or organization), not the workflow actor.')
@minLength(1)
param githubOwnerId string

@description('Numeric GitHub repository ID.')
@minLength(1)
param githubRepositoryId string

@secure()
@minLength(24)
param demoApiKey string

var tags = {
  'azd-env-name': environmentName
  workload: 'ExcuseOps'
  stage: deploymentStage
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module web 'web.bicep' = {
  name: 'web-${deploymentStage}'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    environmentName: environmentName
    deploymentStage: deploymentStage
    githubRepository: githubRepository
    githubOwnerId: githubOwnerId
    githubRepositoryId: githubRepositoryId
    demoApiKey: demoApiKey
  }
}

output AZURE_RESOURCE_GROUP string = resourceGroup.name
output AZURE_WEBAPP_NAME string = web.outputs.appName
output APP_URL string = web.outputs.endpoint
output SERVICE_WEB_ENDPOINT string = web.outputs.endpoint
output AZURE_CLIENT_ID string = web.outputs.clientId
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
