// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Replaces GrabPass with a CommandBuffer-based screen copy. GrabPass does not work
// in Unity 6 (fails to register _BackgroundTexture* on the global property sheet)
// and is unsupported on WebGPU.

namespace Crest
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class OceanRenderer
    {
        static readonly int sp_BackgroundTexture = Shader.PropertyToID("_BackgroundTexture");

        readonly Dictionary<Camera, CommandBuffer> _backgroundCaptureCBs = new Dictionary<Camera, CommandBuffer>();

        void EnableBackgroundCapture()
        {
            Camera.onPreRender -= OnPreRenderBackgroundCapture;
            Camera.onPreRender += OnPreRenderBackgroundCapture;
        }

        void DisableBackgroundCapture()
        {
            Camera.onPreRender -= OnPreRenderBackgroundCapture;

            foreach (var kvp in _backgroundCaptureCBs)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, kvp.Value);
                }
                kvp.Value.Dispose();
            }
            _backgroundCaptureCBs.Clear();
        }

        void OnPreRenderBackgroundCapture(Camera camera)
        {
            if (!Helpers.MaskIncludesLayer(camera.cullingMask, Layer))
            {
                return;
            }

            if (!_backgroundCaptureCBs.TryGetValue(camera, out var cmd))
            {
                cmd = new CommandBuffer { name = "Crest Background Capture" };
                camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
                _backgroundCaptureCBs[camera] = cmd;
            }

            cmd.Clear();

            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(camera);
            descriptor.useDynamicScale = camera.allowDynamicResolution;
            descriptor.depthBufferBits = 0;
            if (camera.allowHDR)
            {
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            }

            cmd.GetTemporaryRT(sp_BackgroundTexture, descriptor);
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, sp_BackgroundTexture);
        }
    }
}
