using System;
using System.IO;
using System.Security.Permissions;


namespace DirWatcher
{
    public class Watcher
    {


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Run()
        {
            //string[] args = System.Environment.GetCommandLineArgs();

            // If a directory is not specified, exit program. 
            /*if (args.Length != 2)
            {
                // Display the proper way to call the program.
                //console.WriteLine("Usage: Watcher.exe (directory)");
                return;
            }*/

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();

            watcher.Path = "C:\\citrixinstantupload";

            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);//original
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;

            // Wait for the user to quit the program.
            //console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
        }

        //takes a string and return non-null if it is a valid action
        private static String actionFilter(String one)
        {

            return null;
        }


        // Define the event handlers. 
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            //console.WriteLine("File: " + e.FullPath + " " + e.ChangeType + " FileName: " + e.Name);

            String outputString = "File: " + e.FullPath +" " + e.ChangeType;
           
            //console.WriteLine(outputString);
  


            //add the command into the queue
            DataStore.Filenode toBeQueueNode = new DataStore.Filenode();
            toBeQueueNode.setNewPath(e.FullPath);
            toBeQueueNode.setAction(e.ChangeType.ToString());
            toBeQueueNode.setOldPath(null); //no such thing as no path
            toBeQueueNode.setCloudDestinationPath(null);//indetermined, require modification

            //do not enqueue .tmp or file that begins with ~
            if (Entry.Entry.checkTmp(e.Name) == false && Entry.Entry.checkWave(e.Name) == false)
            {
                Entry.Entry.fileQueue.Enqueue(toBeQueueNode);
            }


            

         

            


        }



        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            ////console.WriteLine("File: {0} renamed to {1} ", e.OldFullPath, e.FullPath);
            

            //console.WriteLine("File: {0} {1} to {2} and FileName: {3} finally, Old File Name: {4}", e.OldFullPath, e.ChangeType, e.FullPath, e.Name, e.OldName);

            DataStore.Filenode toBeQueueNode = new DataStore.Filenode();
            toBeQueueNode.setNewPath(e.FullPath);
            toBeQueueNode.setAction(e.ChangeType.ToString());
            toBeQueueNode.setOldPath(e.OldFullPath);
            toBeQueueNode.setCloudDestinationPath(null); //indetermined, requires modification

            /*try
            {
                FileInfo oldfileinfo = new FileInfo(e.OldFullPath);
                FileInfo newfileinfo = new FileInfo(e.FullPath);

                oldfileinfo.Refresh();
                //console.WriteLine("Old File LastWriteTime" + oldfileinfo.LastWriteTime);
                newfileinfo.Refresh();
                //console.WriteLine("New File lastWriteTime" + newfileinfo.LastWriteTime);

                if (oldfileinfo.Exists)
                {
                    //console.WriteLine("Old File Length in Bytes: " + oldfileinfo.Length);
                }

                if (newfileinfo.Exists)
                {
                    //console.WriteLine("New File Length in Bytes: " + newfileinfo.Length);
                }

                if (oldfileinfo.LastWriteTime.CompareTo(newfileinfo.LastWriteTime) < 0)
                {
                    //console.WriteLine("New File Info has later modification time");
                }
                else if (oldfileinfo.LastWriteTime.CompareTo(newfileinfo.LastWriteTime) > 0)
                {
                    //console.WriteLine("Old File Info has later modification time");
                }
                else
                {
                    //they are the same
                    //console.WriteLine("Same Modification Time");
                }

            }
            catch (Exception ex)
            {
                //exception thrown
                //console.WriteLine("Length Write Time Exception");
            }*/

            if (Entry.Entry.checkWave(e.Name) || Entry.Entry.checkTmp(e.Name))
                return;
            
            

           Entry.Entry.fileQueue.Enqueue(toBeQueueNode);
            
            
            
           

        }
    }
}
