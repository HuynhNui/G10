using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace G10.Prototype.Tests
{
    /// <summary>Editor test automation with durable XML across PlayMode domain reloads.</summary>
    [InitializeOnLoad]
    public static class MissionRefactorTestRunner
    {
        static MissionRefactorTestRunner() => TestRunnerApi.RegisterTestCallback(new Results());
        public static string Run(bool playMode, string[] filters, string output)
        {
            SessionState.SetString("G10.MissionTests.Output", Path.GetFullPath(output));
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            return api.Execute(new ExecutionSettings(new Filter { testMode = playMode ? TestMode.PlayMode : TestMode.EditMode, testNames = filters }));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test)
            {
                string path = SessionState.GetString("G10.MissionTests.Output", "");
                if (!string.IsNullOrEmpty(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path + ".current", test.FullName);
                }
            }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path = SessionState.GetString("G10.MissionTests.Output", "");
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                TestRunnerApi.SaveResultToFile(result, path);
                File.WriteAllText(path + ".summary", $"Passed={result.PassCount}; Failed={result.FailCount}; Skipped={result.SkipCount}; Inconclusive={result.InconclusiveCount}");
                SessionState.EraseString("G10.MissionTests.Output");
            }
        }
    }
}
