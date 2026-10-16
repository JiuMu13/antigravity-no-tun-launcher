using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AntigravityLauncher
{
    public static class PathDetector
    {
        private const string ConfigFileName = "launcher_config.json";

        public static string DetectAntigravityPath(string customPath = null)
        {
            // 1. 如果用户通过命令行或参数指定了路径
            if (!string.IsNullOrEmpty(customPath) && ValidateDir(customPath))
            {
                SaveConfig(customPath);
                return customPath;
            }

            // 2. 检查本地保存的历史配置
            string savedPath = LoadConfig();
            if (!string.IsNullOrEmpty(savedPath) && ValidateDir(savedPath))
            {
                return savedPath;
            }

            // 3. 从运行中进程自动获取
            string procPath = FindFromRunningProcess();
            if (!string.IsNullOrEmpty(procPath) && ValidateDir(procPath))
            {
                SaveConfig(procPath);
                return procPath;
            }

            // 4. 从 Windows 注册表读取安装位置
            string regPath = FindFromRegistry();
            if (!string.IsNullOrEmpty(regPath) && ValidateDir(regPath))
            {
                SaveConfig(regPath);
                return regPath;
            }

            // 5. 检查常用默认安装路径
            string commonPath = FindFromCommonPaths();
            if (!string.IsNullOrEmpty(commonPath) && ValidateDir(commonPath))
            {
                SaveConfig(commonPath);
                return commonPath;
            }

            // 6. 检查桌面或开始菜单快捷方式
            string lnkPath = FindFromShortcuts();
            if (!string.IsNullOrEmpty(lnkPath) && ValidateDir(lnkPath))
            {
                SaveConfig(lnkPath);
                return lnkPath;
            }

            // 7. 若均未找到，弹出文件选择对话框让用户手动选择
            string manualPath = PromptUserForPath();
            if (!string.IsNullOrEmpty(manualPath) && ValidateDir(manualPath))
            {
                SaveConfig(manualPath);
                return manualPath;
            }

            return null;
        }

        public static bool ValidateDir(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return false;
            }
            string exe = Path.Combine(dir, "Antigravity.exe");
            return File.Exists(exe);
        }

        private static string FindFromRunningProcess()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("Antigravity");
                foreach (Process p in processes)
                {
                    try
                    {
                        if (p.MainModule != null && !string.IsNullOrEmpty(p.MainModule.FileName))
                        {
                            string dir = Path.GetDirectoryName(p.MainModule.FileName);
                            if (ValidateDir(dir)) return dir;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static string FindFromRegistry()
        {
            string[] uninstallKeys = new string[]
            {
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            RegistryKey[] rootKeys = new RegistryKey[]
            {
                Registry.CurrentUser,
                Registry.LocalMachine
            };

            foreach (RegistryKey root in rootKeys)
            {
                foreach (string keyPath in uninstallKeys)
                {
                    try
                    {
                        using (RegistryKey baseKey = root.OpenSubKey(keyPath))
                        {
                            if (baseKey == null) continue;
                            foreach (string subName in baseKey.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(subName))
                                {
                                    if (subKey == null) continue;
                                    object displayNameObj = subKey.GetValue("DisplayName");
                                    string displayName = displayNameObj != null ? displayNameObj.ToString() : "";
                                    if (displayName.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        // 检查 InstallLocation
                                        object locObj = subKey.GetValue("InstallLocation");
                                        if (locObj != null && ValidateDir(locObj.ToString()))
                                        {
                                            return locObj.ToString();
                                        }

                                        // 检查 DisplayIcon
                                        object iconObj = subKey.GetValue("DisplayIcon");
                                        if (iconObj != null)
                                        {
                                            string iconStr = iconObj.ToString().Split(',')[0].Trim('"', ' ');
                                            string dir = Path.GetDirectoryName(iconStr);
                                            if (ValidateDir(dir)) return dir;
                                        }

                                        // 检查 UninstallString
                                        object uninstObj = subKey.GetValue("UninstallString");
                                        if (uninstObj != null)
                                        {
                                            string uninstStr = uninstObj.ToString();
                                            string clean = uninstStr.Trim('"').Split('"')[0];
                                            string dir = Path.GetDirectoryName(clean);
                                            if (ValidateDir(dir)) return dir;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        private static string FindFromCommonPaths()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string[] candidates = new string[]
            {
                Path.Combine(localAppData, @"Programs\antigravity"),
                Path.Combine(localAppData, @"antigravity"),
                Path.Combine(programFiles, @"antigravity"),
                Path.Combine(programFilesX86, @"antigravity")
            };

            foreach (string path in candidates)
            {
                if (ValidateDir(path)) return path;
            }

            return null;
        }

        private static string FindFromShortcuts()
        {
            string[] searchDirs = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
            };

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;
            dynamic shell = Activator.CreateInstance(shellType);

            foreach (string searchDir in searchDirs)
            {
                if (!Directory.Exists(searchDir)) continue;
                try
                {
                    string[] lnkFiles = Directory.GetFiles(searchDir, "*antigravity*.lnk", SearchOption.AllDirectories);
                    foreach (string lnk in lnkFiles)
                    {
                        try
                        {
                            dynamic sc = shell.CreateShortcut(lnk);
                            string target = sc.TargetPath;
                            if (!string.IsNullOrEmpty(target) && target.IndexOf("Antigravity.exe", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string dir = Path.GetDirectoryName(target);
                                if (ValidateDir(dir)) return dir;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string PromptUserForPath()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "请选择 Antigravity.exe 所在路径";
            dlg.Filter = "Antigravity 主程序 (Antigravity.exe)|Antigravity.exe|所有文件 (*.*)|*.*";
            dlg.FileName = "Antigravity.exe";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string dir = Path.GetDirectoryName(dlg.FileName);
                if (ValidateDir(dir)) return dir;
            }
            return null;
        }

        private static string GetConfigFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, ConfigFileName);
        }

        private static string LoadConfig()
        {
            try
            {
                string cfgFile = GetConfigFilePath();
                if (File.Exists(cfgFile))
                {
                    string content = File.ReadAllText(cfgFile).Trim();
                    if (!string.IsNullOrEmpty(content) && content.Contains("target_path"))
                    {
                        // 简单提取 JSON 字段，避免引入大型依赖
                        int idx = content.IndexOf("\"target_path\"");
                        if (idx >= 0)
                        {
                            int colon = content.IndexOf(':', idx);
                            int quote1 = content.IndexOf('"', colon);
                            int quote2 = content.IndexOf('"', quote1 + 1);
                            if (quote1 >= 0 && quote2 > quote1)
                            {
                                return content.Substring(quote1 + 1, quote2 - quote1 - 1).Replace("\\\\", "\\");
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static void SaveConfig(string path)
        {
            try
            {
                string cfgFile = GetConfigFilePath();
                string json = "{\n  \"target_path\": \"" + path.Replace("\\", "\\\\") + "\"\n}";
                File.WriteAllText(cfgFile, json);
            }
            catch { }
        }
    }
}
