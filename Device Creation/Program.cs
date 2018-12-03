using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlimDX;
using SlimDX.Windows;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;

namespace Device_Creation
{
    class Program
    {
        static void Main(string[] args)
        {
            var form = new RenderForm("Device Creation");
            SwapChain swapChain;
            Device device;
            Viewport viewport;
            RenderTargetView renderTargetView;

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

            using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
                renderTargetView = new RenderTargetView(device, resource);

            var context = device.ImmediateContext;
            viewport = new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height);
            context.OutputMerger.SetTargets(renderTargetView);
            context.Rasterizer.SetViewports(viewport);

            MessagePump.Run(form, () => {
                context.ClearRenderTargetView(renderTargetView, Color.Blue);
                swapChain.Present(0, PresentFlags.None);
            });

            renderTargetView.Dispose();
            swapChain.Dispose();
            device.Dispose();
        }
    }
}
