using G10.Prototype.Computer;
using G10.Prototype.Tutorial;

namespace G10.Prototype.Tests
{
    internal static class TutorialTestSave
    {
        // Regression fixtures exercise returning players; fresh onboarding has its own fixture.
        public static void SeedReturningPlayer() => ExpeditionSaveStore.Write(new ExpeditionSave
        { tutorial = new TutorialProgressState { completed = true } });
    }
}
