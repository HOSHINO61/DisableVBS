# 原版 bat → C# 逐条对照表

> 说明：`v1.0.0-Beta` 的系统操作序列是从早期 **302 行 bat 脚本**逐条平移而来。
> 本表用于证明：顺序、参数、返回码判定**没有改动**，变化的只有 UI 呈现方式。

---

## 0. 全局差异（必须先认可）

| 项 | 旧 bat | 新 C# | 说明 |
|---|---|---|---|
| 提权 | L8 `REG QUERY HKU\S-1-5-19\Environment` 自检 | **应用清单 requireAdministrator** | 系统层保证，不再需要运行时自检 |
| 编码 | L2 `chcp 936` + GBK 源文件 | UTF-16 托管字符串 | 不再有代码页问题 |
| 控制台 | `color` / `title` / `cls` | WPF 主题 | 纯表现层 |
| 弹窗 | 写 `%temp%\temp_msgN.vbs` → `cscript` | 自绘深色对话框 | 不再落地临时脚本文件（**顺带消除一个杀软敏感行为**） |
| 进程调用 | 直接继承控制台 | `Process` + `UseShellExecute=false` + `CreateNoWindow=true` | 需捕获 stdout/stderr 进日志 |

---

## 1. 状态检测（L23–L38）

| bat 行 | 原命令 | C# 实现 | 判据 |
|---|---|---|---|
| L24 | `Get-CimInstance Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard` → `.VirtualizationBasedSecurityStatus` | 同一条 PowerShell，捕获 stdout | `"2"` → 已开启；其余 → 已关闭 |
| L27 | `reg query ...\HypervisorEnforcedCodeIntegrity /v Enabled` + `find "0x1"` | `Registry.GetValue` 或 `reg query` 捕获 | 值 == 1 → 已开启 |
| L31 | `bcdedit /enum OSLOADER` + `findstr hypervisorlaunchtype`，取第 2 个 token | 同命令 + 正则解析 | `Off`（不区分大小写）→ 已禁用 |
| L36 | `Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All` → `.State` | 同命令 | `Enabled` / `Disabled` / 其余 → 未知 |

> ⚠️ 红线：**不要**用 BCD 里 `hypervisorlaunchtype` 是否存在来判断 VBS 开关（26H2 工作站版会被 SecConfig 清掉）。判据只认上面两条。

---

## 2. 菜单 1 —— 关闭（L85–L157）

| bat 行 | 原命令 | C# 实现 | 返回码处理 |
|---|---|---|---|
| L88–L92 | PIN 提示 VBScript 弹窗 | 确认页（已实现） | — |
| L102 | 读 `%SystemRoot%\Logs\CBS\Logs\CBS.log` 尾部 20000 行，匹配 `[SR]`，取最后一条，前 10 字符与今天比对 | 同一条 PowerShell | 命中 → 跳过 sfc；判据失效只会多跑一次，方向安全 |
| L112 | `sfc /scannow` | `sfc.exe /scannow`，stdout 流式进日志 | 不判定，仅记录 |
| L123 | `for %%f in (microsoft-hyper-v-all IsolatedUserMode Microsoft-Hyper-V-Hypervisor Microsoft-Hyper-V-Online HypervisorPlatform)` → `dism /Online /Disable-Feature:%%f /NoRestart` | 逐个 `dism.exe /Online /Disable-Feature:<name> /NoRestart` | `0` 成功；`3010` 记「可忽略」；`-2146498548`(0x800F080C) 记「本机无此功能」；其余记错误码 |
| L134 | `bcdedit /set hypervisorlaunchtype off` | 同 | 忽略（原版也忽略） |
| L135 | `reg add ...\DeviceGuard /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f` | 同 | 忽略 |
| L136 | `reg add ...\HypervisorEnforcedCodeIntegrity /v Enabled /t REG_DWORD /d 0 /f` | 同 | 忽略（**这行曾经漏掉导致 VBS 复燃，必须有**） |
| L139 | `mountvol X: /s` | 同 | 忽略 |
| L140 | `copy %WINDIR%\System32\SecConfig.efi X:\EFI\Microsoft\Boot\SecConfig.efi /Y` | `File.Copy(src, dst, true)` | 忽略 |
| L141 | `bcdedit /delete {0cb3b571-2f2e-4343-a879-d86a476d7215} /f` | 同 | 忽略 |
| L142 | `bcdedit /create {0cb3b571-...} /d "DebugTool" /application osloader` | 同（**/d 值必须一字不差**） | 忽略 |
| L143 | `bcdedit /set {0cb3b571-...} path "\EFI\Microsoft\Boot\SecConfig.efi"` | 同 | 忽略 |
| L144 | `bcdedit /set {bootmgr} bootsequence {0cb3b571-...}` | 同 | 忽略 |
| L145 | `bcdedit /set {0cb3b571-...} loadoptions DISABLE-LSA-ISO,DISABLE-VBS` | 同 | 忽略 |
| L146 | `bcdedit /set {0cb3b571-...} device partition=X:` | 同 | 忽略 |
| L147 | `mountvol X: /d` | 同 | 忽略 |
| L151 | 有失败项 → 追加写 `DisableVBS_日志.txt` | 同（保留同名日志文件） | 无失败则不生成日志 |

**关键顺序约束**：L139–L147 是一组，中间不能插入其他命令；X: 的挂载与卸载必须成对。

---

## 3. 菜单 2 —— 还原（L163–L212）

| bat 行 | 原命令 | C# 实现 | 返回码处理 |
|---|---|---|---|
| L179 | `bcdedit /deletevalue {bootmgr} bootsequence` | 同 | 忽略 |
| L180 | `bcdedit /delete {0cb3b571-...} /f` | 同 | 忽略 |
| L184 | 4 个功能 `dism /Online /Enable-Feature:%%f /NoRestart`（IsolatedUserMode / Microsoft-Hyper-V-Hypervisor / Microsoft-Hyper-V-Online / HypervisorPlatform） | 同 | 同菜单 1 分类 |
| L191 | `dism /Online /Enable-Feature:Microsoft-Hyper-V-All /All /NoRestart` | 同 | 同上 |
| L198 | `reg add ...\DeviceGuard EnableVirtualizationBasedSecurity = 1` | 同 | 忽略 |
| L199 | `reg add ...\HypervisorEnforcedCodeIntegrity Enabled = 1` | 同 | 忽略 |
| L202 | `bcdedit /set hypervisorlaunchtype auto` | 同 | 忽略 |
| L206 | 有失败项 → 写日志 | 同 | — |

---

## 4. 菜单 5 —— 安装 Hyper-V（L221–L263）

| bat 行 | 原命令 | C# 实现 | 注意 |
|---|---|---|---|
| L243 | `dir /b %SystemRoot%\servicing\Packages\*Hyper-V*.mum > hyper-v.txt` | `Directory.GetFiles(svc, "*Hyper-V*.mum")` | 不再落地临时 txt |
| L244–L247 | 逐个 `dism /online /norestart /add-package:"<pkg>"`，`if errorlevel 1` 记失败 | 同 | 任一失败即置 ADD_FAIL |
| L251 | `bcdedit /set hypervisorlaunchtype off`（防崩加固） | 同 | **必须在两个分支前都执行** |
| L255 | `echo n \| Dism /online /enable-feature /featurename:Microsoft-Hyper-V-All /LimitAccess /ALL /NoRestart` | 建议改 `/NoRestart` 直接调用（**行为等价，但需实测确认无提示差异**） | 原版用 `echo n` 回答重启询问 |
| — | ⚠️ 待办 4：这行**没有返回码检查** | 新实现**补上** | 待用户同意才补 |

---

## 5. 菜单 3 / 4、退出（L290–L301）

| bat 行 | 原命令 | C# 实现 |
|---|---|---|
| L292 | `dxdiag` | `Process.Start("dxdiag.exe")` |
| L297 | `powershell Start-Process windowsdefender:` | 同（注意：提权进程打开 UWP 的限制，可能需要 explorer 代理） |
| L283 | `ping 127.0.0.1 -n 3` 延时退出 | 直接 Close |

---

## 6. 必须保留的行为

1. `-2146498548` = `0x800F080C`，cmd 的 errorlevel 是**有符号十进制** —— C# 用 `int` 比较即可，但要按 -2146498548 判，不要按 0x800F080C 判。
2. 失败详情格式：`%%f[3010（可忽略）]；` / `%%f[本机无此功能]；` / `%%f（错误码 N）；` 三种，末尾统一 `；`。
3. 日志行格式：`%date% %time%  关闭时未成功: <详情>` / `还原时未成功: <详情>`。
4. `SecConfig.efi` 的 GUID `{0cb3b571-2f2e-4343-a879-d86a476d7215}` 与 `/d "DebugTool"` 必须原样。
5. 菜单 5 的两个分支之后、弹窗之前都插 `bcdedit /set hypervisorlaunchtype off`。

---
