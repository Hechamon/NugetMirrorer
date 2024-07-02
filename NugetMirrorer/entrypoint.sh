#!/bin/bash

# Update CA certificates
update-ca-certificates

# Start the application
exec dotnet /app/NugetMirrorer.dll "$@"