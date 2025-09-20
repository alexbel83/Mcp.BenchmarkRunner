# Mcp.BenchmarkRunner
**Run .NET benchmarks from an LLM.**  
An MCP (Model Context Protocol) server in C# that executes BenchmarkDotNet on demand and returns results & artifacts. Works with **MCP Inspector** and **GitHub Copilot Chat (Tools) in Visual Studio**.

Maintained by **Aliaksandr Marozka** — [amarozka.dev](https://amarozka.dev)

> TL;DR – Ask your AI to “run the hash benchmarks,” get back Mean/Alloc deltas and Markdown/JSON/CSV artifacts. Perfect for demoing MCP, perf checks in PRs, or quick regression hunts.

---

## ✨ Features
- **MCP tools**
  - `run_bench(filter?, job?, exporters?, timeoutSec?)` – run benchmarks, get a `runId` + brief JSON summary
  - `get_results(runId)` – parsed JSON summary (Mean, Alloc/Op, etc.)
  - `list_artifacts(runId)` – list all files (JSON / Markdown / CSV / logs)
  - `compare_runs(baseRunId, headRunId)` – simple deltas (% change for Mean & AllocatedBytes/Op)
- **Job presets**: `Short`, `Medium`, `Long`
- **Exporters**: `json`, `fulljson`, `md`, `csv`
- **Clients**: MCP Inspector (stdio) and Visual Studio (Copilot Chat Tools)

---

## 🗂 Project Layout
```
Mcp.BenchmarkRunner.sln
│
├─ src/
│  ├─ Mcp.BenchmarkRunner.Server/          # MCP server (console)
│  │  ├─ Program.cs                        # host + stdio transport + attribute discovery
│  │  └─ Tools/
│  │     ├─ BenchTools.cs                  # run_bench / get_results / list_artifacts
│  │     └─ CompareTools.cs                # compare_runs
│  │
│  └─ SampleBenchmarks/                    # BenchmarkDotNet executable
│     ├─ Program.cs                        # CLI: --artifacts/--exporters/--job/--filter
│     ├─ Jobs.cs                           # Short/Medium/Long presets
│     └─ HashBench.cs                      # MD5 vs SHA256 demo
│
└─ runs/                                   # output per runId (created at runtime)
```

**Flow**: Inspector/Copilot → MCP server (stdio) → spawns `dotnet run` on `SampleBenchmarks` → BDN writes artifacts → server returns JSON/paths.

---

## 🚀 Quickstart

### Prerequisites
- .NET SDK 9.0 (or switch both projects to **.NET 8.0**)
- (Optional) Node.js for MCP Inspector (`npx`)

### Build & sanity check (CLI)
```bash
dotnet restore
dotnet build

# Run benchmarks without MCP (sanity)
dotnet run --project src/SampleBenchmarks/SampleBenchmarks.csproj -c Release -- \
  --artifacts runs/manual1 --exporters json,md --job Short --filter *Hash*

# Start the MCP server
dotnet run --project src/Mcp.BenchmarkRunner.Server
```

Artifacts appear under `runs/<runId or manual1>`.

---

## 🧪 MCP Inspector (standalone)

Start Inspector:
```bash
npx -y @modelcontextprotocol/inspector
# or:
npm i -g @modelcontextprotocol/inspector
mcp-inspector
```

Connect (stdio):
- **Command:** `dotnet`
- **Args:** `run --project src/Mcp.BenchmarkRunner.Server/Mcp.BenchmarkRunner.Server.csproj`

Example payloads:
```json
{ "filter": "*Hash*", "job": "Short", "exporters": "json,md" }
```
```json
{ "BaseRunId": "<first>", "HeadRunId": "<second>" }
```

> **Windows npm ENOENT fix**  
> If `npx` fails with `%APPDATA%\npm` not found:  
> `mkdir "%APPDATA%\npm" && npm config set prefix "%APPDATA%\npm"` and add `%APPDATA%\npm` to PATH.

---

## 🧰 Visual Studio (Copilot Chat Tools)

### Option A — `.mcp.json` in the **solution root** (recommended)
```json
{
  "servers": {
    "benchrunner": {
      "name": "Benchmark Runner",
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Mcp.BenchmarkRunner.Server/Mcp.BenchmarkRunner.Server.csproj"
      ]
    }
  }
}
```

### Option B — global `%USERPROFILE%\.mcp.json` (use absolute path)
```json
{
  "servers": {
    "benchrunner": {
      "name": "Benchmark Runner",
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\repo\\src\\Mcp.BenchmarkRunner.Server\\Mcp.BenchmarkRunner.Server.csproj"
      ]
    }
  }
}
```

**Use it:** *View → GitHub Copilot Chat* → Tools/Agent → enable **Benchmark Runner** → run:
- `run_bench` → grab `runId`
- `get_results` / `list_artifacts`
- `compare_runs` with two runIds

Logs: *View → Output → GitHub Copilot*.

---

## 🧩 Tool Contracts

### `run_bench`
```json
{ "filter": "string?", "job": "Short|Medium|Long", "exporters": "json|fulljson|md|csv", "timeoutSec": 600 }
```
**Returns:** `{ "runId": "string", "artifactsDir": "path", "summary": { ... } }`

### `get_results`
```json
{ "runId": "string" }
```
**Returns:** parsed JSON exporter object or `null`.

### `list_artifacts`
```json
{ "runId": "string" }
```
**Returns:** list of file paths under `runs/<runId>`.

### `compare_runs`
```json
{ "BaseRunId": "string", "HeadRunId": "string" }
```
**Returns:** rows with Mean/Alloc base/head and `%Δ`.

---

## 🛠 Troubleshooting

- **`restore failed`**  
  - Enable `nuget.org` source; clear caches:  
    `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org`  
    `dotnet nuget locals all --clear`  
  - Enable **prerelease** (MCP SDK is preview) or switch both projects to `net8.0`.
- **Benchmarks work in CLI but fail via MCP**  
  - Typically a **path issue** when launching from `bin/…`.  
  - Walk up **5 levels** from `AppContext.BaseDirectory` to repo root *or* auto-detect the root (look for `.sln`/`src`).
- **`dotnet` not found in VS**  
  - Add `C:\\Program Files\\dotnet\\` to user PATH and restart Visual Studio.

---

## 🗺 Roadmap
- `upload_project` tool: zip → sandbox → run user benchmarks
- `DisassemblyDiagnoser` tool for hot-path asm dumps
- Publish as `dotnet new` template or a NuGet tool
- CI hook: post `compare_runs` deltas on PRs

---

## 📎 Credits
Built by **Aliaksandr Marozka** — [amarozka.dev](https://amarozka.dev)

## 📄 License
MIT
