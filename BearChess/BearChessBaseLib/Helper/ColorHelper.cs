using System.Windows.Media;

namespace www.SoLaNoSoft.com.BearChessWin;

public static class ColorHelper
{
    public static SolidColorBrush GetSolidColorBrush(int value)
    {
        var color = System.Drawing.Color.FromArgb(value);
        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
    }
}