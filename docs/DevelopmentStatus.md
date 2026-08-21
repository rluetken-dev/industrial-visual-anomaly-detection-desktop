# Industrial Visual Anomaly Detection Desktop - Development Status

## Purpose

This document records verified implementation progress and the immediate next steps for the Windows desktop client.

It is intentionally concise. Stable scope belongs in `ProjectSpecification.md`, architecture in `ArchitectureOverview.md`, and backend HTTP details in `ApiIntegration.md`.

## Current Phase

**Phase 5 - Selectable multi-model desktop workflow**

The current objective is to document, commit, publish, and release the locally verified model-discovery and model-selection workflow built on top of the existing image-analysis and heatmap client.

## Verified Environment

- development environment: Windows and Visual Studio;
- .NET SDK: .NET 10;
- application framework: WPF;
- presentation pattern: MVVM with `CommunityToolkit.Mvvm`;
- hosting and dependency injection: .NET Generic Host;
- test framework: xUnit;
- repository location is separate from the model, backend, and stack repositories;
- the complete solution builds successfully in Release configuration;
- all 60 automated tests pass;
- the WPF application starts and performs automatic backend-health and model-catalog refreshes;
- the full desktop-to-backend-to-Python multi-model workflow has been verified locally;
- required Base64 PNG heatmaps are transported, decoded, aligned, and displayed successfully;
- inference service `v0.6.0` and backend `v0.3.0` provide the compatible published service contracts.

The desktop multi-model changes have not yet completed their own commit, push, CI, and release sequence. Local verification must not be confused with a published desktop release.

## Implemented

### Repository foundation

- Git repository initialized with the `main` branch;
- `IndustrialVisualAnomalyDetection.Desktop.slnx` created at the repository root;
- WPF application project created under `src`;
- xUnit test project created under `tests`;
- repository hygiene configured through `.gitignore`, `.gitattributes`, and `.editorconfig`;
- README, specification, architecture, status, integration, and commit-guideline documents created;
- initial feature baseline committed and pushed to GitHub;
- GitHub Actions CI configured for Windows, .NET 10, Release build, and automated tests;
- CI, release, framework, UI, and platform badges added to the README;
- repository made public;
- initial desktop release `v0.1.0` published;
- normal and anomalous screenshots stored under `docs/screenshots` and referenced from the README.

### Application composition

- .NET Generic Host startup and shutdown implemented;
- dependency injection configured in `App.xaml.cs`;
- strongly typed backend configuration bound from `appsettings.json`;
- backend base address and timeout validation implemented;
- typed HTTP clients registered for health, model-catalog, and image-analysis services;
- file-picker, image-preview, and heatmap-image services registered behind interfaces;
- main window and view model resolved through dependency injection.

### Model discovery and selection

- `InferenceModel` introduced for model ID, display name, category, input size, and default state;
- `InferenceModelCatalog` introduced for the available model collection and authoritative default ID;
- model identities and catalog invariants validated before presentation;
- `IInferenceModelCatalogService` introduced as the application-facing catalog boundary;
- `BackendInferenceModelCatalogService` implemented for `GET /api/v1/models`;
- backend catalog responses mapped through private transport types into application models;
- available models exposed by `MainWindowViewModel` as an observable collection;
- the backend-declared default selected after a successful catalog refresh;
- manual model selection implemented in `MainWindow`;
- explicit **Refresh models** command implemented;
- analysis disabled when no model is selected;
- selected stable model ID passed to the image-analysis service;
- model display text remains separate from the routing identifier;
- failed catalog loading clears stale model state and produces a user-facing status.

### User interface and MVVM workflow

- centralized industrial dark-theme resources implemented for colors, typography, layout, and controls;
- production main-window layout implemented;
- backend liveness and inference readiness indicators implemented with semantic colors;
- automatic health and model-catalog refresh implemented at startup;
- manual health and model-catalog refresh actions available;
- model selector displays the available models and readable display names;
- model-count status displayed;
- ComboBox text contrast corrected for readable selected values;
- PNG and JPEG image selection implemented;
- selected image path and file-lock-free preview displayed;
- analysis, busy, cancellation, and command-enabled states implemented;
- results display decision, score, threshold, returned model, category, processing time, and trace ID;
- source image and heatmap aligned in the same display area using `Uniform` scaling;
- preview area enlarged while preserving the full image and heatmap alignment;
- window height adjusted to make better use of the available preview space;
- heatmap visibility and opacity controls implemented;
- default heatmap opacity remains 40 percent;
- heatmap presentation controls do not alter the backend result.

### Backend integration

- `GET /health/live` client implemented;
- `GET /health/ready` client implemented;
- `GET /api/v1/models` client implemented;
- `POST /api/v1/analyses` multipart client implemented;
- selected `modelId` added to multipart analysis requests;
- successful catalog responses mapped into validated inference models;
- successful analysis responses mapped into validated result and heatmap models;
- required `image/png` heatmap metadata and Base64 payload validated;
- Base64 heatmaps decoded into immutable WPF image sources;
- unsupported local file types and incomplete backend responses rejected;
- connection, timeout, cancellation, file, catalog, and response failures produce user-facing status messages;
- the desktop communicates only with the backend and never directly with Python.

### Verification

- 60 automated tests cover configuration, health, catalog mapping, inference-model invariants, default and manual selection, multipart model forwarding, analysis mapping, heatmap validation and decoding, view-model state, command availability, and error paths;
- Release build verified without errors;
- automatic startup health and model-catalog refresh verified with the complete local stack;
- catalog display and manual model selection verified with four enabled registry entries;
- `mvtec-ad-capsule-320` verified through the native desktop workflow;
- `mvtec-ad-bottle-generalized-320` verified through the native desktop workflow;
- `visa-candle-generalized-q95-320` verified through the native desktop workflow;
- `visa-cashew-generalized-q95-320` verified through the native desktop workflow;
- selected model IDs and returned model identities verified in the UI;
- normal and anomalous decisions verified;
- `320 x 320` PNG heatmaps verified across the workflow;
- enlarged preview, full-image visibility, heatmap alignment, model-selector readability, visibility toggle, and opacity control verified interactively.

The model checks verify discovery, explicit routing, response identity, and visualization integration. They do not constitute new category-specific model-quality benchmarks.

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
|       |   |-- Analysis/
|       |   |-- Inference/
|       |   `-- Status/
|       |-- Resources/Styles/
|       |-- Services/
|       |   |-- Backend/
|       |   |-- Files/
|       |   `-- Images/
|       |-- ViewModels/
|       `-- Views/
|-- tests/
|   `-- IndustrialVisualAnomalyDetection.Desktop.Tests/
|       `-- Unit/
|-- docs/
|   |-- screenshots/
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

- automatic validation that a selected image belongs to the selected model category;
- structured operational logging beyond Generic Host defaults;
- detailed Problem Details parsing and presentation;
- dedicated packaging or installer workflow;
- certified defect segmentation or pixel-accurate masks;
- generalized heatmap alignment for preprocessing pipelines that crop or change aspect ratio;
- analysis history or persistence;
- batch analysis;
- camera integration;
- automated process orchestration for starting backend and inference services.

## Current Decisions

- The application targets Windows with WPF and .NET 10.
- The solution contains one production project and one test project.
- MVVM separates presentation state from views.
- `CommunityToolkit.Mvvm` provides observable state and commands.
- The .NET Generic Host provides configuration, logging, and dependency injection.
- The desktop communicates only with the ASP.NET Core backend.
- Model identities and the default model are discovered through the backend rather than duplicated in desktop configuration.
- The backend remains authoritative for model availability, image validation, and analysis results.
- The selected model's stable ID is explicitly sent with every current desktop analysis request.
- Model and image selection are independent; the operator must choose a compatible category.
- Backend liveness and inference readiness remain distinct states.
- The workflow processes one local image at a time.
- The heatmap is a required part of the current analysis response.
- Heatmap visibility and opacity are presentation-only settings.
- The heatmap represents relative patch responses and is not presented as a certified segmentation mask.
- The current overlay assumes that source and heatmap share the same spatial extent and aspect ratio.

## Immediate Next Steps

1. finish updating the remaining desktop documentation for selectable models;
2. run the complete Release build and 60-test verification sequence again;
3. run whitespace checks and inspect all tracked and untracked changes;
4. stage and review the exact desktop milestone;
5. commit and push the multi-model desktop changes;
6. confirm that GitHub Actions CI succeeds;
7. publish the next desktop release only after CI is green;
8. update the Docker Compose stack to published model, backend, and desktop milestone references where applicable.

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

2026-08-21
