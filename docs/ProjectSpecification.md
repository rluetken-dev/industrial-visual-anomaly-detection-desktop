# Industrial Visual Anomaly Detection Desktop - Project Specification

## Document Purpose

This document defines the stable scope, goals, constraints, and acceptance criteria for the desktop application.

Implementation progress belongs in `DevelopmentStatus.md`. Architectural decisions belong in `ArchitectureOverview.md`. Backend HTTP details belong in `ApiIntegration.md`.

## Project Objective

The project provides a Windows desktop application for operating the Industrial Visual Anomaly Detection system.

The application allows a user to discover available inference models, select a model and compatible industrial image, submit the image and model selection to the ASP.NET Core backend, inspect the authoritative anomaly-detection result, and compare the source image with an interactive anomaly-heatmap overlay.

## System Context

The desktop application is one component of a larger system:

```text
WPF desktop application
        |
        | HTTPS, JSON and multipart form data
        | Discovers models
        | Submits image and selected model ID
        | Receives decision data and Base64 PNG heatmap
        v
ASP.NET Core backend
        |
        | HTTP
        v
Python inference service
        |
        | Resolves a registry entry
        v
Exported anomaly-detection model artifact
```

The desktop application communicates only with the ASP.NET Core backend. It must not invoke Python, read the model registry, or access model artifacts directly.

## Current Functional Scope

The current usable version shall provide:

- configurable backend base address;
- automatic and manual backend-status refresh;
- distinct backend-liveness and inference-readiness status;
- runtime discovery of available models through the backend;
- automatic selection of the backend-declared default model;
- manual inference-model selection;
- manual model-catalog refresh;
- local PNG or JPEG file selection;
- image preview before submission;
- analysis through `POST /api/v1/analyses` with the selected model ID;
- cancellation of an active analysis request;
- display of the anomaly decision;
- display of anomaly score and decision threshold;
- display of the returned model identifier and category;
- display of processing duration and trace identifier;
- validation and decoding of the required PNG heatmap response;
- spatially aligned source-image and heatmap presentation;
- heatmap visibility control;
- adjustable heatmap opacity with a default value of 40 percent;
- understandable catalog, validation, connectivity, timeout, and service-unavailable errors;
- protection against duplicate submissions while analysis is running.

## Deferred Scope

The following capabilities remain intentionally deferred beyond the verified workflow:

- automatic validation that the selected image belongs to the selected model category;
- certified defect segmentation or pixel-accurate masks;
- generalized overlay alignment for preprocessing pipelines that crop or change aspect ratio;
- analysis history and persistence;
- batch image processing;
- camera integration;
- drag-and-drop upload;
- backend configuration editing inside the application;
- authentication and authorization;
- installer packaging and automatic updates;
- localization;
- direct Python, registry, or model-artifact integration.

Deferred capabilities may be introduced later without changing the fundamental client/backend boundary.

## Technology Baseline

- C# and .NET 10;
- WPF for the Windows desktop interface;
- MVVM for presentation separation;
- `CommunityToolkit.Mvvm` for observable state and commands;
- .NET Generic Host for startup, configuration, logging, and dependency injection;
- `IHttpClientFactory` for backend communication;
- `System.Text.Json` for JSON serialization;
- WPF imaging APIs for local preview and heatmap decoding;
- xUnit for automated tests;
- GitHub Actions for continuous integration.

Dependencies shall be added only when they provide a concrete benefit to the implemented scope.

## Architectural Constraints

- Views shall not perform HTTP requests or decode backend payloads directly.
- View models shall depend on application-facing abstractions rather than concrete HTTP implementation details.
- Backend transport contracts shall be represented by explicit private client types.
- Application-facing catalog, inference-model, analysis, and heatmap models shall validate required state.
- Model identities shall be discovered from the backend rather than duplicated in desktop configuration.
- The stable model ID, not display text, shall be used for routing.
- Constructors and public boundaries shall validate required dependencies and invalid state.
- Cancellation tokens shall be propagated through asynchronous operations.
- UI-bound state changes shall remain safe for the WPF dispatcher model.
- Source previews and decoded heatmaps shall not retain unnecessary stream or source-file locks.
- Heatmap visibility and opacity shall remain presentation-only controls.
- Machine-specific URLs, secrets, raw images, model registries, artifacts, standalone heatmaps, logs, and generated output shall not be committed.
- The desktop shall treat the backend as the authoritative API boundary.

## User Workflow

The primary workflow is:

1. Start the desktop application.
2. Observe backend liveness and inference readiness.
3. Wait for the model catalog to load or refresh it manually.
4. Confirm the default model or select another available model.
5. Select a supported local image compatible with that model category.
6. Inspect the image preview.
7. Start analysis.
8. Wait while the application reports a busy state.
9. Inspect the returned decision, model identity, and supporting values.
10. Review the heatmap aligned over the source image.
11. Toggle the heatmap or adjust its opacity for comparison.
12. Correct or retry understandable failures when necessary.

Model and image selection are intentionally independent. The current application does not infer the image category before submission; category compatibility remains the operator's responsibility.

## Main Screen Requirements

The main window shall contain:

- application title;
- backend-liveness and inference-readiness indicators;
- manual status-refresh action;
- inference-model selector;
- available-model status or count;
- manual model-catalog refresh action;
- image-selection action;
- selected file information;
- image preview;
- aligned heatmap overlay;
- heatmap visibility control;
- heatmap opacity control;
- analyze and cancel actions;
- analysis progress state;
- decision result;
- score and threshold;
- returned model identifier and category;
- processing duration and trace identifier;
- non-blocking error presentation where practical.

The selected model text shall remain readable with the application's dark theme. The interface should remain functional at common desktop resolutions, preserve the complete image using proportional scaling, and avoid machine-specific paths.

## Model Catalog Requirements

- The client shall retrieve models from `GET /api/v1/models`.
- The catalog shall contain at least one model.
- Model identifiers shall be unique.
- Every model shall contain a non-empty ID, display name, and category plus a positive input size.
- Exactly one model shall be marked as default.
- The declared default ID shall match the default entry.
- Invalid or incomplete catalog responses shall not be presented as valid model choices.
- The declared default shall be selected after successful loading.
- Users shall be able to choose another available model.
- Users shall be able to refresh the catalog explicitly.
- A catalog failure shall clear stale model choices and produce understandable feedback.
- Analysis shall not start without a selected model.
- Model display names shall be used only for presentation; stable IDs shall be used for requests.

## Analysis Request Requirements

- One supported local image shall be submitted as multipart field `image`.
- The selected stable model identifier shall be submitted as multipart field `modelId`.
- The model selection used by an active request shall remain stable for that request.
- Request streams and multipart content shall be disposed after completion.
- Cancellation shall propagate to the HTTP operation.
- The returned model identifier shall be displayed as the authoritative identity of the model that handled the request.

The backend permits omission of `modelId` for backward compatibility and applies its configured default. The current desktop workflow shall send the explicitly selected catalog model.

## Heatmap Presentation Requirements

- The returned heatmap shall use the `image/png` content type.
- The Base64 payload shall be validated before presentation.
- Heatmap width and height shall be positive.
- The decoded image shall be loaded independently of the response stream and made immutable where supported.
- Source image and heatmap shall use the same layout bounds and scaling mode.
- The heatmap shall be visible by default after successful analysis.
- Default heatmap opacity shall be 40 percent.
- The user shall be able to hide the heatmap or adjust opacity from 0 to 100 percent.
- Changing presentation shall not alter the decision, score, threshold, model selection, or backend state.
- The interface and documentation shall describe the heatmap as relative patch responses, not certified segmentation.

The alignment contract assumes that source preview and returned heatmap describe the same spatial extent and aspect ratio. Cropping or aspect-ratio-changing preprocessing requires an expanded transform-metadata contract.

## Backend Contract Assumptions

The client targets:

- `GET /health/live`;
- `GET /health/ready`;
- `GET /api/v1/models`;
- `POST /api/v1/analyses` using multipart fields `image` and `modelId`.

The catalog response includes:

- default model identifier;
- model collection;
- model identifier;
- display name;
- category;
- input size;
- default flag.

The analysis response includes:

- model identifier;
- model category;
- anomaly score;
- decision threshold;
- textual decision;
- processing time in milliseconds;
- trace identifier;
- required heatmap content type;
- required heatmap width and height;
- required Base64-encoded PNG heatmap data.

Backend failures use Problem Details where applicable. The client shall tolerate unknown additional JSON properties for compatible evolution while rejecting missing or invalid fields required by the current workflow.

## Configuration Requirements

The backend base address shall be configurable outside compiled source code.

The application shall validate required configuration during startup and provide an actionable failure when configuration is missing or invalid. Model identities shall not be duplicated in configuration. No private hostnames, credentials, or personal filesystem paths shall be committed.

## Quality Requirements

- The complete solution shall build without errors.
- Automated tests shall cover view-model behavior, catalog and HTTP mapping, model and heatmap invariants, and image decoding where practical.
- HTTP integration tests shall use controlled handlers or local test doubles rather than the real backend.
- The UI shall remain responsive during network operations.
- Expected operational failures shall not terminate the application.
- Invalid catalogs and incomplete analysis or heatmap responses shall not be silently presented as valid.
- Logging shall provide useful diagnostics without exposing image contents, Base64 payloads, or sensitive configuration.
- Repository setup and local execution shall be reproducible from documented commands.

## Baseline Acceptance Criteria

The initial desktop baseline is complete when:

- the application starts through the configured host and dependency-injection setup;
- backend health is visible in the UI;
- a supported image can be selected and previewed;
- the image can be submitted to the real backend;
- a successful response is rendered correctly;
- invalid uploads and unavailable services produce understandable feedback;
- an active request can be cancelled;
- automated tests cover the central non-visual behavior;
- CI restores, builds, and tests the solution;
- an end-to-end run against backend and Python is verified;
- README setup instructions and screenshots describe the verified state.

## Heatmap Milestone Acceptance Criteria

The heatmap milestone is complete when:

- the backend heatmap contract is mapped explicitly;
- missing or invalid heatmap data is rejected;
- a valid Base64 PNG heatmap is decoded into a WPF image source;
- the heatmap is aligned over the selected source image;
- visibility and opacity controls work without changing the result;
- normal and anomalous images are verified through the local stack;
- automated tests cover model, decoder, HTTP mapping, and view-model state;
- Release build and tests succeed;
- documentation and screenshots show the verified workflow.

## Multi-Model Milestone Acceptance Criteria

The selectable-model milestone is complete when:

- the client retrieves `GET /api/v1/models` through a dedicated service boundary;
- catalog and model invariants are represented by application models and tested;
- the backend-declared default model is selected after loading;
- users can select another model and refresh the catalog;
- analysis is disabled without a selected model;
- the selected stable ID is forwarded as multipart `modelId`;
- the returned model identity is displayed;
- catalog, selection, and model-forwarding failures are handled safely;
- automated tests cover catalog transport, model invariants, selection, commands, and analysis forwarding;
- the Release solution and complete test suite succeed;
- multiple model categories are verified through the real local stack;
- documentation describes the verified contract and workflow;
- the changes are committed, pushed, pass CI, and are included in a desktop release.

## Acceptance Status

The initial desktop baseline was completed, published publicly, and released as `v0.1.0`.

The heatmap response mapping, validation, decoding, aligned overlay, visibility control, opacity control, automated tests, full-stack execution, documentation, and screenshots have been verified.

The multi-model implementation is locally complete: the catalog, default and manual selection, explicit model routing, updated UI, 60 automated tests, Release build, and native Capsule, Bottle, Candle, and Cashew workflows have been verified. Publication-related acceptance criteria remain open until the desktop changes are committed, pushed, pass CI, and are released.

## Repository Boundary

This repository owns the Windows desktop client only. It does not own:

- model training or evaluation;
- model-registry authoring or artifact resolution;
- heatmap-generation algorithms;
- exported model artifacts;
- the Python inference runtime;
- backend validation or orchestration;
- datasets or uploaded production images.

The desktop owns retrieval and presentation of model choices and validation and presentation of heatmap data received through the backend. Registry, inference, or transport changes belong in their corresponding repositories.

## Documentation Update Rule

Documentation shall be updated after verified milestones or meaningful groups of changes. Small internal edits do not require immediate full-document revisions.

## Current Status

The WPF selectable-model analysis and interactive heatmap workflow is implemented and locally verified. The application starts through the Generic Host, reports backend availability, retrieves the model catalog, selects the declared default, supports manual model selection, previews images, submits analyses with explicit model IDs, supports cancellation, presents returned results, and overlays heatmaps with visibility and opacity controls.

The Release solution builds successfully, and all 60 automated tests pass. Capsule, Bottle, Candle, and Cashew model selection and analysis have been verified against the ASP.NET Core backend, Python inference service, registry, and exported artifacts.

The multi-model milestone is ready for final repository verification, commit, push, CI, and desktop release.

## Last Updated

2026-08-21
