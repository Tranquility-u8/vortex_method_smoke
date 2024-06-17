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
            public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public FilterSettings FilterSettings = new FilterSettings();
        }

        [System.Serializable]
        public class FilterSettings
        {
            public RenderQueueType RenderQueueType;
            public LayerMask LayerMask;

            public FilterSettings()
            {
                RenderQueueType = RenderQueueType.Transparent;
                LayerMask = 0;
            }
        }

        public WBOITSettings Settings = new WBOITSettings();
        public WBOITRenderPass WBOITRenderPass;


        public override void Create()
        {
            FilterSettings filter = Settings.FilterSettings;
            WBOITRenderPass = new WBOITRenderPass("WBOIT Pass", Settings.RenderPassEvent, filter.RenderQueueType, filter.LayerMask);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData rendering_data)
        {
            WBOITRenderPass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData rendering_data)
        {
            renderer.EnqueuePass(WBOITRenderPass);
        }
    }


    public class WBOITRenderPass : ScriptableRenderPass
    {
        private string _profilerTag;
        private LayerMask _layerMask;
        private FilteringSettings _filteringSettings;
        private RenderStateBlock _renderStateBlock;
        private Material _blendMat = null;

        private RTHandle _colorHandle, _depthHandle; // Camera
        RenderTargetIdentifier m_accumulate;
        RenderTargetIdentifier m_revealage;
        RenderTargetIdentifier m_destination;

        RenderTargetIdentifier[] m_oitBuffers = new RenderTargetIdentifier[2];

        int m_destinationID;
        static readonly int m_accumTexID = Shader.PropertyToID("_AccumTex");
        static readonly int m_revealageTexID = Shader.PropertyToID("_RevealageTex");

        public WBOITRenderPass(string profiler_tag, RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, LayerMask layerMask)
        {
            this.renderPassEvent = renderPassEvent;
            _profilerTag = profiler_tag;

            _layerMask = layerMask;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            _filteringSettings = new FilteringSettings(renderQueueRange, _layerMask);
            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _blendMat = new Material(CoreUtils.CreateEngineMaterial("cdc/FinalBlend"));
            if (_blendMat == null)
            {
                Debug.Log("No shader");
            }
            _blendMat.hideFlags = HideFlags.DontSave;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_accumTexID, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            m_accumulate = new RenderTargetIdentifier(m_accumTexID);
            cmd.SetRenderTarget(m_accumulate);
            cmd.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

            cmd.GetTemporaryRT(m_revealageTexID, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            m_revealage = new RenderTargetIdentifier(m_revealageTexID);
            cmd.SetRenderTarget(m_revealage);
            cmd.ClearRenderTarget(false, true, new Color(1.0f, 1.0f, 1.0f, 1.0f));

            cmd.GetTemporaryRT(m_destinationID, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_destination = new RenderTargetIdentifier(m_destinationID);

            m_oitBuffers[0] = m_accumulate;
            m_oitBuffers[1] = m_revealage;
        }

        public void Setup(RTHandle color, RTHandle depth)
        {
            _colorHandle = color;
            _depthHandle = depth;
        }

        void DoAccumulate(CommandBuffer cmd, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (_filteringSettings.renderQueueRange == RenderQueueRange.transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("WBOIT"), ref renderingData, sortingCriteria);

            cmd.Clear();
            cmd.SetRenderTarget(m_oitBuffers, _depthHandle);
            context.ExecuteCommandBuffer(cmd);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings, ref _renderStateBlock);
        }

        void DoBlend(CommandBuffer cmd, ScriptableRenderContext context)
        {
            cmd.Clear();
            cmd.SetGlobalTexture("_AccumTex", m_accumulate);
            cmd.SetGlobalTexture("_RevealageTex", m_revealage);
            Blit(cmd, _colorHandle, m_destination, _blendMat);
            Blit(cmd, m_destination, _colorHandle);
            context.ExecuteCommandBuffer(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            DoAccumulate(cmd, context, ref renderingData);
            DoBlend(cmd, context);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_accumTexID);
            cmd.ReleaseTemporaryRT(m_revealageTexID);
            cmd.ReleaseTemporaryRT(m_destinationID);
        }


    }
}