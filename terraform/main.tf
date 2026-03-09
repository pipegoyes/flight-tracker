terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }

  # Optional: Configure backend for state storage
  # backend "azurerm" {
  #   resource_group_name  = "terraform-state-rg"
  #   storage_account_name = "tfstate${random_id.suffix.hex}"
  #   container_name       = "tfstate"
  #   key                  = "flight-tracker.tfstate"
  # }
}

provider "azurerm" {
  features {}
}

# Random suffix for unique resource names
resource "random_id" "suffix" {
  byte_length = 4
}

# Resource Group
resource "azurerm_resource_group" "flight_tracker" {
  name     = var.resource_group_name
  location = var.location

  tags = var.tags
}

# Application Insights for monitoring
resource "azurerm_application_insights" "flight_tracker" {
  name                = "${var.app_name}-insights-${random_id.suffix.hex}"
  location            = azurerm_resource_group.flight_tracker.location
  resource_group_name = azurerm_resource_group.flight_tracker.name
  application_type    = "web"

  tags = var.tags
}

# Azure Container Registry
resource "azurerm_container_registry" "flight_tracker" {
  name                = "${var.app_name}acr${random_id.suffix.hex}"
  resource_group_name = azurerm_resource_group.flight_tracker.name
  location            = azurerm_resource_group.flight_tracker.location
  sku                 = var.acr_sku
  admin_enabled       = true

  tags = var.tags
}

# Azure Storage Account for Table Storage (Production database)
resource "azurerm_storage_account" "flight_tracker" {
  name                     = "${var.app_name}data${random_id.suffix.hex}"
  resource_group_name      = azurerm_resource_group.flight_tracker.name
  location                 = azurerm_resource_group.flight_tracker.location
  account_tier             = "Standard"
  account_replication_type = "LRS"  # Locally redundant - cheapest option

  tags = var.tags
}

# Table Storage tables
resource "azurerm_storage_table" "destinations" {
  name                 = "Destinations"
  storage_account_name = azurerm_storage_account.flight_tracker.name
}

resource "azurerm_storage_table" "target_dates" {
  name                 = "TargetDates"
  storage_account_name = azurerm_storage_account.flight_tracker.name
}

resource "azurerm_storage_table" "price_checks" {
  name                 = "PriceChecks"
  storage_account_name = azurerm_storage_account.flight_tracker.name
}

resource "azurerm_storage_table" "target_date_destinations" {
  name                 = "TargetDateDestinations"
  storage_account_name = azurerm_storage_account.flight_tracker.name
}

# App Service Plan (B1 Basic for free tier eligibility)
resource "azurerm_service_plan" "flight_tracker" {
  name                = "${var.app_name}-plan-${random_id.suffix.hex}"
  location            = azurerm_resource_group.flight_tracker.location
  resource_group_name = azurerm_resource_group.flight_tracker.name
  os_type             = "Linux"
  sku_name            = var.app_service_sku

  tags = var.tags
}

# App Service
resource "azurerm_linux_web_app" "flight_tracker" {
  name                = "${var.app_name}-${random_id.suffix.hex}"
  location            = azurerm_resource_group.flight_tracker.location
  resource_group_name = azurerm_resource_group.flight_tracker.name
  service_plan_id     = azurerm_service_plan.flight_tracker.id

  # Enable HTTPS only
  https_only = true

  site_config {
    # Always on (keep app loaded) - available on Basic tier and above
    always_on = var.app_service_sku != "F1" && var.app_service_sku != "D1"

    # Enable WebSockets for SignalR
    websockets_enabled = true

    # Enable HTTP/2
    http2_enabled = true

    # Container configuration
    application_stack {
      docker_image_name   = "${azurerm_container_registry.flight_tracker.login_server}/${var.docker_image_name}:${var.docker_image_tag}"
      docker_registry_url = "https://${azurerm_container_registry.flight_tracker.login_server}"
    }

    # Health check endpoint
    health_check_path = "/health"

    # Minimum TLS version
    minimum_tls_version = "1.2"
  }

  # Application settings (environment variables)
  app_settings = {
    # Docker registry credentials
    DOCKER_REGISTRY_SERVER_URL      = "https://${azurerm_container_registry.flight_tracker.login_server}"
    DOCKER_REGISTRY_SERVER_USERNAME = azurerm_container_registry.flight_tracker.admin_username
    DOCKER_REGISTRY_SERVER_PASSWORD = azurerm_container_registry.flight_tracker.admin_password
    DOCKER_ENABLE_CI                = "true"

    # Application Insights
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.flight_tracker.connection_string
    ApplicationInsightsAgent_EXTENSION_VERSION = "~3"

    # Azure Table Storage connection string
    TableStorage__ConnectionString = azurerm_storage_account.flight_tracker.primary_connection_string

    # Sentry error tracking
    Sentry__Dsn = var.sentry_dsn

    # Flight Tracker configuration
    FlightTracker__Origin = var.flight_tracker_origin

    # Flight Provider configuration
    FlightProvider__Type          = var.flight_provider_type
    FlightProvider__ApiKey        = var.flight_provider_api_key
    FlightProvider__ApiSecret     = var.flight_provider_api_secret
    FlightProvider__ApiHost       = var.flight_provider_api_host
    FlightProvider__UseProduction = tostring(var.flight_provider_use_production)

    # ASP.NET Core environment
    ASPNETCORE_ENVIRONMENT = var.environment

    # Port configuration
    PORT = "8080"
    WEBSITES_PORT = "8080"

    # SignalR / Blazor Server
    WEBSITE_LOCAL_CACHE_OPTION = "Always"
    WEBSITE_DISABLE_ARR_AFFINITY = "false"
  }

  # Connection strings (if needed for future use)
  # connection_string {
  #   name  = "FlightTracker"
  #   type  = "SQLite"
  #   value = "Data Source=/app/data/flighttracker.db"
  # }

  # Logging configuration
  logs {
    application_logs {
      file_system_level = "Information"
    }

    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
  }

  # Identity for managed identity (future use)
  identity {
    type = "SystemAssigned"
  }

  # Authentication configuration (Easy Auth with Microsoft Account)
  # NOTE: Auth is configured via Azure CLI/Portal, not managed by Terraform yet
  # To avoid conflicts, auth_settings_v2 is managed outside Terraform
  # Configuration done via: az webapp auth update commands
  # TODO: Add proper auth_settings_v2 block with correct schema in future version

  tags = var.tags

  depends_on = [
    azurerm_container_registry.flight_tracker
  ]
}

# Custom domain and SSL (optional - uncomment when you have a domain)
# resource "azurerm_app_service_custom_hostname_binding" "flight_tracker" {
#   hostname            = var.custom_domain
#   app_service_name    = azurerm_linux_web_app.flight_tracker.name
#   resource_group_name = azurerm_resource_group.flight_tracker.name
# }

# resource "azurerm_app_service_managed_certificate" "flight_tracker" {
#   custom_hostname_binding_id = azurerm_app_service_custom_hostname_binding.flight_tracker.id
# }

# resource "azurerm_app_service_certificate_binding" "flight_tracker" {
#   hostname_binding_id = azurerm_app_service_custom_hostname_binding.flight_tracker.id
#   certificate_id      = azurerm_app_service_managed_certificate.flight_tracker.id
#   ssl_state           = "SniEnabled"
# }
