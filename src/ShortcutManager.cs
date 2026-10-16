using System;
using System.IO;

namespace AntigravityLauncher
{
    public static class ShortcutManager
    {
        public static bool CreateShortcuts(string launcherExe, string targetDir)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return false;
                dynamic shell = Activator.CreateInstance(shellType);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                string targetExe = Path.Combine(targetDir, "Antigravity.exe");
                string iconPath = File.Exists(targetExe) ? targetExe + ",0" : launcherExe + ",0";

                // 1. 桌面专用快捷方式
                string desktopLink = Path.Combine(desktop, "Antigravity (No-TUN).lnk");
                dynamic scDesktop = shell.CreateShortcut(desktopLink);
                scDesktop.TargetPath = launcherExe;
                scDesktop.WorkingDirectory = targetDir;
                scDesktop.IconLocation = iconPath;
                scDesktop.Description = "Antigravity No-TUN Proxy Launcher";
                scDesktop.Save();

                // 2. 开始菜单快捷方式
                if (Directory.Exists(startMenu))
                {
                    string startMenuLink = Path.Combine(startMenu, "Antigravity (No-TUN).lnk");
                    dynamic scStart = shell.CreateShortcut(startMenuLink);
                    scStart.TargetPath = launcherExe;
                    scStart.WorkingDirectory = targetDir;
                    scStart.IconLocation = iconPath;
                    scStart.Description = "Antigravity No-TUN Proxy Launcher";
                    scStart.Save();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
