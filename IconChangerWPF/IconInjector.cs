using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;

public class IconInjector
{
    [SuppressUnmanagedCodeSecurity()]

    private class NativeMethods
    {
        [DllImport("kernel32")]
        public static extern IntPtr BeginUpdateResource(string fileName, [MarshalAs(UnmanagedType.Bool)] bool deleteExistingResources);
        [DllImport("kernel32")]
        public static extern bool UpdateResource(IntPtr hUpdate, IntPtr type, IntPtr name, short language, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] data, int dataSize);
        [DllImport("kernel32")]
        public static extern bool EndUpdateResource(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)] bool discard);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ICONDIR
    {
        public ushort Reserved;  
        public ushort Type;      
        public ushort Count;    
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONDIRENTRY
    {
        public byte Width;            // Ширина изображения в пискелях.
        public byte Height;           // Высота изображения в пикселях.
        public byte ColorCount;       // Глубина цвета изображения.(0 если >=8bpp)
        public byte Reserved;         // Зарезервировано, должно быть 0
        public ushort Planes;         // Цветовые плоскости
        public ushort BitCount;       // Бит на пиксель
        public int BytesInRes;   // Длина в байтах данных пикселя
        public int ImageOffset;  // Смещение в файле, где начинаются данные пикселей.
    }
    // Содержит ширину, высоту и битность растра, а также формат пикселей, информацию о цветовой таблице и разрешении. 
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }
    // Иконки в exe/dll  файлах хранятся в очень схожей структуре:
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct GRPICONDIRENTRY
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public ushort Planes;
        public ushort BitCount;
        public int BytesInRes;
        public ushort ID;
    }
    public static void InjectIcon(string exeFileName, string iconFileName)
    {
        InjectIcon(exeFileName, iconFileName, 1, 1);
    }
    public static void InjectIcon(string exeFileName, string iconFileName, uint iconGroupID, uint iconBaseID)
    {
        const int RT_ICON = (int)3U;
        const int RT_GROUP_ICON = (int)14U;
        IconFile iconFile = IconFile.FromFile(iconFileName);
        var hUpdate = NativeMethods.BeginUpdateResource(exeFileName, false);
        var data = iconFile.CreateIconGroupData(iconBaseID);
        NativeMethods.UpdateResource(hUpdate, new IntPtr(RT_GROUP_ICON), new IntPtr(iconGroupID), 0, data, data.Length);
        for (var i = 0; i <= iconFile.ImageCount - 1; i++)
        {
            byte[] image = iconFile.ImageData(i);
            NativeMethods.UpdateResource(hUpdate, new IntPtr(RT_ICON), new IntPtr(iconBaseID + i), 0, image, image.Length);
        }
        NativeMethods.EndUpdateResource(hUpdate, false);
    }
    private class IconFile
    {
        private ICONDIR iconDir = new ICONDIR();
        private ICONDIRENTRY[] iconEntry;
        private byte[][] iconImage;
        public int ImageCount
        {
            get
            {
                return iconDir.Count;
            }
        }
        public byte[] ImageData(int index)
        {
                return iconImage[index];
        }
        public static IconFile FromFile(string filename)
        {
            IconFile instance = new IconFile();
            byte[] fileBytes = System.IO.File.ReadAllBytes(filename);
            
            GCHandle pinnedBytes = GCHandle.Alloc(fileBytes, GCHandleType.Pinned);
            instance.iconDir = (ICONDIR)Marshal.PtrToStructure(pinnedBytes.AddrOfPinnedObject(), typeof(ICONDIR));
            // который сообщает нам, сколько изображений находится в файле ico. Для каждого изображения есть ICONDIRENTRY и связанные пиксельные данные
            instance.iconEntry = new ICONDIRENTRY[instance.iconDir.Count - 1 + 1];
            instance.iconImage = new byte[instance.iconDir.Count - 1 + 1][];
            // Первая ICONDIRENTRY будет сразу после ICONDIR, поэтому смещение к ней - это размер ICONDIR
            var offset = Marshal.SizeOf(instance.iconDir);
            // После прочтения ICONDIRENTRY мы делаем шаг вперед размером с ICONDIRENTRY          
            var iconDirEntryType = typeof(ICONDIRENTRY);
            var size = Marshal.SizeOf(iconDirEntryType);
            for (var i = 0; i <= instance.iconDir.Count - 1; i++)
            {
                // Берем структуру
                var entry = (ICONDIRENTRY)Marshal.PtrToStructure(new IntPtr(pinnedBytes.AddrOfPinnedObject().ToInt64() + offset), iconDirEntryType);
                instance.iconEntry[i] = entry;
                // Берем связанные пиксельные данные
                instance.iconImage[i] = new byte[entry.BytesInRes - 1 + 1];
                Buffer.BlockCopy(fileBytes, entry.ImageOffset, instance.iconImage[i], 0, entry.BytesInRes);
                offset += size;
            }
            pinnedBytes.Free();
            return instance;
        }
        public byte[] CreateIconGroupData(uint iconBaseID)
        {
            int sizeOfIconGroupData = Marshal.SizeOf(typeof(ICONDIR)) + Marshal.SizeOf(typeof(GRPICONDIRENTRY)) * ImageCount;
            byte[] data = new byte[sizeOfIconGroupData - 1 + 1];
            var pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
            Marshal.StructureToPtr(iconDir, pinnedData.AddrOfPinnedObject(), false);
            var offset = Marshal.SizeOf(iconDir);
            for (var i = 0; i <= ImageCount - 1; i++)
            {
                GRPICONDIRENTRY grpEntry = new GRPICONDIRENTRY();
                BITMAPINFOHEADER bitmapheader = new BITMAPINFOHEADER();
                var pinnedBitmapInfoHeader = GCHandle.Alloc(bitmapheader, GCHandleType.Pinned);
                Marshal.Copy(ImageData(i), 0, pinnedBitmapInfoHeader.AddrOfPinnedObject(), Marshal.SizeOf(typeof(BITMAPINFOHEADER)));
                pinnedBitmapInfoHeader.Free();
                grpEntry.Width = iconEntry[i].Width;
                grpEntry.Height = iconEntry[i].Height;
                grpEntry.ColorCount = iconEntry[i].ColorCount;
                grpEntry.Reserved = iconEntry[i].Reserved;
                grpEntry.Planes = bitmapheader.Planes;
                grpEntry.BitCount = bitmapheader.BitCount;
                grpEntry.BytesInRes = iconEntry[i].BytesInRes;
                grpEntry.ID = System.Convert.ToUInt16(iconBaseID + i);
                Marshal.StructureToPtr(grpEntry, new IntPtr(pinnedData.AddrOfPinnedObject().ToInt64() + offset), false);
                offset += Marshal.SizeOf(typeof(GRPICONDIRENTRY));
            }
            pinnedData.Free();
            return data;
        }
    }
}