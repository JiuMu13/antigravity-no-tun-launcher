using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace AntigravityLauncher
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string explicitPath = null;
                bool detectOnly = false;
                bool installShortcut = false;
                StringBuilder passThroughArgs = new StringBuilder();

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (arg.Equals("--detect", StringComparison.OrdinalIgnoreCase))
                    {
                        detectOnly = true;
                    }
                    else if (arg.Equals("--install-shortcut", StringComparison.OrdinalIgnoreCase))
                    {
                        installShortcut = true;
                    }
                    else if (arg.Equals("--path", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        explicitPath = args[++i];
                    }
                    else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHelp();
                        return;
                    }
                    else
                    {
                        if (passThroughArgs.Length > 0) passThroughArgs.Append(" ");
                        if (arg.Contains(" "))
                        {
                            passThroughArgs.Append("\"").Append(arg).Append("\"");
                        }
                        else
                        {
                            passThroughArgs.Append(arg);
                        }
                    }
                }

                // 1. 自动定位 Antigravity 安装路径
                string targetDir = PathDetector.DetectAntigravityPath(explicitPath);

                if (string.IsNullOrEmpty(targetDir) || !PathDetector.ValidateDir(targetDir))
                {
                    MessageBox.Show(
                        "未能找到 Antigravity 安装目录。\n\n请确认已安装 Antigravity，或启动时添加参数：\n--path \"C:\\Path\\To\\antigravity\"",
                        "Antigravity 启动失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                if (detectOnly)
                {
                    MessageBox.Show("检测到 Antigravity 安装路径：\n" + targetDir, "路径检测成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2. 检查并补齐 version.dll 与 config.json
                PatchResult patchResult = Patcher.ApplyPatches(targetDir);

                if (installShortcut)
                {
                    string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                    bool ok = ShortcutManager.CreateShortcuts(currentExe, targetDir);
                    if (ok)
                    {
                        MessageBox.Show("已成功在桌面和开始菜单创建「Antigravity (No-TUN)」快捷方式！", "快捷方式安装成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("创建快捷方式失败，请尝试以管理员身份运行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return;
                }

                // 3. 启动 Antigravity.exe
                string targetExe = Path.Combine(targetDir, "Antigravity.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    WorkingDirectory = targetDir,
                    Arguments = passThroughArgs.ToString(),
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动器运行异常:\n" + ex.Message, "Antigravity Launcher 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ShowHelp()
        {
            string msg = "Antigravity No-TUN Launcher 命令行参数帮助：\n\n" +
                         "--detect            仅检测并显示 Antigravity 安装路径\n" +
                         "--install-shortcut  在桌面与开始菜单创建专属启动快捷方式\n" +
                         "--path <路径>       手动指定 Antigravity 安装目录\n" +
                         "--help / -h         显示此帮助信息\n\n" +
                         "其他参数将自动透传给 Antigravity.exe。";
            MessageBox.Show(msg, "Antigravity Launcher 帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
