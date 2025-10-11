# GitHub Actions Setup Guide

This guide will help you configure GitHub Actions for building and publishing your NuGet package.

## Prerequisites

1. A GitHub repository for your project
2. A NuGet.org account
3. Admin access to your GitHub repository

## Step 1: Update Repository URLs

In `src/CodePunk.Highlight/CodePunk.Highlight.csproj`, update these lines with your actual GitHub username/organization:

```xml
<PackageProjectUrl>https://github.com/YOUR_USERNAME/CodePunk.Highlight</PackageProjectUrl>
<RepositoryUrl>https://github.com/YOUR_USERNAME/CodePunk.Highlight</RepositoryUrl>
```

Replace `YOUR_USERNAME` with your actual GitHub username or organization name.

## Step 2: Create a NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org/)
2. Sign in to your account
3. Click on your username → **API Keys**
4. Click **Create** to generate a new API key
5. Configure the key:
   - **Key Name**: `GitHub Actions - CodePunk.Highlight`
   - **Select Scopes**: Check `Push` and `Push new packages and package versions`
   - **Select Packages**: Choose `All Packages` or select specific packages
   - **Glob Pattern**: `CodePunk.Highlight` (or `*` for all)
   - **Expiration**: Choose an appropriate expiration time (e.g., 365 days)
6. Click **Create**
7. **Copy the API key immediately** (you won't be able to see it again!)

## Step 3: Add the API Key to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Configure the secret:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste the API key you copied from NuGet.org
5. Click **Add secret**

## Step 4: Enable GitHub Actions

1. Go to your repository's **Actions** tab
2. If prompted, click **I understand my workflows, go ahead and enable them**
3. You should now see the workflows:
   - **Build and Test**: Runs on every push and pull request
   - **Publish to NuGet**: Runs when you create a release

## Usage

### Automatic Build on Every Push

The **Build and Test** workflow runs automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Manual trigger via the Actions tab

### Publishing to NuGet

You have two options for publishing:

#### Option 1: Automatic Release (Recommended)

1. Update the version in `src/CodePunk.Highlight/CodePunk.Highlight.csproj`:
   ```xml
   <Version>1.0.0</Version>
   ```

2. Commit and push your changes

3. Create a new release on GitHub:
   - Go to **Releases** → **Create a new release**
   - Click **Choose a tag** and create a new tag (e.g., `v1.0.0`)
   - Set the release title (e.g., `v1.0.0`)
   - Add release notes describing changes
   - Click **Publish release**

4. The **Publish to NuGet** workflow will automatically run and publish your package

#### Option 2: Manual Publish

1. Go to **Actions** → **Publish to NuGet**
2. Click **Run workflow**
3. Enter the version number (e.g., `1.0.0`)
4. Click **Run workflow**

## Monitoring Workflows

1. Go to the **Actions** tab in your repository
2. Click on any workflow run to see details
3. Click on individual jobs to see logs
4. Green checkmark ✓ = success, Red X ✗ = failure

## Troubleshooting

### Build Fails

- Check the workflow logs in the Actions tab
- Ensure all dependencies are properly restored
- Verify that tests are passing locally: `dotnet test`

### NuGet Push Fails

- Verify the `NUGET_API_KEY` secret is set correctly
- Check if the package version already exists on NuGet.org
- Ensure the API key has the correct permissions
- Verify the API key hasn't expired

### Version Conflicts

If you get a version conflict error:
1. Update the version number in the `.csproj` file
2. Create a new release with the new version number
3. The `--skip-duplicate` flag prevents errors if the version already exists

## Best Practices

1. **Semantic Versioning**: Use semantic versioning (MAJOR.MINOR.PATCH)
   - MAJOR: Breaking changes
   - MINOR: New features (backward compatible)
   - PATCH: Bug fixes

2. **Release Notes**: Always include detailed release notes explaining changes

3. **Testing**: Ensure all tests pass before creating a release

4. **Version Tagging**: Use `v` prefix for version tags (e.g., `v1.0.0`)

5. **Pre-release Versions**: For beta releases, use suffixes like `1.0.0-beta.1`

## Advanced Configuration

### Adding More Triggers

Edit `.github/workflows/build.yml` to add more branches:

```yaml
on:
  push:
    branches: [ main, develop, feature/* ]
  pull_request:
    branches: [ main, develop ]
```

### Publishing Pre-release Versions

To publish pre-release versions to NuGet, update the version in the manual workflow:

```bash
dotnet pack -p:PackageVersion=1.0.0-beta.1
```

### Multiple Package Feeds

To publish to multiple feeds (e.g., GitHub Packages + NuGet.org), add additional steps:

```yaml
- name: Push to GitHub Packages
  run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.GITHUB_TOKEN }} --source https://nuget.pkg.github.com/YOUR_USERNAME/index.json --skip-duplicate
```

## Security Notes

- Never commit API keys or secrets to your repository
- Use GitHub Secrets for all sensitive data
- Regularly rotate your NuGet API keys
- Use minimal permissions for API keys (only what's needed)
- Set expiration dates on API keys

## Support

If you encounter issues:
1. Check the GitHub Actions logs
2. Review the NuGet.org package validation errors
3. Consult the [GitHub Actions documentation](https://docs.github.com/en/actions)
4. Check [NuGet documentation](https://docs.microsoft.com/en-us/nuget/)
