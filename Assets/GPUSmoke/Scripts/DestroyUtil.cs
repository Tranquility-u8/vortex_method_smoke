using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke {
    class DestroyUtil {
        public static void Release(ref ComputeBuffer buffer) {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
        }
        public static void Release(ref RenderTexture texture) {
            if (texture != null) {
                texture.Release();
                texture = null;
            }            
        }

        public static void Release(ref RTHandle texture) {
            if (texture != null) {
                texture.Release();
                texture = null;
            }            
        }
    }
}