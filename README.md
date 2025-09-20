# MCP Benchmark Runner (OCP)

A ready-to-run Model Context Protocol (MCP) server in .NET that executes BenchmarkDotNet benchmarks on demand and returns results & artifacts. Open in Visual Studio or run from CLI.

## Features
- Tools: `run_bench`, `get_results`, `list_artifacts`, `compare_runs`
- Preset Jobs: Short / Medium / Long
- Exporters: JSON, FullJSON, Markdown, CSV
- Works great with the MCP Inspector

## Getting Started
1. **Restore & Build**
   ```bash
   dotnet build
   ```
2. **Run the MCP server**
   ```bash
   dotnet run --project src/Mcp.BenchmarkRunner.Server
   ```
3. **Test with MCP Inspector**
   - Install & open the Inspector, then connect using `.config/inspector.json` as a reference.
4. **Call tools**
   - `run_bench` with optional `filter`, `job`, `exporters`
   - use returned `runId` in `get_results` and `list_artifacts`
   - run twice and use `compare_runs` to see deltas

## References
- C# MCP SDK & quickstarts: Microsoft .NET Blog and Microsoft Learn
- NuGet packages: `ModelContextProtocol` (preview), `BenchmarkDotNet`
- MCP Inspector guide: modelcontextprotocol.io/docs/tools/inspector
