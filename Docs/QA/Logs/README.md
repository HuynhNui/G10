# Evidence artifacts

Unity Test Runner XML and MCP job summaries are generated verification artifacts. They contain only test data from isolated temporary timelines, never player save JSON. Baseline XML is captured before adding tests or editing production code. Run IDs and exact tool parameters are in RegressionReport.

`regression-editmode.xml` (63/63) and `regression-playmode.xml` (274/274) are the final complete suite logs. Intermediate logs are retained, including failures, rather than replaced with the successful reruns. `prefix` denotes pre-fix production for the specific authority under investigation (the paused-device reproduction occurred before its navigation guard fix, after the SEND fix).

`windows-build.md` preserves the real MCP build result, warnings, command, artifact inspection/checksum and generated-file notes. `postbuild-scene-smoke.xml` preserves the two existing scene-flow cases rerun after native build/import.
