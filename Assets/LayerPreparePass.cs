using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class LayerPreparePass : CustomPass
{
    static string ProfilerTag = "Layer Prepare Pass";
    public LayerMask passLayer = 0;

    public Material prepareMaterial;

    public static RTHandle layerDisplacementData;
    public static RTHandle modelUV;
    
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        int width = Camera.main.pixelWidth;
        int height = Camera.main.pixelHeight;

        if (layerDisplacementData == null) {
            layerDisplacementData = RTHandles.Alloc(
                width: width,
                height: height,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits: DepthBits.None,
                enableRandomWrite: true,
                name: "Layer Displacement Data RT"
            );
        }

        if (modelUV == null) {
            modelUV = RTHandles.Alloc(
                width: width,
                height: height,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits: DepthBits.None,
                name: "Model UV RT"
            );
        }
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.
        using (new ProfilingScope(ctx.cmd, new ProfilingSampler(ProfilerTag)))
        {
            RenderTargetIdentifier[] RTs = {
                modelUV,
                layerDisplacementData
            };
            CoreUtils.SetRenderTarget(ctx.cmd, RTs, ctx.customDepthBuffer.Value);
            CoreUtils.ClearRenderTarget(ctx.cmd, ClearFlag.All, Color.clear);

            prepareMaterial.enableInstancing = false;
            CustomPassUtils.DrawRenderers(ctx, passLayer, CustomPass.RenderQueueType.All, prepareMaterial, 0);
        }
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}