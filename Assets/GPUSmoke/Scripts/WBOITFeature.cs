using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUSmoke
{
    public class WBOITFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class WBOITSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public FilterSettings FilterSettings = new();
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

        public Shader BlendShader;
        public WBOITSettings Settings = new();
        public WBOITRenderPass WBOITRenderPass;


        public override void Create()
        {
            FilterSettings filter = Settings.FilterSettings;
            WBOITRenderPass = new WBOITRenderPass(BlendShader, Settings.RenderPassEvent, filter.RenderQueueType, filter.LayerMask);
        }

        /* public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData rendering_data)
        {
            WBOITRenderPass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        } */

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData rendering_data)
        {
            renderer.EnqueuePass(WBOITRenderPass);
        }
    }


    public class WBOITRenderPass : ScriptableRenderPass
    {
        private LayerMask _layerMask;
        private FilteringSettings _filteringSettings;
        private RenderStateBlock _renderStateBlock;
        private readonly Material _blendMaterial = null;

        // private RTHandle _colorHandle, _depthHandle;

        private RenderTextureDescriptor _accumDescriptor, _revealDescriptor, _destDescriptor;
        private RTHandle _accumHandle, _revealHandle, _destHandle;

        private readonly RenderTargetIdentifier[] _identifiers = new RenderTargetIdentifier[2];

        public WBOITRenderPass(Shader blend_shader, RenderPassEvent render_pass_event, RenderQueueType render_queue_type, LayerMask layer_mask)
        {
            base.renderPassEvent = render_pass_event;

            _layerMask = layer_mask;
            RenderQueueRange render_queue_range = (render_queue_type == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            _filteringSettings = new FilteringSettings(render_queue_range, _layerMask);
            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _blendMaterial = new Material(blend_shader);
            if (_blendMaterial == null)
            {
                Debug.Log("No shader");
            }
            _blendMaterial.hideFlags = HideFlags.DontSave;

            _accumDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf);
            _revealDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.R8);
            _destDescriptor = new(Screen.width, Screen.height, RenderTextureFormat.DefaultHDR);
        }

        /* public void Setup(RTHandle color_handle, RTHandle depth_handle) {
            _colorHandle = color_handle;
            _depthHandle = depth_handle;
        } */

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cam_tex_descriptor)
        {
            _accumDescriptor.width = cam_tex_descriptor.width;
            _accumDescriptor.height = cam_tex_descriptor.height;
            _revealDescriptor.width = cam_tex_descriptor.width;
            _revealDescriptor.height = cam_tex_descriptor.height;
            _destDescriptor.width = cam_tex_descriptor.width;
            _destDescriptor.height = cam_tex_descriptor.height;

            RenderingUtils.ReAllocateIfNeeded(ref _accumHandle, _accumDescriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _revealHandle, _revealDescriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _destHandle, _destDescriptor);

            _identifiers[0] = _accumHandle;
            _identifiers[1] = _revealHandle;

            _blendMaterial.SetTexture("_AccumTexture", _accumHandle);
            _blendMaterial.SetTexture("_RevealTexture", _revealHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData rendering_data)
        {
            var color_handle = rendering_data.cameraData.renderer.cameraColorTargetHandle;
            var depth_handle = rendering_data.cameraData.renderer.cameraDepthTargetHandle;

            if (rendering_data.cameraData.camera.cameraType != CameraType.Game)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();

            SortingCriteria sorting_criteria = (_filteringSettings.renderQueueRange == RenderQueueRange.transparent)
                ? SortingCriteria.CommonTransparent
                : rendering_data.cameraData.defaultOpaqueSortFlags;
            DrawingSettings draw_settings = CreateDrawingSettings(new ShaderTagId("WBOIT"), ref rendering_data, sorting_criteria);

            // Before Render
            cmd.Clear();
            cmd.SetRenderTarget(_accumHandle);
            cmd.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            cmd.SetRenderTarget(_revealHandle);
            cmd.ClearRenderTarget(false, true, new Color(1.0f, 1.0f, 1.0f, 1.0f));
            cmd.SetRenderTarget(_identifiers, depth_handle);
            context.ExecuteCommandBuffer(cmd);

            // Render
            context.DrawRenderers(rendering_data.cullResults, ref draw_settings, ref _filteringSettings, ref _renderStateBlock);

            // Blend
            cmd.Clear();
            Blit(cmd, color_handle, _destHandle, _blendMaterial);
            Blit(cmd, _destHandle, color_handle);
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(_blendMaterial);
            }
            else
            {
                Object.DestroyImmediate(_blendMaterial);
            }
#else
            Object.Destroy(_blendMaterial);
#endif
            DestroyUtil.Release(ref _accumHandle);
            DestroyUtil.Release(ref _revealHandle);
            DestroyUtil.Release(ref _destHandle);
        }

    }
}