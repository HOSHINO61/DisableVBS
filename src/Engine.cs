using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DvbWpf
{
    static class Sys
    {
        public const string SecGuid = "{0cb3b571-2f2e-4343-a879-d86a476d7215}";

        public static readonly string[] DisableFeatures = new string[] {
            "microsoft-hyper-v-all", "IsolatedUserMode", "Microsoft-Hyper-V-Hypervisor",
            "Microsoft-Hyper-V-Online", "HypervisorPlatform" };

        public static readonly string[] EnableFeatures = new string[] {
            "IsolatedUserMode", "Microsoft-Hyper-V-Hypervisor",
            "Microsoft-Hyper-V-Online", "HypervisorPlatform" };

        static ProcessStartInfo Psi(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.GetEncoding(936);
            psi.StandardErrorEncoding = Encoding.GetEncoding(936);
            return psi;
        }

        public static int Run(string exe, string args, Action<string> onLine)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo = Psi(exe, args);
                    if (onLine != null)
                    {
                        p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                        { if (e.Data != null) onLine(e.Data); };
                        p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                        { if (e.Data != null) onLine(e.Data); };
                    }
                    p.Start();
                    if (onLine != null) { p.BeginOutputReadLine(); p.BeginErrorReadLine(); }
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                if (onLine != null) onLine("[启动失败] " + exe + " " + args + " :: " + ex.Message);
                return -9999;
            }
        }

        public static string RunOut(string exe, string args)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo = Psi(exe, args);
                    p.Start();
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return (o + "\n" + e);
                }
            }
            catch { return ""; }
        }

        public static void AppendLog(string kind, string detail)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(dir, "DisableVBS_\u65e5\u5fd7.txt");
                string line = DateTime.Now.ToString("yyyy/MM/dd dddd HH:mm:ss.ff") + "  " + kind + ": " + detail
                            + Environment.NewLine;
                File.AppendAllText(path, line, Encoding.GetEncoding(936));
            }
            catch { }
        }
    }

    static class Detect
    {
        public static string Vbs()
        {
            string o = Sys.RunOut("powershell.exe",
                "-NoP -C \"(Get-CimInstance Win32_DeviceGuard -Namespace root\\Microsoft\\Windows\\DeviceGuard).VirtualizationBasedSecurityStatus\"").Trim();
            if (o == "2") return "\u5df2\u5f00\u542f";
            if (o == "0" || o == "1") return "\u5df2\u5173\u95ed";
            return "\u672a\u77e5";
        }

        public static string Hvci()
        {
            try
            {
                using (var dg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
                {
                    if (dg == null) return "\u672a\u77e5";
                    using (var k = dg.OpenSubKey(@"Scenarios\HypervisorEnforcedCodeIntegrity"))
                    {
                        if (k == null) return "\u5df2\u5173\u95ed";
                        object v = k.GetValue("Enabled");
                        if (v is int) return (int)v == 1 ? "\u5df2\u5f00\u542f" : "\u5df2\u5173\u95ed";
                        return "\u5df2\u5173\u95ed";
                    }
                }
            }
            catch { return "\u672a\u77e5"; }
        }

        public static string Hvl()
        {
            string o = Sys.RunOut("bcdedit.exe", "/enum OSLOADER");
            if (o.Trim().Length == 0) return "\u672a\u77e5";
            if (o.IndexOf("\u62d2\u7edd\u8bbf\u95ee", StringComparison.Ordinal) >= 0) return "\u672a\u77e5";
            if (o.IndexOf("\u65e0\u6cd5\u6253\u5f00", StringComparison.Ordinal) >= 0) return "\u672a\u77e5";
            Match m = Regex.Match(o, @"hypervisorlaunchtype\s+(\S+)", RegexOptions.IgnoreCase);
            if (m.Success && m.Groups[1].Value.Equals("Off", StringComparison.OrdinalIgnoreCase))
                return "\u5df2\u7981\u7528 (\u5173\u95ed)";
            return "\u5df2\u542f\u7528 (\u81ea\u52a8/\u672a\u8bbe\u7f6e)";
        }

        public static string Hv()
        {
            string o = Sys.RunOut("powershell.exe",
                "-NoP -C \"(Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All).State\"");
            string t = o.Trim();
            if (t.Equals("Enabled", StringComparison.OrdinalIgnoreCase)) return "\u5df2\u542f\u7528";
            if (t.Equals("Disabled", StringComparison.OrdinalIgnoreCase)) return "\u672a\u542f\u7528";
            return "\u672a\u77e5";
        }
    }

    class Engine
    {
        public Action<int> OnStep = delegate { };
        public Action<string> OnSub = delegate { };
        public Action<string> OnLog = delegate { };
        public Action<bool, string> OnFinish = delegate { };

        string fails = "";

        public void Run(string code)
        {
            fails = "";
            try
            {
                if (code == "1") Disable();
                else if (code == "2") Restore();
                else if (code == "5") InstallHv();
            }
            catch (Exception ex)
            {
                OnLog("[异常] " + ex.Message);
                fails += "[异常] " + ex.Message + "\uff1b";
            }
            OnFinish(fails.Length == 0, fails);
        }

        void Classify(int rc, string feature, string action)
        {
            if (rc == 0) { OnLog("[ \u6210\u529f ] " + feature + "  " + action); return; }
            if (rc == 3010) fails += feature + "[3010\uff08\u53ef\u5ffd\u7565\uff09]\uff1b";
            else if (rc == -2146498548) fails += feature + "[\u672c\u673a\u65e0\u6b64\u529f\u80fd]\uff1b";
            else fails += feature + "\uff08\u9519\u8bef\u7801 " + rc + "\uff09\uff1b";
            OnLog("[ \u5931\u8d25 ] " + feature + "  \u8fd4\u56de\u7801 " + rc);
        }

        void Disable()
        {
            // ---- 1/4 ----
            OnStep(0);
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string skip = Sys.RunOut("powershell.exe",
                "-NoP -C \"$l=(Get-Content $env:SystemRoot\\Logs\\CBS\\CBS.log -Tail 20000 -ErrorAction SilentlyContinue | Where-Object {$_ -match '[SR]'} | Select-Object -Last 1); $t=(Get-Date).ToString('yyyy-MM-dd'); if($l -and $l.Substring(0,10) -eq $t){$t}\"").Trim();

            if (skip.Contains(today))
            {
                OnLog("[1/4] \u7cfb\u7edf\u7ec4\u4ef6\u68c0\u67e5 \u2014\u2014 \u4e0a\u6b21\u68c0\u67e5\uff1a" + today + "\uff08\u68c0\u6d4b\u5230\u4eca\u65e5\u5df2\u6267\u884c\uff0c\u8df3\u8fc7\u55b5\uff09");
            }
            else
            {
                OnLog("[1/4] \u6b63\u5728\u68c0\u67e5\u5e76\u4fee\u590d\u7cfb\u7edf\u7ec4\u4ef6\uff08\u7ea6 1-5 \u5206\u949f\uff09\u2026\u2026");
                int rc = Sys.Run("sfc.exe", "/scannow", OnLog);
                OnLog("sfc /scannow \u8fd4\u56de\u7801 " + rc);
            }

            // ---- 2/4 ----
            OnStep(1);
            for (int i = 0; i < Sys.DisableFeatures.Length; i++)
            {
                string f = Sys.DisableFeatures[i];
                OnSub("\u6b63\u5728\u5904\u7406\u7b2c " + (i + 1) + " \u9879 / \u5171 " + Sys.DisableFeatures.Length + " \u9879\u2026\u2026");
                int rc = Sys.Run("dism.exe", "/Online /Disable-Feature:" + f + " /NoRestart", OnLog);
                Classify(rc, f, "\u7981\u7528");
            }

            // ---- 3/4 ----
            OnStep(2);
            OnLog("bcdedit /set hypervisorlaunchtype off");
            Sys.Run("bcdedit.exe", "/set hypervisorlaunchtype off", OnLog);
            OnLog("reg add ...\\DeviceGuard  EnableVirtualizationBasedSecurity=0");
            Sys.Run("reg.exe", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f", OnLog);
            OnLog("reg add ...\\HypervisorEnforcedCodeIntegrity  Enabled=0");
            Sys.Run("reg.exe", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 0 /f", OnLog);

            // ---- 4/4 ----
            OnStep(3);
            OnLog("mountvol X: /s");
            Sys.Run("mountvol.exe", "X: /s", OnLog);

            string sec = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "SecConfig.efi");
            string dst = @"X:\EFI\Microsoft\Boot\SecConfig.efi";
            try
            {
                File.Copy(sec, dst, true);
                OnLog("copy SecConfig.efi -> " + dst);
            }
            catch (Exception ex) { OnLog("[copy \u5931\u8d25] " + ex.Message); }

            Sys.Run("bcdedit.exe", "/delete " + Sys.SecGuid + " /f", OnLog);
            Sys.Run("bcdedit.exe", "/create " + Sys.SecGuid + " /d \"DebugTool\" /application osloader", OnLog);
            Sys.Run("bcdedit.exe", "/set " + Sys.SecGuid + " path \"\\EFI\\Microsoft\\Boot\\SecConfig.efi\"", OnLog);
            Sys.Run("bcdedit.exe", "/set {bootmgr} bootsequence " + Sys.SecGuid, OnLog);
            Sys.Run("bcdedit.exe", "/set " + Sys.SecGuid + " loadoptions DISABLE-LSA-ISO,DISABLE-VBS", OnLog);
            Sys.Run("bcdedit.exe", "/set " + Sys.SecGuid + " device partition=X:", OnLog);
            Sys.Run("mountvol.exe", "X: /d", OnLog);

            if (fails.Length > 0) Sys.AppendLog("\u5173\u95ed\u65f6\u672a\u6210\u529f", fails);
        }

        void Restore()
        {
            OnStep(0);
            Sys.Run("bcdedit.exe", "/deletevalue {bootmgr} bootsequence", OnLog);
            Sys.Run("bcdedit.exe", "/delete " + Sys.SecGuid + " /f", OnLog);

            for (int i = 0; i < Sys.EnableFeatures.Length; i++)
            {
                string f = Sys.EnableFeatures[i];
                OnSub("\u6b63\u5728\u5904\u7406\u7b2c " + (i + 1) + " \u9879 / \u5171 " + (Sys.EnableFeatures.Length + 1) + " \u9879\u2026\u2026");
                int rc = Sys.Run("dism.exe", "/Online /Enable-Feature:" + f + " /NoRestart", OnLog);
                Classify(rc, f, "\u542f\u7528");
            }

            OnSub("\u6b63\u5728\u5904\u7406\u7b2c " + (Sys.EnableFeatures.Length + 1) + " \u9879 / \u5171 " + (Sys.EnableFeatures.Length + 1) + " \u9879\u2026\u2026");
            int rc2 = Sys.Run("dism.exe", "/Online /Enable-Feature:Microsoft-Hyper-V-All /All /NoRestart", OnLog);
            Classify(rc2, "Microsoft-Hyper-V-All", "\u542f\u7528");

            Sys.Run("reg.exe", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f", OnLog);
            Sys.Run("reg.exe", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 1 /f", OnLog);
            Sys.Run("bcdedit.exe", "/set hypervisorlaunchtype auto", OnLog);

            if (fails.Length > 0) Sys.AppendLog("\u8fd8\u539f\u65f6\u672a\u6210\u529f", fails);
        }

        void InstallHv()
        {
            OnStep(0);
            bool addFail = false;
            string svc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "servicing", "Packages");
            string[] files = new string[0];
            try { files = Directory.GetFiles(svc, "*Hyper-V*.mum"); }
            catch (Exception ex) { OnLog("[\u5217\u76ee\u5f55\u5931\u8d25] " + ex.Message); }

            OnLog("\u627e\u5230 " + files.Length + " \u4e2a Hyper-V \u5305");
            for (int i = 0; i < files.Length; i++)
            {
                OnSub("\u6b63\u5728\u5904\u7406\u7b2c " + (i + 1) + " \u9879 / \u5171 " + files.Length + " \u9879\u2026\u2026");
                int rc = Sys.Run("dism.exe", "/online /norestart /add-package:\"" + files[i] + "\"", OnLog);
                if (rc != 0) addFail = true;
            }

            Sys.Run("bcdedit.exe", "/set hypervisorlaunchtype off", OnLog);

            if (addFail)
            {
                OnLog("\u6709\u7a0b\u5e8f\u5305\u672a\u80fd\u542f\u7528\uff0c\u5c5e\u4e8e\u9884\u671f\u73b0\u8c61\u3002");
            }
            else
            {
                // 原版: echo n | Dism /online /enable-feature ... /NoRestart
                int rc = Sys.Run("dism.exe", "/online /enable-feature /featurename:Microsoft-Hyper-V-All /LimitAccess /ALL /NoRestart", OnLog);
                if (rc != 0 && rc != 3010)
                    fails += "Microsoft-Hyper-V-All\uff08\u9519\u8bef\u7801 " + rc + "\uff09\uff1b";
            }
        }
    }
}
