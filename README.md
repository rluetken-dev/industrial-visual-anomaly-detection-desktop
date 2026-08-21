# Industrial Visual Anomaly Detection Desktop

[![CI](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-desktop/actions/workflows/ci.yml/badge.svg)](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-desktop/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/rluetken-dev/industrial-visual-anomaly-detection-desktop)](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-desktop/releases/latest)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0C54C2)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4)](https://www.microsoft.com/windows)

Windows desktop client for the Industrial Visual Anomaly Detection system, built with C#, .NET 10, WPF, and MVVM.

The application provides an operator-facing workflow for discovering available inference models, selecting a model and an industrial image, submitting the image to the ASP.NET Core backend, inspecting the returned anomaly-detection result, and reviewing an interactive anomaly-heatmap overlay.

## Current Status

The selectable multi-model analysis workflow, including interactive heatmap visualization, is implemented and verified locally.

Available capabilities include:

- automatic and manual backend health checks;
- separate backend-liveness and inference-readiness indicators;
- runtime model discovery through the backend model catalog;
- default-model selection and explicit operator model selection;
- local PNG and JPEG file selection;
- image preview without retaining an unnecessary source-file lock;
- multipart image analysis with the selected model identifier;
- cancellation of an active analysis request;
- busy state and duplicate-submission protection;
- normal and anomalous decision presentation;
- anomaly score and threshold display;
- model identifier, category, processing time, and trace identifier display;
- validated PNG heatmap response mapping;
- Base64 heatmap decoding into an immutable WPF image;
- aligned source-image and heatmap overlay;
- heatmap visibility control and adjustable opacity;
- user-facing timeout, connectivity, validation, and service-failure states;
- validated backend configuration;
- centralized WPF styling;
- 60 passing automated tests.

The Release solution builds successfully. Capsule, Bottle, Candle, and Cashew models have been selected and verified through the native WPF workflow. The complete request path includes the desktop client, ASP.NET Core backend, Python inference service, model registry, and selected exported model artifact.

The returned `320 x 320` PNG heatmaps were also verified in the desktop overlay. These checks demonstrate application integration and routing, not independent model-quality benchmarks.

## Screenshots

### Normal analysis with heatmap overlay

![Normal capsule analysis with heatmap overlay](docs/screenshots/analysis-normal.png)

### Anomalous analysis with heatmap overlay

![Anomalous capsule analysis with heatmap overlay](docs/screenshots/analysis-anomalous.png)

## Application Workflow

1. Start the Python inference service with a model registry.
2. Start the ASP.NET Core backend.
3. Start the WPF desktop application.
4. Confirm that backend liveness and inference readiness are green.
5. Confirm that the model catalog has loaded.
6. Select an inference model.
7. Select a local PNG or JPEG image that belongs to the selected model category.
8. Inspect the local image preview.
9. Start the analysis.
10. Inspect the backend decision and supporting result values.
11. Review the aligned anomaly-heatmap overlay.
12. Toggle the heatmap or adjust its opacity when comparing it with the source image.

The application performs an initial status and model-catalog refresh after startup. Manual **Refresh status** and **Refresh models** actions are also available.

Model selection and image selection are independent. The operator remains responsible for choosing a model that matches the image category.

## System Context

```text
WPF desktop application
        |
        | HTTPS
        | GET model catalog
        | POST image and selected model ID
        v
ASP.NET Core backend
        |
        | HTTP
        | Forwards model selection
        v
Python inference service
        |
        | Resolves the selected registry entry
        v
Exported anomaly-detection model artifact
```

The desktop application communicates only with the ASP.NET Core backend. It does not invoke Python directly, read the model registry, load model artifacts, or access raw model tensors.

## Technology Stack

- C# and .NET 10;
- WPF;
- MVVM with `CommunityToolkit.Mvvm`;
- .NET Generic Host;
- dependency injection and validated options;
- typed clients through `IHttpClientFactory`;
- `System.Text.Json`;
- xUnit.

## Prerequisites

- Windows;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- Visual Studio with the **.NET desktop development** workload, or another environment capable of building WPF projects;
- Git;
- a running compatible Python inference service;
- a running compatible ASP.NET Core backend;
- a model registry referencing at least one enabled exported artifact.

The verified multi-model workflow is compatible with inference service `v0.6.0` and backend `v0.3.0`.

## Related Repositories

- [Industrial Visual Anomaly Detection Model](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-model)
- [Industrial Visual Anomaly Detection Backend](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-backend)
- [Industrial Visual Anomaly Detection Stack](https://github.com/rluetken-dev/industrial-visual-anomaly-detection-stack)

Clone the repositories into separate directories. The desktop repository does not contain datasets, model artifacts, the model registry, or the backend runtime.

## Repository Structure

```text
industrial-visual-anomaly-detection-desktop/
|-- src/
|   `-- IndustrialVisualAnomalyDetection.Desktop/
|       |-- Configuration/
|       |-- Models/
|       |   `-- Inference/
|       |-- Resources/
|       |   `-- Styles/
|       |-- Services/
|       |   `-- Backend/
|       |-- ViewModels/
|       |-- Views/
|       |-- App.xaml
|       |-- App.xaml.cs
|       `-- appsettings.json
|-- tests/
|   `-- IndustrialVisualAnomalyDetection.Desktop.Tests/
|-- docs/
|   |-- screenshots/
|   |   |-- analysis-anomalous.png
|   |   `-- analysis-normal.png
|   |-- ApiIntegration.md
|   |-- ArchitectureOverview.md
|   |-- DevelopmentStatus.md
|   `-- ProjectSpecification.md
|-- .github/
|   `-- workflows/
|       `-- ci.yml
|-- .editorconfig
|-- .gitattributes
|-- .gitignore
|-- COMMITS.md
|-- README.md
`-- IndustrialVisualAnomalyDetection.Desktop.slnx
```

## Restore and Build

From the desktop repository root:

```powershell
dotnet restore .\IndustrialVisualAnomalyDetection.Desktop.slnx

dotnet build .\IndustrialVisualAnomalyDetection.Desktop.slnx
```

For the verified Release build:

```powershell
dotnet build .\IndustrialVisualAnomalyDetection.Desktop.slnx `
    --configuration Release
```

## Run the Complete Local Stack

The easiest reproducible path is the related Docker Compose stack. For native development, run the inference service, backend, and desktop application as separate processes.

### 1. Start the Python inference service

From the model repository root, configure the exported registry and start Uvicorn:

```powershell
$env:IVAD_MODEL_REGISTRY = "$PWD\outputs\model-artifacts\models.json"
$env:IVAD_MEMORY_CHUNK_SIZE = "4096"

.\.venv\Scripts\python.exe -m uvicorn `
    industrial_visual_anomaly_detection.service.app:app `
    --host 127.0.0.1 `
    --port 8000
```

Verify liveness and the model catalog in another terminal:

```powershell
Invoke-RestMethod http://127.0.0.1:8000/health/live

Invoke-RestMethod http://127.0.0.1:8000/api/v1/models |
    ConvertTo-Json -Depth 5
```

### 2. Start the ASP.NET Core backend

From the backend repository root, run the API project:

```powershell
dotnet run `
    --project .\src\IndustrialVisualAnomalyDetection.Api\IndustrialVisualAnomalyDetection.Api.csproj
```

For first-time local HTTPS setup, trust the development certificate:

```powershell
dotnet dev-certs https --trust
```

Verify backend readiness and the forwarded model catalog:

```powershell
Invoke-RestMethod https://localhost:7056/health/ready

Invoke-RestMethod https://localhost:7056/api/v1/models |
    ConvertTo-Json -Depth 5
```

### 3. Start the WPF desktop application

From the desktop repository root, run the application from Visual Studio or execute:

```powershell
dotnet run `
    --project .\src\IndustrialVisualAnomalyDetection.Desktop\IndustrialVisualAnomalyDetection.Desktop.csproj
```

The application checks the backend and loads the model catalog automatically after startup. Both status indicators should become green, and the model selector should list the enabled registry entries.

## Configuration

Desktop backend settings are stored in:

`src/IndustrialVisualAnomalyDetection.Desktop/appsettings.json`

The default local configuration uses:

```json
{
  "Backend": {
    "BaseAddress": "https://localhost:7056",
    "TimeoutSeconds": 30
  }
}
```

The base address must be an absolute HTTP or HTTPS URI, and the timeout must be greater than zero. Configuration is validated during application startup.

Model identities are discovered at runtime and are not duplicated in desktop configuration. Do not commit private hostnames, credentials, personal paths, or machine-specific local overrides.

## Test

Run the verified Release test sequence from the desktop repository root:

```powershell
dotnet build .\IndustrialVisualAnomalyDetection.Desktop.slnx `
    --configuration Release

dotnet test .\IndustrialVisualAnomalyDetection.Desktop.slnx `
    --configuration Release `
    --no-build
```

The current suite contains 60 tests covering:

- backend configuration validation;
- backend liveness and readiness mapping;
- model-catalog request and response mapping;
- model and catalog invariants;
- default and explicit model selection;
- HTTP timeout and unavailable states;
- multipart image and model-identifier requests;
- normal and anomalous response mapping;
- required heatmap-contract mapping;
- heatmap model invariants and Base64 validation;
- PNG decoding into immutable WPF image sources;
- invalid response handling;
- view-model health, catalog, analysis, and heatmap state transitions;
- command availability and cancellation.

## Backend Integration

The desktop client consumes:

- `GET /health/live`;
- `GET /health/ready`;
- `GET /api/v1/models`;
- `POST /api/v1/analyses`.

The model-catalog response supplies the default model and all available model descriptors. The desktop selects the declared default after loading the catalog and allows the operator to choose another entry.

Image analysis uses multipart form data with the fields `image` and `modelId`. The backend response supplies the authoritative decision together with score, threshold, model information, processing time, trace identifier, and a required `image/png` heatmap encoded as Base64.

The desktop client validates the catalog and analysis responses, decodes the PNG into an immutable WPF image, and overlays it on the source preview. Heatmap visibility and opacity are presentation-only controls and do not change the backend decision or anomaly score.

See [API Integration](docs/ApiIntegration.md) for the complete consumed contract and verified behavior.

## Verified Multi-Model Workflow

The native desktop workflow has been verified with these registry identities:

```text
mvtec-ad-capsule-320
mvtec-ad-bottle-generalized-320
visa-candle-generalized-q95-320
visa-cashew-generalized-q95-320
```

The checks covered catalog loading, default selection, manual selection, explicit request routing, returned model identity, decision data, and heatmap presentation.

Representative Capsule checks produced:

| Image | Score | Threshold | Decision | Heatmap |
| --- | ---: | ---: | --- | --- |
| Capsule `test/good/000.png` | `1.848755` | `2.501822` | Normal | `320 x 320` PNG overlay |
| Capsule `test/poke/000.png` | `4.992109` | `2.501822` | Anomalous | `320 x 320` PNG overlay |

These examples verify desktop integration, model routing, heatmap transport, decoding, alignment, and presentation. They are not new model benchmarks. A heatmap visualizes relative patch responses and must not be interpreted as a certified defect-segmentation mask.

## Deferred Scope

The following capabilities remain intentionally deferred:

- automatic validation that an image belongs to the selected model category;
- certified defect segmentation or pixel-accurate masks;
- generalized overlay alignment for preprocessing pipelines that crop or change aspect ratio;
- analysis history and persistence;
- batch image processing;
- camera integration;
- drag-and-drop upload;
- authentication;
- installer packaging and automatic updates.

The desktop client will continue to consume model discovery and visualization output through the backend contract. It will not access model tensors, registry files, or artifacts directly.

## Documentation

- [Project Specification](docs/ProjectSpecification.md)
- [Architecture Overview](docs/ArchitectureOverview.md)
- [Development Status](docs/DevelopmentStatus.md)
- [API Integration](docs/ApiIntegration.md)
- [Commit Message Guidelines](COMMITS.md)

Documentation is updated after verified milestones or meaningful groups of changes rather than after every small internal edit.

## Data and Artifacts

This repository must not contain:

- MVTec or VisA datasets;
- raw uploaded industrial images;
- model artifacts or model registries;
- standalone generated heatmaps or raw analysis output;
- local logs;
- machine-specific configuration;
- credentials or secrets.

Documented application screenshots under `docs/screenshots` are the intentional exception for portfolio presentation. They show the user interface and verified workflow rather than distributing source datasets or reusable model artifacts.

## License

No license has been selected yet. Until a license is added, normal copyright restrictions apply.
