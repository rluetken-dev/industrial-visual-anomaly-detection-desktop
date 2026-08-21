# Industrial Visual Anomaly Detection Desktop - Architecture Overview

## Purpose

This document describes the verified architecture of the Windows desktop client, its internal responsibilities, and its boundary to the ASP.NET Core backend.

Implementation progress belongs in `DevelopmentStatus.md`. Stable scope belongs in `ProjectSpecification.md`. HTTP details belong in `ApiIntegration.md`.

## Architectural Goal

The desktop application provides a maintainable WPF interface without duplicating backend validation, orchestration, model-inference, model-registry, or heatmap-generation responsibilities.

The design favors a small MVVM application with explicit boundaries over premature separation into multiple production assemblies.

## System Context

```text
User
  |
  v
WPF desktop client
  |
  | HTTPS
  | Retrieves model catalog
  | Submits image and selected model ID
  | Receives decision data and Base64 PNG heatmap
  v
ASP.NET Core backend
  |
  | HTTP
  | Forwards catalog and selected model
  v
Python inference service
  |
  | Resolves enabled registry entry
  v
Exported anomaly-detection model artifact
```

The desktop client depends only on the public backend contract. It does not communicate with Python, read the model registry, load artifacts, or generate heatmaps.

## Solution Structure

The solution contains one production project and one test project:

```text
industrial-visual-anomaly-detection-desktop/
|-- src/
|   `-- IndustrialVisualAnomalyDetection.Desktop/
|-- tests/
|   `-- IndustrialVisualAnomalyDetection.Desktop.Tests/
|-- docs/
`-- IndustrialVisualAnomalyDetection.Desktop.slnx
```

Additional class-library projects shall be introduced only when demonstrated complexity or reuse justifies them.

## Production Project Organization

The production project is organized by responsibility:

```text
IndustrialVisualAnomalyDetection.Desktop/
|-- Configuration/
|-- Models/
|   |-- Analysis/
|   |-- Inference/
|   `-- Status/
|-- Resources/
|   `-- Styles/
|-- Services/
|   |-- Backend/
|   |-- Files/
|   `-- Images/
|-- ViewModels/
|-- Views/
|-- App.xaml
|-- App.xaml.cs
`-- appsettings.json
```

Directories are added when their first real type is introduced. Empty placeholder directories are unnecessary.

## Architectural Responsibilities

### Views

Views own visual composition and purely visual behavior.

They may:

- define layout and controls;
- bind the available and selected models to a selector;
- bind to view-model state and commands;
- layer source and heatmap images using common layout bounds;
- bind heatmap visibility and opacity to view-model state;
- select styles and templates from shared resource dictionaries;
- provide narrowly scoped code-behind for inherently WPF-specific behavior.

Views shall not:

- perform HTTP requests;
- parse backend responses or decode Base64 heatmap data;
- contain anomaly-decision or model-routing logic;
- generate heatmaps;
- resolve dependencies manually;
- access Python, registry files, or model artifacts.

`MainWindow` receives its view model through constructor injection and assigns it as the data context. Its code-behind remains limited to WPF lifecycle behavior.

### View Models

View models expose presentation state and coordinate user interactions.

`MainWindowViewModel` is responsible for:

- command availability;
- busy and cancellation state;
- available-model and selected-model state;
- model-catalog refresh orchestration;
- declared default-model selection;
- selected-image and preview state;
- backend health and readiness orchestration;
- image-analysis orchestration with the selected model identifier;
- result and error presentation;
- heatmap image, visibility, and opacity state;
- translating application results into UI-friendly state.

The view model exposes models through `ObservableCollection<InferenceModel>` and enables analysis only when an image and model are selected and no analysis is running. It delegates catalog retrieval, analysis transport, preview loading, and heatmap decoding to service interfaces.

The selected model is captured before the asynchronous request starts. Its stable identifier is passed to `IImageAnalysisService`; the view model never derives model identities from display text.

The view model is tested without creating a WPF window or making real network requests.

### Services

Services implement non-visual application and infrastructure behavior.

Current service boundaries are:

- `IBackendHealthService` for backend liveness and readiness;
- `IInferenceModelCatalogService` for runtime model discovery and catalog mapping;
- `IImageAnalysisService` for multipart image-and-model requests and result mapping;
- `IImageFilePicker` for selecting supported local images;
- `IImagePreviewLoader` for loading file-lock-free source previews;
- `IHeatmapImageLoader` for decoding validated Base64 heatmap data into an immutable WPF image source.

The corresponding implementations isolate `HttpClient`, file dialogs, source-image decoding, heatmap decoding, JSON deserialization, and transport failure handling from the view model.

### Models

Models represent explicit application state, model discovery, and analysis results.

Current model areas include:

- `AnalysisDecision` for normal and anomalous decisions;
- `AnalysisHeatmap` for validated PNG heatmap metadata and Base64 data;
- `ImageAnalysisResult` for the complete mapped backend result;
- `InferenceModel` for an available model's stable ID, display name, category, input size, and default flag;
- `InferenceModelCatalog` for the available models and declared default model ID;
- `SystemAvailabilityStatus` for unknown, checking, available, and unavailable health states.

`InferenceModel` requires non-empty identity, display name, and category values plus a positive input size. `InferenceModelCatalog` requires:

- at least one model;
- unique model identifiers;
- exactly one entry marked as default;
- a `DefaultModelId` matching that default entry.

`AnalysisHeatmap` requires:

- the `image/png` content type;
- positive width and height;
- non-empty valid Base64 data.

Transport responses are mapped into application-facing models before they reach presentation state. The backend remains authoritative for model availability and analysis results.

### Configuration

Configuration owns strongly typed settings such as the backend base address and HTTP timeout.

`BackendOptions` is bound from the `Backend` section and validated during startup by `BackendOptionsValidator`. Invalid required configuration prevents the host from starting with an ambiguous runtime state.

Model identities are deliberately absent from desktop configuration. They are discovered through the backend at runtime, preventing a second static model list from drifting out of sync.

### Shared UI Resources

The visual system is centralized in resource dictionaries:

- `Colors.xaml` defines the application palette and semantic brushes;
- `Typography.xaml` defines reusable text styles;
- `Layout.xaml` defines panels, spacing, and structural styles;
- `Controls.xaml` defines buttons, status pills, progress, and other control styles.

`App.xaml` merges these dictionaries so views can consume consistent resources without duplicating styling decisions.

## Application Startup

The application uses the .NET Generic Host to provide:

- configuration loading;
- dependency injection;
- logging;
- typed `HttpClient` registration;
- options binding and validation;
- controlled service lifetime management.

`App.xaml.cs` is the composition root. It registers three typed backend clients:

- `IBackendHealthService` to `BackendHealthService`;
- `IInferenceModelCatalogService` to `BackendInferenceModelCatalogService`;
- `IImageAnalysisService` to `BackendImageAnalysisService`.

It also registers the file and image services, `MainWindowViewModel`, and `MainWindow`. During startup it creates and starts the host, resolves the main window and view model, displays the window, and begins the initial application refresh. During shutdown it stops and disposes the host.

Application services and view models do not use a global service locator.

## Dependency Direction

The dependency direction is:

```text
View
  |
  v
View model
  |
  v
Application-facing service interface
  ^
  |
HTTP, file or image infrastructure implementation
```

Concrete infrastructure depends on backend-contract details and platform services. The view model depends on interfaces rather than `HttpClient`, file dialogs, Base64 conversion, or WPF image-decoding implementations.

## Model Discovery Data Flow

The verified catalog flow is:

```text
Application startup or Refresh models
  -> MainWindowViewModel
  -> IInferenceModelCatalogService
  -> GET /api/v1/models
  -> private transport response types
  -> validated InferenceModelCatalog
  -> replace AvailableModels
  -> select the model matching DefaultModelId
  -> enable operator selection
```

Each stage has one responsibility:

- the backend is authoritative for enabled models and the default;
- the catalog service owns transport and response mapping;
- application models enforce catalog invariants;
- the view model owns the observable collection and current selection;
- the view presents display names without using them as routing identities.

On catalog failure the view model clears the available and selected model state. Analysis cannot start without a selected model.

## Analysis and Heatmap Data Flow

The verified analysis flow is:

```text
Selected local image and InferenceModel
  -> IImageAnalysisService
  -> multipart POST /api/v1/analyses
       |-- image
       `-- modelId
  -> backend JSON response
  -> validated ImageAnalysisResult
       |-- returned model identity and result metadata
       `-- validated AnalysisHeatmap
  -> IHeatmapImageLoader
  -> immutable WPF ImageSource
  -> MainWindowViewModel
  -> layered source and heatmap images in MainWindow
```

The returned model identity confirms which model actually handled the request. The client displays it and does not infer it from the selected display name.

## Backend Communication

Backend communication uses typed `HttpClient` registrations configured through `IHttpClientFactory`.

The client boundary covers:

- `GET /health/live`;
- `GET /health/ready`;
- `GET /api/v1/models`;
- `POST /api/v1/analyses`;
- multipart upload using `image` and `modelId`;
- JSON deserialization and application-model mapping;
- backend failure response handling;
- timeout, cancellation, connectivity, and invalid-response handling.

The base address and timeout are configuration values. Backend calls remain inside the service layer and are not duplicated in the view model or view.

## Source Image and Model Selection

The selected image remains a local client-side file until analysis starts. The file picker limits normal selection to PNG and JPEG files. The preview loader decodes the image without retaining an unnecessary file lock.

The selected inference model is an application model obtained from the current backend catalog. Image selection and model selection remain independent. The operator is responsible for choosing a model whose category matches the selected image.

These client-side controls support usability only. The backend remains authoritative for upload validation and the inference service remains authoritative for model resolution.

## Heatmap Handling

The backend response contains a required heatmap object with content type, dimensions, and Base64 data.

The processing sequence is:

1. `BackendImageAnalysisService` deserializes the response.
2. It creates `AnalysisHeatmap`, which validates metadata and Base64 syntax.
3. The heatmap becomes part of `ImageAnalysisResult`.
4. `MainWindowViewModel` passes it to `IHeatmapImageLoader`.
5. `HeatmapImageLoader` decodes the data through an in-memory stream.
6. The loader uses `BitmapCacheOption.OnLoad` and freezes the WPF image.
7. The view model exposes the image, visibility, and opacity.
8. `MainWindow` overlays the heatmap and source with the same bounds and `Uniform` scaling.

The default opacity is 40 percent. Users can hide the heatmap or adjust opacity from 0 to 100 percent. These controls never modify the backend result.

The current alignment is valid because source and heatmap represent the same spatial extent and aspect ratio. Pipelines that crop or change aspect ratio require transform metadata before generalized alignment can be claimed.

The heatmap represents relative patch responses, not certified pixel-accurate segmentation.

## Asynchronous Execution

Health, catalog, and analysis requests use asynchronous operations.

The analysis workflow:

- requires an image and selected model;
- exposes a busy state;
- prevents duplicate execution;
- propagates a cancellation token;
- supports an explicit cancel command;
- distinguishes user cancellation from operational failure;
- loads the heatmap only after a successful mapped result;
- restores command availability after completion or failure;
- avoids blocking the WPF dispatcher thread.

Catalog refresh replaces the observable model collection only after a valid catalog has been mapped. Model-selection changes notify the analysis command so its availability remains correct.

## Error Handling

Expected failures are presented as application state rather than unhandled exceptions.

Relevant categories include:

- invalid local file selection or preview loading;
- model-catalog transport or validation failure;
- missing model selection;
- backend validation failure;
- unsupported media type or oversized upload;
- unknown or unavailable inference model;
- backend or inference unavailability;
- HTTP timeout or connectivity failure;
- malformed or incompatible response;
- missing or invalid heatmap metadata;
- invalid Base64 or undecodable PNG heatmap data;
- user cancellation.

Problem Details information and backend trace identifiers are preserved where available. Raw stack traces, Base64 payloads, and sensitive details are not displayed to ordinary users.

## Health Representation

Liveness and readiness have different meanings:

- liveness indicates that the ASP.NET Core backend process responds;
- readiness indicates that the backend can reach the inference service and is ready to analyze images.

The UI represents these states independently. A live but not-ready backend is not presented as fully operational. Catalog availability is separate application state and is represented through the model selector and catalog-status text.

## Result Representation

The analysis result displays:

- normal or anomalous decision;
- anomaly score;
- decision threshold;
- returned model identifier;
- returned model category;
- backend processing time;
- trace identifier;
- aligned heatmap overlay;
- heatmap visibility and opacity controls.

The UI does not recalculate the anomaly decision because the backend supplies the authoritative result. Heatmap controls affect presentation only and do not trigger another request.

## Testing Strategy

The test project is organized by responsibility:

```text
IndustrialVisualAnomalyDetection.Desktop.Tests/
`-- Unit/
    |-- Configuration/
    |-- Models/
    |   |-- Analysis/
    |   `-- Inference/
    |-- Services/
    |   |-- Backend/
    |   `-- Images/
    `-- ViewModels/
```

The 60-test automated suite covers:

- configuration validation;
- backend health mapping;
- catalog request and response mapping;
- model and catalog invariants;
- default and explicit model selection;
- analysis requests with selected model identifiers;
- result and required heatmap mapping;
- Base64 PNG decoding into immutable WPF images;
- timeout, cancellation, and transport behavior;
- view-model catalog, analysis, and heatmap transitions;
- command availability;
- image selection and successful or failed workflows.

Controlled HTTP handlers and test doubles isolate automated tests from the real backend. Visual alignment, opacity, layout, and styling are verified through manual full-stack inspection rather than brittle UI automation.

## Security and Privacy

- The application does not contain backend credentials, model registries, or artifacts.
- Selected images and decoded heatmaps are not logged or persisted by default.
- Base64 heatmap payloads shall not be written to logs.
- Backend TLS validation is not disabled in production code.
- Server responses, including catalogs and heatmaps, are treated as untrusted input and validated during mapping and decoding.

## Observability

The Generic Host provides the logging foundation for meaningful lifecycle and operational events, including:

- startup and shutdown;
- backend health and catalog failures;
- analysis start and completion;
- selected and returned model identifiers where useful;
- cancellation and request failure categories;
- backend trace identifier where available.

Logs shall avoid image contents, heatmap payloads, secrets, and unnecessary personal paths.

## Current State

The architecture described here is implemented for selectable multi-model analysis and heatmap presentation. The application uses Generic Host composition, validated configuration, dependency injection, three typed backend clients, MVVM presentation, explicit inference and analysis models, startup health and catalog refresh, cancellable analysis, validated Base64 PNG decoding, and an interactive aligned overlay.

The Release solution builds successfully and all 60 automated tests pass. Capsule, Bottle, Candle, and Cashew were discovered, selected, routed, and analyzed through the native desktop workflow against the real backend and inference service.

The architecture intentionally remains within one production assembly and one test assembly. Multi-model discovery fits the existing service and MVVM boundaries and does not justify additional architectural layers.

## Documentation Update Rule

This document should change when architectural boundaries or verified integration decisions change. Routine implementation details do not require a full rewrite.

## Last Updated

2026-08-21
