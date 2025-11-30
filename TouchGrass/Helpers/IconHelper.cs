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

                // Handle .url files specifically to find the real icon target
                if (Path.GetExtension(filePath).Equals(".url", StringComparison.OrdinalIgnoreCase))
                {
                    string? targetPath = GetIconPathFromUrlFile(filePath);
                    if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                    {
                        // Use the target path for icon extraction
                        // Note: We might need the index too, but for simplicity let's try the file first.
                        // If it's a steam game, it might point to the game exe or steam exe.
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
                    {
                        bitmap.Save(outputPath, ImageFormat.Png);
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

        private static string? GetIconPathFromUrlFile(string urlFilePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(urlFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring("IconFile=".Length).Trim();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
