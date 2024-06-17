using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class MeshCombiner
    {
        public static Mesh Combine(IEnumerable<MeshFilter> meshes, IndexFormat index_format)
        {
            var combines = new List<CombineInstance>();

            foreach (MeshFilter mesh in meshes)
            {
                if (mesh.sharedMesh == null)
                    continue;

                for (int i = 0; i < mesh.sharedMesh.subMeshCount; ++i)
                {
                    var combine = new CombineInstance();
                    combine.mesh = mesh.sharedMesh;
                    combine.transform = mesh.transform.localToWorldMatrix;
                    combine.subMeshIndex = i;
                    combines.Add(combine);
                }
            }

            var combine_mesh = new Mesh();
            combine_mesh.indexFormat = index_format;
            combine_mesh.CombineMeshes(combines.ToArray());
            return combine_mesh;
        }

        private static void Iterate_(Transform self, Func<Transform, bool> selector)
        {
            if (!selector(self))
                return;
            foreach (Transform child in self)
                Iterate_(child, selector);
        }

        public static Mesh CombineRoot(IEnumerable<GameObject> roots, Bounds bounds, LayerMask layer_mask, IndexFormat index_format)
        {
            var meshes = new List<MeshFilter>();
            foreach (GameObject obj in roots)
            {
                Iterate_(obj.transform, (Transform t) =>
                {
                    if ((layer_mask & (1 << t.gameObject.layer)) == 0)
                        return false;

                    var renderer = t.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (!bounds.Intersects(renderer.bounds))
                            return true;

                        var mesh_filter = t.GetComponent<MeshFilter>();
                        if (mesh_filter != null)
                        {
                            meshes.Add(mesh_filter);
                        }
                    }

                    return true;
                });
            }

            return Combine(meshes, index_format);
        }
    }

}