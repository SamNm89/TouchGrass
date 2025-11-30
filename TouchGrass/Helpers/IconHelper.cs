using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TouchGrass.Helpers
{
    public static class IconHelper
    {
        // Constants for SHGetImageList
        public const int SHIL_LARGE = 0x0;
        public const int SHIL_SMALL = 0x1;
        public const int SHIL_EXTRALARGE = 0x2;
        public const int SHIL_SYSSMALL = 0x3;
        public const int SHIL_JUMBO = 0x4;

        // Constants for SHGetFileInfo
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SYSICONINDEX = 0x4000;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;
            public int yBitmap;
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig]
            int Draw(ref IMAGELISTDRAWPARAMS pimldp);
            [PreserveSig]
            int Remove(int i);
            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        public static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public static string? ExtractIconToPng(string filePath, string outputFolder)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                // Handle shortcuts (.url and .lnk) to find the real icon target
                string extension = Path.GetExtension(filePath);
                if (extension.Equals(".url", StringComparison.OrdinalIgnoreCase) || 
                    extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    string? targetPath = GetIconLocation(filePath);
                    if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                    {
                        filePath = targetPath;
                    }
                }

                IntPtr hIcon = IntPtr.Zero;
                
                // 1. Try to get Jumbo Icon (256x256)
                hIcon = GetIconFromShell(filePath, SHIL_JUMBO);

                // 2. If failed, try Extra Large (48x48)
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = GetIconFromShell(filePath, SHIL_EXTRALARGE);
                }

                // 3. If still failed, fallback to standard large icon
                if (hIcon == IntPtr.Zero)
                {
                    SHFILEINFO shinfo = new SHFILEINFO();
                    SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
                    hIcon = shinfo.hIcon;
                }

                if (hIcon == IntPtr.Zero) return null;

                using (Icon icon = Icon.FromHandle(hIcon))
                {
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    string uniqueName = $"{Guid.NewGuid()}.png";
                    string outputPath = Path.Combine(outputFolder, uniqueName);

                    using (Bitmap bitmap = icon.ToBitmap())
                    using (Bitmap trimmedBitmap = TrimBitmap(bitmap))
                    {
                        trimmedBitmap.Save(outputPath, ImageFormat.Png);
                    }

                    DestroyIcon(hIcon);
                    return outputPath;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IntPtr GetIconFromShell(string path, int imageListSize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            // Get the index of the icon
            SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_SYSICONINDEX);

            IImageList imageList;
            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            
            int result = SHGetImageList(imageListSize, ref iidImageList, out imageList);

            if (result == 0 && imageList != null)
            {
                IntPtr hIcon = IntPtr.Zero;
                imageList.GetIcon(shinfo.iIcon, 1, ref hIcon); // ILD_TRANSPARENT = 1
                return hIcon;
            }

            return IntPtr.Zero;
        }

        private static string? GetIconLocation(string path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            // SHGFI_ICONLOCATION = 0x1000
            const uint SHGFI_ICONLOCATION = 0x1000;
            
            SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICONLOCATION);
            
            string location = shinfo.szDisplayName;
            if (!string.IsNullOrEmpty(location))
            {
                return location;
            }
            return null;
        }

        private static Bitmap TrimBitmap(Bitmap source)
        {
            Rectangle srcRect = default;
            BitmapData? data = null;
            try
            {
                data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                int xMin = int.MaxValue, xMax = int.MinValue, yMin = int.MaxValue, yMax = int.MinValue;
                bool foundPixel = false;

                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        int alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            if (x < xMin) xMin = x;
                            if (x > xMax) xMax = x;
                            if (y < yMin) yMin = y;
                            if (y > yMax) yMax = y;
                            foundPixel = true;
                        }
                    }
                }

                if (!foundPixel)
                {
                    return new Bitmap(source); // Return copy of original if empty
                }

                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax + 1, yMax + 1);
            }
            finally
            {
                if (data != null)
                    source.UnlockBits(data);
            }

            Bitmap dest = new Bitmap(srcRect.Width, srcRect.Height);
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.DrawImage(source, 0, 0, srcRect, GraphicsUnit.Pixel);
            }
            return dest;
        }
    }
}
