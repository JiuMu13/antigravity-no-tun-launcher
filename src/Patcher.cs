using System;
using System.IO;
using System.Reflection;

namespace AntigravityLauncher
{
    public class PatchResult
    {
        public bool DllRestored { get; set; }
        public bool ConfigRestored { get; set; }
        public bool ConfigSyncedBack { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class Patcher
    {
        public static PatchResult ApplyPatches(string targetDir)
        {
            PatchResult result = new PatchResult();
            try
            {
                string targetDll = Path.Combine(targetDir, "version.dll");
                string targetCfg = Path.Combine(targetDir, "config.json");

                string sourceDll = ResolveAssetPath("version.dll");
                string sourceCfg = ResolveAssetPath("config.json");

                // 1. 检查并补齐 version.dll
                if (!File.Exists(targetDll))
                {
                    if (!string.IsNullOrEmpty(sourceDll) && File.Exists(sourceDll))
                    {
                        File.Copy(sourceDll, targetDll, true);
                        result.DllRestored = true;
                    }
                    else
                    {
                        // 外部无文件时，从 exe 内部嵌入资源释放
                        if (ExtractEmbeddedResource("version.dll", targetDll))
                        {
                            result.DllRestored = true;
                        }
                    }
                }

                // 2. 检查并补齐 config.json，或在有修改时同步备份
                if (!File.Exists(targetCfg))
                {
                    if (!string.IsNullOrEmpty(sourceCfg) && File.Exists(sourceCfg))
                    {
                        File.Copy(sourceCfg, targetCfg, true);
                        result.ConfigRestored = true;
                    }
                    else
                    {
                        // 外部无文件时，从 exe 内部嵌入资源释放
                        if (ExtractEmbeddedResource("config.json", targetCfg))
                        {
                            result.ConfigRestored = true;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(sourceCfg) && File.Exists(sourceCfg))
                {
                    // 如果外部存在配置文件，且目标配置已被修改且更新，则自动回写同步，保护用户自定义端口或规则
                    DateTime targetTime = File.GetLastWriteTimeUtc(targetCfg);
                    DateTime sourceTime = File.GetLastWriteTimeUtc(sourceCfg);
                    if (targetTime > sourceTime)
                    {
                        try
                        {
                            File.Copy(targetCfg, sourceCfg, true);
                            result.ConfigSyncedBack = true;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public static bool ExtractEmbeddedResource(string resourceLogicalName, string outputPath)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string targetRes = null;
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.Equals(resourceLogicalName, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("." + resourceLogicalName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRes = name;
                        break;
                    }
                }

                if (targetRes == null) return false;

                using (Stream stream = assembly.GetManifestResourceStream(targetRes))
                {
                    if (stream == null) return false;
                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[81920];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, read);
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveAssetPath(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // 1. 本地 assets 目录 (如: <appDir>\assets\config.json)
            string p1 = Path.Combine(baseDir, "assets", fileName);
            if (File.Exists(p1)) return p1;

            // 2. 与程序同级目录 (如: <appDir>\config.json)
            string p2 = Path.Combine(baseDir, fileName);
            if (File.Exists(p2)) return p2;

            // 3. 用户默认备份目录 (%LOCALAPPDATA%\Programs\antigravity_backup\...)
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string p3 = Path.Combine(localApp, @"Programs\antigravity_backup", fileName);
            if (File.Exists(p3)) return p3;

            return null;
        }
    }
}
