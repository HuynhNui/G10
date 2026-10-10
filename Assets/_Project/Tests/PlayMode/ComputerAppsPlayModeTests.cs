using System.Collections;
using System.Reflection;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class ComputerAppsPlayModeTests
    {
        private LegacyCabinTestSession session;
        [UnitySetUp] public IEnumerator Setup()
        { session = new LegacyCabinTestSession(); yield return session.Begin(); }

        [UnityTest]
        public IEnumerator AppsReadProvidersWithoutInventingGameplayData()
        {
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenPhotoLab();
            var photoView = screen.GetComponentInChildren<PhotoLabView>(true);
            var repository = screen.GetComponentInChildren<EmptyPhotoRepository>(true);
            Assert.That(photoView, Is.Not.Null); Assert.That(repository, Is.Not.Null);
            // This fixture deliberately exercises offline presentation. The live cabin's real
            // camera is covered elsewhere; do not pretend its connected telemetry is unknown.
            var repositoryField = typeof(PhotoLabView).GetField("repository", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(repositoryField, Is.Not.Null);
            repositoryField.SetValue(photoView, repository); photoView.Refresh();
            Assert.That(repository.Photos.Count, Is.Zero);
            Assert.That(repository.CameraOnline, Is.False);
            Assert.That(photoView.DisplayedText, Does.Contain("NO IMAGES RECORDED"));
            Assert.That(photoView.DisplayedText, Does.Contain("CAMERA MODULE OFFLINE"));

            var statusProvider = screen.GetComponentInChildren<ExistingShipStatusProvider>(true);
            var statusView = screen.GetComponentInChildren<ShipStatusView>(true);
            statusProvider.photoCapture = null; statusView.Expedition = null;
            Assert.That(statusProvider.Radar, Is.SameAs(cabin.Radar));
            Assert.That(statusProvider.ReadStatus().RadarUsesRemaining, Is.Null);
            cabin.OpenRadar();
            cabin.OpenComputer(); screen.OpenShipStatus();
            // Closing Radar now intentionally cancels its foreground sweep. Trigger the existing
            // data API after the window change to test provider-to-view scanning presentation.
            cabin.Scan(); statusView.Refresh();
            Assert.That(statusProvider.ReadStatus().RadarScanning, Is.True);
            Assert.That(statusView.DisplayedText, Does.Contain("SCANNING"));
            Assert.That(statusView.DisplayedText, Does.Contain("ENERGY             -- / --"));
            yield return new WaitForSecondsRealtime(2.4f);
            Assert.That(statusView.DisplayedText, Does.Contain("RADAR MODULE       ONLINE"));
            Assert.That(statusProvider.ReadStatus().RadarUsesRemaining, Is.Null);
            Assert.That(statusProvider.ReadStatus().EnergyCurrent, Is.Null);

            screen.OpenMissionLog();
            var missionView = screen.GetComponentInChildren<MissionLogView>(true);
            var missionProvider = screen.GetComponentInChildren<ZoneMissionProvider>(true);
            missionView.Expedition = null; missionView.Bind(missionProvider);
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
        public IEnumerator Cleanup() { if (session != null) yield return session.End(); }
    }
}
