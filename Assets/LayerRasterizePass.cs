using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class LayerRasterizePass : CustomPass
{
    static string ProfilerTag = "Layer Rasterize Pass";

    public static RTHandle rasterDepthXRT;
    public static RTHandle rasterDepthYRT;

    public ComputeShader rasterizeComputeShader;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        int width = Camera.main.pixelWidth;
        int height = Camera.main.pixelHeight;
        
        if (rasterDepthXRT == null) {
            rasterDepthXRT = RTHandles.Alloc(
                width: width,
                height: height,
                colorFormat: GraphicsFormat.R32_UInt,
                enableRandomWrite: true,
                name: "Raster Depth X RT"
            );
        }

        if (rasterDepthYRT == null) {
            rasterDepthYRT = RTHandles.Alloc(
                width: width,
                height: height,
                colorFormat: GraphicsFormat.R32_UInt,
                enableRandomWrite: true,
                name: "Raster Depth Y RT"
            );
        }
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.
        using (new ProfilingScope(ctx.cmd, new ProfilingSampler(ProfilerTag)))
        {
            rasterizeComputeShader.SetTexture(0, "layerDisplacementData", LayerPreparePass.layerDisplacementData);
            rasterizeComputeShader.SetTexture(0, "rasterDepthX", rasterDepthXRT);
            rasterizeComputeShader.SetTexture(0, "rasterDepthY", rasterDepthYRT);

            int width = (int)ctx.hdCamera.screenSize.x;
            int height = (int)ctx.hdCamera.screenSize.y;
            int dispatchTile = 16;

            int dispatchWidth = (width + dispatchTile - 1) / dispatchTile;
            int dispatchHeight = (height + dispatchTile - 1) / dispatchTile;
            ctx.cmd.DispatchCompute(rasterizeComputeShader, 0, dispatchWidth, dispatchHeight, 2);
        }
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}