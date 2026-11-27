using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DvbWpf
{
    static class Th
    {
        // ===== 分层（半透明）窗口用色：全部统一 90%（alpha 230）=====
        // 统一后不要再单独调某一层，否则卡片和外壳叠起来会互相干扰
        public static readonly Brush Header = Glass(230, "#0C0F15");
        public static readonly Brush Bg = Glass(230, "#101620");
        public static readonly Brush Footer = Glass(230, "#0B1017");
        public static readonly Brush Deep = Glass(230, "#080C12");

        public static readonly Brush Card = VGrad(A("#1A212C", 230), A("#121822", 230));
        public static readonly Brush Hover = VGrad(A("#232C3A", 230), A("#18202B", 230));

        public static readonly Brush Line = Glass(84, "#8FA2BD");
        public static readonly Brush LineHi = Glass(150, "#B7C6DC");

        public static readonly Brush Text = Hex("#EDF1F7");
        public static readonly Brush Body = Hex("#C4CCD9");
        public static readonly Brush Muted = Hex("#8E98A6");
        public static readonly Brush Faint = Hex("#6E7787");

        public static readonly Brush Accent = Hex("#5B93FF");
        public static readonly Brush AccentHi = Hex("#84ADFF");
        public static readonly Brush Ok = Hex("#3ED89E");
        public static readonly Brush Bad = Hex("#FF7F7F");
        public static readonly Brush Warn = Hex("#F6C453");

        public static readonly FontFamily FF = new FontFamily("Microsoft YaHei UI");
        public static readonly FontFamily Mono = new FontFamily("Consolas");

        public static Color C(string s) { return (Color)ColorConverter.ConvertFromString(s); }

        public static Color A(string s, byte a)
        {
            var c = C(s);
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        public static SolidColorBrush Glass(byte a, string s) { return new SolidColorBrush(A(s, a)); }

        public static Color With(Color c, byte a) { return Color.FromArgb(a, c.R, c.G, c.B); }

        public static SolidColorBrush Tint(Color c, byte a) { return new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B)); }

        public static Brush Hex(string s) { return new SolidColorBrush(C(s)); }

        public static LinearGradientBrush VGrad(Color top, Color bottom)
        {
            var sc = new GradientStopCollection();
            sc.Add(new GradientStop(top, 0.0));
            sc.Add(new GradientStop(bottom, 1.0));
            return new LinearGradientBrush(sc, new Point(0.5, 0), new Point(0.5, 1));
        }

        public static LinearGradientBrush HGrad(Color left, Color right)
        {
            var sc = new GradientStopCollection();
            sc.Add(new GradientStop(left, 0.0));
            sc.Add(new GradientStop(right, 1.0));
            return new LinearGradientBrush(sc, new Point(0, 0.5), new Point(1, 0.5));
        }
    }

    static class Spin
    {
        public static Canvas Make(double size, double dotSize, Brush color)
        {
            var canvas = new Canvas { Width = size, Height = size, RenderTransformOrigin = new Point(0.5, 0.5) };
            const int n = 6;
            for (int i = 0; i < n; i++)
            {
                double ang = i * 360.0 / n * Math.PI / 180.0;
                double r = size / 2 - dotSize / 2 - 1.0;
                double cx = size / 2 + r * Math.Sin(ang);
                double cy = size / 2 - r * Math.Cos(ang);
                var dot = new Ellipse { Width = dotSize, Height = dotSize, Fill = color, Opacity = 1.0 - i * (0.75 / n) };
                Canvas.SetLeft(dot, cx - dotSize / 2);
                Canvas.SetTop(dot, cy - dotSize / 2);
                canvas.Children.Add(dot);
            }
            var rt = new RotateTransform(0);
            canvas.RenderTransform = rt;
            rt.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(860)) { RepeatBehavior = RepeatBehavior.Forever });
            return canvas;
        }
    }

    // ---- 中英双语：界面文案统一走 T()，切换后重建当前页 ----
    static class I18n
    {
        public static bool En = false;
        public static bool Hant = false;

        public const string NoteZh =
            "感谢您使用此工具。\n\n"
          + "本次更新重做了界面外观，并对启动速度与页面切换做了适配优化，同时新增了语言切换等若干功能：\n\n"
          + "· 界面改为深色半透明浮层，圆角卡片、投影与细描边统一\n"
          + "· 新增 简体中文 / 繁體中文 / English 三语切换，选择会被记住\n"
          + "· 冷启动时状态区显示「正在读取…」，检测改为后台异步执行，不再阻塞窗口\n"
          + "· 阴影与渲染优化，提高切换语言、翻页时的加载速度\n"
          + "· 英文/繁体环境下遗漏的动态状态文案已补齐翻译\n\n"
          + "若要了解完整更新内容，请前往 https://github.com/HOSHINO61/DisableVBS\n\n"
          + "若您在使用中遇到任何问题，请前往\nhttps://github.com/HOSHINO61/DisableVBS/issues\n"
          + "根据操作提示进行有效反馈。我们会尽快处理您遇到的问题。";

        public const string NoteEn =
            "Thank you for using this tool.\n\n"
          + "This update rebuilds the interface, tunes startup and page switching, and adds a few features:\n\n"
          + "· Dark translucent shell with rounded cards, shadow and hairline borders unified\n"
          + "· Simplified Chinese / Traditional Chinese / English, and your choice is remembered\n"
          + "· Status tiles show \"Loading...\" on cold start; detection runs in the background and never blocks the window\n"
          + "· Shadow and rendering tuned, so switching language or changing pages is faster\n"
          + "· Dynamic status strings left untranslated in English / Traditional are covered now\n\n"
          + "For the full changelog, see https://github.com/HOSHINO61/DisableVBS\n\n"
          + "If you run into any problem, report it at\nhttps://github.com/HOSHINO61/DisableVBS/issues\n"
          + "and follow the instructions there. We will get back to you as soon as possible.";

        static readonly Dictionary<string, string> Exact = new Dictionary<string, string>();
        static readonly List<string[]> Rules = new List<string[]>();

        static I18n()
        {
            Exact["关闭/还原 Hyper-V和VBS"] = "Disable / Restore Hyper-V & VBS";
            Exact["本版本为测试版，不代表最终品质"] = "Test build - not the final quality";
            Exact["当前系统状态"] = "System status";
            Exact["VBS（基于虚拟化的安全）"] = "VBS (Virtualization-Based Security)";
            Exact["HVCI（内存完整性）"] = "HVCI (Memory Integrity)";
            Exact["Hypervisor 启动类型"] = "Hypervisor launch type";
            Exact["Hyper-V 功能"] = "Hyper-V feature";

            // 更新说明弹层的元信息行
            Exact["版本号"] = "Version";
            Exact["更新日期"] = "Released";
            Exact["系统"] = "System";

            Exact["已关闭"] = "Off";
            Exact["已开启"] = "On";
            Exact["已启用"] = "Enabled";
            Exact["已禁用 (关闭)"] = "Disabled (off)";
            Exact["已禁用（关闭）"] = "Disabled (off)";
            Exact["已启用 (自动/未设置)"] = "Enabled (auto / not set)";
            Exact["已启用（自动/未设置）"] = "Enabled (auto / not set)";
            Exact["未启用"] = "Not enabled";
            Exact["未知"] = "Unknown";
            Exact["正在读取…"] = "Loading…";

            Exact["更新说明"] = "Release notes";
            Exact["GitHub 仓库"] = "GitHub repo";
            Exact["反馈问题"] = "Report an issue";
            Exact["会先把版本与系统信息复制到剪贴板"] = "Version and system info are copied to the clipboard first";
            Exact[NoteZh] = NoteEn;
            Exact["检查更新"] = "Check for updates";
            Exact["正在检查更新…"] = "Checking for updates…";
            Exact["已是最新版本"] = "You are on the latest version";
            Exact["发现新版本"] = "New version available";
            Exact["检查失败，请检查网络或代理设置"] = "Check failed; check your network or proxy settings";
            Exact["暂无已发布的版本"] = "No published release yet";

            Exact["关闭VBS和HVCI"] = "Disable VBS and HVCI";
            Exact["还原VBS和HVCI"] = "Restore VBS and HVCI";
            Exact["获取系统配置"] = "Open system info";
            Exact["设置HVCI"] = "HVCI settings";
            Exact["【家庭版/家庭中文版】给系统安装 Hyper-V组件（装完必须重启）"] = "[Home / Home China] Install Hyper-V component (reboot required)";
            Exact["退出脚本"] = "Exit";

            Exact["运行记录"] = "Run log";
            Exact["管理员权限已确认"] = "Administrator privileges confirmed";

            Exact["正在执行，请稍候（通常需要 1-10分钟）……"] = "Running, please wait (usually 1-10 minutes)...";
            Exact["正在还原，请稍候……"] = "Restoring, please wait...";
            Exact["正在添加Hyper-V，请稍候……"] = "Adding Hyper-V, please wait...";
            Exact["正在检查并修复系统组件（约 1-5 分钟）……"] = "Checking and repairing system components (about 1-5 min)...";
            Exact["正在禁用相关功能（共 5 项，每项约5秒-1分钟，请耐心等待）"] = "Disabling related features (5 items, ~5 s to 1 min each, please wait)";
            Exact["正在设置虚拟机监控程序启动类型……"] = "Setting the hypervisor launch type...";
            Exact["即将准备就绪……"] = "Preparing to finish...";

            Exact["正在重启……"] = "Rebooting...";
            Exact["完成"] = "Done";
            Exact["设置已完成。接下来请保存文件并重启。"] = "Done. Please save your files and reboot.";
            Exact["重启过程中若出现黑底白字的英文界面，请连续按 F3 键走完剩余流程"] = "If an English black-and-white screen appears during reboot, press F3 repeatedly to finish the remaining steps.";
            Exact["日志：DisableVBS_日志.txt"] = "Log: DisableVBS_log.txt";
            Exact["需要重启以完成设置。"] = "Reboot required to finish setup.";
            Exact["你现在要重启吗?"] = "Reboot now?";

            Exact["取消"] = "Cancel";
            Exact["继续"] = "Continue";
            Exact["确定"] = "OK";
            Exact["关闭"] = "Close";
            Exact["重启"] = "Reboot";
            Exact["退出"] = "Exit";
            Exact["立即重启"] = "Reboot now";
            Exact["取消并跳转设置"] = "Cancel and open settings";

            Exact["重要提示"] = "Important";
            Exact["因系统安全策略设置，关闭VBS后可能需要重设PIN。为避免微软账户验证失败，强烈建议您临时关闭PIN（路径：系统设置-账户-登录选项）"] =
                "Because of the system security policy, your PIN may need to be reset after disabling VBS. To avoid Microsoft account verification failures, we strongly recommend turning the PIN off temporarily (Settings - Accounts - Sign-in options).";

            Exact["检测到待处理变更"] = "Pending changes detected";
            Exact["还原 VBS 和内存完整性"] = "Restore VBS and Memory Integrity";
            Exact["给系统安装 Hyper-V"] = "Install Hyper-V on this system";
            Exact["重启前请保留你的所有文件，确定要立即重启吗"] = "Save all your files first. Reboot now?";

            Exact["样机"] = "Sample mode";
            Exact["本项为只读操作（dxdiag / 安全中心），未模拟。"] = "Read-only action (dxdiag / Security Center) - not simulated.";
            Exact["真实版此处会执行 shutdown -r -t 0。"] = "The real build runs shutdown -r -t 0 here.";
            Exact["无法打开"] = "Cannot open";
            Exact["无法重启"] = "Cannot reboot";
            Exact["重启失败"] = "Reboot failed";
            Exact["无法打开设置"] = "Cannot open Settings";

            // ---- 组合型文案按片段替换 ----
            Rule("未成功项：未知", "Failed item: unknown");
            Rule("未成功项：", "Failed item: ");
            Rule("返回码", "exit code");
            Rule("正在处理第 ", "Processing item ");
            Rule(" 项 / 共 5 项……", " of 5...");
            Rule("正在重启……（", "Rebooting... (");
            Rule(" 秒，按 Esc 取消）", " s, press Esc to cancel)");
            Rule("系统拒绝了重启请求。", "The system refused the reboot request.");
            Rule("shutdown 返回码：", "shutdown exit code: ");
            Rule("\n\n请手动重启电脑。", "\n\nPlease reboot manually.");

            Rule("上一次操作尚未重启生效。", "The previous operation has not taken effect yet.");
            Rule("现在继续执行，部分功能可能返回错误码 50 或 3010，", "If you continue, some functions may return error 50 or 3010,");
            Rule("严重时重启会进入恢复环境。", "and in serious cases the reboot may land in the recovery environment.");
            Rule("请先重启电脑，再执行本项操作。", "Reboot first, then run this operation.");

            Rule("即将还原 VBS 与内存完整性，这样做可以提升系统安全性", "This restores VBS and memory integrity, which improves system security.");
            Rule("但是，若您需要使用VMware等虚拟机，则不建议还原，", "However, if you use VMware or other virtual machines, restoring is not recommended.");
            Rule("完成后需要重启才会生效。", "A reboot is required to take effect.");
            Rule("您真的要还原吗?", "Do you really want to restore?");

            Rule("本机 Hyper-V 功能当前状态：已启用（已启用时则不建议执行）", "Current Hyper-V feature state: enabled (not recommended to run this item while enabled).");
            Rule("本项会从系统自带的包目录里添加 Hyper-V 组件", "This adds the Hyper-V component from the system package store.");
            Rule("* Pro / 企业版 / 工作站版本本身就带 Hyper-V，不需要执行本项。", "* Pro / Enterprise / Workstation already include Hyper-V; this item is not needed.");
            Rule("* 执行完成后必须重启，让系统安装完，再继续操作。", "* A reboot is required afterwards so the system can finish installing; continue after that.");
            Rule("* 在非家庭版上执行后可能进不去系统；重启若出现“正在更新/为你自定义功能”属正常情况，请勿断电。", "* On non-Home editions this may prevent booting; if the reboot shows an update/customizing screen, that is normal - do not power off.");
            Rule("您真的要执行吗?", "Do you really want to run it?");

            Rule("请手动打开：", "Open manually: ");
            Rule("设置 - 账户 - 登录选项", "Settings - Accounts - Sign-in options");
            Rule("诊断信息已写入：", "Diagnostics written to: ");
            Rule("explorer(提权代理) 异常: ", "explorer (admin proxy) error: ");
            Rule("ShellExecuteW 返回码: ", "ShellExecuteW returned: ");
            Rule("ShellExecuteW 异常: ", "ShellExecuteW error: ");
            Rule("Process.Start 异常: ", "Process.Start error: ");
            Rule("cmd start 异常: ", "cmd start error: ");

            LoadHantMap();
        }

        static void Rule(string zh, string en) { Rules.Add(new string[] { zh, en }); }

        // 简 -> 繁：字表 + 少量台湾用语修正
        static readonly Dictionary<char, char> S2T = new Dictionary<char, char>();
        static readonly string[][] HantTerms = new string[][]
        {
            new string[] { "内存", "記憶體" },
            new string[] { "界面", "介面" },
            new string[] { "账户", "帳戶" },
            new string[] { "登录", "登入" },
            new string[] { "设置", "設定" }
        };

        static void LoadHantMap()
        {
            const string pairs =
                "关 關|闭 閉|还 還|为 為|测 測|试 試|终 終|质 質|当 當|统 統|态 態|于 於|虚 虛|拟 擬|内 內|" +
                "启 啟|动 動|类 類|获 獲|设 設|给 給|装 裝|组 組|须 須|脚 腳|运 運|记 記|录 錄|员 員|权 權|" +
                "确 確|认 認|检 檢|并 並|复 復|约 約|钟 鐘|项 項|请 請|监 監|将 將|备 備|绪 緒|执 執|来 來|" +
                "过 過|现 現|连 連|续 續|键 鍵|余 餘|吗 嗎|转 轉|后 後|软 軟|账 賬|户 戶|验 驗|证 證|败 敗|" +
                "强 強|议 議|临 臨|时 時|径 徑|选 選|变 變|继 繼|错 錯|误 誤|码 碼|严 嚴|进 進|环 環|电 電|" +
                "脑 腦|与 與|则 則|会 會|机 機|从 從|带 帶|里 裡|业 業|让 讓|义 義|属 屬|况 況|断 斷|样 樣|" +
                "读 讀|无 無|实 實|绝 絕|诊 診|写 寫|异 異|处 處|开 開|简 簡|体 體|发 發|务 務|标 標|图 圖|" +
                "线 線|视 視|频 頻|声 聲|档 檔|随 隨|块 塊|总 總|级 級|层 層|称 稱|数 數|据 據|进 進|过 過|" +
                "谢 謝|适 適|优 優|阴 陰|遗 遺|补 補|齐 齊|译 譯|馈 饋|页 頁|应 應|该 該|间 間|择 擇|载 載|" +
                "这 這|个 個|说 說|观 觀|见 見|专 專|达 達|尽 盡|边 邊|圆 圓|忆 憶|细 細|题 題|划 劃|币 幣|暂 暫|";

            foreach (var p in pairs.Split('|'))
            {
                var kv = p.Trim();
                if (kv.Length >= 3) S2T[kv[0]] = kv[2];
            }
        }

        public static string ToHant(string zh)
        {
            string s = zh;
            for (int i = 0; i < HantTerms.Length; i++) s = s.Replace(HantTerms[i][0], HantTerms[i][1]);
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char t;
                sb.Append(S2T.TryGetValue(s[i], out t) ? t : s[i]);
            }
            return sb.ToString();
        }

        public static string T(string zh)
        {
            if (string.IsNullOrEmpty(zh)) return zh;
            if (Hant) return ToHant(zh);
            if (!En) return zh;
            string hit;
            if (Exact.TryGetValue(zh, out hit)) return hit;
            for (int i = 0; i < Rules.Count; i++)
            {
                if (zh.IndexOf(Rules[i][0], StringComparison.Ordinal) >= 0)
                    zh = zh.Replace(Rules[i][0], Rules[i][1]);
            }
            return zh;
        }

        // 遍历可视树翻译：原文缓存在 Tag 里，保证来回切换不丢词
        public static void Apply(DependencyObject root)
        {
            if (root == null) return;
            var tb = root as TextBlock;
            if (tb != null)
            {
                if (tb.Tag is string && (string)tb.Tag == "") { }
                else
                {
                    if (!(tb.Tag is string)) tb.Tag = tb.Text;
                    tb.Text = T((string)tb.Tag);
                }
            }
            // 逻辑树遍历（页面尚未挂到可视树时也更可靠）
            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                Apply(child as DependencyObject);
            }
        }

        public static void Reset() { En = false; Hant = false; }

        public static string Load()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\DisableVBS"))
                {
                    string v = k == null ? null : (string)k.GetValue("Lang");
                    if (v == "en") { En = true; return v; }
                    if (v == "hant") { Hant = true; return v; }
                }
            }
            catch { }
            return "zh";
        }

        public static void Save()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\DisableVBS"))
                {
                    if (k != null) k.SetValue("Lang", En ? "en" : (Hant ? "hant" : "zh"));
                }
            }
            catch { }
        }
    }

    class StepRow : Grid
    {
        Ellipse node;
        Canvas spin;
        TextBlock mark, sub, sentence, badge;
        Border lineUp, lineDown;
        bool isLast;

        public StepRow(string badgeText, string sentenceText, bool first, bool last)
        {
            isLast = last;
            Height = 54;
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ---- 左侧竖向时间线 ----
            var rail = new Grid();
            rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            lineUp = new Border { Width = 1.4, Background = Th.Line, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(9, 0, 0, 0) };
            lineDown = new Border { Width = 1.4, Background = Th.Line, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(9, 0, 0, 0) };
            Grid.SetRow(lineUp, 0); Grid.SetRow(lineDown, 1);
            rail.Children.Add(lineUp); rail.Children.Add(lineDown);
            lineUp.Visibility = first ? Visibility.Collapsed : Visibility.Visible;
            lineDown.Visibility = last ? Visibility.Collapsed : Visibility.Visible;

            var dotHolder = new Grid { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRowSpan(dotHolder, 2);
            node = new Ellipse { Width = 11, Height = 11, StrokeThickness = 1.4, Stroke = Th.Faint, Fill = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            mark = new TextBlock { Text = "", FontFamily = Th.FF, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            spin = Spin.Make(18, 3.0, Th.Accent);
            spin.Visibility = Visibility.Collapsed;
            dotHolder.Children.Add(node); dotHolder.Children.Add(mark); dotHolder.Children.Add(spin);
            rail.Children.Add(dotHolder);
            Grid.SetColumn(rail, 0); Children.Add(rail);

            badge = new TextBlock { Text = badgeText, FontFamily = Th.Mono, FontSize = 11, Foreground = Th.Faint, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(badge, 1); Children.Add(badge);

            var txt = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            sentence = new TextBlock { Text = sentenceText, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Text, TextWrapping = TextWrapping.Wrap };
            txt.Children.Add(sentence);
            sub = new TextBlock { FontFamily = Th.FF, FontSize = 11.5, Foreground = Th.Accent, Margin = new Thickness(0, 4, 0, 0), Visibility = Visibility.Collapsed };
            txt.Children.Add(sub);
            Grid.SetColumn(txt, 2); Children.Add(txt);
        }

        public void SetState(int s, string subText)
        {
            bool isRunning = s == 1;
            spin.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            node.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
            mark.Visibility = (s == 2) ? Visibility.Visible : Visibility.Collapsed;

            if (s == 2)
            {
                node.Stroke = Th.Ok; node.Fill = Th.Ok;
                mark.Text = "\u2713"; mark.Foreground = Th.Hex("#06281F");
                sentence.Foreground = Th.Text; badge.Foreground = Th.Ok;
                lineUp.Background = Th.Glass(150, "#3ED89E");
                lineDown.Background = isLast ? Th.Line : Th.Glass(150, "#3ED89E");
            }
            else if (s == 1)
            {
                sentence.Foreground = Th.Text; badge.Foreground = Th.Accent;
                node.Visibility = Visibility.Collapsed; mark.Visibility = Visibility.Collapsed;
            }
            else
            {
                node.Stroke = Th.Faint; node.Fill = Brushes.Transparent;
                node.Width = 11; node.Height = 11;
                mark.Text = ""; sentence.Foreground = Th.Muted; badge.Foreground = Th.Faint;
                lineUp.Background = Th.Line;
                lineDown.Background = Th.Line;
            }

            if (!string.IsNullOrEmpty(subText) && s == 1)
            {
                sub.Text = subText; sub.Visibility = Visibility.Visible;
            }
            else sub.Visibility = Visibility.Collapsed;
        }
    }

    class MainWindow : Window
    {
        public static bool Simulate = false;
        const double ShellPad = 14;
        const double ShellRadius = 12;
        const double Body = 760;

        string LastFailDetail = "";
        TextBlock rebootLabel;
        DispatcherTimer rebootTimer;
        DispatcherTimer tm = new DispatcherTimer();
        List<StepRow> rows = new List<StepRow>();
        StackPanel logPanel;
        ScrollViewer logScroll;
        Border barFill;
        ScaleTransform barScale;
        TextBlock counter;
        string[] sentences;
        int[] ticks;
        string[] stepLogs;
        int step, tick, logIdx;
        bool running;
        Action rebuildCurrent;
        bool rebuildMode;
        bool lastFail;
        bool runStarted;
        bool menuLive;
        public TextBlock aboutStatus;
        bool detectStarted;
        string stVbs, stHvci, stHvl, stHv;

        public MainWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.Height;
            Width = Body + ShellPad * 2;
            tm.Interval = TimeSpan.FromMilliseconds(100);
            tm.Tick += OnTick;
            if (!Simulate) I18n.Load();   // 演示模式固定中文
            KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape && rebootTimer != null) CancelReboot(); };
            ShowMenu();
        }

        // ---------- 圆角外壳（分层窗口 + 投影 + 圆角裁切） ----------
        void HookClip(FrameworkElement el, double r)
        {
            el.SizeChanged += delegate(object s, SizeChangedEventArgs e)
            {
                var f = (FrameworkElement)s;
                if (f.ActualWidth > 0 && f.ActualHeight > 0)
                    f.Clip = new RectangleGeometry(new Rect(0, 0, f.ActualWidth, f.ActualHeight), r, r);
            };
        }

        // 外阴影改用一次性烘焙的位图：分层窗口每帧都做 30px 模糊会掉到十几帧，
        // 烘焙后只有页面切换时刷一次，成本降到毫秒级。
        static readonly Dictionary<string, BitmapSource> ShadowCache = new Dictionary<string, BitmapSource>();

        static BitmapSource ShadowPic(double w, double h)
        {
            string key = (int)Math.Round(w) + "x" + (int)Math.Round(h);
            BitmapSource bs;
            if (ShadowCache.TryGetValue(key, out bs)) return bs;

            var g = new Grid { Width = w, Height = h, Background = Brushes.Transparent };
            g.Children.Add(new Border
            {
                Margin = new Thickness(ShellPad),
                CornerRadius = new CornerRadius(ShellRadius),
                Background = Brushes.Black,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black, BlurRadius = 30, ShadowDepth = 8, Direction = 315, Opacity = 0.55
                }
            });
            g.Measure(new Size(w, h));
            g.Arrange(new Rect(0, 0, w, h));

            const double sc = 1.0;                       // 原分辨率烘焙，直接 1:1 贴图
            int pw = Math.Max(2, (int)Math.Round(w * sc));
            int ph = Math.Max(2, (int)Math.Round(h * sc));
            var rtb = new RenderTargetBitmap(pw, ph, 96 * sc, 96 * sc, PixelFormats.Pbgra32);
            rtb.Render(g);

            // 关键：上面那块黑色 Border 是为了产生投影，但它在位图里是一整块不透明黑，
            // 正好覆盖外壳区域。直接贴上去等于在外壳下面垫了张实心底板，alpha 全废。
            // 这里把中间（外壳覆盖的范围）抠掉，只留外圈光晕。
            var halo = new RenderTargetBitmap(pw, ph, 96 * sc, 96 * sc, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var full = new RectangleGeometry(new Rect(0, 0, pw, ph));
                var hole = new RectangleGeometry(
                    new Rect(ShellPad, ShellPad, pw - ShellPad * 2, ph - ShellPad * 2), ShellRadius, ShellRadius);
                dc.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude, full, hole));
                dc.DrawImage(rtb, new Rect(0, 0, pw, ph));
                dc.Pop();
            }
            halo.Render(dv);
            halo.Freeze();
            bs = halo;
            ShadowCache[key] = bs;
            return bs;
        }

        UIElement Shell(UIElement inner)
        {
            var face = new Border
            {
                Child = inner,
                CornerRadius = new CornerRadius(ShellRadius),
                Background = Th.Bg,
                BorderBrush = Th.LineHi,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(ShellPad)
            };
            // 注意：face 上不能挂 Clip。硬裁切会让 WPF 先把子树合成到一张不透明中间位图，
            // 再贴到分层窗口上，结果就是 alpha 完全失效、窗口变成实心黑。
            // 圆角交给 CornerRadius，子内容的溢出由各自的 CornerRadius / OpacityMask 处理。

            // 阴影改成一次性烘焙的位图，不再挂 DropShadowEffect：
            // 分层窗口每帧重做 30px 模糊，整窗重绘要 100ms 左右，切页会明显顿一下。
            // Rectangle + ImageBrush 铺满整个窗口：Rectangle 不参与测量，不会被位图尺寸反推窗口大小。
            var brush = new ImageBrush { Stretch = Stretch.Fill, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
            var shade = new System.Windows.Shapes.Rectangle { Fill = brush, IsHitTestVisible = false };
            var root = new Grid { Background = Brushes.Transparent, ClipToBounds = false };
            root.Children.Add(shade);
            root.Children.Add(face);
            root.SizeChanged += delegate
            {
                if (brush.ImageSource != null) return;
                var w = root.ActualWidth;
                var h = root.ActualHeight;
                if (w <= 0 || h <= 0) return;
                // 烘焙要跑一次模糊，放到布局之后做，别在布局过程中重入测量
                root.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (brush.ImageSource == null) brush.ImageSource = ShadowPic(w, h);
                }), System.Windows.Threading.DispatcherPriority.Background);
            };
            return root;
        }
        void Navigate(UIElement page)
        {
            var sp = new StackPanel();
            sp.Children.Add(HeaderBar());
            sp.Children.Add(new Border { Height = 1, Background = Th.Line });
            sp.Children.Add(page);
            sp.Children.Add(FooterBar());
            I18n.Apply(sp);
            // 切语言时的重建不走淡入：分层窗口 + 外阴影逐帧重绘很费，动画会明显顿一下
            if (rebuildMode)
            {
                sp.Opacity = 1;
                Content = Shell(sp);
                return;
            }
            sp.Opacity = 0;
            Content = Shell(sp);
            sp.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)));
        }

        UIElement FooterBar()
        {
            var g = new Grid { Height = 38, Background = Th.Footer };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            var pill = new Border
            {
                Background = Th.Tint(Th.C("#5B93FF"), 30),
                BorderBrush = Th.Glass(110, "#5B93FF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(9, 2, 9, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "更新说明",
                Child = new TextBlock
                {
                    Text = AppVer,
                    FontFamily = Th.Mono, FontSize = 10.5, FontWeight = FontWeights.Bold,
                    Foreground = Th.Accent
                }
            };
            pill.MouseEnter += delegate { pill.Background = Th.Tint(Th.C("#5B93FF"), 46); };
            pill.MouseLeave += delegate { pill.Background = Th.Tint(Th.C("#5B93FF"), 30); };
            pill.MouseLeftButtonUp += delegate { ShowAbout(); };
            Grid.SetColumn(pill, 1); g.Children.Add(pill);

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var ghIcon = new Path
            {
                Data = Geometry.Parse("M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12"),
                Fill = Th.Faint,
                Stretch = Stretch.Uniform,
                Width = 13, Height = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var gh = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent, Cursor = Cursors.Hand,
                ToolTip = "https://github.com/HOSHINO61/DisableVBS",
                Child = ghIcon
            };
            gh.MouseEnter += delegate { ghIcon.Fill = Th.Text; gh.Background = Th.Hover; };
            gh.MouseLeave += delegate { ghIcon.Fill = Th.Faint; gh.Background = Brushes.Transparent; };
            gh.MouseLeftButtonUp += delegate
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "https://github.com/HOSHINO61/DisableVBS") { UseShellExecute = true });
                }
                catch { }
            };
            right.Children.Add(gh);

            var osLine = OsInfo();
            if (osLine.Length > 0)
            {
                var osText = new TextBlock
                {
                    Text = "OS：" + osLine,
                    FontFamily = Th.FF, FontSize = 11, Foreground = Th.Faint,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 8, 0),
                    Tag = ""
                };
                right.Children.Insert(0, osText);
            }
            Grid.SetColumn(right, 3); g.Children.Add(right);
            return g;
        }

        UIElement HeaderBar()
        {
            var g = new Grid { Height = 74, Background = Th.Header };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bar = new Border
            {
                Width = 4, Height = 26,
                Background = Th.VGrad(Th.C("#84ADFF"), Th.C("#3F79F2")),
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(bar, 1); g.Children.Add(bar);

            var title = new TextBlock
            {
                Text = "关闭/还原 Hyper-V和VBS",
                FontFamily = Th.FF, FontSize = 18.5, FontWeight = FontWeights.Bold,
                Foreground = Th.Text, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 3); g.Children.Add(title);

            var caps = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
            caps.Children.Add(CapBtn("\u2014", false, delegate { WindowState = WindowState.Minimized; }));
            caps.Children.Add(CapBtn("\u2715", true, delegate { Close(); }));

            var rightBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 8, 8, 0) };
            rightBox.Children.Add(LangPill());
            rightBox.Children.Add(caps);
            Grid.SetColumn(rightBox, 4); g.Children.Add(rightBox);

            g.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { if (me.LeftButton == MouseButtonState.Pressed) { try { DragMove(); } catch { } } };
            return g;
        }

        // ---- 语言切换：胶囊按钮 + 弹出列表（简 / EN） ----
        UIElement LangPill()
        {
            var txt = new TextBlock
            {
                Text = I18n.En ? "EN" : (I18n.Hant ? "繁" : "简"),
                FontFamily = Th.Mono, FontSize = 10.5, FontWeight = FontWeights.Bold,
                Foreground = Th.Accent, VerticalAlignment = VerticalAlignment.Center
            };
            txt.Tag = "";   // 不参与自动翻译

            var caret = new TextBlock
            {
                Text = "\u25be", FontFamily = Th.FF, FontSize = 8, Foreground = Th.Muted,
                Margin = new Thickness(5, 1, 0, 0), VerticalAlignment = VerticalAlignment.Center
            };

            var inner = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            inner.Children.Add(txt);
            inner.Children.Add(caret);

            var b = new Border
            {
                Width = 46, Height = 26, CornerRadius = new CornerRadius(7),
                Background = Th.Glass(34, "#FFFFFF"),
                BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                Child = inner
            };
            b.MouseEnter += delegate { b.Background = Th.Glass(64, "#FFFFFF"); b.BorderBrush = Th.LineHi; };
            b.MouseLeave += delegate { b.Background = Th.Glass(34, "#FFFFFF"); b.BorderBrush = Th.Line; };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; OpenLangMenu(b); };
            return b;
        }

        void OpenLangMenu(FrameworkElement anchor)
        {
            var pop = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = anchor,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                VerticalOffset = 6
            };
            pop.Child = LangCard(pop);
            pop.IsOpen = true;
        }

        Border LangCard(System.Windows.Controls.Primitives.Popup pop)
        {
            var list = new StackPanel();

            var card = new Border
            {
                Width = 172,
                Background = Th.VGrad(Th.C("#1A212C"), Th.C("#111720")),
                BorderBrush = Th.LineHi, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6),
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 4, Direction = 315, Opacity = 0.5 }
            };
            card.Child = list;
            HookClip(card, 10);
            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; };

            int cur = I18n.En ? 2 : (I18n.Hant ? 1 : 0);
            list.Children.Add(LangRow(pop, "简", "简体中文", cur == 0, 0));
            list.Children.Add(LangRow(pop, "繁", "繁體中文", cur == 1, 1));
            list.Children.Add(LangRow(pop, "EN", "English", cur == 2, 2));
            return card;
        }

        public Border DemoLangCard()
        {
            return LangCard(new System.Windows.Controls.Primitives.Popup());
        }

        UIElement LangRow(System.Windows.Controls.Primitives.Popup pop, string badge, string label, bool active, int mode)
        {
            var badgeBox = new Border
            {
                Width = 24, Height = 20, CornerRadius = new CornerRadius(5),
                Background = active ? Th.Tint(Th.C("#5B93FF"), 46) : Th.Glass(58, "#FFFFFF"),
                BorderBrush = active ? Th.Accent : Th.Line,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = badge, FontFamily = Th.Mono, FontSize = 10.5, FontWeight = FontWeights.Bold,
                    Foreground = active ? Th.Accent : Th.Body,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            };

            var text = new TextBlock
            {
                Text = label, FontFamily = Th.FF, FontSize = 12.5,
                Foreground = active ? Th.Accent : Th.Text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(badgeBox, 0); g.Children.Add(badgeBox);
            Grid.SetColumn(text, 1); g.Children.Add(text);

            var row = new Border
            {
                Height = 36, CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 0, 8, 0),
                Background = active ? Th.Tint(Th.C("#5B93FF"), 40) : Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = g
            };
            row.MouseEnter += delegate { if (!active) row.Background = Th.Glass(56, "#FFFFFF"); };
            row.MouseLeave += delegate { if (!active) row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; };
            row.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs me)
            {
                me.Handled = true;
                pop.IsOpen = false;
                int now = I18n.En ? 2 : (I18n.Hant ? 1 : 0);
                if (now == mode) return;
                I18n.Reset();
                if (mode == 1) I18n.Hant = true;
                else if (mode == 2) I18n.En = true;
                I18n.Save();
                if (rebuildCurrent != null)
                {
                    rebuildMode = true;
                    rebuildCurrent();
                    rebuildMode = false;
                }
            };
            return row;
        }

        UIElement CapBtn(string glyph, bool isClose, Action act)
        {
            var label = new TextBlock
            {
                Text = glyph, FontFamily = Th.FF, FontSize = 11, Foreground = Th.Muted,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            var b = new Border
            {
                Width = 34, Height = 26, CornerRadius = new CornerRadius(7),
                Background = Brushes.Transparent, Cursor = Cursors.Hand,
                Child = label
            };
            b.MouseEnter += delegate
            {
                b.Background = isClose ? Th.Tint(Th.C("#FF7F7F"), 44) : Th.Glass(46, "#FFFFFF");
                label.Foreground = isClose ? Th.Bad : Th.Text;
            };
            b.MouseLeave += delegate
            {
                b.Background = Brushes.Transparent;
                label.Foreground = Th.Muted;
            };
            // 关键：阻止按下事件冒泡到标题栏，否则 DragMove() 会抢走鼠标导致点击失效
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; act(); };
            return b;
        }

        TextBlock T(string text, double size, Brush fg, bool bold)
        {
            return new TextBlock
            {
                Text = text, FontFamily = Th.FF, FontSize = size, Foreground = fg,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        Border CardB(UIElement child, double h, Thickness margin)
        {
            var b = new Border
            {
                Child = child, Height = h, Margin = margin,
                Background = Th.Card,
                BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10)
            };
            HookClip(b, 10);
            return b;
        }

        Border ActionBtn(string text, bool primary, Action act, double w)
        {
            var label = new TextBlock
            {
                Text = text, FontFamily = Th.FF, FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = primary ? Brushes.White : Th.Text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Brush bgNormal, bgHover, brdNormal, brdHover;
            if (primary)
            {
                bgNormal = Th.HGrad(Th.C("#6FA0FF"), Th.C("#3F79F2"));
                bgHover = Th.HGrad(Th.C("#84B0FF"), Th.C("#4C86F5"));
                brdNormal = Th.Glass(170, "#9CC0FF");
                brdHover = Th.Glass(210, "#B8D4FF");
            }
            else
            {
                bgNormal = Brushes.Transparent;
                bgHover = Th.Glass(56, "#FFFFFF");
                brdNormal = Th.Line;
                brdHover = Th.LineHi;
            }

            var b = new Border
            {
                Width = w > 0 ? w : double.NaN,
                MinWidth = w > 0 ? 0 : 96,
                Padding = w > 0 ? new Thickness(0) : new Thickness(16, 0, 16, 0),
                Height = 40, CornerRadius = new CornerRadius(9),
                Background = bgNormal,
                BorderBrush = brdNormal,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                SnapsToDevicePixels = true,
                Child = label,
                RenderTransform = new TranslateTransform(0, 0)
            };
            var lift = (TranslateTransform)b.RenderTransform;

            b.MouseEnter += delegate
            {
                b.Background = bgHover;
                b.BorderBrush = brdHover;
                label.Foreground = primary ? Brushes.White : Th.Text;
            };
            b.MouseLeave += delegate
            {
                b.Background = bgNormal;
                b.BorderBrush = brdNormal;
                lift.Y = 0;
            };
            // 关键：按钮必须自己吞掉按下事件，否则会冒泡到容器上的 DragMove()，点击失效
            DateTime lastAct = DateTime.MinValue;
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; lift.Y = 1; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs me)
            {
                me.Handled = true;
                lift.Y = 0;
                if ((DateTime.Now - lastAct).TotalMilliseconds < 350) return;   // 防双击重复触发
                lastAct = DateTime.Now;
                act();
            };
            return b;
        }

        bool PendingReboot()
        {
            try
            {
                using (var cbs = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing"))
                {
                    if (cbs == null) return false;
                    using (var rp = cbs.OpenSubKey("RebootPending")) { if (rp != null) return true; }
                    using (var pp = cbs.OpenSubKey("PackagesPending")) { if (pp != null) return true; }
                }
            }
            catch { return false; }   // 读不到就不拦，避免误报
            return false;
        }

        Brush Pick(string v, Brush whenOn, Brush whenOff)
        {
            if (v == "未知") return Th.Muted;
            if (v.StartsWith("已开启") || v.StartsWith("已启用")) return whenOn;
            return whenOff;
        }

        UIElement SectionLabel(string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            sp.Children.Add(new Border
            {
                Width = 3, Height = 13,
                Background = Th.VGrad(Th.C("#84ADFF"), Th.C("#3F79F2")),
                CornerRadius = new CornerRadius(1.5),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(T(text, 12, Th.Muted, true));
            return sp;
        }

        // ---------- 状态瓷砖（2×2） ----------
        Border StatusTile(string name, string value, Brush dot)
        {
            var c = ((SolidColorBrush)dot).Color;
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var head = new StackPanel { Orientation = Orientation.Horizontal };
            head.Children.Add(new Border
            {
                Width = 7, Height = 7, CornerRadius = new CornerRadius(4),
                Background = dot, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            });
            head.Children.Add(new TextBlock
            {
                Text = name, FontFamily = Th.FF, FontSize = 11.5, Foreground = Th.Muted,
                VerticalAlignment = VerticalAlignment.Center
            });
            g.Children.Add(head);

            var v = new TextBlock
            {
                Text = value, FontFamily = Th.FF, FontSize = 17, FontWeight = FontWeights.Bold,
                Foreground = dot, Margin = new Thickness(14, 6, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(v, 1); g.Children.Add(v);

            var b = new Border
            {
                Height = 78,
                CornerRadius = new CornerRadius(10),
                Background = Th.VGrad(Th.With(c, 26), Th.With(c, 10)),
                BorderBrush = Th.Glass(78, "#93A8C6"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 11, 14, 10),
                Child = g
            };
            return b;
        }

        const string LoadingText = "正在读取…";

        // 检测要跑 powershell / bcdedit 子进程，一次一两秒；放到后台线程做，
        // 主页面先用占位值立刻显示，值回来再刷一次。之后一律用缓存，切语言不再重新检测。
        void EnsureDetect()
        {
            if (Simulate || detectStarted) return;
            detectStarted = true;
            var th = new System.Threading.Thread(delegate()
            {
                string a = "", b = "", c = "", d = "";
                var t1 = new System.Threading.Thread(delegate() { try { a = Detect.Vbs(); } catch { } });
                var t2 = new System.Threading.Thread(delegate() { try { b = Detect.Hvci(); } catch { } });
                var t3 = new System.Threading.Thread(delegate() { try { c = Detect.Hvl(); } catch { } });
                var t4 = new System.Threading.Thread(delegate() { try { d = Detect.Hv(); } catch { } });
                t1.IsBackground = t2.IsBackground = t3.IsBackground = t4.IsBackground = true;
                t1.Start(); t2.Start(); t3.Start(); t4.Start();
                t1.Join(); t2.Join(); t3.Join(); t4.Join();
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    stVbs = a; stHvci = b; stHvl = c; stHv = d;
                    if (menuLive) ShowMenu();
                }));
            });
            th.IsBackground = true;
            th.Start();
        }

        UIElement StatusGrid()
        {
            string vbs, hvci, hvl, hv;
            bool pend = false;
            if (Simulate)
            {
                vbs = "已关闭"; hvci = "已关闭"; hvl = "已禁用 (关闭)"; hv = "已启用";
            }
            else
            {
                EnsureDetect();
                pend = stVbs == null;
                vbs = pend ? LoadingText : stVbs;
                hvci = pend ? LoadingText : stHvci;
                hvl = pend ? LoadingText : stHvl;
                hv = pend ? LoadingText : stHv;
            }
            Brush c1 = pend ? Th.Muted : Pick(vbs, Th.Bad, Th.Ok);
            Brush c2 = pend ? Th.Muted : Pick(hvci, Th.Bad, Th.Ok);
            Brush c3 = pend ? Th.Muted : Pick(hvl, Th.Warn, Th.Ok);
            Brush c4 = pend ? Th.Muted : Pick(hv, Th.Accent, Th.Muted);

            var grid = new Grid { Height = 170 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var t1 = StatusTile("VBS（基于虚拟化的安全）", vbs, c1);
            t1.Margin = new Thickness(0, 0, 7, 7);
            var t2 = StatusTile("HVCI（内存完整性）", hvci, c2);
            t2.Margin = new Thickness(7, 0, 0, 7);
            var t3 = StatusTile("Hypervisor 启动类型", hvl, c3);
            t3.Margin = new Thickness(0, 7, 7, 0);
            var t4 = StatusTile("Hyper-V 功能", hv, c4);
            t4.Margin = new Thickness(7, 7, 0, 0);

            Grid.SetColumn(t2, 1); Grid.SetRow(t3, 1); Grid.SetColumn(t4, 1); Grid.SetRow(t4, 1);
            grid.Children.Add(t1); grid.Children.Add(t2); grid.Children.Add(t3); grid.Children.Add(t4);
            return grid;
        }

        // ---------- 菜单（合为一张列表卡） ----------
        UIElement MenuList()
        {
            var list = new StackPanel();
            string[] codes = new string[] { "1", "2", "3", "4", "5", "0" };
            string[] caps = new string[] {
                "关闭VBS和HVCI",
                "还原VBS和HVCI",
                "获取系统配置",
                "设置HVCI",
                "【家庭版/家庭中文版】给系统安装 Hyper-V组件（装完必须重启）",
                "退出脚本"
            };
            for (int i = 0; i < codes.Length; i++)
            {
                if (i > 0)
                {
                    var sep = new Border { Height = 1, Background = Th.Line, Margin = new Thickness(58, 0, 16, 0) };
                    list.Children.Add(sep);
                }
                list.Children.Add(MenuRow(codes[i], caps[i]));
            }

            var card = new Border
            {
                Background = Th.Card,
                BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 16, 0, 0),
                Child = new Border { Padding = new Thickness(0, 8, 0, 8), Child = list }
            };
            HookClip(card, 12);
            return card;
        }

        UIElement MenuRow(string code, string caption)
        {
            var key = new Border
            {
                Width = 24, Height = 24, CornerRadius = new CornerRadius(7),
                Background = Th.Glass(40, "#FFFFFF"),
                BorderBrush = Th.Glass(110, "#B7C6DC"),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = code, FontFamily = Th.Mono, FontSize = 11.5, FontWeight = FontWeights.Bold,
                    Foreground = Th.Accent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var cap = T(caption, 13.5, Th.Text, false);
            cap.TextTrimming = TextTrimming.CharacterEllipsis;

            var chevron = T("\u203A", 15, Th.Faint, false);
            chevron.HorizontalAlignment = HorizontalAlignment.Right;
            chevron.Margin = new Thickness(0, 0, 2, 0);

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            key.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(key, 0); g.Children.Add(key);
            Grid.SetColumn(cap, 1); g.Children.Add(cap);
            Grid.SetColumn(chevron, 2); g.Children.Add(chevron);

            var bd = new Border
            {
                Height = 44, Background = Brushes.Transparent, Cursor = Cursors.Hand,
                Padding = new Thickness(16, 0, 14, 0),
                Child = g
            };
            bd.MouseEnter += delegate
            {
                bd.Background = Th.Hover;
                key.BorderBrush = Th.Accent;
                key.Background = Th.Tint(Th.C("#5B93FF"), 40);
                chevron.Foreground = Th.Accent;
            };
            bd.MouseLeave += delegate
            {
                bd.Background = Brushes.Transparent;
                key.BorderBrush = Th.Glass(110, "#B7C6DC");
                key.Background = Th.Glass(40, "#FFFFFF");
                chevron.Foreground = Th.Faint;
            };
            bd.MouseLeftButtonUp += delegate { OnMenu(code); };
            return bd;
        }

        void ShowMenu()
        {
            running = false; tm.Stop();
            runStarted = false;
            menuLive = true;
            rebuildCurrent = ShowMenu;
            var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 22) };
            sp.Children.Add(SectionLabel("当前系统状态"));
            sp.Children.Add(StatusGrid());
            sp.Children.Add(MenuList());
            Navigate(sp);
        }

        void OnMenu(string code)
        {
            if (code == "0") { Close(); return; }
            if (code == "3" || code == "4")
            {
                if (Simulate)
                {
                    DarkMessage("样机", "本项为只读操作（dxdiag / 安全中心），未模拟。", "确定");
                    return;
                }
                try
                {
                    if (code == "3") System.Diagnostics.Process.Start("dxdiag.exe");
                    else System.Diagnostics.Process.Start("windowsdefender:");
                }
                catch (Exception ex) { DarkMessage("无法打开", ex.Message, "确定"); }
                return;
            }
            if (PendingReboot())
            {
                if (Ask("检测到待处理变更",
                    "上一次操作尚未重启生效。\n\n"
                  + "现在继续执行，部分功能可能返回错误码 50 或 3010，\n"
                  + "严重时重启会进入恢复环境。\n\n"
                  + "请先重启电脑，再执行本项操作。",
                    540, 268, "立即重启"))
                {
                    BeginReboot();
                }
                return;
            }
            if (code == "1") { ShowPinConfirm(); return; }
            if (code == "2")
            {
                if (Ask("还原 VBS 和内存完整性",
                    "即将还原 VBS 与内存完整性，这样做可以提升系统安全性\n"
                  + "但是，若您需要使用VMware等虚拟机，则不建议还原，\n"
                  + "完成后需要重启才会生效。\n\n"
                  + "您真的要还原吗?", 470, 250)) StartRun("2");
                return;
            }
            if (code == "5")
            {
                if (Ask("给系统安装 Hyper-V",
                    "本机 Hyper-V 功能当前状态：已启用（已启用时则不建议执行）\n\n"
                  + "本项会从系统自带的包目录里添加 Hyper-V 组件\n\n"
                  + "* Pro / 企业版 / 工作站版本本身就带 Hyper-V，不需要执行本项。\n"
                  + "* 执行完成后必须重启，让系统安装完，再继续操作。\n"
                  + "* 在非家庭版上执行后可能进不去系统；重启若出现“正在更新/为你自定义功能”属正常情况，请勿断电。\n\n"
                  + "您真的要执行吗?", 650, 380)) StartRun("5");
                return;
            }
            StartRun(code);
        }

        bool Ask(string title, string body, int w, int h)
        {
            return Ask(title, body, w, h, "继续");
        }

        bool Ask(string title, string body, int w, int h, string okText)
        {
            bool ok = false;
            DateTime openedAt = DateTime.Now;
            var d = new Window
            {
                Owner = this, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true, Background = Brushes.Transparent,
                SizeToContent = SizeToContent.Manual,
                Width = w + ShellPad * 2, Height = h + ShellPad * 2 + (I18n.En ? 34 : 0),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };

            var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };
            sp.Children.Add(new TextBlock { Text = title, FontFamily = Th.FF, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Th.Text, Margin = new Thickness(0, 0, 0, 14) });
            sp.Children.Add(new TextBlock { Text = body, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, LineHeight = 22 });

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
            btns.Children.Add(ActionBtn("取消", false, delegate
            {
                if ((DateTime.Now - openedAt).TotalMilliseconds < 450) return;
                d.Close();
            }, 116));
            var okBtn = ActionBtn(okText, true, delegate
            {
                if ((DateTime.Now - openedAt).TotalMilliseconds < 450) return;   // 挡住双击串进来的那一下
                ok = true; d.Close();
            }, 116);
            okBtn.Margin = new Thickness(12, 0, 0, 0);
            btns.Children.Add(okBtn);
            sp.Children.Add(btns);

            I18n.Apply(sp);
            var shell = Shell(sp);
            d.Content = shell;
            d.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape) d.Close(); };
            shell.MouseLeftButtonDown += delegate { try { d.DragMove(); } catch { } };
            d.ShowDialog();
            return ok;
        }

        static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        // 点左下角版本胶囊打开：更新说明 + 仓库/反馈入口
        public Window BuildAbout()
        {
            var d = new Window
            {
                Owner = this, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true, Background = Brushes.Transparent,
                SizeToContent = SizeToContent.Height, Width = 620 + ShellPad * 2,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };

            var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };
            sp.Children.Add(new TextBlock
            {
                Text = "更新说明", FontFamily = Th.FF, FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = Th.Text
            });
            sp.Children.Add(new Border
            {
                Height = 1, Background = Th.Line, Margin = new Thickness(0, 12, 0, 14)
            });

            // 版本号 / 更新日期 / 系统版本：按当前语言直接拼好，Tag 置空让 Apply 跳过
            string sep = I18n.En ? ": " : "：";
            string meta = I18n.T("版本号") + sep + AppVer + "\n"
                        + I18n.T("更新日期") + sep + AppDate;
            string os = OsInfo();
            if (os.Length > 0) meta += "\n" + I18n.T("系统") + sep + os;
            var metaBlock = new TextBlock
            {
                Text = meta, FontFamily = Th.FF, FontSize = 11.5, Foreground = Th.Muted,
                TextWrapping = TextWrapping.Wrap, LineHeight = 19, MaxWidth = 580, Tag = ""
            };
            sp.Children.Add(metaBlock);

            sp.Children.Add(new TextBlock
            {
                Text = I18n.NoteZh, FontFamily = Th.FF, FontSize = 12, Foreground = Th.Body,
                TextWrapping = TextWrapping.Wrap, LineHeight = 20, MaxWidth = 580,
                Margin = new Thickness(0, 12, 0, 0)
            });

            var status = new TextBlock
            {
                Text = "", FontFamily = Th.FF, FontSize = 11.5, Foreground = Th.Muted,
                TextWrapping = TextWrapping.Wrap, MaxWidth = 580, Margin = new Thickness(0, 20, 0, 0)
            };
            sp.Children.Add(status);
            aboutStatus = status;

            var btns = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            btns.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var chk = ActionBtn("检查更新", false, delegate { CheckUpdate(status); }, 148);
            Grid.SetColumn(chk, 0); btns.Children.Add(chk);

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            right.Children.Add(ActionBtn("GitHub 仓库", false, delegate { OpenUrl("https://github.com/HOSHINO61/DisableVBS"); }, 128));
            var issues = ActionBtn("反馈问题", false, delegate
            {
                // 先把版本与系统信息放进剪贴板，方便直接粘到 issue 里
                try
                {
                    string osTxt = OsInfo();
                    Clipboard.SetText("DisableVBS " + AppVer + (osTxt.Length > 0 ? "\n" + osTxt : ""));
                }
                catch { }
                OpenUrl("https://github.com/HOSHINO61/DisableVBS/issues");
            }, 148);
            issues.ToolTip = I18n.T("会先把版本与系统信息复制到剪贴板");
            issues.Margin = new Thickness(10, 0, 0, 0);
            right.Children.Add(issues);
            var ok = ActionBtn("关闭", true, delegate { d.Close(); }, 108);
            ok.Margin = new Thickness(10, 0, 0, 0);
            right.Children.Add(ok);
            Grid.SetColumn(right, 1); btns.Children.Add(right);
            sp.Children.Add(btns);

            I18n.Apply(sp);
            var shell = Shell(sp);
            d.Content = shell;
            d.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape) d.Close(); };
            shell.MouseLeftButtonDown += delegate { try { d.DragMove(); } catch { } };
            return d;
        }

        void ShowAbout() { BuildAbout().ShowDialog(); }

        // 界面显示带括号，tag 用连字符写法 1.0.1-Release261127，两者靠 VerParts 归一化对上
        const string AppVer = "1.0.1 (Release 261127)";
        const string AppDate = "2026-11-27";
        const string RepoApi = "https://api.github.com/repos/HOSHINO61/DisableVBS/releases";
        const string RepoReleases = "https://github.com/HOSHINO61/DisableVBS/releases";

        static string HttpGet(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "DisableVBS";
            req.Accept = "application/vnd.github+json";
            req.Timeout = 8000;
            req.ReadWriteTimeout = 8000;
            using (var resp = req.GetResponse())
            using (var reader = new System.IO.StreamReader(resp.GetResponseStream()))
                return reader.ReadToEnd();
        }

        // 归一化：界面 "1.0.1 (Release 261127)" 与 tag "1.0.1-Release261127" 统一成同一形态再比较
        static string NormVer(string v)
        {
            return Regex.Replace((v ?? "").Trim(), @"[\(\)\s]+", "-").Trim('-');
        }

        // 返回 [主, 次, 修订, 阶段, 日期]
        // 阶段：Beta 取序号，正式版记 999（比同版本的 Beta 新）；日期：Release 的 6 位编号，用于同版本再发一次时区分
        static int[] VerParts(string v)
        {
            var m = Regex.Match(NormVer(v),
                @"([0-9]+)\.([0-9]+)\.([0-9]+)(?:-?[Bb]eta[-]*([0-9]*))?(?:.*?[Rr]elease[-\s]*([0-9]{6}))?");
            if (!m.Success) return new int[] { 0, 0, 0, 999, 0 };
            int stage = 999;
            if (m.Groups[4].Success)
                stage = m.Groups[4].Value.Length == 0 ? 1 : int.Parse(m.Groups[4].Value);
            int date = 0;
            if (m.Groups[5].Success) date = int.Parse(m.Groups[5].Value);
            return new int[] { int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), stage, date };
        }

        static bool IsNewer(string local, string remote)
        {
            var a = VerParts(local);
            var b = VerParts(remote);
            for (int i = 0; i < 5; i++)
            {
                if (b[i] != a[i]) return b[i] > a[i];
            }
            return false;
        }

        class NoReleaseException : Exception { }

        // ---------- 系统版本（注册表直读，不启子进程，毫秒级，不用进后台线程） ----------
        static readonly string[][] OsEditions = new string[][]
        {
            new string[] { "Core", "家庭版", "Home" },
            new string[] { "CoreSingleLanguage", "家庭单语言版", "Home Single Language" },
            new string[] { "CoreCountrySpecific", "家庭中文版", "Home China" },
            new string[] { "Professional", "专业版", "Pro" },
            new string[] { "ProfessionalN", "专业版 N", "Pro N" },
            new string[] { "ProfessionalWorkstation", "专业工作站版", "Pro for Workstations" },
            new string[] { "ProfessionalEducation", "专业教育版", "Pro Education" },
            new string[] { "Enterprise", "企业版", "Enterprise" },
            new string[] { "EnterpriseN", "企业版 N", "Enterprise N" },
            new string[] { "EnterpriseS", "企业版 LTSC", "Enterprise LTSC" },
            new string[] { "Education", "教育版", "Education" },
            new string[] { "IoTEnterprise", "IoT 企业版", "IoT Enterprise" },
            new string[] { "ServerStandard", "服务器标准版", "Server Standard" },
            new string[] { "ServerDatacenter", "服务器数据中心版", "Server Datacenter" }
        };

        static string OsEditionName(string id, bool en)
        {
            for (int i = 0; i < OsEditions.Length; i++)
                if (OsEditions[i][0] == id) return en ? OsEditions[i][2] : OsEditions[i][1];
            return id ?? "";
        }

        static string osName, osDisp, osEd, osBuild;
        static int osUbr = -1;
        static bool osRead;

        // 全语言统一格式：Windows 11 Pro 24H2 (build 26100.2314)
        static string OsInfo()
        {
            if (!osRead)
            {
                osRead = true;
                try
                {
                    using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (k != null)
                        {
                            osName = "" + k.GetValue("ProductName");
                            osDisp = "" + k.GetValue("DisplayVersion");
                            osBuild = "" + k.GetValue("CurrentBuildNumber");
                            osEd = "" + k.GetValue("EditionID");
                            var u = k.GetValue("UBR");
                            if (u != null) int.TryParse(u.ToString(), out osUbr);
                        }
                    }
                }
                catch { }
                if (osName == null) osName = osDisp = osEd = osBuild = "";
            }

            int bn = 0;
            int.TryParse(osBuild, out bn);
            if (bn == 0 && osName.Length == 0) return "";

            // Win11 的 ProductName 仍写着 "Windows 10"，只能靠 build 号区分（22000 起）
            string win = (bn > 0 && bn < 22000) || (bn == 0 && osName.IndexOf("Windows 10", StringComparison.OrdinalIgnoreCase) >= 0)
                ? "Windows 10" : "Windows 11";
            if (osName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0) win = osName;

            var sb = new System.Text.StringBuilder();
            sb.Append(win);
            // 全语言统一：版本名一律用英文（Pro / Home …），不随界面语言变
            string edName = OsEditionName(osEd, true);
            if (edName.Length > 0) sb.Append(" ").Append(edName);
            if (osDisp != null && osDisp.Length > 0) sb.Append(" ").Append(osDisp);
            if (bn > 0)
            {
                sb.Append(" (build ").Append(bn);
                if (osUbr >= 0) sb.Append(".").Append(osUbr);
                sb.Append(")");
            }
            return sb.ToString();
        }

        // 直接走 GitHub Releases API：取列表里比本地新的第一条（含 prerelease），未鉴权每小时 60 次足够
        public void CheckUpdate(TextBlock status)
        {
            status.Foreground = Th.Muted;
            status.Cursor = Cursors.Arrow;
            status.Text = I18n.T("正在检查更新…");
            var th = new System.Threading.Thread(delegate()
            {
                string tag = null, page = RepoReleases, txt;
                try
                {
                    string json;
                    try { json = HttpGet(RepoApi); }
                    catch (WebException we)
                    {
                        // 404 = 仓库存在但还没发过 Release，不是网络问题，要分开提示
                        var r404 = we.Response as HttpWebResponse;
                        if (r404 != null && r404.StatusCode == HttpStatusCode.NotFound)
                            throw new NoReleaseException();
                        throw;
                    }
                    var tags = Regex.Matches(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (tags.Count == 0) throw new NoReleaseException();
                    // 列表按发布时间倒序，逐个找比本地新的第一条
                    string best = null;
                    for (int i = 0; i < tags.Count && i < 20; i++)
                        if (IsNewer(AppVer, tags[i].Groups[1].Value)) { best = tags[i].Groups[1].Value; break; }
                    if (best != null)
                    {
                        tag = best;
                        var urls = Regex.Matches(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
                        for (int i = 0; i < urls.Count && i < 40; i++)
                            if (urls[i].Groups[1].Value.Contains("/releases/tag/" + best)) { page = urls[i].Groups[1].Value.Replace("\\/", "/"); break; }
                        txt = I18n.T("发现新版本") + " " + tag + " · " + page;
                    }
                    else
                    {
                        txt = I18n.T("已是最新版本") + " · " + AppVer;
                    }
                }
                catch (NoReleaseException)
                {
                    tag = null;
                    txt = I18n.T("暂无已发布的版本") + " · " + RepoReleases;
                }
                catch
                {
                    tag = null;
                    txt = I18n.T("检查失败，请检查网络或代理设置");
                }
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (tag != null && IsNewer(AppVer, tag))
                    {
                        status.Foreground = Th.Accent;
                        status.Cursor = Cursors.Hand;
                        status.MouseLeftButtonUp += delegate { OpenUrl(page); };
                    }
                    status.Text = txt;
                }));
            });
            th.IsBackground = true;
            th.Start();
        }

        void DarkMessage(string title, string body, string btnText)
        {
            var d = new Window
            {
                Owner = this, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true, Background = Brushes.Transparent,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false
            };

            var sp = new StackPanel { Margin = new Thickness(26, 24, 26, 20), Width = 420 };
            sp.Children.Add(new TextBlock { Text = title, FontFamily = Th.FF, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Th.Text, Margin = new Thickness(0, 0, 0, 12) });
            sp.Children.Add(new TextBlock { Text = body, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, MaxWidth = 420, LineHeight = 24 });
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            btns.Children.Add(ActionBtn(btnText, true, delegate { d.Close(); }, 116));
            sp.Children.Add(btns);

            I18n.Apply(sp);
            var shell = Shell(sp);
            d.Content = shell;
            d.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape) d.Close(); };
            shell.MouseLeftButtonDown += delegate { try { d.DragMove(); } catch { } };
            d.ShowDialog();
        }

        UIElement AccentRule(double w)
        {
            return new Border
            {
                Width = w, Height = 2.5, CornerRadius = new CornerRadius(1.5),
                Background = Th.HGrad(Th.C("#84ADFF"), Th.C("#3F79F2")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 14, 0, 0)
            };
        }

        void ShowPinConfirm()
        {
            menuLive = false;
            rebuildCurrent = ShowPinConfirm;
            var sp = new StackPanel { Margin = new Thickness(26, 26, 26, 26) };
            sp.Children.Add(IconBadge("\u0021", Th.Warn));
            sp.Children.Add(AccentRule(28));
            var t = new TextBlock { Text = "重要提示", FontFamily = Th.FF, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0) };
            sp.Children.Add(t);
            var body = new TextBlock
            {
                Text = "因系统安全策略设置，关闭VBS后可能需要重设PIN。为避免微软账户验证失败，强烈建议您临时关闭PIN（路径：系统设置-账户-登录选项）",
                FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center, MaxWidth = 620, Margin = new Thickness(0, 14, 0, 0), LineHeight = 24
            };
            sp.Children.Add(body);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 26, 0, 0) };
            btns.Children.Add(ActionBtn("继续", true, delegate { StartRun("1"); }, 118));
            var b2 = ActionBtn("取消并跳转设置", false, delegate { OpenSignInOptions(); ShowMenu(); }, 178);
            b2.Margin = new Thickness(14, 0, 0, 0);
            btns.Children.Add(b2);
            sp.Children.Add(btns);
            Navigate(sp);
        }

        UIElement IconBadge(string glyph, Brush color)
        {
            var c = ((SolidColorBrush)color).Color;
            var g = new Grid { Width = 84, Height = 84, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };

            g.Children.Add(new Ellipse
            {
                Width = 84, Height = 84,
                Fill = Th.VGrad(Th.With(c, 30), Th.With(c, 6)),
                Stroke = Th.Glass(90, "#93A8C6"), StrokeThickness = 1
            });

            var ring = new Ellipse
            {
                Width = 66, Height = 66,
                Stroke = color, StrokeThickness = 1.6,
                Fill = Th.Glass(120, "#0E141C"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            ring.Effect = new DropShadowEffect { Color = c, BlurRadius = 18, ShadowDepth = 0, Opacity = 0.45 };
            g.Children.Add(ring);

            g.Children.Add(new TextBlock
            {
                Text = glyph, FontFamily = Th.FF, FontSize = 28, FontWeight = FontWeights.Bold,
                Foreground = color, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            return g;
        }

        void SetProgress(double p)
        {
            if (barScale == null) return;
            barScale.ScaleX = Math.Max(0.0, Math.Min(1.0, p));
        }

        void StartRun(string code)
        {
            DoneShown = false;
            bool single = code != "1";
            if (single)
            {
                sentences = new string[] { code == "2" ? "正在还原，请稍候……" : "正在添加Hyper-V，请稍候……" };
                ticks = new int[] { 40 };
                stepLogs = new string[] { };
            }
            else
            {
                sentences = new string[] {
                    "正在检查并修复系统组件（约 1-5 分钟）……",
                    "正在禁用相关功能（共 5 项，每项约5秒-1分钟，请耐心等待）",
                    "正在设置虚拟机监控程序启动类型……",
                    "即将准备就绪……" };
                ticks = new int[] { 14, 30, 10, 8 };
                stepLogs = new string[] {
                    "[ 1 / 5 ] microsoft-hyper-v-all            返回码 0",
                    "[ 2 / 5 ] IsolatedUserMode                 返回码 0",
                    "[ 3 / 5 ] Microsoft-Hyper-V-Hypervisor     返回码 0",
                    "[ 4 / 5 ] Microsoft-Hyper-V-Online         返回码 0",
                    "[ 5 / 5 ] HypervisorPlatform               返回码 3010（可忽略）" };
            }
            BuildProgress(code, single);
            runStarted = true;

            if (!Simulate)
            {
                RunReal(code);
                return;
            }

            step = 0; tick = 0; logIdx = -1; running = true;
            tm.Start();
        }

        void RunReal(string code)
        {
            var eng = new Engine();
            eng.OnStep = delegate(int i) { Dispatcher.BeginInvoke(new Action(delegate { ApplyStep(i); })); };
            eng.OnSub = delegate(string s) { Dispatcher.BeginInvoke(new Action(delegate { if (step >= 0 && step < rows.Count) rows[step].SetState(1, s); })); };
            eng.OnLog = delegate(string s) { Dispatcher.BeginInvoke(new Action(delegate { AddLog(s); })); };
            eng.OnFinish = delegate(bool ok, string detail)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < rows.Count; i++) rows[i].SetState(2, null);
                    SetProgress(1.0);
                    if (counter != null) counter.Text = rows.Count + " / " + rows.Count;
                    LastFailDetail = detail == null ? "" : detail;
                    var d2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
                    d2.Tick += delegate { d2.Stop(); ShowDone(!ok); };
                    d2.Start();
                }));
            };
            System.Threading.Tasks.Task.Run(new Action(delegate { eng.Run(code); }));
        }

        void ApplyStep(int i)
        {
            step = i;
            for (int k = 0; k < rows.Count; k++) rows[k].SetState(k < i ? 2 : (k == i ? 1 : 0), null);
            if (counter != null) counter.Text = (i + 1) + " / " + rows.Count;
            SetProgress(rows.Count == 0 ? 0 : (double)i / rows.Count);
        }

        UIElement CounterPill()
        {
            counter = new TextBlock
            {
                Text = "", FontFamily = Th.Mono, FontSize = 11.5, FontWeight = FontWeights.Bold,
                Foreground = Th.Accent, VerticalAlignment = VerticalAlignment.Center
            };
            return new Border
            {
                Background = Th.Tint(Th.C("#5B93FF"), 34),
                BorderBrush = Th.Glass(110, "#5B93FF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(9, 3, 9, 4),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Child = counter
            };
        }

        UIElement StepCard(int count, bool single)
        {
            var list = new StackPanel();
            rows.Clear();
            for (int i = 0; i < count; i++)
            {
                var badge = single ? "" : ((i + 1) + " / " + count);
                var r = new StepRow(badge, sentences[i], i == 0, i == count - 1);
                r.SetState(0, null);
                rows.Add(r);
                list.Children.Add(r);
            }
            var card = new Border
            {
                Background = Th.Card,
                BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 2, 0, 0),
                Child = list
            };
            HookClip(card, 12);
            return card;
        }

        UIElement LogBlock()
        {
            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var br in new Brush[] { Th.Bad, Th.Warn, Th.Ok })
            {
                dots.Children.Add(new Border
                {
                    Width = 7, Height = 7, CornerRadius = new CornerRadius(4),
                    Background = Th.Tint(((SolidColorBrush)br).Color, 200),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            var head = new Grid { Height = 32, Background = Brushes.Transparent };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(dots, 1); head.Children.Add(dots);
            var label = new TextBlock
            {
                Text = "运行记录", FontFamily = Th.FF, FontSize = 11.5, FontWeight = FontWeights.Bold,
                Foreground = Th.Muted, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(label, 2); head.Children.Add(label);

            logScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(12, 8, 6, 9), Height = 88 };
            logPanel = new StackPanel();
            logScroll.Content = logPanel;

            var inner = new StackPanel();
            inner.Children.Add(head);
            inner.Children.Add(new Border { Height = 1, Background = Th.Line });
            inner.Children.Add(logScroll);

            var card = new Border
            {
                Background = Th.Deep,
                BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 14, 0, 0),
                Child = inner
            };
            HookClip(card, 10);
            return card;
        }

        void BuildProgress(string code, bool single)
        {
            menuLive = false;
            rebuildCurrent = delegate { BuildProgress(code, single); };
            var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 22) };

            var head = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cap = new TextBlock
            {
                Text = code == "1" ? "正在执行，请稍候（通常需要 1-10分钟）……"
                     : (code == "2" ? "正在还原，请稍候……" : "正在添加Hyper-V，请稍候……"),
                FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Muted, VerticalAlignment = VerticalAlignment.Center
            };
            head.Children.Add(cap);
            var pill = CounterPill();
            Grid.SetColumn(pill, 1); head.Children.Add(pill);
            sp.Children.Add(head);

            if (!single)
            {
                var track = new Border
                {
                    Height = 8, CornerRadius = new CornerRadius(4),
                    Background = Th.Glass(150, "#070A0F"),
                    BorderBrush = Th.Glass(60, "#93A8C6"), BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 16)
                };
                var layer = new Grid();
                barScale = new ScaleTransform(0, 1);
                barFill = new Border
                {
                    Height = 6, CornerRadius = new CornerRadius(3),
                    Background = Th.HGrad(Th.C("#84ADFF"), Th.C("#3F79F2")),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    RenderTransformOrigin = new Point(0, 0.5),
                    RenderTransform = barScale,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                barFill.Margin = new Thickness(1);
                layer.Children.Add(barFill);
                track.Child = layer;
                sp.Children.Add(track);
            }

            sp.Children.Add(StepCard(sentences.Length, single));
            sp.Children.Add(LogBlock());

            Navigate(sp);
            if (rebuildMode && runStarted) RestoreProgress();
            else rows[0].SetState(1, null);
            AddLog("管理员权限已确认");
        }

        void RestoreProgress()
        {
            if (rows.Count == 0) return;
            for (int k = 0; k < rows.Count; k++)
                rows[k].SetState(k < step ? 2 : (k == step ? 1 : 0), null);
            if (sentences != null && sentences.Length > 0) SetProgress((double)step / sentences.Length);
            if (counter != null) counter.Text = Math.Min(step + 1, rows.Count) + " / " + rows.Count;
        }

        void AddLog(string text)
        {
            if (logPanel == null) return;
            var tb = new TextBlock
            {
                Text = I18n.T(text), FontFamily = Th.Mono, FontSize = 11.5,
                Foreground = text.Contains("3010") ? Th.Warn : Th.Hex("#98A1AD"),
                Margin = new Thickness(0, 0, 0, 3),
                TextWrapping = TextWrapping.NoWrap
            };
            logPanel.Children.Add(tb);
            if (logScroll != null) logScroll.ScrollToEnd();
        }

        void OnTick(object s, EventArgs e)
        {
            if (!running) return;
            tick++;
            if (step == 1 && stepLogs.Length > 0 && logIdx < stepLogs.Length && tick % 6 == 0)
            {
                logIdx++;
                AddLog(stepLogs[logIdx]);
                rows[1].SetState(1, I18n.T("正在处理第 " + (logIdx + 1) + " 项 / 共 5 项……"));
            }
            if (tick >= ticks[step])
            {
                tick = 0;
                rows[step].SetState(2, null);
                step++;
                if (step >= sentences.Length) { Finish(); return; }
                rows[step].SetState(1, null);
                if (step == 1 && stepLogs.Length > 0) { logIdx = -1; }
            }
            double pct = Math.Min(1.0, (step + (double)tick / ticks[step]) / sentences.Length);
            SetProgress(pct);
            if (counter != null) counter.Text = (step + 1) + " / " + sentences.Length;
        }

        void Finish()
        {
            running = false; tm.Stop();
            if (rows.Count > 0)
            {
                for (int i = 0; i < rows.Count; i++) rows[i].SetState(2, null);
                if (counter != null) counter.Text = sentences.Length + " / " + sentences.Length;
                SetProgress(1.0);
            }
            var d = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            d.Tick += delegate
            {
                d.Stop();
                ShowDone(false);
            };
            d.Start();
        }

        // 重启页：环状扫光 + 居中说明
        Canvas BigSpinner(double size)
        {
            var canvas = Spin.Make(size, 9, Th.Accent);
            return canvas;
        }

        void ShowRebooting()
        {
            menuLive = false;
            rebuildCurrent = ShowRebooting;
            var sp = new StackPanel { Margin = new Thickness(26, 30, 26, 30) };
            var g = new Grid { Height = 168 };
            var ringOuter = new Ellipse
            {
                Width = 78, Height = 78,
                Stroke = Th.Glass(80, "#93A8C6"), StrokeThickness = 1,
                Fill = Th.Glass(90, "#0E141C"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            g.Children.Add(ringOuter);
            var spin = BigSpinner(56);
            spin.HorizontalAlignment = HorizontalAlignment.Center;
            spin.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(spin);
            sp.Children.Add(g);

            rebootLabel = new TextBlock
            {
                Text = "正在重启……",
                FontFamily = Th.FF, FontSize = 15, Foreground = Th.Body,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sp.Children.Add(rebootLabel);
            var cancel = ActionBtn("取消", false, delegate { CancelReboot(); }, 110);
            cancel.HorizontalAlignment = HorizontalAlignment.Center;
            cancel.Margin = new Thickness(0, 18, 0, 0);
            sp.Children.Add(cancel);
            Navigate(sp);
        }

        void BeginReboot()
        {
            ShowRebooting();
            int left = 3;
            if (rebootLabel != null) rebootLabel.Text = "正在重启……（" + left + " 秒，按 Esc 取消）";
            rebootTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            rebootTimer.Tick += delegate
            {
                left--;
                if (left <= 0) { rebootTimer.Stop(); rebootTimer = null; DoShutdown(); return; }
                if (rebootLabel != null) rebootLabel.Text = "正在重启……（" + left + " 秒，按 Esc 取消）";
            };
            rebootTimer.Start();
        }

        void CancelReboot()
        {
            if (rebootTimer != null) { rebootTimer.Stop(); rebootTimer = null; }
            ShowDone(false);
        }

        void DoShutdown()
        {
            rebootTimer = null;
            int rc = Sys.Run("shutdown.exe", "/r /t 0", null);
            if (rc != 0)
            {
                Sys.AppendLog("重启失败", "shutdown rc=" + rc);
                ShowDone(true);
                DarkMessage("无法重启",
                    "系统拒绝了重启请求。\nshutdown 返回码：" + rc
                  + "\n\n请手动重启电脑。", "确定");
                return;
            }
            Close();
        }

        void ShowDone(bool fail)
        {
            DoneShown = true;
            lastFail = fail;
            menuLive = false;
            rebuildCurrent = delegate { ShowDone(lastFail); };
            var sp = new StackPanel { Margin = new Thickness(26, 24, 26, 26) };
            sp.Children.Add(IconBadge(fail ? "\u0021" : "\u2713", fail ? Th.Warn : Th.Ok));
            sp.Children.Add(AccentRule(28));
            sp.Children.Add(new TextBlock { Text = "完成", TextAlignment = TextAlignment.Center, FontFamily = Th.FF, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "设置已完成。接下来请保存文件并重启。", FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 620, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "重启过程中若出现黑底白字的英文界面，请连续按 F3 键走完剩余流程", FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 620, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });

            if (fail)
            {
                var bs = new StackPanel();
                string detail = string.IsNullOrEmpty(LastFailDetail) ? "未成功项：未知" : LastFailDetail;
                bs.Children.Add(new TextBlock { Text = "未成功项：" + detail, FontFamily = Th.FF, FontSize = 12, Foreground = Th.Warn, TextWrapping = TextWrapping.Wrap });
                bs.Children.Add(new TextBlock { Text = "日志：DisableVBS_日志.txt", FontFamily = Th.FF, FontSize = 12, Foreground = Th.Faint, Margin = new Thickness(0, 5, 0, 0) });
                var box = new Border
                {
                    Background = Th.Deep, BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10), Margin = new Thickness(44, 18, 44, 0),
                    Padding = new Thickness(16, 10, 16, 10), Child = bs
                };
                sp.Children.Add(box);
            }

            sp.Children.Add(new Border { Height = 1, Background = Th.Line, Margin = new Thickness(0, 24, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "需要重启以完成设置。", FontFamily = Th.FF, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "你现在要重启吗?", FontFamily = Th.FF, FontSize = 13, Foreground = Th.Muted, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
            btns.Children.Add(ActionBtn("重启", true, delegate
            {
                if (Simulate)
                {
                    DarkMessage("样机", "真实版此处会执行 shutdown -r -t 0。", "确定");
                    return;
                }
                if (Ask("重启",
                    "重启前请保留你的所有文件，确定要立即重启吗",
                    480, 210, "确定"))
                {
                    BeginReboot();
                }
            }, 132));
            var b2 = ActionBtn("退出", false, delegate { Close(); }, 132);
            b2.Margin = new Thickness(14, 0, 0, 0);
            btns.Children.Add(b2);
            sp.Children.Add(btns);

            Navigate(sp);
        }

        public bool DoneShown;
        public void GoConfirm() { ShowPinConfirm(); }
        public void GoRebooting() { ShowRebooting(); }
        public void GoRun(string code) { StartRun(code); }
        public void GoMenu() { ShowMenu(); }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool IsUserAnAdmin();

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr ShellExecuteW(IntPtr hwnd, string op, string file, string param, string dir, int show);

        void OpenSignInOptions()
        {
            const string uri = "ms-settings:signinoptions";
            var sb = new System.Text.StringBuilder();

            if (IsUserAnAdmin())
            {
                try { System.Diagnostics.Process.Start("explorer.exe", uri); return; }
                catch (Exception ex) { sb.AppendLine("explorer(提权代理) 异常: " + ex.Message); }
            }

            try
            {
                IntPtr r = ShellExecuteW(IntPtr.Zero, "open", uri, null, null, 1);
                if (r.ToInt64() > 32) return;
                sb.AppendLine("ShellExecuteW 返回码: " + r.ToInt64());
            }
            catch (Exception ex) { sb.AppendLine("ShellExecuteW 异常: " + ex.Message); }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(uri);
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
                return;
            }
            catch (Exception ex) { sb.AppendLine("Process.Start 异常: " + ex.Message); }

            try { System.Diagnostics.Process.Start("cmd.exe", "/c start \"\" " + uri); return; }
            catch (Exception ex) { sb.AppendLine("cmd start 异常: " + ex.Message); }

            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DisableVBS_UI_diag.txt");
            try { System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + sb.ToString() + Environment.NewLine); } catch { }

            DarkMessage("无法打开设置",
                "请手动打开：设置 - 账户 - 登录选项\n\n诊断信息已写入：\n" + logPath + "\n\n" + sb.ToString(),
                "确定");
        }
    }

    static class Program
    {
        static void Pump(int ms)
        {
            var end = DateTime.Now.AddMilliseconds(ms);
            while (DateTime.Now < end)
            {
                Dispatcher.CurrentDispatcher.Invoke(new Action(delegate { }), DispatcherPriority.Background);
                System.Threading.Thread.Sleep(15);
            }
        }

        static void Save(Window w, string path)
        {
            w.UpdateLayout();
            int ww = (int)Math.Ceiling(w.ActualWidth);
            int hh = (int)Math.Ceiling(w.ActualHeight);
            if (ww <= 0 || hh <= 0) return;
            var rtb = new RenderTargetBitmap(ww, hh, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(w);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = System.IO.File.Create(path)) enc.Save(fs);
        }

        [STAThread]
        static void Main(string[] args)
        {
            var app = new Application();
            // 关掉主窗口即退出进程（Cannot: OnExplicitShutdown 会导致窗口关了进程还在，占着 exe）
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;

            if (args.Length >= 2 && args[0] == "--demo")
            {
                string dir = args[1];
                MainWindow.Simulate = true;
                var w2 = new MainWindow();
                w2.Show();
                w2.UpdateLayout();
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-1-menu.png"));
                w2.GoConfirm();
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-2-confirm.png"));
                w2.GoRun("1");
                Pump(1700);
                Save(w2, System.IO.Path.Combine(dir, "wpf-3-running.png"));
                int guard = 0;
                while (!w2.DoneShown && guard < 300) { Pump(100); guard++; }
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-4-done.png"));
                w2.GoRebooting();
                Pump(600);
                Save(w2, System.IO.Path.Combine(dir, "wpf-5-rebooting.png"));

                // 英文版对照
                I18n.En = true;
                w2.GoMenu();
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-6-menu-en.png"));
                w2.GoRun("1");
                Pump(1700);
                Save(w2, System.IO.Path.Combine(dir, "wpf-7-running-en.png"));
                guard = 0;
                while (!w2.DoneShown && guard < 300) { Pump(100); guard++; }
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-9-done-en.png"));
                w2.GoConfirm();
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-10-confirm-en.png"));

                // 繁体版对照
                I18n.Reset();
                I18n.Hant = true;
                w2.GoMenu();
                Pump(400);
                Save(w2, System.IO.Path.Combine(dir, "wpf-11-menu-hant.png"));
                w2.GoRun("1");
                Pump(1700);
                Save(w2, System.IO.Path.Combine(dir, "wpf-12-running-hant.png"));

                // 更新说明弹层：简 / 繁 / EN 各一张
                I18n.Reset();
                var ab = w2.BuildAbout();
                ab.Show();
                Pump(400);
                Save(ab, System.IO.Path.Combine(dir, "wpf-14-about.png"));
                // 走一次真实的 GitHub 检查（沙箱内可能没网，失败态也算覆盖）
                w2.CheckUpdate(w2.aboutStatus);
                string t0 = w2.aboutStatus.Text;
                int cg = 0;
                while (cg < 120 && w2.aboutStatus.Text == t0) { Pump(100); cg++; }
                Pump(300);
                Save(ab, System.IO.Path.Combine(dir, "wpf-17-about-check.png"));
                ab.Close();

                I18n.Hant = true;
                var ab2 = w2.BuildAbout();
                ab2.Show();
                Pump(400);
                Save(ab2, System.IO.Path.Combine(dir, "wpf-15-about-hant.png"));
                ab2.Close();

                I18n.Reset(); I18n.En = true;
                var ab3 = w2.BuildAbout();
                ab3.Show();
                Pump(400);
                Save(ab3, System.IO.Path.Combine(dir, "wpf-16-about-en.png"));
                ab3.Close();

                I18n.Reset(); I18n.Hant = true;

                // 语言切换弹层单独渲染（Popup 不在主窗口可视树里）
                var card = w2.DemoLangCard();
                card.Measure(new Size(200, double.PositiveInfinity));
                double ch = Math.Ceiling(card.DesiredSize.Height);
                card.Arrange(new Rect(0, 0, 200, ch));
                var rtb2 = new RenderTargetBitmap(200, (int)ch, 96, 96, PixelFormats.Pbgra32);
                rtb2.Render(card);
                var enc2 = new PngBitmapEncoder();
                enc2.Frames.Add(BitmapFrame.Create(rtb2));
                using (var fs2 = System.IO.File.Create(System.IO.Path.Combine(dir, "wpf-8-lang-menu.png"))) enc2.Save(fs2);

                w2.Close();
                return;
            }

            if (args.Length >= 2 && args[0] == "--shot")
            {
                MainWindow.Simulate = true;
                var w = new MainWindow();
                w.Show();
                w.UpdateLayout();
                System.Threading.Thread.Sleep(200);
                w.UpdateLayout();
                int ww = (int)Math.Ceiling(w.ActualWidth);
                int hh = (int)Math.Ceiling(w.ActualHeight);
                var rtb = new RenderTargetBitmap(ww, hh, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(w);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var fs = System.IO.File.Create(args[1])) enc.Save(fs);
                w.Close();
                return;
            }

            app.Run(new MainWindow());
        }
    }
}
