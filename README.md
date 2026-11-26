# Drive Space Checker by Path

按路径检查磁盘空间

[English](#english) · [中文](#中文)

# English

## Introduction

Drive Space Checker by Path is a read-only command-line tool. It lists the drives Windows can read, including a USB drive, and scans the one you choose. It lists the folders that take the most space. You can then paste a folder path and see how that path is divided among its subfolders.

It only checks folders, and the size of a folder path you paste in. It does not open file contents, and it does not delete, move, or clean anything. After you see the sizes, you decide what to remove.

Use it when a drive is full and you want to know which folders take the most space. A USB drive is chosen by the letter Windows gave it.

## Installation

### Use the program

This program runs on Windows. On the GitHub repository page, choose **Code**, then **Download ZIP**. Unzip it and open this file:

```text
DriveSpaceChecker.exe
```

It sits in the project root, next to this README. Double-click it. The .NET runtime is already inside this file.

### Build it yourself

To change the program and run it from source, install the [.NET 9 SDK](https://dotnet.microsoft.com/download). In a terminal, `dotnet --version` should print a version that starts with `9`.

## Steps

### 1. Start the program

Double-click `DriveSpaceChecker.exe` in the project root. A command window opens.

To run from source instead, open a terminal in the `CheckDrive` folder and run:

```text
dotnet run
```

The report is saved as `CheckResult.txt` in the folder where the program starts. For the downloaded exe, that folder is the project root, next to `DriveSpaceChecker.exe`. For `dotnet run`, that folder is `CheckDrive`. English and Chinese use the same file name.

When you start the exe by double-clicking, the window closes after the program ends. The report stays in that file.

### 2. Choose a language

The program asks, in both languages:

```text
界面语言 / Language. 输入 1 使用中文，输入 2 for English：
```

Enter `2`, `en`, or `English`. The prompts and the report after this step are in English.

### 3. Choose a drive

The program lists the drives it can read, then asks:

```text
Drives you can scan:
C:  Local disk
D:  Local disk
E:  Removable  KINGSTON
0  Cancel
Which drive? Enter a drive letter, then press Enter: 
```

A USB drive is marked `Removable`. Its name, when it has one, is shown after that. Type its letter, such as `E` or `E:`, then press Enter. Letters that are not in the list are not accepted. A drive that is unplugged does not appear. Enter `0` or `cancel` to stop the program without scanning.

It scans that drive and lists the largest folders. The same list is saved to `CheckResult.txt`.

The first list has four levels:

1. The 3 largest folders on the drive.
2. Inside each of those, the 5 largest folders.
3. Inside each of those, the 3 largest folders.
4. Inside each of those, 3 more folders. It stops there.

### 4. Paste a folder path

When the first list is done, the program asks:

```text
Which path next? Enter a full path, or press Enter to quit: 
```

Copy a folder path from the list, or from File Explorer, and paste it. Then press Enter.

The program reports the size of that folder, its 3 largest subfolders, and 3 folders inside each of those. This new section is added to the end of `CheckResult.txt`. The first drive report stays in the file.

You can paste another path and repeat. Press Enter on an empty line to quit.

The path has to be a folder on the drive you chose in step 3. A path on another drive is not accepted. You can paste a full path, such as `E:\Photos` or `C:\Users\YourName\Downloads`, or a path starting from that drive, such as `Photos` or `Users\YourName\Downloads`.

## Folders it skips

It does not enter `Windows`, `Program Files`, `Program Files (x86)`, `ProgramData`, or the Unity folders the app needs in order to run. A Unity project stored somewhere else is still counted.

Some system folders cannot be opened without administrator rights. The end of the report says how many folders were skipped.

## How sizes are counted

The sizes are the sum of file lengths the program could read. They can be a little different from the used-space number shown in File Explorer.

# 中文

## 简介

按路径检查磁盘空间是一个只读的命令行工具。它先列出 Windows 当前能读取的盘，包括 U 盘，再扫描你选的那一块，列出最占空间的文件夹。之后你可以粘贴一个文件夹路径，查看这个路径下面的空间是怎么分到各个子文件夹的。

这个程序只能查看文件夹，以及你粘贴进去的那个路径占了多少空间。它不打开文件内容，也不删除、不移动、不清理任何东西。看到大小之后，删不删由你自己决定。

某块盘快满的时候，用它看哪些文件夹最占地方。U 盘用 Windows 分配给它的盘符来选。

## 安装

### 直接使用

这个程序在 Windows 上运行。在 GitHub 仓库页面选择 **Code**，再选 **Download ZIP**。解压后打开这个文件：

```text
DriveSpaceChecker.exe
```

它在项目根目录，和这份 README 放在一起。双击它。这个文件里已经带有 .NET 运行时。

### 自己编译

要改程序、从源代码运行，需要安装 [.NET 9 SDK](https://dotnet.microsoft.com/download)。在终端里运行 `dotnet --version`，版本号应以 `9` 开头。

## 操作步骤

### 1. 启动程序

双击项目根目录里的 `DriveSpaceChecker.exe`。会打开一个命令行窗口。

要从源代码运行，则在 `CheckDrive` 文件夹里打开终端，运行：

```text
dotnet run
```

报告会写成启动时所在文件夹里的 `CheckResult.txt`。下载来的 exe，这个文件夹是项目根目录，和 `DriveSpaceChecker.exe` 放在一起。用 `dotnet run` 时，这个文件夹是 `CheckDrive`。中文和英文使用同一个文件名。

双击 exe 启动时，程序结束后窗口会关掉。报告留在这个文件里。

### 2. 选择语言

程序会用中英双语询问：

```text
界面语言 / Language. 输入 1 使用中文，输入 2 for English：
```

输入 `1`、`zh` 或 `中文`。这一步之后的提示和报告都是中文。

### 3. 选择磁盘

程序先列出能读取的盘，然后询问：

```text
可扫描的盘：
C:  本地磁盘
D:  本地磁盘
E:  可移动磁盘  KINGSTON
0  取消扫盘
要扫描哪个盘？输入盘符，然后回车：
```

U 盘标成「可移动磁盘」。它有名称时，名称写在后面。输入它的盘符，例如 `E` 或 `E:`，然后按回车。不在列表里的盘符不会被接受。已经拔掉的盘不会出现。输入 `0`、`取消` 或 `取消扫盘`，程序结束，不开始扫描。

它会扫描这块盘，列出最大的文件夹。同一份名单会写到 `CheckResult.txt`。

第一份名单有四层：

1. 这块盘上最大的 3 个文件夹。
2. 这 3 个里面，各自最大的 5 个文件夹。
3. 再往里，各自最大的 3 个文件夹。
4. 再往里，各自再列 3 个文件夹。到这里停止。

### 4. 粘贴文件夹路径

第一份名单出来之后，程序会问：

```text
要继续查看哪个路径？输入完整路径后回车，直接回车结束：
```

从名单里或资源管理器里复制一个文件夹路径，粘贴到这里，然后按回车。

程序会给出这个文件夹的大小、它里面最大的 3 个子文件夹，以及这 3 个里面各自最大的 3 个文件夹。这段结果会追加到 `CheckResult.txt` 末尾，前面的整盘名单还在。

可以再粘贴下一个路径，重复这一步。不输入内容，直接按回车，程序就结束。

路径必须是第 3 步所选那块盘上的文件夹。另一块盘上的路径不会被接受。可以粘贴完整路径，例如 `E:\照片` 或 `C:\Users\YourName\Downloads`，也可以粘贴从盘符开始的路径，例如 `照片` 或 `Users\YourName\Downloads`。

## 会跳过的文件夹

它不进入 `Windows`、`Program Files`、`Program Files (x86)`、`ProgramData`，也不进入 Unity 运行所需的文件夹。放在其他位置的 Unity 工程仍会计入大小。

有些系统文件夹需要管理员权限才能打开。报告末尾会写出跳过了多少个文件夹。

## 大小是怎么算的

这里的大小是程序能读到的文件长度相加，和资源管理器里的“已用空间”可能有一点差别。
