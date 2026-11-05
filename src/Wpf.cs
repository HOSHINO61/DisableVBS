using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;

namespace DvbWpf
{
    static class Th
    {
        public static readonly Brush Bg = Hex("#14161A");
        public static readonly Brush Header = Hex("#0E1014");
        public static readonly Brush Deep = Hex("#181B20");
        public static readonly Brush Card = Hex("#1E2127");
        public static readonly Brush Hover = Hex("#282C34");
        public static readonly Brush Line = Hex("#2C3038");
        public static readonly Brush LineHi = Hex("#464C58");
        public static readonly Brush Text = Hex("#E8EAED");
        public static readonly Brush Body = Hex("#C6CDD6");
        public static readonly Brush Muted = Hex("#8B939F");
        public static readonly Brush Faint = Hex("#606772");
        public static readonly Brush Accent = Hex("#4C8DFF");
        public static readonly Brush AccentHi = Hex("#5E9BFF");
        public static readonly Brush Ok = Hex("#34D399");
        public static readonly Brush Bad = Hex("#F87171");
        public static readonly Brush Warn = Hex("#FBBF24");
        public static readonly FontFamily FF = new FontFamily("Microsoft YaHei UI");

        public static Brush Hex(string s) { return (Brush)new BrushConverter().ConvertFromString(s); }
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
                double r = size / 2 - dotSize / 2 - 1.5;
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
                new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(900)) { RepeatBehavior = RepeatBehavior.Forever });
            return canvas;
        }
    }

    class StepRow : Grid
    {
        Ellipse ring;
        Canvas spin;
        TextBlock mark, sub, sentence, badge;
        int state = -1;

        public StepRow(string badgeText, string sentenceText)
        {
            Height = 58;
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconGrid = new Grid { Width = 22, Height = 22, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) };
            ring = new Ellipse { Width = 20, Height = 20, StrokeThickness = 1.6, Stroke = Th.Faint, Fill = Brushes.Transparent };
            mark = new TextBlock { FontFamily = Th.FF, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            spin = Spin.Make(20, 3.4, Th.Accent);
            spin.HorizontalAlignment = HorizontalAlignment.Center;
            spin.VerticalAlignment = VerticalAlignment.Center;
            spin.Visibility = Visibility.Collapsed;
            iconGrid.Children.Add(ring); iconGrid.Children.Add(mark); iconGrid.Children.Add(spin);
            Grid.SetColumn(iconGrid, 0); Children.Add(iconGrid);

            badge = new TextBlock { Text = badgeText, FontFamily = Th.FF, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Th.Accent, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(badge, 1); Children.Add(badge);

            var txt = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sentence = new TextBlock { Text = sentenceText, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Text, TextWrapping = TextWrapping.Wrap };
            txt.Children.Add(sentence);
            sub = new TextBlock { FontFamily = Th.FF, FontSize = 11.5, Foreground = Th.Accent, Margin = new Thickness(0, 4, 0, 0), Visibility = Visibility.Collapsed };
            txt.Children.Add(sub);
            Grid.SetColumn(txt, 2); Children.Add(txt);
        }

        public void SetState(int s, string subText)
        {
            state = s;
            bool isRunning = s == 1;
            spin.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            ring.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
            mark.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;

            if (s == 2)
            {
                ring.Stroke = Th.Ok; ring.Fill = Th.Hex("#1B3A31");
                mark.Text = "\u2713"; mark.Foreground = Th.Ok;
                sentence.Foreground = Th.Text; badge.Foreground = Th.Muted;
            }
            else if (s == 1)
            {
                sentence.Foreground = Th.Text; badge.Foreground = Th.Accent;
            }
            else
            {
                ring.Stroke = Th.Faint; ring.Fill = Brushes.Transparent;
                mark.Text = ""; sentence.Foreground = Th.Muted; badge.Foreground = Th.Faint;
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
        string LastFailDetail = "";
        TextBlock rebootLabel;
        DispatcherTimer rebootTimer;
        DispatcherTimer tm = new DispatcherTimer();
        List<StepRow> rows = new List<StepRow>();
        StackPanel logPanel;
        ScrollViewer logScroll;
        Border barFill;
        TextBlock counter;
        string[] sentences;
        int[] ticks;
        string[] stepLogs;
        int step, tick, logIdx;
        bool running;

        public MainWindow()
        {
            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Th.Bg;
            SizeToContent = SizeToContent.Height;
            Width = 760;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                CornerRadius = new CornerRadius(10),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });
            tm.Interval = TimeSpan.FromMilliseconds(100);
            tm.Tick += OnTick;
            KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape && rebootTimer != null) CancelReboot(); };
            ShowMenu();
        }

        void Navigate(UIElement page)
        {
            var sp = new StackPanel();
            sp.Children.Add(HeaderBar());
            sp.Children.Add(new Border { Height = 1, Background = Th.Line });
            sp.Children.Add(page);
            sp.Children.Add(FooterBar());
            sp.Opacity = 0;
            Content = sp;
            sp.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        }

        UIElement FooterBar()
        {
            var g = new Grid { Height = 34, Background = Th.Header };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            var pill = new Border
            {
                Background = Th.Hex("#1B2A44"),
                BorderBrush = Th.Hex("#2C4A78"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "v1.0.0-Beta",
                    FontFamily = Th.FF, FontSize = 11, FontWeight = FontWeights.Bold,
                    Foreground = Th.Accent
                }
            };
            Grid.SetColumn(pill, 1); g.Children.Add(pill);

            var right = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var ghIcon = new Path
            {
                Data = Geometry.Parse("M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12"),
                Fill = Th.Faint,
                Stretch = Stretch.Uniform,
                Width = 14, Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var gh = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent, Cursor = Cursors.Hand,
                ToolTip = "https://github.com/HOSHINO61/DisableVBS",
                Child = ghIcon
            };
            gh.MouseEnter += delegate { ghIcon.Fill = Th.Text; };
            gh.MouseLeave += delegate { ghIcon.Fill = Th.Faint; };
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

            var disclaimer = new TextBlock
            {
                Text = "\u672c\u7248\u672c\u4e3a\u6d4b\u8bd5\u7248\uff0c\u4e0d\u4ee3\u8868\u6700\u7ec8\u54c1\u8d28",
                FontFamily = Th.FF, FontSize = 11, Foreground = Th.Faint,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            right.Children.Add(disclaimer);
            Grid.SetColumn(right, 3); g.Children.Add(right);
            return g;
        }

        UIElement HeaderBar()
        {
            var g = new Grid { Height = 72, Background = Th.Header };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bar = new Border { Width = 4, Height = 28, Background = Th.Accent, CornerRadius = new CornerRadius(2), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(bar, 1); g.Children.Add(bar);

            var title = new TextBlock
            {
                Text = "\u5173\u95ed/\u8fd8\u539f Hyper-V\u548cVBS",
                FontFamily = Th.FF, FontSize = 18.5, FontWeight = FontWeights.Bold,
                Foreground = Th.Text, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 3); g.Children.Add(title);

            var caps = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 8, 0) };
            caps.Children.Add(CapBtn("\u2014", delegate { WindowState = WindowState.Minimized; }));
            caps.Children.Add(CapBtn("\u2715", delegate { Close(); }));
            Grid.SetColumn(caps, 4); g.Children.Add(caps);

            g.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { if (me.LeftButton == MouseButtonState.Pressed) { try { DragMove(); } catch { } } };
            return g;
        }

        UIElement CapBtn(string glyph, Action act)
        {
            var b = new Border
            {
                Width = 34, Height = 26, CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Child = new TextBlock { Text = glyph, FontFamily = Th.FF, FontSize = 11, Foreground = Th.Muted, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            b.MouseEnter += delegate { b.Background = Th.Card; };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; };
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
            return new Border
            {
                Child = child, Height = h, Margin = margin,
                Background = Th.Card, BorderBrush = Th.Line, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
        }

        Border ActionBtn(string text, bool primary, Action act, double w)
        {
            var b = new Border
            {
                Width = w, Height = 40, CornerRadius = new CornerRadius(8),
                Background = primary ? Th.Accent : Th.Card,
                BorderBrush = primary ? Th.Accent : Th.Line,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = text, FontFamily = Th.FF, FontSize = 13.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = primary ? Brushes.White : Th.Text,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            b.MouseEnter += delegate
            {
                b.Background = primary ? Th.AccentHi : Th.Hover;
                b.BorderBrush = primary ? Th.AccentHi : Th.LineHi;
            };
            b.MouseLeave += delegate
            {
                b.Background = primary ? Th.Accent : Th.Card;
                b.BorderBrush = primary ? Th.Accent : Th.Line;
            };
            // 关键：按钮必须自己吞掉按下事件，否则会冒泡到容器上的 DragMove()，点击失效
            DateTime lastAct = DateTime.MinValue;
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs me) { me.Handled = true; };
            b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs me)
            {
                me.Handled = true;
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
            if (v == "\u672a\u77e5") return Th.Muted;
            if (v.StartsWith("\u5df2\u5f00\u542f") || v.StartsWith("\u5df2\u542f\u7528")) return whenOn;
            return whenOff;
        }

        UIElement SectionLabel(string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            sp.Children.Add(new Border { Width = 3, Height = 14, Background = Th.Accent, CornerRadius = new CornerRadius(1.5), Margin = new Thickness(0, 0, 9, 0), VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(T(text, 12, Th.Muted, true));
            return sp;
        }

        UIElement StatusCard(string name, string value, Brush dot)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var d = new Ellipse { Width = 9, Height = 9, Fill = dot, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(d, 0); g.Children.Add(d);

            var n = T(name, 13.5, Th.Text, false);
            Grid.SetColumn(n, 1); g.Children.Add(n);

            var v = T(value, 12.5, dot, true);
            v.Margin = new Thickness(0, 0, 18, 0);
            Grid.SetColumn(v, 2); g.Children.Add(v);

            return CardB(g, 44, new Thickness(0, 0, 0, 8));
        }

        UIElement MenuRow(string code, string caption)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var idx = T(code, 12, Th.Accent, true);
            idx.Margin = new Thickness(16, 0, 0, 0);
            Grid.SetColumn(idx, 0); g.Children.Add(idx);

            var cap = T(caption, 13.5, Th.Text, false);
            cap.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(cap, 1); g.Children.Add(cap);

            var ch = T("\u203A", 15, Th.Faint, false);
            ch.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(ch, 2); g.Children.Add(ch);

            var bd = CardB(g, 42, new Thickness(0, 0, 0, 6));
            bd.Cursor = Cursors.Hand;
            bd.MouseEnter += delegate { bd.Background = Th.Hover; bd.BorderBrush = Th.LineHi; };
            bd.MouseLeave += delegate { bd.Background = Th.Card; bd.BorderBrush = Th.Line; };
            bd.MouseLeftButtonUp += delegate { OnMenu(code); };
            return bd;
        }

        void ShowMenu()
        {
            running = false; tm.Stop();
            var sp = new StackPanel { Margin = new Thickness(26, 20, 26, 24) };
            sp.Children.Add(SectionLabel("\u5f53\u524d\u7cfb\u7edf\u72b6\u6001"));
            string vbs = Simulate ? "\u5df2\u5173\u95ed" : Detect.Vbs();
            string hvci = Simulate ? "\u5df2\u5173\u95ed" : Detect.Hvci();
            string hvl = Simulate ? "\u5df2\u7981\u7528 (\u5173\u95ed)" : Detect.Hvl();
            string hv = Simulate ? "\u5df2\u542f\u7528" : Detect.Hv();
            sp.Children.Add(StatusCard("VBS\uff08\u57fa\u4e8e\u865a\u62df\u5316\u7684\u5b89\u5168\uff09", vbs, Pick(vbs, Th.Bad, Th.Ok)));
            sp.Children.Add(StatusCard("HVCI\uff08\u5185\u5b58\u5b8c\u6574\u6027\uff09", hvci, Pick(hvci, Th.Bad, Th.Ok)));
            sp.Children.Add(StatusCard("Hypervisor \u542f\u52a8\u7c7b\u578b", hvl, Pick(hvl, Th.Warn, Th.Ok)));
            sp.Children.Add(StatusCard("Hyper-V \u529f\u80fd", hv, Pick(hv, Th.Accent, Th.Muted)));
            sp.Children.Add(new Border { Height = 1, Background = Th.Line, Margin = new Thickness(0, 12, 0, 14) });
            sp.Children.Add(MenuRow("1", "\u5173\u95edVBS\u548cHVCI"));
            sp.Children.Add(MenuRow("2", "\u8fd8\u539fVBS\u548cHVCI"));
            sp.Children.Add(MenuRow("3", "\u83b7\u53d6\u7cfb\u7edf\u914d\u7f6e"));
            sp.Children.Add(MenuRow("4", "\u8bbe\u7f6eHVCI"));
            sp.Children.Add(MenuRow("5", "\u3010\u5bb6\u5ead\u7248/\u5bb6\u5ead\u4e2d\u6587\u7248\u3011\u7ed9\u7cfb\u7edf\u5b89\u88c5 Hyper-V\u7ec4\u4ef6\uff08\u88c5\u5b8c\u5fc5\u987b\u91cd\u542f\uff09"));
            sp.Children.Add(MenuRow("0", "\u9000\u51fa\u811a\u672c"));
            Navigate(sp);
        }

        void OnMenu(string code)
        {
            if (code == "0") { Close(); return; }
            if (code == "3" || code == "4")
            {
                if (Simulate)
                {
                    DarkMessage("\u6837\u673a", "\u672c\u9879\u4e3a\u53ea\u8bfb\u64cd\u4f5c\uff08dxdiag / \u5b89\u5168\u4e2d\u5fc3\uff09\uff0c\u672a\u6a21\u62df\u3002", "\u786e\u5b9a");
                    return;
                }
                try
                {
                    if (code == "3") System.Diagnostics.Process.Start("dxdiag.exe");
                    else System.Diagnostics.Process.Start("windowsdefender:");
                }
                catch (Exception ex) { DarkMessage("\u65e0\u6cd5\u6253\u5f00", ex.Message, "\u786e\u5b9a"); }
                return;
            }
            if (PendingReboot())
            {
                if (Ask("\u68c0\u6d4b\u5230\u5f85\u5904\u7406\u53d8\u66f4",
                    "\u4e0a\u4e00\u6b21\u64cd\u4f5c\u5c1a\u672a\u91cd\u542f\u751f\u6548\u3002\n\n"
                  + "\u73b0\u5728\u7ee7\u7eed\u6267\u884c\uff0c\u90e8\u5206\u529f\u80fd\u53ef\u80fd\u8fd4\u56de\u9519\u8bef\u7801 50 \u6216 3010\uff0c\n"
                  + "\u4e25\u91cd\u65f6\u91cd\u542f\u4f1a\u8fdb\u5165\u6062\u590d\u73af\u5883\u3002\n\n"
                  + "\u8bf7\u5148\u91cd\u542f\u7535\u8111\uff0c\u518d\u6267\u884c\u672c\u9879\u64cd\u4f5c\u3002",
                    540, 268, "\u7acb\u5373\u91cd\u542f"))
                {
                    BeginReboot();
                }
                return;
            }
            if (code == "1") { ShowPinConfirm(); return; }
            if (code == "2")
            {
                if (Ask("\u8fd8\u539f VBS \u548c\u5185\u5b58\u5b8c\u6574\u6027",
                    "\u5373\u5c06\u8fd8\u539f VBS \u4e0e\u5185\u5b58\u5b8c\u6574\u6027\uff0c\u8fd9\u6837\u505a\u53ef\u4ee5\u63d0\u5347\u7cfb\u7edf\u5b89\u5168\u6027\n"
                  + "\u4f46\u662f\uff0c\u82e5\u60a8\u9700\u8981\u4f7f\u7528VMware\u7b49\u865a\u62df\u673a\uff0c\u5219\u4e0d\u5efa\u8bae\u8fd8\u539f\uff0c\n"
                  + "\u5b8c\u6210\u540e\u9700\u8981\u91cd\u542f\u624d\u4f1a\u751f\u6548\u3002\n\n"
                  + "\u60a8\u771f\u7684\u8981\u8fd8\u539f\u5417?", 470, 250)) StartRun("2");
                return;
            }
            if (code == "5")
            {
                if (Ask("\u7ed9\u7cfb\u7edf\u5b89\u88c5 Hyper-V",
                    "\u672c\u673a Hyper-V \u529f\u80fd\u5f53\u524d\u72b6\u6001\uff1a\u5df2\u542f\u7528\uff08\u5df2\u542f\u7528\u65f6\u5219\u4e0d\u5efa\u8bae\u6267\u884c\uff09\n\n"
                  + "\u672c\u9879\u4f1a\u4ece\u7cfb\u7edf\u81ea\u5e26\u7684\u5305\u76ee\u5f55\u91cc\u6dfb\u52a0 Hyper-V \u7ec4\u4ef6\n\n"
                  + "* Pro / \u4f01\u4e1a\u7248 / \u5de5\u4f5c\u7ad9\u7248\u672c\u672c\u8eab\u5c31\u5e26 Hyper-V\uff0c\u4e0d\u9700\u8981\u6267\u884c\u672c\u9879\u3002\n"
                  + "* \u6267\u884c\u5b8c\u6210\u540e\u5fc5\u987b\u91cd\u542f\uff0c\u8ba9\u7cfb\u7edf\u5b89\u88c5\u5b8c\uff0c\u518d\u7ee7\u7eed\u64cd\u4f5c\u3002\n"
                  + "* \u5728\u975e\u5bb6\u5ead\u7248\u4e0a\u6267\u884c\u540e\u53ef\u80fd\u8fdb\u4e0d\u53bb\u7cfb\u7edf\uff1b\u91cd\u542f\u82e5\u51fa\u73b0\u201c\u6b63\u5728\u66f4\u65b0/\u4e3a\u4f60\u81ea\u5b9a\u4e49\u529f\u80fd\u201d\u5c5e\u6b63\u5e38\u60c5\u51b5\uff0c\u8bf7\u52ff\u65ad\u7535\u3002\n\n"
                  + "\u60a8\u771f\u7684\u8981\u6267\u884c\u5417?", 650, 380)) StartRun("5");
                return;
            }
            StartRun(code);
        }

        bool Ask(string title, string body, int w, int h)
        {
            return Ask(title, body, w, h, "\u7ee7\u7eed");
        }

        bool Ask(string title, string body, int w, int h, string okText)
        {
            bool ok = false;
            DateTime openedAt = DateTime.Now;
            var d = new Window
            {
                Owner = this, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Manual, Width = w, Height = h,
                Background = Th.Bg, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };
            WindowChrome.SetWindowChrome(d, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(0), CornerRadius = new CornerRadius(8), GlassFrameThickness = new Thickness(0) });
            var root = new Border { BorderBrush = Th.LineHi, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Background = Th.Bg };
            var sp = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };
            sp.Children.Add(new TextBlock { Text = title, FontFamily = Th.FF, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Th.Text, Margin = new Thickness(0, 0, 0, 14) });
            sp.Children.Add(new TextBlock { Text = body, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, LineHeight = 22 });
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
            btns.Children.Add(ActionBtn("\u53d6\u6d88", false, delegate
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
            root.Child = sp;
            d.Content = root;
            d.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape) d.Close(); };
            root.MouseLeftButtonDown += delegate { try { d.DragMove(); } catch { } };
            d.ShowDialog();
            return ok;
        }

        void DarkMessage(string title, string body, string btnText)
        {
            var d = new Window
            {
                Owner = this, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight, Background = Th.Bg,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false
            };
            WindowChrome.SetWindowChrome(d, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(0), CornerRadius = new CornerRadius(8), GlassFrameThickness = new Thickness(0) });

            var root = new Border { Background = Th.Bg, BorderBrush = Th.LineHi, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
            var sp = new StackPanel { Margin = new Thickness(26, 22, 26, 20) };
            sp.Children.Add(new TextBlock { Text = title, FontFamily = Th.FF, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Th.Text, Margin = new Thickness(0, 0, 0, 12) });
            sp.Children.Add(new TextBlock { Text = body, FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap, MaxWidth = 420, LineHeight = 24 });
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            btns.Children.Add(ActionBtn(btnText, true, delegate { d.Close(); }, 116));
            sp.Children.Add(btns);
            root.Child = sp;
            d.Content = root;
            d.KeyDown += delegate(object s, KeyEventArgs ke) { if (ke.Key == Key.Escape) d.Close(); };
            root.MouseLeftButtonDown += delegate { try { d.DragMove(); } catch { } };
            d.ShowDialog();
        }

        void ShowPinConfirm()
        {
            var sp = new StackPanel { Margin = new Thickness(26, 26, 26, 26) };
            sp.Children.Add(IconBadge("\u0021", Th.Warn));
            var t = new TextBlock { Text = "\u91cd\u8981\u63d0\u793a", FontFamily = Th.FF, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0) };
            sp.Children.Add(t);
            var body = new TextBlock
            {
                Text = "\u56e0\u7cfb\u7edf\u5b89\u5168\u7b56\u7565\u8bbe\u7f6e\uff0c\u5173\u95edVBS\u540e\u53ef\u80fd\u9700\u8981\u91cd\u8bbePIN\u3002\u4e3a\u907f\u514d\u5fae\u8f6f\u8d26\u6237\u9a8c\u8bc1\u5931\u8d25\uff0c\u5f3a\u70c8\u5efa\u8bae\u60a8\u4e34\u65f6\u5173\u95edPIN\uff08\u8def\u5f84\uff1a\u7cfb\u7edf\u8bbe\u7f6e-\u8d26\u6237-\u767b\u5f55\u9009\u9879\uff09",
                FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center, MaxWidth = 620, Margin = new Thickness(0, 14, 0, 0), LineHeight = 24
            };
            sp.Children.Add(body);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 26, 0, 0) };
            btns.Children.Add(ActionBtn("\u7ee7\u7eed", true, delegate { StartRun("1"); }, 118));
            var b2 = ActionBtn("\u53d6\u6d88\u5e76\u8df3\u8f6c\u8bbe\u7f6e", false, delegate { OpenSignInOptions(); ShowMenu(); }, 178);
            b2.Margin = new Thickness(14, 0, 0, 0);
            btns.Children.Add(b2);
            sp.Children.Add(btns);
            Navigate(sp);
        }

        UIElement IconBadge(string glyph, Brush color)
        {
            var g = new Grid { Width = 56, Height = 56, HorizontalAlignment = HorizontalAlignment.Center };
            g.Children.Add(new Ellipse { Width = 54, Height = 54, Stroke = color, StrokeThickness = 2, Fill = Th.Hex("#1F2330") });
            var tb = new TextBlock { Text = glyph, FontFamily = Th.FF, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = color, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(tb);
            return g;
        }

        void StartRun(string code)
        {
            bool single = code != "1";
            if (single)
            {
                sentences = new string[] { code == "2" ? "\u6b63\u5728\u8fd8\u539f\uff0c\u8bf7\u7a0d\u5019\u2026\u2026" : "\u6b63\u5728\u6dfb\u52a0Hyper-V\uff0c\u8bf7\u7a0d\u5019\u2026\u2026" };
                ticks = new int[] { 40 };
                stepLogs = new string[] { };
            }
            else
            {
                sentences = new string[] {
                    "\u6b63\u5728\u68c0\u67e5\u5e76\u4fee\u590d\u7cfb\u7edf\u7ec4\u4ef6\uff08\u7ea6 1-5 \u5206\u949f\uff09\u2026\u2026",
                    "\u6b63\u5728\u7981\u7528\u76f8\u5173\u529f\u80fd\uff08\u5171 5 \u9879\uff0c\u6bcf\u9879\u7ea65\u79d2-1\u5206\u949f\uff0c\u8bf7\u8010\u5fc3\u7b49\u5f85\uff09",
                    "\u6b63\u5728\u8bbe\u7f6e\u865a\u62df\u673a\u76d1\u63a7\u7a0b\u5e8f\u542f\u52a8\u7c7b\u578b\u2026\u2026",
                    "\u5373\u5c06\u51c6\u5907\u5c31\u7eea\u2026\u2026" };
                ticks = new int[] { 14, 30, 10, 8 };
                stepLogs = new string[] {
                    "[ 1 / 5 ] microsoft-hyper-v-all            \u8fd4\u56de\u7801 0",
                    "[ 2 / 5 ] IsolatedUserMode                 \u8fd4\u56de\u7801 0",
                    "[ 3 / 5 ] Microsoft-Hyper-V-Hypervisor     \u8fd4\u56de\u7801 0",
                    "[ 4 / 5 ] Microsoft-Hyper-V-Online         \u8fd4\u56de\u7801 0",
                    "[ 5 / 5 ] HypervisorPlatform               \u8fd4\u56de\u7801 3010\uff08\u53ef\u5ffd\u7565\uff09" };
            }
            BuildProgress(code, single);

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
                    if (barFill != null) barFill.Width = 708;
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
            if (barFill != null) barFill.Width = rows.Count == 0 ? 0 : 708.0 * i / rows.Count;
        }

        void BuildProgress(string code, bool single)
        {
            var sp = new StackPanel { Margin = new Thickness(26, 20, 26, 22) };

            var head = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cap = new TextBlock
            {
                Text = code == "1" ? "\u6b63\u5728\u6267\u884c\uff0c\u8bf7\u7a0d\u5019\uff08\u901a\u5e38\u9700\u8981 1-10\u5206\u949f\uff09\u2026\u2026"
                     : (code == "2" ? "\u6b63\u5728\u8fd8\u539f\uff0c\u8bf7\u7a0d\u5019\u2026\u2026" : "\u6b63\u5728\u6dfb\u52a0Hyper-V\uff0c\u8bf7\u7a0d\u5019\u2026\u2026"),
                FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Muted, VerticalAlignment = VerticalAlignment.Center
            };
            head.Children.Add(cap);
            counter = new TextBlock { Text = "", FontFamily = Th.FF, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Th.Accent, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(counter, 1); head.Children.Add(counter);
            sp.Children.Add(head);

            var track = new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Th.Card, Margin = new Thickness(0, 0, 0, 14) };
            var trackGrid = new Grid();
            barFill = new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Th.Accent, HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
            trackGrid.Children.Add(barFill);
            track.Child = trackGrid;
            if (!single) sp.Children.Add(track);

            rows.Clear();
            for (int i = 0; i < sentences.Length; i++)
            {
                var badge = single ? "" : ((i + 1) + " / " + sentences.Length);
                var r = new StepRow(badge, sentences[i]);
                r.SetState(0, null);
                rows.Add(r);
                sp.Children.Add(r);
            }

            sp.Children.Add(new TextBlock { Text = "\u8fd0\u884c\u8bb0\u5f55", FontFamily = Th.FF, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Th.Muted, Margin = new Thickness(0, 14, 0, 8) });

            var logBorder = new Border { Height = 128, CornerRadius = new CornerRadius(8), Background = Th.Deep, BorderBrush = Th.Line, BorderThickness = new Thickness(1) };
            logScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(12, 8, 8, 8) };
            logPanel = new StackPanel();
            logScroll.Content = logPanel;
            logBorder.Child = logScroll;
            sp.Children.Add(logBorder);

            Navigate(sp);
            rows[0].SetState(1, null);
            AddLog("\u7ba1\u7406\u5458\u6743\u9650\u5df2\u786e\u8ba4");
        }

        void AddLog(string text)
        {
            if (logPanel == null) return;
            var tb = new TextBlock
            {
                Text = text, FontFamily = new FontFamily("Consolas"), FontSize = 12,
                Foreground = text.Contains("3010") ? Th.Warn : Th.Hex("#969EA9"),
                Margin = new Thickness(0, 0, 0, 3)
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
                rows[1].SetState(1, "\u6b63\u5728\u5904\u7406\u7b2c " + (logIdx + 1) + " \u9879 / \u5171 5 \u9879\u2026\u2026");
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
            if (barFill != null) barFill.Width = Math.Max(0, 708 * pct);
            if (counter != null) counter.Text = (step + 1) + " / " + sentences.Length;
        }

        void Finish()
        {
            running = false; tm.Stop();
            if (rows.Count > 0)
            {
                for (int i = 0; i < rows.Count; i++) rows[i].SetState(2, null);
                if (counter != null) counter.Text = sentences.Length + " / " + sentences.Length;
                if (barFill != null) barFill.Width = 708;
            }
            var d = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            d.Tick += delegate
            {
                d.Stop();
                ShowDone(false);
            };
            d.Start();
        }

        Canvas Spinner(double size)
        {
            return Spin.Make(size, 7, Brushes.White);
        }

        void ShowRebooting()
        {
            var sp = new StackPanel { Margin = new Thickness(26, 30, 26, 34) };
            var g = new Grid { Height = 170 };
            var spin = Spinner(64);
            spin.HorizontalAlignment = HorizontalAlignment.Center;
            spin.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(spin);
            sp.Children.Add(g);
            rebootLabel = new TextBlock
            {
                Text = "\u6b63\u5728\u91cd\u542f\u2026\u2026",
                FontFamily = Th.FF, FontSize = 15, Foreground = Th.Body,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sp.Children.Add(rebootLabel);
            var cancel = ActionBtn("\u53d6\u6d88", false, delegate { CancelReboot(); }, 110);
            cancel.HorizontalAlignment = HorizontalAlignment.Center;
            cancel.Margin = new Thickness(0, 18, 0, 0);
            sp.Children.Add(cancel);
            Navigate(sp);
        }

        void BeginReboot()
        {
            ShowRebooting();
            int left = 3;
            if (rebootLabel != null) rebootLabel.Text = "\u6b63\u5728\u91cd\u542f\u2026\u2026\uff08" + left + " \u79d2\uff0c\u6309 Esc \u53d6\u6d88\uff09";
            rebootTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            rebootTimer.Tick += delegate
            {
                left--;
                if (left <= 0) { rebootTimer.Stop(); rebootTimer = null; DoShutdown(); return; }
                if (rebootLabel != null) rebootLabel.Text = "\u6b63\u5728\u91cd\u542f\u2026\u2026\uff08" + left + " \u79d2\uff0c\u6309 Esc \u53d6\u6d88\uff09";
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
                Sys.AppendLog("\u91cd\u542f\u5931\u8d25", "shutdown rc=" + rc);
                ShowDone(true);
                DarkMessage("\u65e0\u6cd5\u91cd\u542f",
                    "\u7cfb\u7edf\u62d2\u7edd\u4e86\u91cd\u542f\u8bf7\u6c42\u3002\nshutdown \u8fd4\u56de\u7801\uff1a" + rc
                  + "\n\n\u8bf7\u624b\u52a8\u91cd\u542f\u7535\u8111\u3002", "\u786e\u5b9a");
                return;
            }
            Close();
        }

        void ShowDone(bool fail)
        {
            DoneShown = true;
            var sp = new StackPanel { Margin = new Thickness(26, 24, 26, 26) };
            sp.Children.Add(IconBadge(fail ? "\u0021" : "\u2713", fail ? Th.Warn : Th.Ok));
            sp.Children.Add(new TextBlock { Text = "\u5b8c\u6210", FontFamily = Th.FF, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "\u8bbe\u7f6e\u5df2\u5b8c\u6210\u3002\u63a5\u4e0b\u6765\u8bf7\u4fdd\u5b58\u6587\u4ef6\u5e76\u91cd\u542f\u3002", FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "\u91cd\u542f\u8fc7\u7a0b\u4e2d\u82e5\u51fa\u73b0\u9ed1\u5e95\u767d\u5b57\u7684\u82f1\u6587\u754c\u9762\uff0c\u8bf7\u8fde\u7eed\u6309 F3 \u952e\u8d70\u5b8c\u5269\u4f59\u6d41\u7a0b", FontFamily = Th.FF, FontSize = 13.5, Foreground = Th.Body, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });

            if (fail)
            {
                var box = new Border { Background = Th.Deep, BorderBrush = Th.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Margin = new Thickness(44, 18, 44, 0), Padding = new Thickness(16, 10, 16, 10) };
                var bs = new StackPanel();
                string detail = string.IsNullOrEmpty(LastFailDetail) ? "\u672a\u6210\u529f\u9879\uff1a\u672a\u77e5" : LastFailDetail;
                bs.Children.Add(new TextBlock { Text = "\u672a\u6210\u529f\u9879\uff1a" + detail, FontFamily = Th.FF, FontSize = 12, Foreground = Th.Warn, TextWrapping = TextWrapping.Wrap });
                bs.Children.Add(new TextBlock { Text = "\u65e5\u5fd7\uff1aDisableVBS_\u65e5\u5fd7.txt", FontFamily = Th.FF, FontSize = 12, Foreground = Th.Faint, Margin = new Thickness(0, 5, 0, 0) });
                box.Child = bs;
                sp.Children.Add(box);
            }

            sp.Children.Add(new Border { Height = 1, Background = Th.Line, Margin = new Thickness(0, 22, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "\u9700\u8981\u91cd\u542f\u4ee5\u5b8c\u6210\u8bbe\u7f6e\u3002", FontFamily = Th.FF, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Th.Text, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) });
            sp.Children.Add(new TextBlock { Text = "\u4f60\u73b0\u5728\u8981\u91cd\u542f\u5417?", FontFamily = Th.FF, FontSize = 13, Foreground = Th.Muted, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
            btns.Children.Add(ActionBtn("\u91cd\u542f", true, delegate
            {
                if (Simulate)
                {
                    DarkMessage("\u6837\u673a", "\u771f\u5b9e\u7248\u6b64\u5904\u4f1a\u6267\u884c shutdown -r -t 0\u3002", "\u786e\u5b9a");
                    return;
                }
                if (Ask("\u91cd\u542f",
                    "\u91cd\u542f\u524d\u8bf7\u4fdd\u7559\u4f60\u7684\u6240\u6709\u6587\u4ef6\uff0c\u786e\u5b9a\u8981\u7acb\u5373\u91cd\u542f\u5417",
                    480, 210, "\u786e\u5b9a"))
                {
                    BeginReboot();
                }
            }, 132));
            var b2 = ActionBtn("\u9000\u51fa", false, delegate { Close(); }, 132);
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
                catch (Exception ex) { sb.AppendLine("explorer(\u63d0\u6743\u4ee3\u7406) \u5f02\u5e38: " + ex.Message); }
            }

            try
            {
                IntPtr r = ShellExecuteW(IntPtr.Zero, "open", uri, null, null, 1);
                if (r.ToInt64() > 32) return;
                sb.AppendLine("ShellExecuteW \u8fd4\u56de\u7801: " + r.ToInt64());
            }
            catch (Exception ex) { sb.AppendLine("ShellExecuteW \u5f02\u5e38: " + ex.Message); }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(uri);
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
                return;
            }
            catch (Exception ex) { sb.AppendLine("Process.Start \u5f02\u5e38: " + ex.Message); }

            try { System.Diagnostics.Process.Start("cmd.exe", "/c start \"\" " + uri); return; }
            catch (Exception ex) { sb.AppendLine("cmd start \u5f02\u5e38: " + ex.Message); }

            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DisableVBS_UI_diag.txt");
            try { System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + sb.ToString() + Environment.NewLine); } catch { }

            DarkMessage("\u65e0\u6cd5\u6253\u5f00\u8bbe\u7f6e",
                "\u8bf7\u624b\u52a8\u6253\u5f00\uff1a\u8bbe\u7f6e - \u8d26\u6237 - \u767b\u5f55\u9009\u9879\n\n\u8bca\u65ad\u4fe1\u606f\u5df2\u5199\u5165\uff1a\n" + logPath + "\n\n" + sb.ToString(),
                "\u786e\u5b9a");
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
