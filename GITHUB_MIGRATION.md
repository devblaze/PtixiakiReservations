# Gitea to GitHub Migration Guide

## Overview
This document outlines the migration from Gitea CI/CD pipelines to GitHub Actions for the PtixiakiReservations project.

## Changes Made

### 1. GitHub Actions Workflows Created

#### CI Pipeline (`.github/workflows/ci.yml`)
- **Build and Test**: Runs on every push and pull request
- **Code Analysis**: Runs on pull requests only
- **Features**:
  - .NET 9.0 support
  - NuGet package caching for faster builds
  - Test reporting with TRX format
  - Code coverage with Codecov integration
  - Security scanning
  - Code formatting checks

#### Docker Publish (`.github/workflows/docker-publish.yml`)
- **Multi-registry support**: Publishes to both GitHub Container Registry and Docker Hub
- **Multi-platform builds**: Supports linux/amd64 and linux/arm64
- **Features**:
  - Semantic versioning support
  - Image vulnerability scanning with Trivy
  - Docker layer caching
  - Automatic README sync to Docker Hub
  - Triggered on:
    - Push to main branch
    - Git tags (v*)
    - Release creation
    - Manual workflow dispatch

#### Deployment Pipeline (`.github/workflows/deploy.yml`)
- **Manual deployment**: Triggered via workflow dispatch
- **Environment support**: Staging and Production
- **Features**:
  - SSH-based deployment
  - Docker Compose orchestration
  - Health checks
  - Automatic rollback on failure
  - Smoke tests
  - Slack notifications

### 2. Dependency Management
- **Dependabot configuration** (`.github/dependabot.yml`)
  - Weekly updates for NuGet packages
  - Docker base image updates
  - GitHub Actions version updates
  - Grouped updates for related packages

### 3. Docker Configuration
- **Updated Dockerfile**: Migrated from .NET 8.0 to .NET 9.0

## Required GitHub Secrets

Before the workflows can run, you need to configure the following secrets in your GitHub repository settings:

### For Docker Publishing:
- `DOCKER_USERNAME`: Your Docker Hub username
- `DOCKER_PASSWORD`: Your Docker Hub password or access token
- `CODECOV_TOKEN`: (Optional) Token for Codecov integration

### For Deployment:
- `SSH_PRIVATE_KEY`: Private SSH key for server access
- `SERVER_HOST`: Target server hostname/IP
- `SERVER_USER`: SSH username for deployment
- `APP_URL`: Application URL for smoke tests
- `SLACK_WEBHOOK`: (Optional) Slack webhook URL for notifications

## Migration Steps

1. **Remove Gitea workflows**:
   ```bash
   rm -rf .gitea/
   ```

2. **Push to GitHub**:
   ```bash
   git add .github/
   git add Dockerfile
   git add GITHUB_MIGRATION.md
   git commit -m "Migrate CI/CD from Gitea to GitHub Actions"
   git push origin main
   ```

3. **Configure GitHub Secrets**:
   - Go to Settings → Secrets and variables → Actions
   - Add the required secrets listed above

4. **Configure Environments** (Optional):
   - Go to Settings → Environments
   - Create `staging` and `production` environments
   - Add environment-specific secrets and protection rules

5. **Enable GitHub Actions**:
   - Go to Actions tab in your repository
   - Enable Actions if not already enabled

## Workflow Usage

### Manual Deployment
To deploy manually:
1. Go to Actions → Deploy to Production
2. Click "Run workflow"
3. Select environment (staging/production)
4. Enter version tag (e.g., v1.0.0 or latest)
5. Click "Run workflow"

### Docker Images
Images are published to:
- GitHub Container Registry: `ghcr.io/{owner}/ptixiakireservations`
- Docker Hub: `{docker_username}/ptixiakireservations`

### Tags Format
- `latest`: Always points to the latest main branch build
- `main-{sha}`: Specific commit on main branch
- `v1.0.0`: Semantic version tags
- `develop`: Develop branch builds

## Benefits of GitHub Actions

1. **Native GitHub Integration**: Seamless integration with PRs, issues, and releases
2. **Matrix Builds**: Easy multi-platform and multi-version testing
3. **Marketplace**: Extensive ecosystem of pre-built actions
4. **Security**: Built-in secret scanning and vulnerability alerts
5. **Free Tier**: Generous free tier for public repositories
6. **Environments**: Built-in environment management with approval workflows
7. **Insights**: Detailed workflow analytics and visualization

## Monitoring

- **Actions Tab**: View all workflow runs and their status
- **Security Tab**: View vulnerability scan results from Trivy
- **Insights → Code frequency**: Track deployment frequency
- **Settings → Webhooks**: Configure additional notifications

## Troubleshooting

### Common Issues

1. **Permission Denied for Docker Push**:
   - Ensure DOCKER_USERNAME and DOCKER_PASSWORD are correctly set
   - For GHCR, ensure the repository has package write permissions

2. **SSH Deployment Fails**:
   - Verify SSH_PRIVATE_KEY matches the public key on the server
   - Check SERVER_HOST and SERVER_USER are correct
   - Ensure the deployment user has Docker permissions

3. **Build Failures**:
   - Check .NET SDK version compatibility
   - Verify all NuGet packages support .NET 9.0
   - Review test failures in the Actions log

## Rollback Instructions

If you need to revert to Gitea:
1. The Gitea configuration is preserved in git history
2. Restore `.gitea/workflows/ci.yml`
3. Remove `.github/` directory
4. Revert Dockerfile to .NET 8.0 if needed