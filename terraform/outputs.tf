output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.flight_tracker.name
}

output "app_service_name" {
  description = "Name of the App Service"
  value       = azurerm_linux_web_app.flight_tracker.name
}

output "app_service_url" {
  description = "Default URL of the App Service"
  value       = "https://${azurerm_linux_web_app.flight_tracker.default_hostname}"
}

output "container_registry_name" {
  description = "Name of the Azure Container Registry"
  value       = azurerm_container_registry.flight_tracker.name
}

output "container_registry_login_server" {
  description = "Login server for the Azure Container Registry"
  value       = azurerm_container_registry.flight_tracker.login_server
}

output "container_registry_admin_username" {
  description = "Admin username for ACR (use for Docker login)"
  value       = azurerm_container_registry.flight_tracker.admin_username
  sensitive   = true
}

output "container_registry_admin_password" {
  description = "Admin password for ACR (use for Docker login)"
  value       = azurerm_container_registry.flight_tracker.admin_password
  sensitive   = true
}

output "application_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.flight_tracker.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.flight_tracker.connection_string
  sensitive   = true
}

output "app_service_identity_principal_id" {
  description = "Principal ID of the App Service managed identity"
  value       = azurerm_linux_web_app.flight_tracker.identity[0].principal_id
}

output "storage_account_name" {
  description = "Name of the Storage Account for Table Storage"
  value       = azurerm_storage_account.flight_tracker.name
}

output "storage_account_connection_string" {
  description = "Connection string for Table Storage"
  value       = azurerm_storage_account.flight_tracker.primary_connection_string
  sensitive   = true
}

output "app_service_identity_tenant_id" {
  description = "Tenant ID of the App Service managed identity"
  value       = azurerm_linux_web_app.flight_tracker.identity[0].tenant_id
}

# Output deployment instructions
output "deployment_instructions" {
  description = "Instructions for deploying the app"
  value       = <<-EOT
    
    ✅ Infrastructure deployed successfully!
    
    📦 Push Docker image to ACR:
    
      # Login to ACR
      docker login ${azurerm_container_registry.flight_tracker.login_server}
      
      # Tag your image
      docker tag flight-tracker:latest ${azurerm_container_registry.flight_tracker.login_server}/flight-tracker:latest
      
      # Push to ACR
      docker push ${azurerm_container_registry.flight_tracker.login_server}/flight-tracker:latest
    
    🌐 Access your app:
      https://${azurerm_linux_web_app.flight_tracker.default_hostname}
    
    📊 Monitor with Application Insights:
      https://portal.azure.com/#resource${azurerm_application_insights.flight_tracker.id}
    
    🔧 Manage App Service:
      https://portal.azure.com/#resource${azurerm_linux_web_app.flight_tracker.id}
    
    🐳 View Container Registry:
      https://portal.azure.com/#resource${azurerm_container_registry.flight_tracker.id}
    
  EOT
}
