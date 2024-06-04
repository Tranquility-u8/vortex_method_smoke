using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class Postprocessing : MonoBehaviour
{
    public Shader PostProcessingShader;
    private Material _material;
    public Material Mat
    {
        get
        {
            if (PostProcessingShader == null)
            {
                Debug.LogError("没有赋予Shader");
                return null;
            }
            if (!PostProcessingShader.isSupported)
            {
                Debug.LogError("当前Shader不支持");
                return null;
            }

            if (_material == null)
            {
                Material _newMaterial = new Material(PostProcessingShader);
                _newMaterial.hideFlags = HideFlags.HideAndDontSave;
                _material = _newMaterial;
                return _newMaterial;
            }
            return _material;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, Mat);
    }
}

