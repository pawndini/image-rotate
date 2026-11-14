using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Setup
{
    private const string Title = "Image Rotate";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ImageRotate";
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Image Rotate");
    private static readonly string Shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Image Rotate.lnk");
    private static readonly string App = Path.Combine(Folder, "ImageRotate.exe");
    private static readonly string Uninstaller = Path.Combine(Folder, "Uninstall.exe");
    private static readonly string CurrentExe = Application.ExecutablePath;

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        try
        {
            if (args.Length == 2 && args[0] == "/remove")
            {
                if (!WaitForExit(Int32.Parse(args[1]))) throw new IOException("Close the uninstaller and try again.");
                Remove();
                MessageBox.Show("Image Rotate was removed.", Title);
                return;
            }

            if (args.Length == 1 && args[0] == "/uninstall")
            {
                if (MessageBox.Show("Uninstall Image Rotate?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                string copy = Path.Combine(Path.GetTempPath(), "ImageRotate-Uninstall.exe");
                File.Copy(CurrentExe, copy, true);
                Process.Start(copy, "/remove " + Process.GetCurrentProcess().Id);
                return;
            }

            if (MessageBox.Show("Install Image Rotate for your Windows account?\r\n\r\nSetup will add Image Rotate to the Start menu.", Title + " Setup",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            Install();
            if (MessageBox.Show("Image Rotate is installed. Open it now?", Title + " Setup",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) Process.Start(App);
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, Title + " Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }

    private static bool WaitForExit(int id)
    {
        try
        {
            using (var process = Process.GetProcessById(id)) return process.WaitForExit(10000);
        }
        catch (ArgumentException) { return true; }
    }

    private static void Install()
    {
        Directory.CreateDirectory(Folder);
        using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageRotate.exe"))
        {
            if (payload == null) throw new InvalidOperationException("The installer is missing its application payload.");
            string staged = Path.Combine(Folder, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var output = new FileStream(staged, FileMode.CreateNew)) payload.CopyTo(output);
                if (File.Exists(App)) File.Replace(staged, App, null);
                else File.Move(staged, App);
            }
            finally { File.Delete(staged); }
        }
        if (!String.Equals(CurrentExe, Uninstaller, StringComparison.OrdinalIgnoreCase))
            File.Copy(CurrentExe, Uninstaller, true);

        Directory.CreateDirectory(Path.GetDirectoryName(Shortcut));
        var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic link = ((dynamic)shell).CreateShortcut(Shortcut);
        link.TargetPath = App;
        link.WorkingDirectory = Folder;
        link.Description = "Rotate and save images";
        link.Save();

        using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
        {
            key.SetValue("DisplayName", Title);
            key.SetValue("DisplayVersion", "1.1.0");
            key.SetValue("DisplayIcon", App);
            key.SetValue("InstallLocation", Folder);
            key.SetValue("UninstallString", "\"" + Uninstaller + "\" /uninstall");
            key.SetValue("NoModify", 1);
            key.SetValue("NoRepair", 1);
        }
    }

    private static void Remove()
    {
        File.Delete(App);
        File.Delete(Uninstaller);
        File.Delete(Shortcut);
        Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
        if (Directory.Exists(Folder) && Directory.GetFileSystemEntries(Folder).Length == 0) Directory.Delete(Folder);
    }
}
