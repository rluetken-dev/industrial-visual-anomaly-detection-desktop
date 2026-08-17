# Industrial Visual Anomaly Detection Desktop - Architecture Overview

## Purpose

This document describes the verified architecture of the Windows desktop client, its internal responsibilities, and its boundary to the existing ASP.NET Core backend.

Implementation progress belongs in `DevelopmentStatus.md`. Stable scope belongs in `ProjectSpecification.md`. HTTP details belong in `ApiIntegration.md`.

## Architectural Goal

The desktop application provides a maintainable WPF user interface without duplicating backend validation, orchestration, or model-inference responsibilities.

The design favors a small MVVM application with explicit boundaries over premature separation into multiple production assemblies.

## System Context

```text
User
  |
  v
WPF desktop client
  |
  | HTTPS, multipart form data, JSON and Problem Details
  v
ASP.NET Core backend
  |
  | HTTP
  v
Python inference service
  |
  v
Exported anomaly-detection model artifact
```

The desktop client depends only on the public backend contract. It does not communicate with the Python service and does not load model artifacts.

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
|   `-- Status/
|-- Resources/
|   `-- Styles/
|-- Services/
|   |-- Backend/
|   `-- Files/
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
- bind to view-model state and commands;
- select styles and templates from shared resource dictionaries;
- provide narrowly scoped code-behind for behavior that is inherently tied to WPF UI infrastructure.

Views shall not:

- perform HTTP requests;
- parse backend responses;
- contain anomaly-decision logic;
- resolve dependencies manually;
- access the Python service or model artifacts.

`MainWindow` receives its view model through constructor injection and assigns it as the data context. Its code-behind remains limited to WPF lifecycle behavior.

### View Models

View models expose presentation state and coordinate user interactions.

`MainWindowViewModel` is responsible for:

- command availability;
- busy and cancellation state;
- selected-image and preview state;
- backend health and readiness orchestration;
- image-analysis orchestration;
- result and error presentation;
- translating application results into UI-friendly state.

The view model depends on service abstractions and is tested without creating a WPF window or making real network requests.

### Services

Services implement non-visual application and infrastructure behavior.

Current service boundaries are:

- `IBackendHealthService` for backend liveness and readiness;
- `IImageAnalysisService` for multipart analysis requests and response mapping;
- `IImageFilePicker` for selecting supported local images;
- `IImagePreviewLoader` for loading file-lock-free previews.

The corresponding implementations isolate `HttpClient`, file dialogs, image decoding, JSON deserialization, and transport failure handling from the view model.

### Models

Models represent explicit application state and analysis results.

Current model areas include:

- `AnalysisDecision` and `ImageAnalysisResult` for backend analysis results;
- `SystemAvailabilityStatus` for unknown, checking, available, and unavailable health states.

Transport responses are mapped into application-facing models before they reach presentation state. The backend remains authoritative for the decision.

### Configuration

Configuration owns strongly typed settings such as the backend base address and HTTP timeout.

`BackendOptions` is bound from the `Backend` configuration section and validated during application startup by `BackendOptionsValidator`. Invalid required configuration prevents the application host from starting with an ambiguous runtime state.

Machine-specific values may be supplied through local configuration that is excluded from source control.

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

`App.xaml.cs` is the composition root. It creates and starts the host, resolves and displays `MainWindow`, initiates the first backend status refresh, and stops and disposes the host during application shutdown.

Registrations are grouped by responsibility so configuration, HTTP services, desktop services, view models, and views remain easy to locate. Application services and view models do not use a global service locator.

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
HTTP or desktop infrastructure implementation
```

Concrete infrastructure depends on backend contract details and .NET platform services. The view model depends on application-facing interfaces rather than `HttpClient`, file dialogs, or image-decoding implementations.

## Backend Communication

Backend communication uses typed `HttpClient` registrations configured through `IHttpClientFactory`.

The client boundary covers:

- `GET /health/live`;
- `GET /health/ready`;
- `POST /api/v1/analyses`;
- multipart image upload using the field name `image`;
- JSON response deserialization;
- Problem Details handling;
- timeout, cancellation, connectivity, and invalid-response handling.

The base address and timeout are configuration values. Backend calls remain inside the service layer and are not duplicated in the view model or view.

## Image Handling

The selected image remains a local client-side file until the user starts analysis.

The file picker limits normal selection to PNG and JPEG files. The preview loader decodes the selected image into a WPF-compatible representation without retaining an unnecessary lock on the source file.

These client-side checks support usability only. The backend remains authoritative for upload size, media type, file signature, and inference validation.

Image bytes are sent as multipart form data under the field name `image`. The application does not persist uploaded images by default.

## Asynchronous Execution

Health requests, analysis requests, and preview loading use asynchronous operations where practical.

The analysis workflow:

- exposes a busy state;
- prevents duplicate execution;
- propagates a cancellation token;
- supports an explicit cancel command;
- distinguishes user cancellation from operational failure;
- reliably restores command availability after completion or failure;
- avoids blocking the WPF dispatcher thread.

## Error Handling

Expected failures are presented as application state rather than unhandled exceptions.

Relevant categories include:

- invalid local file selection or preview loading;
- backend validation failure;
- unsupported media type;
- oversized upload;
- backend or inference unavailability;
- HTTP timeout;
- connectivity failure;
- malformed or incompatible response;
- user cancellation.

Problem Details information and the backend trace identifier are preserved where available. Raw stack traces and sensitive details are not displayed to ordinary users.

Unexpected exceptions may be logged and handled by a final application-level safety boundary, but this boundary is not a replacement for specific error handling.

## Health Representation

Liveness and readiness have different meanings:

- liveness indicates that the ASP.NET Core backend process responds;
- readiness indicates that the backend can reach the configured inference service and is ready to analyze images.

The UI represents these states independently. A live but not-ready backend is not presented as fully operational.

Each indicator supports unknown, checking, available, and unavailable states with matching text and semantic color. The application performs an initial refresh after startup and also provides an explicit refresh command. Continuous aggressive polling is not required for the initial version.

## Result Representation

The analysis result displays:

- normal or anomalous decision;
- anomaly score;
- decision threshold;
- model identifier;
- model category;
- backend processing time;
- trace identifier.

Decision presentation uses semantic visual states while preserving readable text. The UI does not recalculate the anomaly decision from score and threshold because the backend supplies the authoritative decision.

## Future Heatmap Extension

Heatmaps are intentionally outside the initial contract.

When introduced, the preferred flow remains:

```text
Python inference service
  -> ASP.NET Core backend
  -> WPF desktop client
```

The desktop client shall not derive a heatmap by reading model tensors or artifacts directly. The API should expose a controlled heatmap representation, such as a separate image resource or another explicitly versioned response contract.

Large binary images should not be embedded as Base64 in every ordinary analysis response unless measurements justify that design.

## Testing Strategy

The test project is currently organized by responsibility:

```text
IndustrialVisualAnomalyDetection.Desktop.Tests/
`-- Unit/
    |-- Configuration/
    |-- Services/
    |   `-- Backend/
    `-- ViewModels/
```

The automated suite covers:

- configuration validation;
- backend health response mapping;
- analysis response mapping;
- timeout, cancellation, and transport behavior;
- view-model state transitions;
- command availability;
- image selection and analysis orchestration;
- successful and failed analysis workflows.

Controlled HTTP handlers and test doubles isolate automated tests from the real backend. Visual layout is verified through manual inspection and screenshots rather than brittle UI automation.

## Security and Privacy

- The application does not contain backend credentials or model artifacts.
- Selected images are not logged or persisted by default.
- Logs should use filenames and paths only when necessary and appropriate for local diagnostics.
- Backend TLS validation is not disabled in production code.
- Development certificate trust is a local setup concern.
- Server responses are treated as untrusted input and validated during deserialization and mapping.

## Observability

The Generic Host provides the logging foundation for meaningful lifecycle and operational events, including:

- startup and shutdown;
- backend health failures;
- analysis start and completion;
- cancellation;
- request failure category;
- backend trace identifier where available.

Logs shall avoid image contents, secrets, and unnecessary personal paths. Additional operational logging should be added only where it provides useful diagnostics without duplicating user-facing state.

## Current State

The architecture described in this document is implemented for the initial desktop workflow. The application uses Generic Host composition, validated configuration, dependency injection, typed HTTP clients, MVVM presentation, file and preview services, centralized styling, startup health refresh, cancellable analysis, and explicit result models.

The Release solution builds successfully, all 35 automated tests pass, and end-to-end normal and anomalous image analysis has been verified against the real backend and Python inference service.

The current architecture intentionally remains within one production assembly and one test assembly. The verified CI workflow and the remaining screenshot and release preparation are repository concerns and do not justify additional architectural layers.

## Documentation Update Rule

This document should change when architectural boundaries or verified integration decisions change. Routine implementation details do not require a full rewrite.

## Last Updated

2026-08-17
