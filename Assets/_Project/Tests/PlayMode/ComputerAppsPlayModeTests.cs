using System.Collections;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class ComputerAppsPlayModeTests
    {
        [UnityTest]
        public IEnumerator AppsReadProvidersWithoutInventingGameplayData()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenPhotoLab();
            var photoView = screen.GetComponentInChildren<PhotoLabView>();
            var repository = screen.GetComponentInChildren<EmptyPhotoRepository>();
            Assert.That(repository.Photos.Count, Is.Zero);
            Assert.That(repository.CameraOnline, Is.False);
            Assert.That(photoView.DisplayedText, Does.Contain("NO IMAGES RECORDED"));
            Assert.That(photoView.DisplayedText, Does.Contain("CAMERA MODULE OFFLINE"));

            var statusProvider = screen.GetComponentInChildren<ExistingShipStatusProvider>();
            Assert.That(statusProvider.Radar, Is.SameAs(cabin.Radar));
            Assert.That(statusProvider.ReadStatus().RadarUsesRemaining, Is.Null);
            cabin.OpenRadar(); cabin.Scan();
            cabin.OpenComputer(); screen.OpenShipStatus();
            var statusView = screen.GetComponentInChildren<ShipStatusView>();
            Assert.That(statusProvider.ReadStatus().RadarScanning, Is.True);
            Assert.That(statusView.DisplayedText, Does.Contain("SCANNING"));
            Assert.That(statusView.DisplayedText, Does.Contain("ENERGY             -- / --"));
            yield return new WaitForSecondsRealtime(2.4f);
            Assert.That(statusView.DisplayedText, Does.Contain("RADAR MODULE       ONLINE"));
            Assert.That(statusProvider.ReadStatus().RadarUsesRemaining, Is.Null);
            Assert.That(statusProvider.ReadStatus().EnergyCurrent, Is.Null);

            screen.OpenMissionLog();
            var missionView = screen.GetComponentInChildren<MissionLogView>();
            var missionProvider = screen.GetComponentInChildren<ZoneMissionProvider>();
            Assert.That(missionProvider.CurrentMission, Is.Not.Null);
            Assert.That(missionProvider.CurrentMission.isTemplate, Is.True);
            Assert.That(missionView.DisplayedText, Does.Contain(missionProvider.ZoneName));
            Assert.That(missionView.DisplayedText, Does.Contain(missionProvider.CurrentMission.objective));
            Assert.That(missionView.DisplayedText, Does.Contain("PROGRESS NOT CONNECTED"));
            foreach (MissionStep step in missionProvider.CurrentMission.steps)
                Assert.That(step.state, Is.EqualTo(MissionState.Locked));

            // A separate in-memory fixture verifies presentation of configurable data;
            // it is never saved to the project or installed as real mission progress.
            MissionDefinition fixture = ScriptableObject.CreateInstance<MissionDefinition>();
            try
            {
                fixture.objective = "Configurable objective"; fixture.state = MissionState.Active;
                fixture.targetPoiId = "test-poi";
                fixture.steps = new[] {
                    new MissionStep { description = "Step one", state = MissionState.Completed },
                    new MissionStep { description = "Step two", state = MissionState.Active },
                    new MissionStep { description = "Step three", state = MissionState.Locked }
                };
                missionView.Bind(new TestMissionProvider(fixture));
                Assert.That(missionView.DisplayedText, Does.Contain("Configurable objective"));
                Assert.That(missionView.DisplayedText, Does.Contain("TARGET POI: test-poi"));
                Assert.That(missionView.DisplayedText, Does.Contain("[x] Step one  [COMPLETED]"));
                Assert.That(missionView.DisplayedText, Does.Contain("[ ] Step two  [ACTIVE]"));
                Assert.That(missionView.DisplayedText, Does.Contain("[ ] Step three  [LOCKED]"));
            }
            finally { Object.Destroy(fixture); }
            screen.Exit();
        }

        private sealed class TestMissionProvider : IMissionProvider
        {
            public string ZoneName => "TEST ZONE";
            public MissionDefinition CurrentMission { get; }
            public TestMissionProvider(MissionDefinition mission) => CurrentMission = mission;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneFlowController.Instance != null)
            { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
        }
    }
}
