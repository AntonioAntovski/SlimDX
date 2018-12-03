using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Core;
using Buffer = SlimDX.Direct3D11.Buffer;
using Core.Vertex;
using System.Diagnostics;
using System.Windows.Forms;

namespace PointList
{
    public class PointList : D3DApp
    {
        Buffer vertexBuffer;

        InputLayout inputLayout;

        Effect effect;
        EffectTechnique effectTechnique;
        EffectMatrixVariable fxWVP;

        Matrix world;
        Matrix view;
        Matrix projection;

        float theta, phi, radius;

        bool disposed;

        public PointList(IntPtr hInstance) : base(hInstance)
        {
            vertexBuffer = null;
            inputLayout = null;

            effect = null;
            effectTechnique = null;
            fxWVP = null;

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            theta = 0.5f * MathF.PI;
            phi = 1.5f * MathF.PI;
            radius = 5.0f;
        }

        public override void DrawScene()
        {
            base.DrawScene();

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.White);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexPC.Stride, 0));

            var wvp = world * view * projection;
            fxWVP.SetMatrix(wvp);

            for(int i = 0; i < effectTechnique.Description.PassCount; i++)
            {
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.Draw(8, 0);
            }
            SwapChain.Present(0, PresentFlags.None);
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }
            BuildGeometryBuffers();
            BuildFX();
            BuildVertexLayout();
            return true;
        }

        public override void OnResize()
        {
            base.OnResize();
            projection = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt)
        {
            base.UpdateScene(dt);

            var x = radius * MathF.Sin(phi) * MathF.Cos(theta);
            var z = radius * MathF.Sin(phi) * MathF.Sin(theta);
            var y = radius * MathF.Cos(phi);
            
            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            view = Matrix.LookAtLH(pos, target, up);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    Util.ReleaseCom(ref vertexBuffer);
                    Util.ReleaseCom(ref inputLayout);
                    Util.ReleaseCom(ref effect);
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }

        private void BuildGeometryBuffers()
        {
            var vertices = new[]
            {
                new VertexPC(new Vector3(2, 1, 0), Color.Black),
                new VertexPC(new Vector3(1, 1, 0), Color.Black),
                new VertexPC(new Vector3(0, 1, 0), Color.Black),
                new VertexPC(new Vector3(1, 0, 0), Color.Black),
                new VertexPC(new Vector3(0, 0, 0), Color.Black),
                new VertexPC(new Vector3(0, -1, 0), Color.Black),
                new VertexPC(new Vector3(-1, -1, 0), Color.Black),
                new VertexPC(new Vector3(-2, -1, 0), Color.Black)
            };

            var vbd = new BufferDescription(
                VertexPC.Stride * vertices.Length,
                ResourceUsage.Default,
                BindFlags.VertexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            vertexBuffer = new Buffer(Device, new DataStream(vertices, true, true), vbd);
        }

        private void BuildFX()
        {
            ShaderBytecode compiledShader = ShaderBytecode.CompileFromFile("D:\\3Shape\\SlimDX Antonio\\SlimDX Tutorial\\PointList\\point_list.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
            effect = new Effect(Device, compiledShader);

            effectTechnique = effect.GetTechniqueByName("ColorTech");
            fxWVP = effect.GetVariableByName("gWorldViewProj").AsMatrix();
            Util.ReleaseCom(ref compiledShader);
        }

        private void BuildVertexLayout()
        {
            var vertexDesc = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, InputClassification.PerVertexData, 0)
            };
            var passDesc = effectTechnique.GetPassByIndex(0).Description;
            inputLayout = new InputLayout(Device, passDesc.Signature, vertexDesc);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Configuration.EnableObjectTracking = true;
            var app = new PointList(Process.GetCurrentProcess().Handle);
            if (!app.Init())
            {
                return;
            }
            app.Run();
        }
    }
}
