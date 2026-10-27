using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ScreenLine
{
    // 角色图取自 build.ps1 的 basket-*.png 通配符。上游 packaging\prepare-assets.ps1 会把它们
    // 强制缩成正方形，所以素材按"正方形画布 + 固定相对位置"制作（见 personal\make-clip-assets.ps1）：
    // 角色占高 176/384，顶边在 68/384，底边在 244/384。缺图时 For 返回 null，调用方退回矢量外观。
    internal static class ClipArt
    {
        public const double TopFraction = 68 / 384.0, BottomFraction = 244 / 384.0, HeightFraction = 176 / 384.0;
        static readonly BitmapSource[] portraits = Load();

        public static int Count { get { return portraits.Length; } }

        // 想让角色在界面上高 height 时，正方形画布应渲染的边长。
        public static double BoxFor(double height) { return height / HeightFraction; }

        public static BitmapSource For(int index)
        {
            return portraits.Length == 0 ? null : portraits[Math.Abs(index) % portraits.Length];
        }

        static BitmapSource[] Load()
        {
            var loaded = new List<BitmapSource>();
            for (int index = 1; ; index++)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PicRope.Basket.clip" + index))
                {
                    if (stream == null) break;
                    var image = new BitmapImage();
                    image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit();
                    image.Freeze(); loaded.Add(image);
                }
            }
            return loaded.ToArray();
        }
    }
}
