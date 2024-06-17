using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class MeshUtil
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
            combine_mesh.RecalculateBounds();
            return combine_mesh;
        }

        private static void Iterate_(Transform self, Func<Transform, bool> selector)
        {
            if (!selector(self))
                return;
            foreach (Transform child in self)
                Iterate_(child, selector);
        }

        public static Mesh Combine(IEnumerable<GameObject> roots, Bounds bounds, LayerMask layer_mask, IndexFormat index_format)
        {
            var meshes = new List<MeshFilter>();
            foreach (GameObject obj in roots)
            {
                Iterate_(obj.transform, (Transform t) =>
                {
                    if (!t.gameObject.activeInHierarchy)
                        return false;

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

        public static Bounds GetSubBounds(Bounds bounds, Mesh mesh, float margin)
        {
            Bounds sub_bounds = new Bounds(), tri_bounds = new Bounds();
            bool flag = false;
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;
            for (int i = 0; i < triangles.Count(); i += 3)
            {
                Vector3 v0 = vertices[triangles[i + 0]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];
                tri_bounds.SetMinMax(
                    Vector3.Min(Vector3.Min(v0, v1), v2),
                    Vector3.Max(Vector3.Max(v0, v1), v2)
                );
                if (bounds.Intersects(tri_bounds))
                {
                    if (flag == false)
                    {
                        sub_bounds = tri_bounds;
                        flag = true;
                    }
                    else
                        sub_bounds.Encapsulate(tri_bounds);
                }
            }
            if (flag)
            {
                var r_vec3 = new Vector3(margin, margin, margin);
                sub_bounds.SetMinMax(
                    Vector3.Max(sub_bounds.min - r_vec3, bounds.min),
                    Vector3.Min(sub_bounds.max + r_vec3, bounds.max)
                );
            }
            return sub_bounds;
        }
    }

}