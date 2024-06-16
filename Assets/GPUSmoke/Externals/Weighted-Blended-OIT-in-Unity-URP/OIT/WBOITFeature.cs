using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace GPUSmoke
{
    public class WBOITFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class WBOITSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public FilterSettings filterSettings = new FilterSettings();
        }

        [System.Serializable]
        public class FilterSettings
        {
            public RenderQueueType renderQueueType;
            public LayerMask layerMask;

            public FilterSettings()
            {
                renderQueueType = RenderQueueType.Transparent;
                layerMask = 0;
            }
        }

        public WBOITSettings settings = new WBOITSettings();
        public WBOITRenderPass m_WBOITRenderPass;


        public override void Create()
        {
            FilterSettings filter = settings.filterSettings;
            m_WBOITRenderPass = new WBOITRenderPass("WBOIT Pass", settings.renderPassEvent, filter.renderQueueType, filter.layerMask);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            m_WBOITRenderPass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_WBOITRenderPass);
        }
    }

}