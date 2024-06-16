using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace GPUSmoke {

    [System.Serializable]
    public class DrawConfig {
        public Material Material = null;
        public Camera Camera = null;
        public ShadowCastingMode CastShadows = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        [SingleLayer] public int Layer = 0;
    }

}