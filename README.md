# Altruista834OutboundMonitor

Production-grade .NET Framework 4.8 console application for real-time outbound file monitoring, SLA validation, resilient alerting, and incident reporting.

## Architecture

- **Models**: File metadata, rule contracts, SLA model.
- **Config**: Centralized `config.json` parsing and validation.
- **Services**:
  - `FolderMonitorService`: workflow orchestration + watcher/polling.
  - `SlaService`: dynamic completion estimate and breach detection.
  - `EmailService`: SMTP with retry.
  - `LoggingService`: NLog abstraction.
- **Utilities**:
  - file stability/lock checks,
  - retry helper,
  - timezone handling (IST with fallback).
- **Tests**: End-to-end simulation with mocked email channel and synthetic folder/file behaviors.

## Build & Run

### Prerequisites

- Windows Server 2016+ (recommended)
- .NET Framework 4.8 Runtime + Developer Pack
- Visual Studio 2022 Build Tools (MSBuild)

### Commands

```powershell
msbuild Altruista834OutboundMonitor.sln /p:Configuration=Release
.\Altruista834OutboundMonitor\bin\Release\Altruista834OutboundMonitor.exe
```

## Test Execution

```powershell
vstest.console.exe .\Altruista834OutboundMonitor\Tests\bin\Release\Altruista834OutboundMonitor.Tests.dll
```

## Deployment (Windows Server)

1. Create service account with least privilege to monitored network shares.
2. Create folders configured in `Config/config.json`.
3. Update SMTP creds and recipient distribution lists.
4. Deploy binaries + `Config/config.json` + `NLog.config`.
5. Configure scheduled task:
   - Trigger: daily before 6:00 AM IST
   - Run whether user logged in or not
   - Restart on failure: 3 times every 5 minutes
6. Enable monitoring of output logs under `logs/` and report folder.

## Example log output

See [`Docs/ExampleOutput.log`](Docs/ExampleOutput.log).

## Example email template

See [`Docs/EmailTemplates.md`](Docs/EmailTemplates.md).
