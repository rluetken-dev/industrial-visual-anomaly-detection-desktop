# Industrial Visual Anomaly Detection Desktop - Project Specification

## Document Purpose

This document defines the stable scope, goals, constraints, and acceptance criteria for the desktop application.

Implementation progress belongs in `DevelopmentStatus.md`. Architectural decisions belong in `ArchitectureOverview.md`. Details of the backend HTTP integration belong in `ApiIntegration.md`.

## Project Objective

The project provides a Windows desktop application for operating the Industrial Visual Anomaly Detection system.

The application allows a user to select an industrial image, submit it to the existing ASP.NET Core backend, inspect the returned anomaly-detection result, and compare the source image with an interactive anomaly-heatmap overlay through a clear graphical interface.

## System Context

The desktop application is one component of a larger system:

```text
WPF desktop application
        |
        | HTTPS, JSON and multipart form data
        | Receives decision data and Base64 PNG heatmap
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

The desktop application communicates only with the ASP.NET Core backend. It must not invoke the Python inference service or access model artifacts directly.

## Current Functional Scope

The current usable version shall provide:

- configurable backend base address;
- automatic and manual backend status refresh;
- distinct backend liveness and inference readiness status;
- local PNG or JPEG file selection;
- image preview before submission;
- image analysis through `POST /api/v1/analyses`;
- cancellation of an active analysis request;
- display of the anomaly decision;
- display of anomaly score and decision threshold;
- display of model identifier and category;
- display of processing duration and trace identifier;
- validation and decoding of the required PNG heatmap response;
- spatially aligned source-image and heatmap presentation;
- heatmap visibility control;
- adjustable heatmap opacity with a default value of 40 percent;
- understandable validation, connectivity, timeout, and service-unavailable errors;
- protection against duplicate submissions while an analysis is running.

## Deferred Scope

The following capabilities remain intentionally deferred beyond the verified current workflow:

- certified defect segmentation or pixel-accurate masks;
- generalized overlay alignment for preprocessing pipelines that crop images or change their aspect ratio;
- analysis history and persistence;
- batch image processing;
- camera integration;
- drag-and-drop upload;
- backend configuration editing inside the application;
- authentication and authorization;
- installer packaging and automatic updates;
- localization;
- direct Python or model-artifact integration.

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
- Backend request and response contracts shall be represented by explicit client models.
- Domain-facing analysis and heatmap models shall validate required state.
- Constructors and public boundaries shall validate required dependencies and invalid state.
- Cancellation tokens shall be propagated through asynchronous operations.
- UI-bound state changes shall remain safe for the WPF dispatcher model.
- Source previews and decoded heatmaps shall not retain unnecessary stream or source-file locks.
- Heatmap visibility and opacity shall remain presentation-only controls.
- Machine-specific URLs, secrets, raw images, standalone generated heatmaps, logs, and generated output shall not be committed.
- The desktop application shall treat the backend as the authoritative API boundary.

## User Workflow

The primary workflow is:

1. Start the desktop application.
2. Observe backend liveness and inference readiness.
3. Select a supported local image.
4. Inspect the image preview.
5. Start analysis.
6. Wait while the application reports a busy state.
7. Inspect the returned decision and supporting values.
8. Review the heatmap aligned over the source image.
9. Toggle the heatmap or adjust its opacity for comparison.
10. Correct or retry understandable failures when necessary.

## Main Screen Requirements

The main window shall contain:

- application title;
- backend liveness and inference readiness indicators;
- manual status-refresh action;
- image selection action;
- selected file information;
- image preview;
- aligned heatmap overlay;
- heatmap visibility control;
- heatmap opacity control;
- analyze and cancel actions;
- analysis progress state;
- decision result;
- score and threshold;
- model identifier and category;
- processing duration and trace identifier;
- non-blocking error presentation where practical.

The interface should remain functional at common desktop resolutions and should not rely on a fixed machine-specific path.

## Heatmap Presentation Requirements

- The returned heatmap shall use the `image/png` content type.
- The Base64 payload shall be validated before presentation.
- Heatmap width and height shall be positive.
- The decoded image shall be loaded independently of the response stream and made immutable where supported.
- The source image and heatmap shall use the same layout bounds and scaling mode.
- The heatmap shall be visible by default after successful analysis.
- The default heatmap opacity shall be 40 percent.
- The user shall be able to hide the heatmap or adjust opacity from 0 to 100 percent.
- Changing heatmap presentation shall not change the decision, score, threshold, or backend state.
- The interface and documentation shall describe the heatmap as a visualization of relative patch responses, not as a certified segmentation mask.

The current alignment contract assumes that the source preview and returned heatmap describe the same spatial extent and aspect ratio. Preprocessing that crops or changes aspect ratio requires an expanded transform-metadata contract before generalized alignment can be claimed.

## Backend Contract Assumptions

The client targets these backend endpoints:

- `GET /health/live`;
- `GET /health/ready`;
- `POST /api/v1/analyses` using multipart form data with the field name `image`.

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

Backend failures use Problem Details where applicable. The client shall tolerate unknown additional JSON properties so the API can evolve compatibly, while rejecting missing or invalid fields required by the current workflow.

## Configuration Requirements

The backend base address shall be configurable outside compiled source code.

The application shall validate required configuration during startup and shall provide an actionable failure when configuration is missing or invalid. No private hostnames, credentials, or personal filesystem paths shall be committed.

## Quality Requirements

- The complete solution shall build without warnings or errors.
- Automated tests shall cover view-model behavior, HTTP client mapping, heatmap invariants, and image decoding where practical.
- HTTP integration tests shall use controlled handlers or local test doubles rather than the real backend.
- The UI shall remain responsive during network operations.
- Expected operational failures shall not terminate the application.
- Invalid or incomplete heatmap responses shall not be silently presented as valid results.
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
- an end-to-end run against the backend and Python inference service is verified;
- README setup instructions and screenshots describe the verified state.

## Heatmap Milestone Acceptance Criteria

The heatmap milestone is complete when:

- the backend heatmap contract is mapped explicitly;
- missing or invalid heatmap data is rejected;
- a valid Base64 PNG heatmap is decoded into a WPF image source;
- the heatmap is aligned over the selected source image;
- visibility and opacity controls work without changing the analysis result;
- normal and anomalous images are verified through the complete local stack;
- automated tests cover the heatmap model, decoder, HTTP mapping, and view-model state;
- Release build and tests succeed;
- README and screenshots show the verified heatmap workflow.

## Acceptance Status

The initial desktop baseline was completed, published publicly, and released as `v0.1.0`.

The heatmap response mapping, validation, decoding, aligned overlay, visibility control, opacity control, automated tests, full-stack execution, README content, and updated screenshots have also been verified. All heatmap milestone acceptance criteria are satisfied in the current working state.

The remaining milestone work is documentation completion, final repository verification, commit and push, successful CI execution, and publication of the next desktop release.

## Repository Boundary

This repository owns the Windows desktop client only. It does not own:

- model training or evaluation;
- heatmap generation algorithms;
- exported model artifacts;
- the Python inference runtime;
- backend validation or orchestration;
- datasets or uploaded production images.

The desktop owns validation and presentation of the heatmap data received through the backend contract. Changes to heatmap generation or transport belong in their corresponding model or backend repositories.

## Documentation Update Rule

Documentation shall be updated after verified milestones or meaningful groups of changes. Small internal edits do not require immediate full-document revisions.

## Current Status

The WPF analysis and interactive heatmap workflow is implemented and verified. The application starts through the Generic Host, reports backend availability, selects and previews images, submits analyses to the backend, supports cancellation, presents normal and anomalous results, and overlays the returned heatmap on the source image with visibility and opacity controls.

The complete Release solution builds successfully, and all 49 automated tests pass. End-to-end analysis and `320 x 320` PNG heatmap presentation have been verified against the ASP.NET Core backend, Python inference service, and exported capsule model artifact.

CI, README setup instructions, release badges, and updated heatmap screenshots are present. The heatmap milestone is ready for final documentation verification, commit, push, CI, and its next release.

## Last Updated

2026-08-18
