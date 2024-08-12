using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnakinShared.Utils;
using Unakin.ToolWindows;
using Unakin.Utils;
using Microsoft.Extensions.Primitives;
using System.Threading;


namespace UnakinShared.Utils
{
    internal class SyncWatcher 
    {
        private System.Windows.Forms.Timer fileWatcher;
        FileSystemWatcher watcher;
        private static readonly object LockWatch = new object();
        List<String> created;
        List<String> changed;
        List<String> deleted;

        #region "Constructor"
        internal SyncWatcher(SemanticSearchUnakinControl sender)
        {
            directoryChageDetails = new List<DirectoryChageDetails> { };
            Sender = sender;

            watcher = new FileSystemWatcher();

        }
        #endregion

        #region Properties

        private SemanticSearchUnakinControl Sender {  get; set; }
        private string MasterDirectory { get; set; }
        private bool IsFileChanged { get; set; }
        private List<DirectoryChageDetails> directoryChageDetails { get; set; }

        #endregion

        public void AddFileWatch(string workingDirectory)
        {
            MasterDirectory = workingDirectory;
            watcher.Path = MasterDirectory;
            watcher.Filter = "*.*";
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.Attributes| NotifyFilters.FileName; //more options
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            fileWatcher = new System.Windows.Forms.Timer();
            fileWatcher.Interval = 3000;
            fileWatcher.Start();
            fileWatcher.Tick += fileWatcherTimer;

            watcher.EnableRaisingEvents = true;
        }

        public void RemoveFileWatch()
        {
            fileWatcher.Stop();
            watcher.EnableRaisingEvents = false;
        }


        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Sender.Dispatcher.BeginInvoke(() =>
            {
                lock (LockWatch)
                {
                    if (!directoryChageDetails.Any(x => x.Path.Contains(e.FullPath))){
                        IsFileChanged = true;
                        directoryChageDetails.Add(new DirectoryChageDetails { Path = e.FullPath, Changetype = e.ChangeType });
                    }
                }
            });
        }

        private void fileWatcherTimer(object sender, EventArgs e)
        {

            var tmpWorkingFiles = Directory.GetFiles(CommonUtils.WorkingDir, "*.*", SearchOption.AllDirectories).ToList();
            created = tmpWorkingFiles.Except(Sender.workingFiles).ToList();
            deleted = Sender.workingFiles.Except(tmpWorkingFiles).ToList();

            if (IsFileChanged == true || created.Count>0 || deleted.Count>0)
            {
                List<DirectoryChageDetails> tmpSyncFiles;
                //1. Stop Timer
                fileWatcher.Stop();
                //2. Empty main list, fill to temp list
                lock (LockWatch)
                {
                    tmpSyncFiles = new List<DirectoryChageDetails>( directoryChageDetails);
                    directoryChageDetails.Clear();
                    IsFileChanged = false;
                }
     
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Detected changes in files - ");
                foreach(var file in directoryChageDetails)
                {
                    sb.AppendLine(string.Concat(file.Changetype.ToString(), "-->", file.Path));
                } 
                UnakinLogger.LogInfo(sb.ToString());

                //Call Sync Functions
                var task = Task.Run(async () => await updateFiles(tmpSyncFiles)); 
                var result = task.Result;

                Sender.workingFiles = tmpWorkingFiles;

                //Start Timer
                fileWatcher.Start();
            }
        }

        async Task<bool> updateFiles(List<DirectoryChageDetails> tmpSyncFiles)
        {
            try
            {
                List<String> createdResult = null;
                List<String> changedResult = null;
                List<String> deletedResult = null;

                if (created.Count > 0)
                    createdResult = await Sender.serverHelper.SendFilesCreatedUpdateAsync(CommonUtils.WorkingDir, created, CancellationToken.None);

                if (deleted.Count > 0)
                    deletedResult = await Sender.serverHelper.SendFilesDeletedUpdateAsync(CommonUtils.WorkingDir, deleted, CancellationToken.None);

                var changed = tmpSyncFiles.Where(x => x.Changetype == WatcherChangeTypes.Changed).Select(x => x.Path).ToList();
                if (changed.Count > 0)
                    changedResult = await Sender.serverHelper.SendFilesChangedUpdateAsync(CommonUtils.WorkingDir, changed, CancellationToken.None);


                if (createdResult != null && created.Count > createdResult.Count)
                {
                    var sb = new StringBuilder("Following files added to working directory are updated to server - ");
                    foreach (var f in created.Except(createdResult).ToList())
                    {
                        sb.AppendLine(f);
                    }
                    UnakinLogger.LogInfo(sb.ToString());
                }

                if (changedResult != null && changed.Count > changedResult.Count)
                {
                    var sb = new StringBuilder("Following files changed to working directory are updated to server - ");
                    foreach (var f in changed.Except(changedResult).ToList())
                    {
                        sb.AppendLine(f);
                    }
                    UnakinLogger.LogInfo(sb.ToString());
                }

                if (deletedResult != null && deleted.Count>deletedResult.Count   )
                {
                    var sb = new StringBuilder("Following files deleted in working directory are updated to server - ");
                    foreach (var f in deleted.Except(deletedResult).ToList())
                    {
                        sb.AppendLine(f);
                    }
                    UnakinLogger.LogInfo(sb.ToString());
                }

                created.Clear();
                changed.Clear();
                deleted.Clear();

                return true;
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while syncing files");
                UnakinLogger.HandleException(ex);
                return false;
            }
        }

        private class DirectoryChageDetails{
            internal string Path { get; set; }
            internal WatcherChangeTypes Changetype { get; set; }

        }
    }


}
