using UnityEngine;

namespace SoulBlocks
{
    public sealed class LabSurface : MonoBehaviour
    {
        public Color baseColor = new Color(.025f, .025f, .035f);
        public bool darkPath;
        public float LitUntil { get; private set; }
        public bool Lit => Time.time < LitUntil;
        Renderer view;
        public void Illuminate(float seconds) => LitUntil = Mathf.Max(LitUntil, Time.time + seconds);
        public void ResetLight() => LitUntil = 0;
        public void Refresh(bool inverse)
        {
            if (view == null) view = GetComponent<Renderer>();
            var color = darkPath && Lit ? new Color(1, .8f, .12f) : baseColor;
            if (inverse) color = new Color(1 - color.r, 1 - color.g, 1 - color.b);
            var block = new MaterialPropertyBlock();
            view.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", darkPath && Lit ? color * .8f : Color.black);
            view.SetPropertyBlock(block);
        }
    }
}
