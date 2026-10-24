using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Bounded still-photo composition evaluated at shutter time; no observation UI.</summary>
    public sealed class ZoneOneSurveyFrame
    {
        public struct Layer
        {
            public Texture2D Texture;
            public Rect Rect;
            public Color Color;
            public bool Subject, Occludes;
            public float Distance;
        }
        public const int Capacity = 9;
        public readonly Layer[] Layers = new Layer[Capacity];
        private readonly Layer[] objects = new Layer[3];
        public int Count { get; private set; }
        public MapPoi Poi { get; private set; }
        public bool HasSubject { get; private set; }
        public bool SubjectSettled { get; private set; }
        public float Visibility { get; private set; }
        public float LightLevel { get; private set; }
        public Rect SubjectRect { get; private set; }

        public void Build(PhotoCaptureService capture, float clock, float light, float observation, float response)
        {
            Count = 0; Poi = null; HasSubject = false; SubjectRect = default;
            LightLevel = Mathf.Clamp01(light);
            var nav = capture.navigation;
            var profile = capture.profile;
            var survey = capture.survey;
            bool roots = survey.MissionRuntime?.config?.zoneId == "Zone02";
            float deep = Mathf.InverseLerp(250, 460, nav.Depth);
            Color water = Color.Lerp(new Color(.74f, .84f, .91f), new Color(.34f, .55f, .66f), deep);
            water = Color.Lerp(water, Color.white, light * .8f);
            Add(nav.Depth < profile.shallowDepth ? profile.shallow : nav.Depth < profile.deepDepth ? profile.mid : profile.deep,
                new Rect(0, 0, 1, 1), water);
            if (roots)
            {
                // Existing organic art becomes large, still background forms; never adds contacts or subjects.
                Add(profile.kelp, new Rect(-.30f, -.12f, .85f, 1.35f), new Color(.28f, .42f, .39f, .65f));
                Add(profile.kelp, new Rect(.55f, -.2f, .85f, 1.45f), new Color(.22f, .35f, .32f, .55f));
            }
            bool found = survey.TryGetPhotoContact(nav, profile.visibleDistance, profile.fieldOfView, out var poi, out var world);
            Poi = poi;
            float shelf = Mathf.Max(deep * .85f, found ? 1 : 0);
            // Re-use the supplied seabed art: the lamp reveals detail on the existing route, not a new POI.
            Add(profile.seabed, new Rect(-.025f, -.02f, 1.05f, 1), Tint(water, shelf * Mathf.Lerp(.18f, .67f, light)));
            int objectCount = 0;
            if (found)
            {
                float distance = Vector3.Distance(nav.WorldPosition, world);
                Visibility = Mathf.Clamp01(1 - distance / profile.visibleDistance * .7f) *
                    Mathf.Lerp(nav.Depth > profile.deepDepth ? .55f : .9f, .96f, light);
                HasSubject = capture.TryProject(nav.WorldPosition, nav.Heading, world, profile.creatureHeight, out var subjectRect);
                bool algae = poi.id == "zone01-left";
                float wave = Mathf.Sin(clock * (algae ? 1.15f : .8f));
                SubjectSettled = algae || Mathf.Abs(Mathf.Cos(clock * .8f)) < .45f;
                float scale = 1 + wave * (algae ? .012f : .025f);
                Vector2 center = subjectRect.center + new Vector2(wave * (algae ? .006f : .012f), 0);
                subjectRect.size *= scale; subjectRect.center = center; SubjectRect = subjectRect;
                var subject = survey.MissionRuntime.ContentForPoi(poi.id);
                var texture = subject != null && subject.Image != null ? subject.Image : profile.creature;
                Color tint = Color.Lerp(water, Color.white, algae ? .75f : .35f);
                if (algae) tint = Color.Lerp(tint, new Color(.68f, 1, .91f), observation * (.16f + .12f * wave) + response * .16f);
                if (HasSubject) objects[objectCount++] = new Layer { Texture = texture, Rect = subjectRect,
                    Color = Tint(tint, Visibility), Distance = distance, Subject = true };
                AddProp(capture, world + new Vector3(-9, 7, -3), algae ? profile.kelp : profile.rock, algae ? 20 : 13, water, ref objectCount);
                AddProp(capture, world + new Vector3(7, -8, -5), profile.rock, 10, water, ref objectCount);
            }
            else { Visibility = 0; SubjectSettled = false; }
            for (int i = 1; i < objectCount; i++)
            {
                var item = objects[i]; int j = i - 1;
                while (j >= 0 && objects[j].Distance < item.Distance) { objects[j + 1] = objects[j]; j--; }
                objects[j + 1] = item;
            }
            bool painted = false;
            for (int i = 0; i < objectCount; i++)
            {
                var layer = objects[i]; layer.Occludes = painted && !layer.Subject;
                Layers[Count++] = layer; painted |= layer.Subject;
            }
            Add(profile.fog, new Rect(0, 0, 1, 1), Tint(water, Mathf.Lerp(.12f + deep * .20f, .07f, light)));
            Add(profile.particles, new Rect(-.025f + Mathf.Sin(clock * .09f) * .02f,
                -.025f + Mathf.Sin(clock * .065f) * .02f, 1.05f, 1.05f), Tint(Color.white, .11f + light * .15f));
        }
        private void AddProp(PhotoCaptureService capture, Vector3 world, Texture2D texture, float height, Color water, ref int count)
        {
            if (capture.TryProject(capture.navigation.WorldPosition, capture.navigation.Heading, world, height, out var rect))
                objects[count++] = new Layer { Texture = texture, Rect = rect, Color = Tint(water, .9f),
                    Distance = Vector3.Distance(capture.navigation.WorldPosition, world) };
        }
        private void Add(Texture2D texture, Rect rect, Color tint)
        { if (texture != null) Layers[Count++] = new Layer { Texture = texture, Rect = rect, Color = tint }; }
        private static Color Tint(Color color, float alpha) { color.a = alpha; return color; }
    }
}
