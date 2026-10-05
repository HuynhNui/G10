# Helm minimap removal

Removed only NavigationPanel/MiniMap (including its survey overlay) and
NavigationPanel/OpenMapHotspot from Zone01. The texture/artwork, full map,
navigation controls and top-bar Map tab remain unchanged. Serialized reference
inspection found no references to these targets outside their parent hierarchy.

CabinStationEditor no longer creates these objects. PhotoSurveyEditor no longer
adds the removed minimap overlay. NavigationArtworkEditor no longer expects or
positions them, preventing missing-reference failures during setup.

Tutorial already requires opening an actual map/world-map panel; it does not
depend on the helm minimap. No tutorial step IDs, save state or progression
conditions needed changing. Updated the obsolete director comment and added
regression checks: the removed objects are absent, staying at Helm cannot
satisfy Map practice, and clicking WatercolorHUD/Map advances the real tutorial.

Validation: Unity compilation has zero errors; 18/18 PlayMode tests passed:
TutorialPlayModeTests 6/6, MainMenuTutorialResetPlayModeTests 7/7,
MissionDepthAuthoringPlayModeTests 3/3, MissionLogScrollPlayModeTests 2/2.
The full tutorial photo/send/collection/hull-upgrade route passed.
git diff --check passed. No commit/push.

Files changed for this request:
- Assets/_Project/Scenes/Gameplay/Zone01.unity
- Assets/_Project/Scripts/Editor/CabinStationEditor.cs
- Assets/_Project/Scripts/Editor/PhotoSurveyEditor.cs
- Assets/_Project/Scripts/Editor/NavigationArtworkEditor.cs
- Assets/_Project/Scripts/Tutorial/ZoneOneTutorialDirector.cs (comment only)
- Assets/_Project/Tests/PlayMode/TutorialPlayModeTests.cs
- Docs/AI/HelmMinimapRemoval.md
- Docs/AI/MissionDepthAndScroll.md (updated validation status)

Existing depth/scroll and font-cache worktree changes were preserved.
