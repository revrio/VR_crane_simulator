#if UNITY_WEBGL && !UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public static class PortBrowserExport
{
    [DllImport("__Internal")]
    private static extern void PortLabDownload(byte[] bytes, int length, string filename);

    public static void Download(string directory)
    {
        using (var stream = new MemoryStream())
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
                foreach (string file in Directory.GetFiles(directory))
                {
                    var entry = zip.CreateEntry(Path.GetFileName(file));
                    using (var destination = entry.Open())
                    {
                        byte[] data = File.ReadAllBytes(file);
                        destination.Write(data, 0, data.Length);
                    }
                }
            byte[] bytes = stream.ToArray();
            PortLabDownload(bytes, bytes.Length, "portlab-" + Path.GetFileName(directory) + ".zip");
        }
    }
}
#endif
