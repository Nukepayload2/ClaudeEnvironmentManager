# Claude Environment Manager

## Overview
The Claude Environment Manager is a graphical user interface (GUI) application for configuring and launching Claude environments with specific settings.

## Features
- Manage API keys for different services
- Configure custom base URLs for API endpoints
- Select and specify models to use
- Choose working directories for the environment
- Preview configured environment variables
- Launch Claude environments with configured settings

## Usage
1. **API Key Management**: Enter or select API keys for authentication
   - Environment variables with names ending in `_API_KEY` (excluding `ANTHROPIC_API_KEY`) will be loaded into the API Key combo box
2. **Base URL Configuration**: Set custom base URLs for API endpoints
   - Environment variables with names ending in `_BASE_URL` (excluding `ANTHROPIC_BASE_URL`) will be loaded into the Base URL combo box
3. **Model Selection**: Choose or input models for your operations
4. **Folder Selection**: Select working directories using the browse button
5. **Environment Preview**: View environment variables before launching
6. **Launch**: Start the Claude environment with your configurations

## Runtime Environment
- Requires .NET 8.0 or later
- Windows, Linux, and macOS support

## Getting Started
1. Clone the repository
2. Build the solution with Visual Studio or .NET CLI (`dotnet build`)
3. Run the application and configure your settings
4. Launch a Claude environment

## License
This project is licensed under the terms of the LICENSE.txt file in this repository.
