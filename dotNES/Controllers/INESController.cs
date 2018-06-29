using System.Windows.Forms;

namespace dotNES.Controllers
{
    public interface INESController
    {
        void Write(byte value);

        byte Read();
    }
}
