using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessWin;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    public class SplashProgressControlEventArgs : EventArgs
    {
        public SplashProgressControlContent Content
        {
            get;
        }

        public SplashProgressControlEventArgs(SplashProgressControlContent content)
        {
            Content = content;
        }
    }
}
