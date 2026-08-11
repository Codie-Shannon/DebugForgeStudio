# Architecture

`DebugForgeStudio.Core` contains deterministic scan, triage, reproduction, hypothesis, comparison, and report logic.

`DebugForgeStudio.Web` exposes minimal ASP.NET Core endpoints and serves the evidence UI.

`DebugForgeStudio.Tests` is an executable deterministic harness.

No external system is called and no fix is automatically executed.
