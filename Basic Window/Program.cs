using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Windows;
using SlimDX.DXGI;

namespace Basic_Window
{
    class Program
    {
        static void Main(string[] args)
        {
            var form = new RenderForm("Basic Window");
            MessagePump.Run(form, () => { });
        }
    }
}
