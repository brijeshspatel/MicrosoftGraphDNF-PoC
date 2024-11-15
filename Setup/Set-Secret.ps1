param (
    [string]$SecretKey,
    [string]$SecretValue
)

# Check if the environment variable already exists
if ([System.Environment]::GetEnvironmentVariable($SecretKey, [System.EnvironmentVariableTarget]::User)) {
    Write-Host "Updating existing environment variable..."
} else {
    Write-Host "Creating new environment variable..."
}

# Set the environment variable
[System.Environment]::SetEnvironmentVariable($SecretKey, $SecretValue, [System.EnvironmentVariableTarget]::User)

Write-Host "Environment variable '$SecretKey' has been set to '$SecretValue'."
