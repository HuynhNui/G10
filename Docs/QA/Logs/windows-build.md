# Windows Release build evidence — 2026-10-13 local

This is a real Unity Editor BuildPipeline result through AnkleBreaker MCP, not a launched player or human playtest. Project D:/G10, Unity 6000.4.2f1, port 7890. Authored QA source was unchanged between the full suites and this build.

## Exact build call and returned result

```javascript
unity_build({port:7890,target:"StandaloneWindows64",outputPath:"D:/G10/Temp/QAWindows-2026-10-13-001/G10.exe",developmentBuild:false})
```

```json
{"success":true,"data":{"success":true,"result":"Succeeded","totalErrors":0,"totalWarnings":3,"totalTime":76.9821773,"outputPath":"D:/G10/Temp/QAWindows-2026-10-13-001/G10.exe","totalSize":463520717,"platform":"StandaloneWindows64"}}
```

Preflight: output directory did not exist; target supported, Mono2x backend, eight enabled scene assets all exist (Bootstrap, MainMenu, GameplayCore, Zone01–04, Ending). Same enabled Build Settings used, no target/backend/package switch. Development=false. No executable launched; no real save accessed.

## Compiler warnings (unchanged source files)

- ZoneMapPresentation.cs(111,106): CS0618, FindObjectsSortMode obsolete.
- ZoneMapPresentation.cs(111,39): CS0618, FindObjectsByType overload taking FindObjectsSortMode obsolete.
- ShipUpgradeAction.cs(14,57): CS0414, amount assigned but never used.

Post-build MCP console query reported 0 errors and these three compiler warnings, plus older expected archive-fault test warnings (`Không lưu được ảnh ra ổ đĩa; ảnh vẫn còn trong phiên này.` and `Skipped unreadable photo archive entry.`). No global Console.Clear or unexpected-exception suppression was used. Compilation query: count=0, isCompiling=false. Editor clean Bootstrap, stopped/not paused.

## Artifact inspection

```powershell
Get-Item -LiteralPath 'D:\G10\Temp\QAWindows-2026-10-13-001\G10.exe'
Get-FileHash -LiteralPath 'D:\G10\Temp\QAWindows-2026-10-13-001\G10.exe' -Algorithm SHA256
Get-ChildItem -LiteralPath 'D:\G10\Temp\QAWindows-2026-10-13-001\G10_Data\Managed' -Filter '*G10*'
Get-ChildItem -LiteralPath 'D:\G10\Temp\QAWindows-2026-10-13-001\G10_Data\Managed' -Filter '*Test*'
```

G10.exe exists, length 667648 bytes, last write 2026-10-13 00:20:23 local. EXE SHA256: `CFCC57EA09F1BB8548BF2B3850CC3B05B28413DE3D2008DDF94149286EB5E92E`. Managed output contains G10.Prototype.dll (361472 bytes), no *Test* assemblies. UnityPlayer.dll, MonoBleedingEdge and G10_Data exist. BuildReport totalSize includes the player data/dependencies; EXE size alone is not the total package size.

Output is inside Git-ignored Temp and can be removed by Unity cleanup; it is not a published build or durable release artifact. Preserve the whole folder to run elsewhere, not just G10.exe. Do not run it in the current user's normal profile without save isolation.

## Unity-generated file preservation

Native import/tests/build updated dynamic font caches:

- Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset
- Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset

Build also temporarily populated ProjectSettings/ProjectSettings.asset preloadedAssets with the existing Input System asset `{fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}`, already referenced by EditorBuildSettings. This was the entire ProjectSettings diff immediately after build. Native post-build PlayMode smoke/cleanup removed that entry again; final ProjectSettings has no diff. Font changes remain. No generated files were manually reset/hand-edited; these are separate from the seven authored production QA code files.
