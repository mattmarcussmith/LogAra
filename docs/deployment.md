# Deployment Notes

This repository is prepared for deployment from GitHub `main` to Azure App Service for Containers using Azure Container Registry.

## Prerequisites

1. Create an Azure Container Registry and an Azure App Service (Linux, container).
2. Configure federated credentials for the GitHub repository on an Azure Entra application/service principal.
3. Grant the service principal enough access for:
   - pushing images to ACR;
   - updating App Service container configuration.
4. Ensure App Service can pull from ACR.
5. The production container listens on port `8080`; the deployment workflow sets `WEBSITES_PORT=8080` on the App Service.

## GitHub Configuration

Set repository secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Set repository variables:

- `ACR_NAME`
- `ACR_LOGIN_SERVER` (example: `myregistry.azurecr.io`)
- `APP_SERVICE_NAME`
- `RESOURCE_GROUP`

Do not commit secrets to source control.

## Workflow

The workflow file `.github/workflows/deploy-appservice-container.yml` runs on pushes to `main` (and manual dispatch):

1. Logs in to Azure using OIDC.
2. Builds a production image using `Dockerfile`.
3. Pushes the image to ACR.
4. Updates App Service to use the new image.
5. Configures the App Service container port.
6. Restarts App Service.
