using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class LayerApplyPass : CustomPass
{
    static string ProfilerTag = "Layer Apply Pass";

    public Material applyMaterial;
    
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.
        using (new ProfilingScope(ctx.cmd, new ProfilingSampler(ProfilerTag)))
        {
            RenderTargetIdentifier[] RTs = {
                ctx.cameraColorBuffer,
                ctx.cameraNormalBuffer
            };
            CoreUtils.SetRenderTarget(ctx.cmd, RTs, ctx.cameraDepthBuffer);
            CoreUtils.ClearRenderTarget(ctx.cmd, ClearFlag.All, Color.clear);
            

            ctx.propertyBlock.SetTexture("rasterDepthX", LayerRasterizePass.rasterDepthXRT);
            ctx.propertyBlock.SetTexture("rasterDepthY", LayerRasterizePass.rasterDepthYRT);
            ctx.propertyBlock.SetTexture("modelUV", LayerPreparePass.modelUV);

            CoreUtils.DrawFullScreen(ctx.cmd, applyMaterial, ctx.propertyBlock, 1);
        }
    }

    protected override void Cleanup()
    {
        // Cleanup code
        LayerPreparePass.layerDisplacementData.Release();
        LayerPreparePass.modelUV.Release();

        LayerRasterizePass.rasterDepthXRT.Release();
        LayerRasterizePass.rasterDepthYRT.Release();
    }
}