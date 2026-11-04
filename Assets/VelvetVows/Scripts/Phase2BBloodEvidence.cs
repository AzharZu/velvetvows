using System.Collections;
using UnityEngine;

namespace VelvetVows
{
    public sealed class Phase2BBloodEvidence : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        private Material[] runtimeMaterials;
        public bool Cleaned { get; private set; }
        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
            runtimeMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) runtimeMaterials[i] = renderers[i].material;
        }
        public IEnumerator CleanVisible(float seconds = 2f)
        {
            if (Cleaned) yield break;
            var originalScale = transform.localScale;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float f = Mathf.Clamp01(t / seconds);
                transform.localScale = new Vector3(originalScale.x * (1 - .8f * f), originalScale.y, originalScale.z * (1 - .8f * f));
                foreach (var material in runtimeMaterials)
                {
                    if (material.HasProperty("_BaseColor"))
                    {
                        var color = material.GetColor("_BaseColor"); color.a = 1 - f;
                        material.SetColor("_BaseColor", color);
                    }
                }
                yield return null;
            }
            Cleaned = true;
            foreach (var renderer in renderers) renderer.enabled = false;
        }
        public void ResetEvidence()
        {
            Cleaned = false;
            transform.localScale = Vector3.one;
            foreach (var renderer in renderers) renderer.enabled = true;
            foreach (var material in runtimeMaterials)
                if (material.HasProperty("_BaseColor")) { var c = material.GetColor("_BaseColor"); c.a = 1; material.SetColor("_BaseColor", c); }
        }
    }
}
