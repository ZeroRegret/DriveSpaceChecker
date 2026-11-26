using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Enumeration;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (!PromptLanguage())
{
    return 1;
}

var prompt = PromptDriveLetter(out var driveLetter);
if (prompt == DrivePromptResult.Cancelled)
{
    return 0;
}

if (prompt != DrivePromptResult.Selected)
{
    return 1;
}

var root = driveLetter + ":\\";
if (!TryReadDiskUsage(root, out var totalBytes, out var freeBytes, out var error))
{
    Console.WriteLine(error);
    return 1;
}

var outputPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), Ui.ResultFileName));
Console.WriteLine(Ui.StartScan($"{driveLetter}:\\"));
Console.WriteLine();

ScanResult scan;
try
{
    scan = DriveScanner.Scan(root, outputPath);
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine(Ui.CannotReadDriveRoot);
    return 1;
}
catch (IOException)
{
    Console.WriteLine(Ui.CannotReadDriveRoot);
    return 1;
}
var report = ReportBuilder.Build(driveLetter, totalBytes, freeBytes, scan);

ClearProgressLine();
Console.WriteLine(report);

try
{
    File.WriteAllText(outputPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    Console.WriteLine();
    Console.WriteLine(Ui.SavedTo(outputPath));
}
catch (IOException ex)
{
    Console.WriteLine();
    Console.WriteLine(Ui.WriteFailed(ex.Message));
    return 1;
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine();
    Console.WriteLine(Ui.WriteFailed(ex.Message));
    return 1;
}

while (PromptAndScanPath(root, outputPath))
{
}

return 0;

static bool PromptLanguage()
{
    while (true)
    {
        Console.Write("界面语言 / Language. 输入 1 使用中文，输入 2 for English：");
        var line = Console.ReadLine();
        if (line is null)
        {
            Console.WriteLine("未选择语言，已退出。 / No language was chosen. Exiting.");
            return false;
        }

        var text = line.Trim();
        if (text is "1" || text.Equals("zh", StringComparison.OrdinalIgnoreCase) || text.Equals("中文", StringComparison.Ordinal))
        {
            return true;
        }

        if (text is "2" || text.Equals("en", StringComparison.OrdinalIgnoreCase) || text.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            Ui.UseEnglish();
            return true;
        }

        Console.WriteLine("请输入 1 或 2。 / Enter 1 or 2.");
    }
}

static DrivePromptResult PromptDriveLetter(out char driveLetter)
{
    driveLetter = default;
    var ready = ListReadyDrives();
    if (ready.Count == 0)
    {
        Console.WriteLine(Ui.NoDrives);
        return DrivePromptResult.Closed;
    }

    Console.WriteLine(Ui.DriveListHeader);
    foreach (var drive in ready)
    {
        Console.WriteLine(Ui.DriveListItem(drive.Letter, drive.Kind, drive.Label));
    }

    Console.WriteLine(Ui.CancelOption);

    while (true)
    {
        Console.Write(Ui.AskDrive);
        var line = Console.ReadLine();
        if (line is null)
        {
            Console.WriteLine(Ui.NoDriveEntered);
            return DrivePromptResult.Closed;
        }

        if (IsCancelScan(line))
        {
            Console.WriteLine(Ui.ScanCancelled);
            return DrivePromptResult.Cancelled;
        }

        if (TryParseDriveLetter(line, out var letter) && ready.Exists(drive => drive.Letter == letter))
        {
            driveLetter = letter;
            return DrivePromptResult.Selected;
        }

        Console.WriteLine(Ui.EnterListedDrive);
    }
}

static bool IsCancelScan(string input)
{
    var text = input.Trim();
    return text is "0" or "取消" or "取消扫盘"
        || text.Equals("cancel", StringComparison.OrdinalIgnoreCase);
}

static List<ReadyDrive> ListReadyDrives()
{
    var drives = new List<ReadyDrive>();
    DriveInfo[] found;
    try
    {
        found = DriveInfo.GetDrives();
    }
    catch (IOException)
    {
        return drives;
    }
    catch (UnauthorizedAccessException)
    {
        return drives;
    }

    foreach (var drive in found)
    {
        try
        {
            if (!drive.IsReady || drive.Name.Length == 0 || !char.IsLetter(drive.Name[0]))
            {
                continue;
            }

            _ = drive.TotalSize;
            var label = "";
            try
            {
                label = drive.VolumeLabel;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            drives.Add(new ReadyDrive(char.ToUpperInvariant(drive.Name[0]), drive.DriveType, label));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    drives.Sort(static (left, right) => left.Letter.CompareTo(right.Letter));
    return drives;
}

static bool TryParseDriveLetter(string input, out char letter)
{
    letter = default;
    var text = input.Trim().TrimEnd('\\', '/');
    if (text.Length == 2 && text[1] == ':')
    {
        text = text[..1];
    }

    if (text.Length != 1 || !char.IsLetter(text[0]))
    {
        return false;
    }

    letter = char.ToUpperInvariant(text[0]);
    return true;
}

static bool TryReadDiskUsage(string root, out long totalBytes, out long freeBytes, out string error)
{
    totalBytes = 0;
    freeBytes = 0;
    error = "";

    try
    {
        var drive = new DriveInfo(root);
        if (!drive.IsReady)
        {
            error = Ui.DriveUnavailable(root[..2]);
            return false;
        }

        totalBytes = drive.TotalSize;
        freeBytes = drive.TotalFreeSpace;
        if (freeBytes > totalBytes)
        {
            freeBytes = totalBytes;
        }

        return true;
    }
    catch (IOException)
    {
        error = Ui.DriveUnavailable(root[..2]);
        return false;
    }
    catch (UnauthorizedAccessException)
    {
        error = Ui.DriveUnavailable(root[..2]);
        return false;
    }
}

static bool PromptAndScanPath(string driveRoot, string outputPath)
{
    Console.WriteLine();
    Console.Write(Ui.AskPath);
    var line = Console.ReadLine();
    if (line is null || line.Trim().Length == 0)
    {
        return false;
    }

    if (!TryResolveUserPath(line, driveRoot, out var folder, out var pathError))
    {
        Console.WriteLine(pathError);
        return true;
    }

    Console.WriteLine(Ui.StartScan(folder));
    Console.WriteLine();
    var pathScan = DriveScanner.ScanFolder(folder, driveRoot, outputPath);
    var pathReport = ReportBuilder.BuildPath(pathScan);
    ClearProgressLine();
    Console.WriteLine(pathReport);

    try
    {
        File.AppendAllText(outputPath, Environment.NewLine + pathReport, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine(Ui.AppendedTo(outputPath));
    }
    catch (IOException ex)
    {
        Console.WriteLine(Ui.AppendFailed(ex.Message));
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine(Ui.AppendFailed(ex.Message));
    }

    return true;
}

static bool TryResolveUserPath(string input, string driveRoot, out string fullPath, out string error)
{
    fullPath = "";
    error = "";
    var text = input.Trim().Trim('"');

    var candidate = text.Contains(':') || Path.IsPathRooted(text)
        ? text
        : Path.Combine(driveRoot, text);

    try
    {
        fullPath = Path.GetFullPath(candidate);
    }
    catch (ArgumentException)
    {
        error = Ui.InvalidPath;
        return false;
    }
    catch (NotSupportedException)
    {
        error = Ui.InvalidPath;
        return false;
    }
    catch (IOException)
    {
        error = Ui.InvalidPath;
        return false;
    }

    var normalizedRoot = Path.GetFullPath(driveRoot);
    if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
    {
        error = Ui.PathOnOtherDrive(driveRoot[..2]);
        return false;
    }

    if (DriveScanner.IsExcluded(fullPath, normalizedRoot))
    {
        error = Ui.ExcludedPath;
        return false;
    }

    if (File.Exists(fullPath))
    {
        error = Ui.NeedFolder;
        return false;
    }

    if (!Directory.Exists(fullPath))
    {
        error = Ui.FolderNotFound;
        return false;
    }

    try
    {
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            error = Ui.LinkNotEntered;
            return false;
        }
    }
    catch (IOException)
    {
        error = Ui.CannotReadFolder;
        return false;
    }
    catch (UnauthorizedAccessException)
    {
        error = Ui.CannotReadFolder;
        return false;
    }

    return true;
}

static void ClearProgressLine()
{
    if (Console.IsOutputRedirected)
    {
        Console.WriteLine();
        return;
    }

    Console.Write("\r" + new string(' ', 78) + "\r");
}

internal enum DrivePromptResult
{
    Selected,
    Cancelled,
    Closed,
}

internal readonly record struct ReadyDrive(char Letter, DriveType Kind, string Label);

internal sealed class FolderNode
{
    public required string Path { get; init; }
    public long Bytes { get; init; }
    public bool ExpandChildren { get; init; }
    public required IReadOnlyList<FolderNode> Children { get; init; }
}

internal sealed class ScanResult
{
    public required IReadOnlyList<FolderNode> Top { get; init; }
    public long IncludedBytes { get; init; }
    public int DeniedDirectories { get; init; }
}

internal static class DriveScanner
{
    private const int ProgressEvery = 200;
    private const int TopFolderCount = 3;
    private const int SecondLevelCount = 5;
    private const int DeeperLevelCount = 3;
    private const int ChildGenerations = 3;

    public static ScanResult Scan(string root, string outputPathToIgnore)
    {
        var denied = 0;
        var processedDirectories = 0;
        var activeTops = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var progressLock = new object();

        var (topFolders, rootFileBytes) = ReadRoot(root, outputPathToIgnore);

        var measured = new FolderNode[topFolders.Count];
        Parallel.For(0, topFolders.Count, i =>
        {
            var folder = topFolders[i];
            activeTops[folder] = 1;
            PrintProgress(root, activeTops, Volatile.Read(ref processedDirectories), progressLock);

            measured[i] = MeasureNode(
                folder,
                SecondLevelCount,
                ChildGenerations,
                root,
                outputPathToIgnore,
                ref denied,
                ref processedDirectories,
                activeTops,
                progressLock);
            activeTops.TryRemove(folder, out _);
        });

        var ranked = new List<FolderNode>(measured.Length);
        long included = rootFileBytes;
        foreach (var folder in measured)
        {
            included += folder.Bytes;
            if (folder.Bytes > 0)
            {
                ranked.Add(folder);
            }
        }

        ranked.Sort(static (a, b) => CompareBySize(a.Bytes, a.Path, b.Bytes, b.Path));
        var top = ranked.Take(TopFolderCount).ToList();

        return new ScanResult
        {
            Top = top,
            IncludedBytes = included,
            DeniedDirectories = denied,
        };
    }

    public static ScanResult ScanFolder(string folder, string driveRoot, string outputPathToIgnore)
    {
        var denied = 0;
        var processedDirectories = 0;
        var activeTops = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var progressLock = new object();
        activeTops[folder] = 1;

        var node = MeasureNode(
            folder,
            DeeperLevelCount,
            2,
            driveRoot,
            outputPathToIgnore,
            ref denied,
            ref processedDirectories,
            activeTops,
            progressLock);

        return new ScanResult
        {
            Top = [node],
            IncludedBytes = node.Bytes,
            DeniedDirectories = denied,
        };
    }

    private static (List<string> Folders, long RootFileBytes) ReadRoot(
        string root,
        string outputPathToIgnore)
    {
        var folders = new List<string>();
        long rootFileBytes = 0;

        foreach (var child in EnumerateChildren(root))
        {
            if (child.IsReparsePoint)
            {
                continue;
            }

            if (child.IsDirectory)
            {
                if (IsExcluded(child.FullPath, root))
                {
                    continue;
                }

                folders.Add(child.FullPath);
                continue;
            }

            if (IsIgnoredOutput(child.FullPath, outputPathToIgnore))
            {
                continue;
            }

            rootFileBytes += child.Length;
        }

        return (folders, rootFileBytes);
    }

    /// <summary>
    /// childGenerations 是还要展开的子目录层数。
    /// 顶层文件夹传入 3：先保留 5 个子目录，这两层之下再各保留 3 个。
    /// 传入 0 时只累加大小，不再列出子目录。
    /// </summary>
    private static FolderNode MeasureNode(
        string folder,
        int childLimit,
        int childGenerations,
        string root,
        string outputPathToIgnore,
        ref int denied,
        ref int processedDirectories,
        ConcurrentDictionary<string, byte> activeTops,
        object progressLock)
    {
        var seen = Interlocked.Increment(ref processedDirectories);
        if (seen % ProgressEvery == 0)
        {
            PrintProgress(root, activeTops, seen, progressLock);
        }

        List<ChildEntry> entries;
        try
        {
            entries = EnumerateChildren(folder);
        }
        catch (UnauthorizedAccessException)
        {
            Interlocked.Increment(ref denied);
            return new FolderNode
            {
                Path = folder,
                Bytes = 0,
                ExpandChildren = childGenerations > 0,
                Children = [],
            };
        }
        catch (IOException)
        {
            Interlocked.Increment(ref denied);
            return new FolderNode
            {
                Path = folder,
                Bytes = 0,
                ExpandChildren = childGenerations > 0,
                Children = [],
            };
        }

        long total = 0;
        var children = new List<FolderNode>();
        foreach (var child in entries)
        {
            if (child.IsReparsePoint)
            {
                continue;
            }

            if (child.IsDirectory)
            {
                if (IsExcluded(child.FullPath, root))
                {
                    continue;
                }

                FolderNode childNode;
                if (childGenerations > 0)
                {
                    childNode = MeasureNode(
                        child.FullPath,
                        DeeperLevelCount,
                        childGenerations - 1,
                        root,
                        outputPathToIgnore,
                        ref denied,
                        ref processedDirectories,
                        activeTops,
                        progressLock);
                }
                else
                {
                    var size = MeasureFolder(
                        child.FullPath,
                        root,
                        outputPathToIgnore,
                        ref denied,
                        ref processedDirectories,
                        activeTops,
                        progressLock);
                    childNode = new FolderNode
                    {
                        Path = child.FullPath,
                        Bytes = size,
                        ExpandChildren = false,
                        Children = [],
                    };
                }

                total += childNode.Bytes;
                if (childNode.Bytes > 0)
                {
                    children.Add(childNode);
                }

                continue;
            }

            if (IsIgnoredOutput(child.FullPath, outputPathToIgnore))
            {
                continue;
            }

            total += child.Length;
        }

        if (childGenerations > 0)
        {
            children.Sort(static (a, b) => CompareBySize(a.Bytes, a.Path, b.Bytes, b.Path));
            if (children.Count > childLimit)
            {
                children.RemoveRange(childLimit, children.Count - childLimit);
            }
        }
        else
        {
            children.Clear();
        }

        return new FolderNode
        {
            Path = folder,
            Bytes = total,
            ExpandChildren = childGenerations > 0,
            Children = children,
        };
    }

    private static int CompareBySize(long leftBytes, string leftPath, long rightBytes, string rightPath)
    {
        var bySize = rightBytes.CompareTo(leftBytes);
        return bySize != 0
            ? bySize
            : string.Compare(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
    }

    private static long MeasureFolder(
        string folder,
        string root,
        string outputPathToIgnore,
        ref int denied,
        ref int processedDirectories,
        ConcurrentDictionary<string, byte> activeTops,
        object progressLock)
    {
        long total = 0;
        var pending = new Stack<string>();
        pending.Push(folder);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            var seen = Interlocked.Increment(ref processedDirectories);
            if (seen % ProgressEvery == 0)
            {
                PrintProgress(root, activeTops, seen, progressLock);
            }

            List<ChildEntry> children;
            try
            {
                children = EnumerateChildren(current);
            }
            catch (UnauthorizedAccessException)
            {
                Interlocked.Increment(ref denied);
                continue;
            }
            catch (IOException)
            {
                Interlocked.Increment(ref denied);
                continue;
            }

            foreach (var child in children)
            {
                if (child.IsReparsePoint)
                {
                    continue;
                }

                if (child.IsDirectory)
                {
                    if (IsExcluded(child.FullPath, root))
                    {
                        continue;
                    }

                    pending.Push(child.FullPath);
                    continue;
                }

                if (IsIgnoredOutput(child.FullPath, outputPathToIgnore))
                {
                    continue;
                }

                total += child.Length;
            }
        }

        return total;
    }

    private static List<ChildEntry> EnumerateChildren(string directory)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint,
            ReturnSpecialDirectories = false,
            BufferSize = 65536,
        };

        var enumerable = new FileSystemEnumerable<ChildEntry>(
            directory,
            static (ref FileSystemEntry entry) => new ChildEntry(
                entry.ToFullPath(),
                entry.IsDirectory,
                entry.IsDirectory ? 0 : entry.Length,
                (entry.Attributes & FileAttributes.ReparsePoint) != 0),
            options);

        var children = new List<ChildEntry>();
        using var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }

        return children;
    }

    /// <summary>
    /// 整支跳过系统和程序安装目录，以及 Unity 的安装、运行目录。
    /// 其他路径上的 Unity 工程不排除。
    /// </summary>
    internal static bool IsExcluded(string fullPath, string root)
    {
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relative = fullPath[root.Length..].TrimStart('\\', '/');
        if (relative.Length == 0)
        {
            return false;
        }

        var parts = relative.Split('\\', '/');
        var top = parts[0];
        if (top.Equals("Windows", StringComparison.OrdinalIgnoreCase)
            || top.Equals("Program Files", StringComparison.OrdinalIgnoreCase)
            || top.Equals("Program Files (x86)", StringComparison.OrdinalIgnoreCase)
            || top.Equals("ProgramData", StringComparison.OrdinalIgnoreCase)
            || top.Equals("Unity", StringComparison.OrdinalIgnoreCase)
            || top.Equals("Unity Hub", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // {盘}:\Users\*\AppData\Local\Unity
        // {盘}:\Users\*\AppData\Local\UnityHub
        // {盘}:\Users\*\AppData\Roaming\Unity
        // {盘}:\Users\*\AppData\Roaming\Unity Hub
        if (parts.Length >= 5
            && parts[0].Equals("Users", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("AppData", StringComparison.OrdinalIgnoreCase))
        {
            var leaf = parts[4];
            if (parts[3].Equals("Local", StringComparison.OrdinalIgnoreCase)
                && (leaf.Equals("Unity", StringComparison.OrdinalIgnoreCase)
                    || leaf.Equals("UnityHub", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (parts[3].Equals("Roaming", StringComparison.OrdinalIgnoreCase)
                && (leaf.Equals("Unity", StringComparison.OrdinalIgnoreCase)
                    || leaf.Equals("Unity Hub", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIgnoredOutput(string fullPath, string outputPath)
    {
        return string.Equals(
            Path.GetFullPath(fullPath),
            outputPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintProgress(
        string root,
        ConcurrentDictionary<string, byte> activeTops,
        int processedDirectories,
        object progressLock)
    {
        var names = activeTops.Keys
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Take(3);
        var label = string.Join("、", names);
        if (label.Length == 0)
        {
            label = root;
        }

        var line = Ui.Progress(label, processedDirectories);
        lock (progressLock)
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            if (line.Length > 76)
            {
                line = line[..76];
            }

            Console.Write("\r" + line.PadRight(78));
        }
    }
}

internal readonly record struct ChildEntry(string FullPath, bool IsDirectory, long Length, bool IsReparsePoint);

internal static class ReportBuilder
{
    private const int ManyDeniedDirectories = 30;

    public static string Build(char driveLetter, long totalBytes, long freeBytes, ScanResult scan)
    {
        var usedBytes = Math.Max(0, totalBytes - freeBytes);
        var builder = new StringBuilder();
        builder.AppendLine(Ui.DiskSummary(driveLetter, FormatSize(totalBytes), FormatSize(usedBytes), FormatSize(freeBytes)));
        builder.AppendLine();

        if (scan.Top.Count == 0)
        {
            builder.AppendLine(Ui.NoFolders);
        }
        else
        {
            for (var i = 0; i < scan.Top.Count; i++)
            {
                AppendFolder(builder, scan.Top[i], i + 1, 0);
                builder.AppendLine();
            }
        }

        builder.AppendLine();
        builder.AppendLine(Ui.Included(FormatSize(scan.IncludedBytes)));
        builder.AppendLine(Ui.Denied(scan.DeniedDirectories));

        if (scan.DeniedDirectories >= ManyDeniedDirectories)
        {
            builder.AppendLine(Ui.ManyDenied);
        }

        return builder.ToString();
    }

    public static string BuildPath(ScanResult scan)
    {
        var node = scan.Top[0];
        var builder = new StringBuilder();
        builder.AppendLine(Ui.ChosenPath);
        AppendFolder(builder, node, 1, 0);
        builder.AppendLine();
        builder.AppendLine(Ui.Included(FormatSize(scan.IncludedBytes)));
        builder.AppendLine(Ui.Denied(scan.DeniedDirectories));

        if (scan.DeniedDirectories >= ManyDeniedDirectories)
        {
            builder.AppendLine(Ui.ManyDenied);
        }

        return builder.ToString();
    }

    private static void AppendFolder(StringBuilder builder, FolderNode node, int index, int depth)
    {
        var indent = new string(' ', depth * 3);
        builder.Append(indent);
        builder.Append(index);
        builder.Append("  ");
        builder.Append(FormatSize(node.Bytes).PadLeft(10));
        builder.Append("  ");
        builder.AppendLine(node.Path);

        if (!node.ExpandChildren)
        {
            return;
        }

        if (node.Children.Count == 0)
        {
            builder.Append(indent);
            builder.AppendLine(Ui.NoSubfolders);
            return;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            AppendFolder(builder, node.Children[i], i + 1, depth + 1);
        }
    }

    internal static string FormatSize(long bytes)
    {
        const double kb = 1024d;
        const double mb = 1024d * 1024d;
        const double gb = 1024d * 1024d * 1024d;

        if (bytes >= gb)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} GB", bytes / gb);
        }

        if (bytes >= mb)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", bytes / mb);
        }

        if (bytes >= kb)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", bytes / kb);
        }

        return $"{bytes} B";
    }
}

internal static class Ui
{
    public static bool English { get; private set; }

    public static void UseEnglish() => English = true;

    public static string ResultFileName => "CheckResult.txt";

    public static string DriveListHeader => English
        ? "Drives you can scan:"
        : "可扫描的盘：";

    public static string AskDrive => English
        ? "Which drive? Enter a drive letter, then press Enter: "
        : "要扫描哪个盘？输入盘符，然后回车：";

    public static string NoDriveEntered => English
        ? "No drive was entered. Exiting."
        : "未输入盘符，已退出。";

    public static string NoDrives => English
        ? "No drives can be scanned."
        : "没有可以扫描的盘。";

    public static string CancelOption => English
        ? "0  Cancel"
        : "0  取消扫盘";

    public static string ScanCancelled => English
        ? "Scan cancelled."
        : "已取消扫盘。";

    public static string EnterListedDrive => English
        ? "Enter a drive letter from the list above, or enter 0 to cancel."
        : "请输入上面列出的盘符，或输入 0 取消扫盘。";

    public static string DriveListItem(char letter, DriveType kind, string label)
    {
        var kindText = DriveKind(kind);
        return string.IsNullOrWhiteSpace(label)
            ? $"{letter}:  {kindText}"
            : $"{letter}:  {kindText}  {label}";
    }

    private static string DriveKind(DriveType kind)
    {
        if (English)
        {
            return kind switch
            {
                DriveType.Fixed => "Local disk",
                DriveType.Removable => "Removable",
                DriveType.Network => "Network drive",
                DriveType.CDRom => "Optical disc",
                DriveType.Ram => "RAM disk",
                _ => "Drive",
            };
        }

        return kind switch
        {
            DriveType.Fixed => "本地磁盘",
            DriveType.Removable => "可移动磁盘",
            DriveType.Network => "网络磁盘",
            DriveType.CDRom => "光盘",
            DriveType.Ram => "内存盘",
            _ => "磁盘",
        };
    }

    public static string AskPath => English
        ? "Which path next? Enter a full path, or press Enter to quit: "
        : "要继续查看哪个路径？输入完整路径后回车，直接回车结束：";

    public static string CannotReadDriveRoot => English
        ? "Cannot read the drive root."
        : "无法读取盘符根目录。";

    public static string InvalidPath => English
        ? "The path is not valid."
        : "路径无效。";

    public static string ExcludedPath => English
        ? "This path is a program or system folder that is not scanned."
        : "这个路径属于不扫描的程序或系统目录。";

    public static string NeedFolder => English
        ? "Enter a folder path."
        : "请输入文件夹路径。";

    public static string FolderNotFound => English
        ? "This folder cannot be found."
        : "找不到这个文件夹。";

    public static string LinkNotEntered => English
        ? "This path is a link and will not be opened."
        : "这个路径是链接，不进入。";

    public static string CannotReadFolder => English
        ? "This folder cannot be read."
        : "无法读取这个文件夹。";

    public static string NoFolders => English
        ? "No folders to list."
        : "没有可列出的文件夹。";

    public static string ManyDenied => English
        ? "Many directories were skipped. Run again as administrator to read more of them."
        : "跳过的目录较多，可以用管理员身份再运行一次，以读到更多目录。";

    public static string ChosenPath => English
        ? "Chosen path"
        : "指定路径";

    public static string NoSubfolders => English
        ? "   (no subfolders)"
        : "   （没有子文件夹）";

    public static string StartScan(string target) => English
        ? $"Starting scan {target}"
        : $"开始扫描 {target}";

    public static string SavedTo(string path) => English
        ? $"Saved to {path}"
        : $"已写入 {path}";

    public static string WriteFailed(string message) => English
        ? $"The result is on screen, but writing the file failed: {message}"
        : $"结果已显示在屏幕上，但写入文件失败：{message}";

    public static string AppendedTo(string path) => English
        ? $"Appended to {path}"
        : $"已追加到 {path}";

    public static string AppendFailed(string message) => English
        ? $"The result is on screen, but appending to the file failed: {message}"
        : $"结果已显示在屏幕上，但追加到文件失败：{message}";

    public static string DriveUnavailable(string drive) => English
        ? $"{drive} does not exist or its capacity cannot be read."
        : $"{drive} 不存在或无法读取容量。";

    public static string PathOnOtherDrive(string drive) => English
        ? $"Enter a folder path on {drive}."
        : $"请输入 {drive} 上的文件夹路径。";

    public static string Progress(string label, int processedDirectories) => English
        ? $"Scanning {label}    Directories processed {processedDirectories}"
        : $"正在扫描 {label}    已处理目录 {processedDirectories}";

    public static string DiskSummary(char driveLetter, string total, string used, string free) => English
        ? $"{driveLetter}:  Total {total}    Used {used}    Free {free}"
        : $"{driveLetter}:  总容量 {total}    已用 {used}    可用 {free}";

    public static string Included(string size) => English
        ? $"Included {size}"
        : $"列入统计 {size}";

    public static string Denied(int count) => English
        ? $"Skipped inaccessible directories {count}"
        : $"跳过无权限目录 {count} 个";
}
