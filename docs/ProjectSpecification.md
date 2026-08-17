# Industrial Visual Anomaly Detection Desktop - Project Specification

## Document Purpose

This document defines the stable scope, goals, constraints, and acceptance criteria for the desktop application.

Implementation progress belongs in `DevelopmentStatus.md`. Architectural decisions belong in `ArchitectureOverview.md`. Details of the backend HTTP integration belong in `ApiIntegration.md`.

## Project Objective

The project provides a Windows desktop application for operating the Industrial Visual Anomaly Detection system.

The application allows a user to select an industrial image, submit it to the existing ASP.NET Core backend, and inspect the returned anomaly-detection result through a clear graphical interface.

## System Context

The desktop application is one component of a larger system:

```text
WPF desktop application
        |
        | HTTPS and JSON/multipart
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

## Initial Functional Scope

The first usable version shall provide:

- configurable backend base address;
- backend liveness and readiness status;
- local PNG or JPEG file selection;
- image preview before submission;
- image analysis through `POST /api/v1/analyses`;
- cancellation of an active analysis request;
- display of the anomaly decision;
- display of anomaly score and decision threshold;
- display of model identifier and category;
- display of processing duration and trace identifier;
- understandable validation, connectivity, timeout, and service-unavailable errors;
- protection against duplicate submissions while an analysis is running.

## Deferred Scope

The following capabilities remain intentionally deferred beyond the verified initial workflow:

- anomaly heatmap display;
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
- xUnit for automated tests;
- GitHub Actions for continuous integration.

Dependencies shall be added only when they provide a concrete benefit to the implemented scope.

## Architectural Constraints

- Views shall not perform HTTP requests directly.
- View models shall depend on application-facing abstractions rather than concrete HTTP implementation details.
- Backend request and response contracts shall be represented by explicit client models.
- Constructors and public boundaries shall validate required dependencies and invalid state.
- Cancellation tokens shall be propagated through asynchronous operations.
- UI-bound state changes shall remain safe for the WPF dispatcher model.
- Machine-specific URLs, secrets, images, logs, and generated output shall not be committed.
- The desktop application shall treat the backend as the authoritative API boundary.

## User Workflow

The primary workflow is:

1. Start the desktop application.
2. Observe backend availability.
3. Select a supported local image.
4. Inspect the image preview.
5. Start analysis.
6. Wait while the application reports a busy state.
7. Inspect the returned decision and supporting values.
8. Correct or retry understandable failures when necessary.

## Initial Screen Requirements

The first main window should contain:

- application title;
- backend liveness and readiness indicators;
- image selection action;
- selected file information;
- image preview;
- analyze and cancel actions;
- analysis progress state;
- decision result;
- score and threshold;
- model identifier and category;
- processing duration and trace identifier;
- non-blocking error presentation where practical.

The interface should remain functional at common desktop resolutions and should not rely on a fixed machine-specific path.

## Backend Contract Assumptions

The initial client targets these backend endpoints:

- `GET /health/live`;
- `GET /health/ready`;
- `POST /api/v1/analyses` using multipart form data with the field name `image`.

The analysis response currently includes:

- model identifier;
- model category;
- anomaly score;
- decision threshold;
- textual decision;
- processing time in milliseconds;
- trace identifier.

Backend failures use Problem Details where applicable. The client shall tolerate unknown additional JSON properties so the API can evolve compatibly.

## Configuration Requirements

The backend base address shall be configurable outside compiled source code.

The application shall validate required configuration during startup and shall provide an actionable failure when configuration is missing or invalid. No private hostnames, credentials, or personal filesystem paths shall be committed.

## Quality Requirements

- The complete solution shall build without warnings or errors.
- Automated tests shall cover view-model behavior and HTTP client mapping where practical.
- HTTP integration tests shall use controlled handlers or local test doubles rather than the real backend.
- The UI shall remain responsive during network operations.
- Expected operational failures shall not terminate the application.
- Logging shall provide useful diagnostics without exposing image contents or sensitive configuration.
- Repository setup and local execution shall be reproducible from documented commands.

## Initial Acceptance Criteria

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

## Acceptance Status

The application workflow, backend communication, health reporting, image selection and preview, cancellation behavior, result rendering, automated tests, full-stack execution, GitHub Actions CI, README setup instructions, and release screenshots have been verified.

All initial desktop baseline acceptance criteria are satisfied. The remaining work is final public repository and release preparation.

## Repository Boundary

This repository owns the Windows desktop client only. It does not own:

- model training or evaluation;
- exported model artifacts;
- the Python inference runtime;
- backend validation or orchestration;
- datasets or uploaded production images.

Changes to those responsibilities belong in their corresponding repositories.

## Documentation Update Rule

Documentation shall be updated after verified milestones or meaningful groups of changes. Small internal edits do not require immediate full-document revisions.

## Current Status

The initial WPF analysis workflow is implemented and verified. The application starts through the Generic Host, reports backend availability, selects and previews images, submits analyses to the backend, supports cancellation, and presents normal and anomalous results with their supporting metadata.

The complete Release solution builds successfully, and all 35 automated tests pass. End-to-end analysis has been verified against the ASP.NET Core backend, Python inference service, and exported capsule model artifact.

CI, README setup instructions, and release screenshots are verified. Final public repository and release preparation remain open.

## Last Updated

2026-08-17
