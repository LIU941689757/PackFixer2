using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace PackFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string changeExe = Path.Combine(baseDir, "Changeexe");
            string changeFile = Path.Combine(baseDir, "Changefile");
            string workDir = Path.Combine(baseDir, "work");
            string srcDir = Path.Combine(baseDir, "src");
            string sevenZip = @"C:\Program Files\7-Zip\7z.exe";

            string newNTADM = Path.Combine(changeFile, "NTADM002.exe");
            string newNTDOM = Path.Combine(changeFile, "NTDOM.dll");

            var candidates = Directory.GetFiles(changeExe, "*.exe", SearchOption.TopDirectoryOnly);
            if (candidates.Length != 1) { Console.WriteLine("Changeexe 目录里只能有一个 exe"); return; }

            string targetExe = candidates[0];
            string name = Path.GetFileNameWithoutExtension(targetExe);

            if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
            Directory.CreateDirectory(workDir);

            // 仍用 7z.exe 解压：解到 work\
            Run(sevenZip, $"x -y -o\"{workDir}\" \"{targetExe}\"", baseDir);

            // 常见为 work\<name>\...；若顶层直接是文件，则用 work\
            string copyRoot = Directory.Exists(Path.Combine(workDir, name))
                                ? Path.Combine(workDir, name)
                                : workDir;

            // .NET 覆盖替换
            ReplaceAll(copyRoot, "NTADM002.exe", newNTADM);
            ReplaceAll(copyRoot, "NTDOM.dll", newNTDOM);

            // .NET 递归复制（把 copyRoot 的内容铺到 src\）
            if (Directory.Exists(srcDir)) Directory.Delete(srcDir, true);
            Directory.CreateDirectory(srcDir);
            CopyTree(copyRoot, srcDir);

            // 直接启动 ch.bat（不经 cmd /c）
            Run("ch.bat", name, baseDir);

            Console.WriteLine($"{name} 完成");
        }

        static void ReplaceAll(string root, string fileName, string newFile)
        {
            foreach (var f in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                File.Copy(newFile, f, true);
        }

        // 把 src 的“内容”复制到 dst（不额外套一层目录）
        static void CopyTree(string worksrc, string dstsrc)
        {
            // 根层文件
            foreach (var file in Directory.EnumerateFiles(worksrc, "*", SearchOption.TopDirectoryOnly))
                File.Copy(file, Path.Combine(dstsrc, Path.GetFileName(file)), true);

            // 子目录
            foreach (var dir in Directory.EnumerateDirectories(worksrc, "*", SearchOption.TopDirectoryOnly))
            {
                var childDst = Path.Combine(dstsrc, Path.GetFileName(dir));
                Directory.CreateDirectory(childDst);
                CopyTree(dir, childDst);
            }
        }

        //命令启动方法
        static void Run(string fileName, string arguments, string workingDir)
        {
            var p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();
        }
    }
}
