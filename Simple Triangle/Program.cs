using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace Simple_Triangle
{
    class Program
    {
        static void Main(string[] args)
        {
            var form = new RenderForm("Simple Triangle");
            SwapChain swapChain;
            Device device;
            RenderTargetView renderTargetView;
            Viewport viewport;
            VertexShader vertexShader;
            PixelShader pixelShader;
            ShaderSignature shaderSignature;

            var description = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = form.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, description, out device, out swapChain);

            var context = device.ImmediateContext;

            using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
                renderTargetView = new RenderTargetView(device, resource);

            viewport = new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height);

            context.OutputMerger.SetTargets(renderTargetView);
            context.Rasterizer.SetViewports(viewport);

            using (var bytecode = ShaderBytecode.CompileFromFile(@"D:\3Shape\SlimDX Antonio\SlimDX Tutorial\Simple Triangle\triangle.fx", "VShader", "vs_4_0", ShaderFlags.None, EffectFlags.None))
            {
                shaderSignature = ShaderSignature.GetInputSignature(bytecode);
                vertexShader = new VertexShader(device, bytecode);
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"D:\3Shape\SlimDX Antonio\SlimDX Tutorial\Simple Triangle\triangle.fx", "PShader", "ps_4_0", ShaderFlags.None, EffectFlags.None))
                pixelShader = new PixelShader(device, bytecode);

            var vertices = new DataStream(12 * 3, true, true);
            vertices.Write(new Vector3(0.0f, 0.5f, 0.5f));
            vertices.Write(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Write(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Position = 0;

            var elements = new[] { new InputElement("POSITION", 0, Format.R32G32B32_Float, 0) };
            var layout = new InputLayout(device, shaderSignature, elements);
            var vertexBuffer = new Buffer(device, vertices, 12 * 3, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 12, 0));

            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            MessagePump.Run(form, () =>
            {
                context.ClearRenderTargetView(renderTargetView, Color.White);
                context.Draw(3, 0);
                swapChain.Present(0, PresentFlags.None);
            });

            swapChain.Dispose();
            device.Dispose();
            renderTargetView.Dispose();
            shaderSignature.Dispose();
            layout.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();
            vertexBuffer.Dispose();
        }
    }
}
