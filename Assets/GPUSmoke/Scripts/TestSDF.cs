using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class TestSDF : MonoBehaviour
    {
        public Bounds Bounds;
        public LayerMask LayerMask;
        public List<GameObject> Roots;
        public IndexFormat IndexFormat;
        public MeshFilter MeshFilter;
        
        // Start is called before the first frame update
        void Start()
        {

        }
        
        [ContextMenu("Extract Mesh")]
        private void ExtractMesh() {
            MeshFilter.mesh = MeshCombiner.CombineRoot(Roots, Bounds, LayerMask, IndexFormat);
        }

        // Update is called once per frame
        void Update()
        {

        }

        #if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Matrix4x4 prev_matrix = Gizmos.matrix;
            Color prev_color = Gizmos.color;
            
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            Gizmos.color = prev_color;
            Gizmos.matrix = prev_matrix;
        }
        #endif
    }

}
