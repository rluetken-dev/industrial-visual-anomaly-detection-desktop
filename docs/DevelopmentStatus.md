# Industrial Visual Anomaly Detection Desktop - Development Status

## Purpose

This document records verified implementation progress and the immediate next steps for the Windows desktop client.

It is intentionally concise. Stable scope belongs in `ProjectSpecification.md`, architecture in `ArchitectureOverview.md`, and backend HTTP details in `ApiIntegration.md`.

## Current Phase

**Phase 4 - Heatmap visualization milestone**

The current objective is to document, commit, and release the verified interactive heatmap-overlay workflow built on top of the initial desktop baseline.

## Verified Environment

- development environment: Windows and Visual Studio;
- .NET SDK: .NET 10;
- application framework: WPF;
- presentation pattern: MVVM with `CommunityToolkit.Mvvm`;
- hosting and dependency injection: .NET Generic Host;
- test framework: xUnit;
- repository location is separate from the model and backend repositories;
- the complete solution builds successfully in Debug and Release configurations;
- all 49 automated tests pass;
- the WPF application starts and performs an automatic backend health check;
- the full desktop-to-backend-to-Python inference workflow has been verified locally;
- required Base64 PNG heatmaps are transported, decoded, and displayed successfully;
- the GitHub Actions Release build and test workflow completes successfully.

## Implemented

### Repository foundation

- Git repository initialized with the `main` branch;
- `IndustrialVisualAnomalyDetection.Desktop.slnx` created at the repository root;
- WPF application project created under `src`;
- xUnit test project created under `tests`;
- repository hygiene configured through `.gitignore`, `.gitattributes`, and `.editorconfig`;
- initial README, specification, architecture, status, integration, and commit-guideline documents created;
- initial feature baseline committed and pushed to the remote GitHub repository;
- GitHub Actions CI configured for Windows, .NET 10, Release build, and automated tests;
- CI, release, framework, UI, and platform badges added to the README;
- repository made public;
- initial desktop release `v0.1.0` published;
- normal and anomalous screenshots stored under `docs/screenshots` and referenced from the README;
- screenshots updated to show the verified heatmap overlay at 40 percent opacity.

### Application composition

- .NET Generic Host startup and shutdown implemented;
- dependency injection configured in `App.xaml.cs`;
- strongly typed backend configuration bound from `appsettings.json`;
- backend base address and timeout validation implemented;
- typed HTTP clients registered for health and image analysis;
- image preview and heatmap image loaders registered behind interfaces;
- main window and view model resolved through dependency injection.

### User interface and MVVM workflow

- centralized industrial dark-theme resources implemented for colors, typography, layout, and controls;
- production main-window layout implemented;
- backend liveness and inference readiness indicators implemented with semantic colors;
- automatic health refresh implemented at application startup;
- manual health refresh remains available;
- image file selection supports PNG and JPEG files;
- selected image path and preview are displayed;
- preview loading releases the source file after reading;
- analysis, busy, cancellation, and command-enabled states implemented;
- analysis results display decision, score, threshold, model, category, processing time, and trace identifier;
- normal decisions use success styling and anomalous decisions use danger styling;
- source image and heatmap are aligned in the same display area;
- heatmap visibility can be toggled;
- heatmap opacity can be adjusted from 0 to 100 percent;
- the default heatmap opacity is 40 percent;
- heatmap presentation controls do not alter the backend result.

### Backend integration

- `GET /health/live` client implemented;
- `GET /health/ready` client implemented;
- `POST /api/v1/analyses` multipart upload client implemented;
- successful backend responses are mapped into validated desktop result and heatmap models;
- the required `image/png` heatmap metadata and Base64 payload are validated;
- Base64 heatmap data is decoded into an immutable WPF image source;
- unsupported local file types and incomplete backend responses are rejected;
- connection, timeout, cancellation, file, and response failures produce user-facing status messages;
- the desktop communicates only with the ASP.NET Core backend and never directly with the Python service.

### Verification

- 49 automated tests cover configuration, health communication, multipart analysis requests, response mapping, constructor guards, heatmap validation and decoding, view-model state, image selection, preview handling, command availability, and error paths;
- Release build verified without errors;
- normal Capsule image verified with score `1.848755` below threshold `2.501822`;
- anomalous Capsule poke image verified with score `4.992109` above threshold `2.501822`;
- model identifier `mvtec-ad-capsule-320`, category `capsule`, processing time, and trace identifier verified in the UI;
- automatic startup health refresh and green ready indicators verified with the complete local stack;
- `320 x 320` PNG heatmaps verified for normal and anomalous images;
- overlay alignment verified at 0, 40, and 100 percent opacity;
- heatmap visibility and opacity controls verified interactively;
- updated normal and anomalous portfolio screenshots verified.

## Current Repository Shape

```text
industrial-visual-anomaly-detection-desktop/
|-- .github/
|   `-- workflows/
|       `-- ci.yml
|-- src/
|   `-- IndustrialVisualAnomalyDetection.Desktop/
|       |-- Configuration/
|       |-- Models/
|       |-- Resources/Styles/
|       |-- Services/
|       |-- ViewModels/
|       `-- Views/
|-- tests/
|   `-- IndustrialVisualAnomalyDetection.Desktop.Tests/
|       `-- Unit/
|-- docs/
|   |-- screenshots/
|   |   |-- analysis-anomalous.png
|   |   `-- analysis-normal.png
|   |-- ApiIntegration.md
|   |-- ArchitectureOverview.md
|   |-- DevelopmentStatus.md
|   `-- ProjectSpecification.md
|-- .editorconfig
|-- .gitattributes
|-- .gitignore
|-- COMMITS.md
|-- README.md
`-- IndustrialVisualAnomalyDetection.Desktop.slnx
```

## Not Yet Implemented

- structured operational logging beyond Generic Host defaults;
- detailed Problem Details parsing and presentation;
- dedicated packaging or installer workflow;
- certified defect segmentation or pixel-accurate masks;
- generalized heatmap alignment for preprocessing pipelines that crop images or change their aspect ratio;
- analysis history or persistence;
- batch analysis;
- camera integration;
- automated process orchestration for starting the backend and Python service.

## Current Decisions

- The application targets Windows with WPF and .NET 10.
- The solution contains one production project and one test project.
- MVVM separates presentation state from views.
- `CommunityToolkit.Mvvm` provides observable state and commands.
- The .NET Generic Host provides configuration, logging, and dependency injection.
- The desktop application communicates only with the ASP.NET Core backend.
- The backend remains authoritative for image validation and anomaly decisions.
- Backend liveness and inference readiness remain distinct states.
- The workflow processes one local image at a time.
- The heatmap is a required part of the current analysis response.
- Heatmap visibility and opacity are presentation-only settings.
- The heatmap represents relative patch responses and is not presented as a certified segmentation mask.
- The current overlay assumes that the source preview and returned heatmap share the same spatial extent and aspect ratio.

## Immediate Next Steps

1. update the remaining desktop documentation for the heatmap contract and presentation workflow;
2. run the complete Release build and 49-test verification sequence;
3. inspect the final staged changes and repository status;
4. commit and push the heatmap milestone;
5. publish the next desktop release after CI succeeds;
6. coordinate compatible model and backend releases for the completed full-stack heatmap contract.

## Verification Commands

Build the solution in Release configuration:

```powershell
dotnet build .\IndustrialVisualAnomalyDetection.Desktop.slnx `
    --configuration Release
```

Run all tests after a successful Release build:

```powershell
dotnet test .\IndustrialVisualAnomalyDetection.Desktop.slnx `
    --configuration Release `
    --no-build
```

List solution projects:

```powershell
dotnet sln .\IndustrialVisualAnomalyDetection.Desktop.slnx list
```

Check repository whitespace and status:

```powershell
git diff --check
git status --short --untracked-files=all
```

## Documentation Update Rule

Update this document after a verified milestone or meaningful group of changes. Do not update it for every small internal edit.

## Last Updated

2026-08-18
