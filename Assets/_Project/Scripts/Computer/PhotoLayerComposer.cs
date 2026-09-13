using System.Collections.Generic;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Small CPU still-image compositor. Cached readable kit copies; no camera or per-frame readback.</summary>
    public sealed class PhotoLayerComposer
    {
        public const int Width = 640, Height = 360;
        private readonly Dictionary<Texture2D, Color32[]> pixels = new();
        private readonly Color[] output = new Color[Width * Height];
        private readonly float[] subject = new float[Width * Height];
        private readonly float[] covered = new float[Width * Height];
        public void Begin() { System.Array.Fill(output, new Color(.02f,.07f,.1f,1)); System.Array.Clear(subject,0,subject.Length); System.Array.Clear(covered,0,covered.Length); }
        public void Draw(Texture2D texture, Rect rect, float opacity, bool isSubject = false, bool occludes = false)
        {
            if (texture == null || rect.width <= 0 || rect.height <= 0) return;
            if (!pixels.TryGetValue(texture, out var source)) { source = texture.GetPixels32(); pixels.Add(texture,source); }
            int x0 = Mathf.Clamp(Mathf.FloorToInt(rect.xMin * Width),0,Width), x1 = Mathf.Clamp(Mathf.CeilToInt(rect.xMax * Width),0,Width);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(rect.yMin * Height),0,Height), y1 = Mathf.Clamp(Mathf.CeilToInt(rect.yMax * Height),0,Height);
            for (int y=y0;y<y1;y++) for (int x=x0;x<x1;x++)
            {
                int sx=Mathf.Clamp((int)(((x+.5f)/Width-rect.x)/rect.width*texture.width),0,texture.width-1);
                int sy=Mathf.Clamp((int)(((y+.5f)/Height-rect.y)/rect.height*texture.height),0,texture.height-1);
                Color c=source[sy*texture.width+sx]; int i=y*Width+x; float a=c.a*Mathf.Clamp01(opacity);
                if (isSubject) subject[i]=c.a;
                if (occludes) covered[i]=1-(1-covered[i])*(1-a);
                output[i]=new Color(c.r*a+output[i].r*(1-a),c.g*a+output[i].g*(1-a),c.b*a+output[i].b*(1-a),1);
            }
        }
        public Texture2D Finish(out float coverage,out float occlusion)
        {
            float total=0, hidden=0;
            for(int i=0;i<subject.Length;i++) { total+=subject[i];hidden+=subject[i]*covered[i]; }
            coverage=total/subject.Length; occlusion=total>0?hidden/total:0;
            var image=new Texture2D(Width,Height,TextureFormat.RGB24,false); image.SetPixels(output);image.Apply();return image;
        }
    }
}
