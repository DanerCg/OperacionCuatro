using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OperacionCuatro
{
    /// <summary>
    /// Aplica el brillo del contenido mediante post-exposición (no toca el monitor).
    /// </summary>
    public class BrightnessController : MonoBehaviour
    {
        private Volume volume;
        private ColorAdjustments adjustments;

        private void Awake()
        {
            var go = new GameObject("[OC_BrightnessVolume]");
            go.transform.SetParent(transform, false);

            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -100f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "OC_RuntimeBrightness";

            adjustments = profile.Add<ColorAdjustments>(true);
            if (adjustments != null)
            {
                adjustments.active = true;
                adjustments.postExposure.overrideState = true;
            }

            volume.sharedProfile = profile;

            GameOptions.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            GameOptions.Changed -= Apply;
        }

        public void Apply()
        {
            if (adjustments == null) return;
            adjustments.postExposure.Override(GameOptions.PostExposure);
        }
    }
}
