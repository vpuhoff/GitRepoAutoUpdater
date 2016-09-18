using System;
using System.IO;
using LibGit2Sharp;
using System.Threading;

namespace gitnano
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceUrl = args[0];
            var namedir = args[1];
            string dirname = GetDirName2(namedir);
            if (Directory.Exists(dirname))
            {
                try
                {
                    using (var repo = new Repository(dirname))
                    {
                        Console.WriteLine("Fetching...");
                        PullRepo(sourceUrl, dirname);
                        Console.WriteLine("repository successfully updated");
                    }
                }
                catch (Exception exx)
                {
                    Console.WriteLine(exx.Message);
                    Console.WriteLine("Pull failed, try reload repository...");
                    string newdir = dirname + ".new";
                    string bacdir = dirname + ".bak";

                    if (Directory.Exists(newdir))
                    {
                        RemoveAll(newdir);
                    }

                    Directory.CreateDirectory(newdir);
                    try
                    {
                        Repository.Clone(sourceUrl, newdir);
                        if (Directory.Exists(dirname + ".bak"))
                        {
                            RemoveAll(dirname + ".bak");
                        }
                        Directory.Move(dirname, bacdir);
                        if (Directory.Exists(dirname))
                        {
                            RemoveAll(dirname);
                        }
                        Directory.Move(newdir, dirname);
                        Console.WriteLine("repository successfully reloaded");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Update failed!");
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(dirname);
                Repository.Clone(sourceUrl, dirname);
                Console.WriteLine("repository successfully cloned");
            }
            Thread.Sleep(1000);
        }

        private static void RemoveAll(string newdir)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(newdir);
                if (directoryInfo.Exists)
                {
                    DirectoryInfo[] Dir = directoryInfo.GetDirectories();
                    foreach (DirectoryInfo dir in Dir)
                    {
                        try
                        {
                            // ищем во всех папках
                            FileInfo[] fileInfos = dir.GetFiles("*.*", SearchOption.AllDirectories);
                            foreach (FileInfo fi in fileInfos)
                            {
                                if ((fi.Attributes & System.IO.FileAttributes.ReadOnly) != 0)
                                    // удаляем атрибут "только для чтения"
                                    fi.Attributes -= System.IO.FileAttributes.ReadOnly;
                                File.Delete(fi.FullName);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            Directory.Delete(newdir, true);
        }

        private static void PullRepo(string sourceUrl, string dirname)
        {
            using (Repository repo = new Repository(dirname))
            {
                var tipId = repo.Head.Tip.Tree;
                Console.WriteLine("HEAD tree id: " + tipId.Id.ToString());

                // Pull changes
                PullOptions options = new PullOptions();

                options.FetchOptions = new FetchOptions();
                options.MergeOptions = new MergeOptions();
                // ! Only for trying to fix the bug. Should not be here
                //options.MergeOptions.FileConflictStrategy = CheckoutFileConflictStrategy.Theirs;
                //repo.Reset(repo.Head.Tip);
                repo.Index.Replace(repo.Head.Tip);
                Console.WriteLine("Try pull from remote repository");
                LibGit2Sharp.Commands.Pull(repo, GetSign(), options);
                // get difference in the git tree (file-system)
                foreach (var item in repo.Commits)
                {
                    Console.WriteLine(item.Author + ":\t" + item.MessageShort);
                }
                var diffs = repo.Diff.Compare<TreeChanges>(Directory.GetFiles(dirname), true);
                foreach (var item in diffs)
                {
                    throw new Exception("Need UPDATE!");
                }
                Console.WriteLine("Pull complete!");
            }
        }

        private static Signature GetSign()
        {
            return new Signature(Environment.UserName, Environment.UserName + "@" + Environment.MachineName + ".com", new DateTimeOffset(DateTime.Now));
        }

        private static string GetDirName(string sourceUrl)
        {
            var reponame = Path.GetFileNameWithoutExtension(sourceUrl);
            return Environment.CurrentDirectory + "\\" + reponame;
        }
        private static string GetDirName2(string targetname)
        {
            return Environment.CurrentDirectory + "\\" + targetname;
        }
    }
}
