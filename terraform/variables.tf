variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
  default     = "flight-tracker-rg"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "westeurope" # Germany would be "germanywestcentral" or use West Europe
}

variable "app_name" {
  description = "Base name for the application (used for resource naming)"
  type        = string
  default     = "flighttracker"
}

variable "app_service_sku" {
  description = "App Service Plan SKU (B1 for free tier, F1 for free basic)"
  type        = string
  default     = "B1" # Use "B1" for free tier eligibility (first 12 months)
  
  validation {
    condition     = contains(["F1", "B1", "B2", "B3", "S1", "S2", "S3", "P1v2", "P2v2", "P3v2"], var.app_service_sku)
    error_message = "Invalid SKU. Choose from F1, B1, B2, B3, S1, S2, S3, P1v2, P2v2, P3v2"
  }
}

variable "acr_sku" {
  description = "Azure Container Registry SKU"
  type        = string
  default     = "Basic"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.acr_sku)
    error_message = "ACR SKU must be Basic, Standard, or Premium"
  }
}

variable "docker_image_name" {
  description = "Docker image name"
  type        = string
  default     = "flight-tracker"
}

variable "docker_image_tag" {
  description = "Docker image tag"
  type        = string
  default     = "latest"
}

variable "environment" {
  description = "Environment name (Development, Staging, Production)"
  type        = string
  default     = "Production"
  
  validation {
    condition     = contains(["Development", "Staging", "Production"], var.environment)
    error_message = "Environment must be Development, Staging, or Production"
  }
}

variable "flight_tracker_origin" {
  description = "Origin airport code (e.g., FRA for Frankfurt)"
  type        = string
  default     = "FRA"
}

# NOTE: Secrets are NOT managed by Terraform
# These are set via GitHub Actions from GitHub Secrets:
# - Sentry__Dsn
# - FlightProvider__Type  
# - FlightProvider__ApiKey
# - FlightProvider__ApiHost

variable "custom_domain" {
  description = "Custom domain for the app (optional)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "FlightTracker"
    Owner       = "Felipe"
  }
}
