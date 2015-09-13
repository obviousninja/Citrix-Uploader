using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using ShareFileV3Sample;

namespace Entry
{
    class Entry
    {
        
        public static ConcurrentQueue<DataStore.Filenode> fileQueue = new ConcurrentQueue<DataStore.Filenode>();
        private const String cloudParentFolderPathId = "fo02dd31-c6ea-4135-a6c5-30d811ace862";

        //Authentication overhead for NET SDK
        /*public static SharefileNet.SampleUser verfiedUser;
        public static String verifiedoauthClientId;
        public static String verifiedoauthClientSecret;*/

        //used for autoreset event as handle
        public static AutoResetEvent singleEntranceEvent = new AutoResetEvent(false);

        //used for html authentication
        public static OAuth2Token connectionToken;

        //file handle for waker
        public static String fWakeHandle = null;

        //goes to sleep is there is no work
        public static void worker(){
            //keeps going until terminated
            while (true)
            {
                //put the thread to idle so to save performance
                Thread.Sleep(777);


                if (fileQueue.IsEmpty == false)
                {

                    //file queue is not empty hence we do work
                    
                    //initialize the variables to null
                    DataStore.Filenode containerNode = null;
                    //String fileId = null;
                    String oldFilename = null;
                    String newFilename = null;
                    int fileTypeFlag = -1; //if flag is 0, then file, if flag is 1, then folder. if -1, then error.

                    if (fileQueue.TryDequeue(out containerNode))
                    {
                        //something returns, most likely and possible
                 
                        ////console.WriteLine(containerNode.toString());
                        

                        //action will never be null.
                        if(containerNode.getAction().ToLower().Equals("renamed")){
                            //onrenamed
                            //newpath, oldpath and action
                            //Acquire oldFile name, newFile name

                            ////console.WriteLine("renaming");


                            if (String.IsNullOrWhiteSpace(containerNode.getNewPath()) != true && String.IsNullOrEmpty(containerNode.getNewPath()) != true)
                            {
                                newFilename = containerNode.getNewPath().Substring(containerNode.getNewPath().LastIndexOf("\\") + 1);
                            }

                            if (String.IsNullOrEmpty(containerNode.getOldPath()) != true && String.IsNullOrWhiteSpace(containerNode.getOldPath()) != true)
                            {
                                oldFilename = containerNode.getOldPath().Substring(containerNode.getOldPath().LastIndexOf("\\") + 1);
                            }

                            ////console.WriteLine("newFileName: " + newFilename);
                            //console.WriteLine("oldFileName: " + oldFilename);
                        
                            //doesn't matter if it is oldpath or newpath, if one of the path exist, then do something about it.

                            //if oldFileId is null, then file doesn't exist, otherwise, file exist and fileId is returned
                            StringBuilder oldfileId = new StringBuilder();
                            ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, oldFilename, oldfileId, true);

                            //if newFileId is null, then file doesn't exist, otherwise file exist and fileid is returned
                            StringBuilder newfileId = new StringBuilder();
                            ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, newFilename, newfileId, true);

                            //parent ids of newfile and oldfile in the cloud, if they exist
                            String parentOfOldFileId = ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, oldfileId.ToString()); //null or itemid
                            String parentOfNewFileId = ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, newfileId.ToString()); //null or itemid

                            ////console.WriteLine("Old File Parent: " + parentOfOldFileId);
                            ////console.WriteLine("Old File Id: " + oldfileId.ToString() + " ::oldFilename: "+ oldFilename);
                            //console.WriteLine("New File Parent: " + parentOfNewFileId);
                            //console.WriteLine("New File Id: " + newfileId.ToString() + " ::newFilename: "+ newFilename);

                            //handling oldfile exist as a file
                            if ((new FileInfo(containerNode.getOldPath())).Exists)
                            {

                                if (String.IsNullOrEmpty(oldfileId.ToString()) == false && String.IsNullOrWhiteSpace(oldfileId.ToString()) == false)
                                {
                                    //delete the cloud file
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, oldfileId.ToString());
                                }

                            }
                            else if ((new DirectoryInfo(containerNode.getOldPath()).Exists))
                            {
                                if (String.IsNullOrEmpty(oldfileId.ToString()) == false && String.IsNullOrWhiteSpace(oldfileId.ToString()) == false)
                                {
                                    //delete the cloud folder
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, oldfileId.ToString());
                                }
                               
                            }

                           
                            //newfile is a file
                            if ((new FileInfo(containerNode.getNewPath())).Exists)
                            {
                                //exist in the cloud
                                if (String.IsNullOrEmpty(newfileId.ToString()) == false && String.IsNullOrWhiteSpace(newfileId.ToString()) == false)
                                {
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, newfileId.ToString());

                                    String toBeUsedId = null;

                                    if(checkFolderCode(parentOfNewFileId)){
                                        toBeUsedId = parentOfNewFileId;
                                    }
                                    if(checkFolderCode(parentOfOldFileId)){
                                        toBeUsedId = parentOfOldFileId;
                                    }

                                    ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, toBeUsedId, containerNode.getNewPath());

                                }
                                else
                                {
                                    //doesn't exist in the cloud
                                    ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());

                                }

                            }
                            else if ((new DirectoryInfo(containerNode.getNewPath())).Exists) //newfile is a folder
                            {

                                if (String.IsNullOrEmpty(newfileId.ToString()) == false && String.IsNullOrWhiteSpace(newfileId.ToString()) == false)
                                {
                                    //folder exist in the cloud
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, newfileId.ToString());

                                    String toBeUsedId = null;

                                    if (checkFolderCode(parentOfNewFileId))
                                    {
                                        toBeUsedId = parentOfNewFileId;
                                    }
                                    if (checkFolderCode(parentOfOldFileId))
                                    {
                                        toBeUsedId = parentOfOldFileId;
                                    }

                                    ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, toBeUsedId, containerNode.getNewPath());

                                }
                                else
                                {
                                    //folder doesn't exist in the cloud
                                    ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());
                                }

                            }
                         


                        }else if(containerNode.getAction().ToLower().Equals("deleted")){
                            //ondelete
                            //console.WriteLine("deletion occurring");
                            
                            //if file also exist on the cloud, then delete, if not do nothing
                            try
                            {
                                //delete the file or folder

                                if (String.IsNullOrWhiteSpace(containerNode.getNewPath()) != true && String.IsNullOrEmpty(containerNode.getNewPath()) != true)
                                {
                                    newFilename = containerNode.getNewPath().Substring(containerNode.getNewPath().LastIndexOf("\\") + 1);
                                }

                                if (String.IsNullOrEmpty(containerNode.getOldPath()) != true && String.IsNullOrWhiteSpace(containerNode.getOldPath()) != true)
                                {
                                    oldFilename = containerNode.getOldPath().Substring(containerNode.getOldPath().LastIndexOf("\\") + 1);

                                }

                                ////console.WriteLine("deletion path: {0}", newFilename);

                                StringBuilder deletefileId = new StringBuilder();
                                ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, newFilename, deletefileId, true );
                                ////console.WriteLine("DeleteFileId: {0}", deletefileId.ToString());

                                ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, deletefileId.ToString());


                            }
                            catch (Exception e)
                            {
                                //something happens when doing deletion, so we just let it go.
                                //the only thing that can happen is that cloud doesnt have the file
                               
                            }


                        }
                        else if (containerNode.getAction().ToLower().Equals("created"))
                        {
                            //onCreate
                         

                            if (String.IsNullOrWhiteSpace(containerNode.getNewPath()) != true && String.IsNullOrEmpty(containerNode.getNewPath()) != true)
                            {
                                //console.WriteLine("Create New Path Check");

                                newFilename = containerNode.getNewPath().Substring(containerNode.getNewPath().LastIndexOf("\\") + 1);
                            }

                            if (String.IsNullOrEmpty(containerNode.getOldPath()) != true && String.IsNullOrWhiteSpace(containerNode.getOldPath()) != true)
                            {
                              
                                oldFilename = containerNode.getOldPath().Substring(containerNode.getOldPath().LastIndexOf("\\") + 1);

                            }


                            //item id
                          
                            StringBuilder createFileId = new StringBuilder();
                            ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, newFilename, createFileId, true);




                            if (String.IsNullOrEmpty(createFileId.ToString()) == false && String.IsNullOrWhiteSpace(createFileId.ToString()) == false)
                            {
                                //already exist in the cloud
                                //parent of item id
                              
                                String itemParentId = ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, createFileId.ToString());

                               
                            
                                if (File.Exists(containerNode.getNewPath()))
                                {    //if it is a file

                                  
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, createFileId.ToString());

                                    ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, itemParentId, containerNode.getNewPath());


                                }
                                else if (Directory.Exists(containerNode.getNewPath()))
                                {   //if it is a folder

                                   
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, createFileId.ToString());

                                    ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, itemParentId, containerNode.getNewPath());



                                }
                                else
                                {
                                    //an item cannot be both not a file and not a folder
                                }

                             

                            }
                            else
                            {
                                //doesn't exist in the cloud
                                //get the local parent folder
                           
                                String localParentFolder = ShareFileV3Sample.ShareFileV3Sample.getLocalParentFolder(containerNode.getNewPath());
                       
                                

                                //find the local parent folder's equivilant in the cloud, get its fileid
                                StringBuilder currentCloudParentFolderId = new StringBuilder();
                                ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, localParentFolder, currentCloudParentFolderId, true);
                                //console.WriteLine(currentCloudParentFolderId.ToString());

                                if (File.Exists(containerNode.getNewPath()))
                                {
                                    fileTypeFlag = 0;
                                }
                                else if (Directory.Exists(containerNode.getNewPath()))
                                {
                                    fileTypeFlag = 1;
                                }
                                else
                                {
                                    //error, cannot be possible
                                    fileTypeFlag = -1;
                                }
                               
                                if (String.IsNullOrEmpty(currentCloudParentFolderId.ToString()) == true || String.IsNullOrWhiteSpace(currentCloudParentFolderId.ToString()) == true)
                                {
                                    //no local folder equivilant
                                    
                                    if (fileTypeFlag == 0)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());
                                    }
                                    else if (fileTypeFlag == 1)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());
                                    }
                                    else
                                    {
                                        //do nothing, because error
                                    }


                                }
                                else
                                {
                                    //there is local folder equivilant
                                    //upload the file to the file in the cloud
                                   
                                    if (fileTypeFlag == 0)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                       // ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                    }
                                    else if (fileTypeFlag == 1)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                    }
                                    else
                                    {
                                        //do nothing, because error
                                    }

                                }
                                
                                

                              

                            }
                            


                        }
                        else
                        {
                            //oncreate, ondelete, onchange
                            //newpath and actions are avaliable

                            //verify if the newpath or file exist
                            //verify if newpath file or directory exist
                            //console.WriteLine("changed occurring");
                            
                            if (String.IsNullOrWhiteSpace(containerNode.getNewPath()) != true && String.IsNullOrEmpty(containerNode.getNewPath()) != true)
                            {
                                newFilename = containerNode.getNewPath().Substring(containerNode.getNewPath().LastIndexOf("\\") + 1);
                            }

                            if (String.IsNullOrEmpty(containerNode.getOldPath()) != true && String.IsNullOrWhiteSpace(containerNode.getOldPath()) != true)
                            {
                                oldFilename = containerNode.getOldPath().Substring(containerNode.getOldPath().LastIndexOf("\\") + 1);

                            }

                            if (File.Exists(containerNode.getNewPath()))
                            {
                                fileTypeFlag = 0;
                            }
                            else if (Directory.Exists(containerNode.getNewPath()))
                            {
                                fileTypeFlag = 1;
                            }
                            else
                            {
                                //impossible
                                fileTypeFlag = -1;
                            }
                            


                            //item id
                            StringBuilder createFileId = new StringBuilder();
                            ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, newFilename, createFileId, true);


                            if (String.IsNullOrEmpty(createFileId.ToString()) == false && String.IsNullOrWhiteSpace(createFileId.ToString()) == false)
                            {
                                //already exist in the cloud
                                //parent of item id
                                String itemParentId = ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, createFileId.ToString());

                                //ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, createFileId.ToString()); //mustn't do deletion, too dangerous
                                
                                
                                
                                if (fileTypeFlag == 0)
                                {//if it is a file

                                    //deletion may be needed
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, createFileId.ToString()); //file can be deleted
                                    ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, itemParentId, containerNode.getNewPath());

                                }
                                else if (fileTypeFlag == 1)
                                { //if it is a folder
                                    ShareFileV3Sample.ShareFileV3Sample.DeleteItem(connectionToken, createFileId.ToString()); //folder can be deleted too
                                    ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, itemParentId, containerNode.getNewPath());

                                }
                                else
                                {
                                    //impossible

                                }
                               



                            }
                            else
                            {
                                //doesn't exist in the cloud
                                //get the local parent folder
                                String localParentFolder = ShareFileV3Sample.ShareFileV3Sample.getLocalParentFolder(containerNode.getNewPath());
                                //console.WriteLine(localParentFolder);


                                //find the local parent folder's equivilant in the cloud, get its fileid
                                StringBuilder currentCloudParentFolderId = new StringBuilder();
                                ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(connectionToken, cloudParentFolderPathId, localParentFolder, currentCloudParentFolderId, true);
                                //console.WriteLine(currentCloudParentFolderId.ToString());


                                if (String.IsNullOrEmpty(currentCloudParentFolderId.ToString()) == true || String.IsNullOrWhiteSpace(currentCloudParentFolderId.ToString()) == true)
                                {
                                    //no local folder equivilant

                                    if (fileTypeFlag == 0)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());
                                    }
                                    else if (fileTypeFlag == 1)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, cloudParentFolderPathId, containerNode.getNewPath());
                                    }
                                    else
                                    {
                                        //do nothing, because error
                                    }


                                }
                                else
                                {
                                    //there is local folder equivilant
                                    //upload the file to the file in the cloud

                                    if (fileTypeFlag == 0)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFile(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                        // ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                    }
                                    else if (fileTypeFlag == 1)
                                    {
                                        ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(connectionToken, currentCloudParentFolderId.ToString(), containerNode.getNewPath());
                                    }
                                    else
                                    {
                                        //do nothing, because error
                                    }

                                }





                            }

                        }

                        //letting the waker know that there may be chance queue is now empty
                        if (fileQueue.IsEmpty == true)
                        {
                            //console.WriteLine("fileQueue emptied by worker");
                            singleEntranceEvent.WaitOne();
                        }
                    }
                    else
                    {
                        //console.WriteLine("worker didn't take an item from the fileQueue");
                        //nothing returns, unlikely, but possibly due to the last item has been retrieved
                    }

                }
                else
                {
                    //filequeue is empty, hence we signal waker thread
                    //console.WriteLine("worker waiting because fileQueue has no work");
                    
                    singleEntranceEvent.WaitOne();

                }


            }
            
            
       
        }

        //waits until the queue is empty
        public static void waker()
        {

            //keeps going until terminated
            while (true)
            {  

                //waker thread frequency would indicate the responsiveness of worker thread
               Thread.Sleep(777);
                if(String.IsNullOrEmpty(fWakeHandle) == false){

                    //if the file is no longer blocked, then wake the worker thread
                    if (ShareFileV3Sample.ShareFileV3Sample.IsFileLocked(new FileInfo(fWakeHandle)) == false)
                    {
                        //signal the worker thread since the fwakehandle is no longer blocked
                        //console.WriteLine("waker checks if fwakehandle is still blocked...");
                        fWakeHandle = null;
                        singleEntranceEvent.Set();
                    }
                    else
                    {
                        //still blocked, so we do nothing
                    }
                   

                }

                if (fileQueue.IsEmpty == false)
                {
                    //filequeue is not empty, hence we block this thread
                    //console.WriteLine("waker notifying worker");
                    singleEntranceEvent.Set();
                    
                }
                
            }
            
                
            

        }

        //return false if s is null, empty, or isn't a cloud folder, if it is a cloud folder, it returns true
        public static bool checkFolderCode(String s){

            if (s == null || s.Length == 0)
            {
                return false;
            }

            if (s.IndexOf("fo") == 0)
                return true;


            return false;
        }


        //HTML API
        static bool authen()
        {
            //authentication first
            string hostname = "isoa2015.sharefile.com";
            string username = "vendors@stability-operations.org";
            string password = "Conduct2015";
            string clientId = "fZYqiGVX1ZwFZvSWn2UqjZNtOT1uSyWT";
            string clientSecret = "7sKJVwIpcIGYHWBLeXnSNcRIXE2al2AgVGihKEgdq0W4CZpJ";

            OAuth2Token token = ShareFileV3Sample.ShareFileV3Sample.Authenticate(hostname, clientId, clientSecret, username, password);

            if (token != null)
            {
                //initialize OAuth2Token
                connectionToken = token;


                //testground
               // //console.WriteLine("performing initial actions");

               // StringBuilder newString = new StringBuilder();

                
              //  //console.WriteLine("Evil Things Are Occurring File" + ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "3455.pub", newString, true));
               // //console.WriteLine("StringBuilder String is here: " + newString);

               // newString.Clear();
               // //console.WriteLine("Evil Things Are Occurring Folder" + ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "absolutely.docx", newString, true));
               // //console.WriteLine("StringBuilder String is here: " + newString);

                //clear string builder so it can be used again
                //newString.Clear();
               // //console.WriteLine("Evil Things Are Occurring Folder" + ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "randomnothing", newString, true));
                ////console.WriteLine("StringBuilder String is here: " + newString);

                //get parent of child test
               // //console.WriteLine("Parent of child moo is: " + ShareFileV3Sample.ShareFileV3Sample.getParentofChild(token, ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "moo", true)));
               // //console.WriteLine("Parent of child nothing is: " + ShareFileV3Sample.ShareFileV3Sample.getParentofChild(token, ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "nothing", true)));
               // //console.WriteLine("Parent of child a file is: " + ShareFileV3Sample.ShareFileV3Sample.getParentofChild(token, ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "aaw.jnt", true)));
                

                //update file test
             //   ShareFileV3Sample.ShareFileV3Sample.UpdateItem(token, ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "newitem.docx", true), "newerItem.docx", "sickitem");

                //update folder test
               // ShareFileV3Sample.ShareFileV3Sample.UpdateItem(token, ShareFileV3Sample.ShareFileV3Sample.searchExpandFolderById(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "testintest", true), "chickenFolder", "sickitem");

                //upload file test
                //File.SetAttributes("C:\\Users\\Randy\\Desktop\\monitored\\Printer Driver.giberish", FileAttributes.Normal);
              //  ShareFileV3Sample.ShareFileV3Sample.UploadFile(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "C:\\Users\\Randy\\Desktop\\monitored\\Printer Driver.giberish");
                
                //create folder test
               ////console.WriteLine( ShareFileV3Sample.ShareFileV3Sample.CreateFolder(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "strange", "strangeFolder"));

                //upload folder test
                //ShareFileV3Sample.ShareFileV3Sample.UploadFolder(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "C:\\Users\\Randy\\Desktop\\monitored");
                
                //test folder upload wrapper
               // ShareFileV3Sample.ShareFileV3Sample.UploadFolderWraper(token, "foeefacf-7f53-48ff-a5fd-29adcd016419", "C:\\Users\\Randy\\Desktop\\monitored");

                 //test iscloudfile
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFile("fi3jo2iajwoeiaj"); //is
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFile("fowaoweuaheiauhwiea"); //is not
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFile("wewafieuaheiauhwiea"); //is not
                ////console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFile("fi3jo2iajwoeiaj"));
                ////console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFile("fowaoweuaheiauhwiea"));
                ////console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFile("wewafieuaheiauhwiea"));
                 
                 //test iscloudfolder
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("fi3jo2iajwoeiaj"); //is not
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("fowaoweuaheiauhwiea"); //is
                //ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("wewafoeuaheiauhwiea"); //is not
                ////console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("fi3jo2iajwoeiaj"));
                ////console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("fowaoweuaheiauhwiea"));
               // //console.WriteLine(ShareFileV3Sample.ShareFileV3Sample.isCloudFolder("wewafoeuaheiauhwiea"));
                //String samplePath = "C:\\Users\\Randy\\Desktop\\monitored\\Printer Driver.giberish";
                //ShareFileV3Sample.ShareFileV3Sample.getLocalParentFolder(samplePath);

                //filter out files that ends in .tmp
                //unit tests starts here

                //tmp
                //testcase(checkTmp("creates.tmp"), 1, true);
                //testcase(checkTmp("crea.tmper"), 2, false);
                //testcase(checkTmp(null), 3, false);
                //testcase(checkTmp("jw"), 7, false);

                //wave
                //bool wavetestflag = true;
                //if (wavetestflag == true)
                //{
                //    testcase(checkWave(null), 4, false);
                //    testcase(checkWave("~muchapprici"), 5, true);
                //    testcase(checkWave("muchappri~"), 6, false);
                //    testcase(checkWave("~"), 8, true);
                //    testcase(checkWave("g"), 9, true);
                    
                //}

                //testcaseString(ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, "fi983uu9ashwaoeupraw"), 10, null);
                //testcaseString(ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, "fo316ec0-057f-4931-9a28-23afa1695a28"), 11, "fo02dd31-c6ea-4135-a6c5-30d811ace862");
                //testcaseString(ShareFileV3Sample.ShareFileV3Sample.getParentofChild(connectionToken, null), 12, null);

                //testcase( checkFolderCode("fo293u2932ih9as"),13, true);
                //testcase(checkFolderCode(""), 14, false); 
                //testcase(checkFolderCode(null), 15, false);
                //testcase(checkFolderCode("fi293uaohshdaweh"), 16, false);
              


                return true;
            }
            else
            {
                //console.WriteLine("Authentication Failure, Abort");
                //might as well terminate, nothing can be done without the token
                return false;
                

            }
        }

        public static void testcaseString(String s, int num, String expectedAnswer)
        {

            if (String.IsNullOrEmpty(s))
            {
                if (s == expectedAnswer)
                {
                    //console.WriteLine("testcase " + num + " SUCCEEDED " + s + " expected answer: " + expectedAnswer);
                    return;
                }
                else
                {
                    //console.WriteLine("testcase " + num + " FAILED " + s + " expected answer: " + expectedAnswer);
                    return;
                }
            }

            if (s.Equals(expectedAnswer))
            {
                //console.WriteLine("testcase " + num + " SUCCEEDED " + s + " expected answer: " + expectedAnswer);
                return;
            }
            else
            {
                //console.WriteLine("testcase " + num + " FAILED " + s + " expected answer: " + expectedAnswer);
                return;
            }
        }

        public static void testcase(bool b, int num, bool expectedAnswer)
        {
            
            if (b == expectedAnswer)
            {
                //console.WriteLine("testcase " + num + " SUCCEEDED " + b + " expected answer: " + expectedAnswer);
            }
            else
            {
                //console.WriteLine("testcase " + num + " FAILED " + b + " expected answer: " + expectedAnswer);
            }
        }
     
        //if filename has .tmp tail, then return true, else return false
        public static bool checkTmp(String filename){

            if (String.IsNullOrEmpty(filename) == true && String.IsNullOrWhiteSpace(filename) == true)
                return false;

            if (filename.Length < 4)
            {
                //if filename has a length of less than 4, then it doesn't have .tmp tail
                return false;
            }


            //string length
            
            int position = filename.IndexOf(".tmp");

            if (position == filename.Length - 4)
            {
                return true;
            }


            return false;
        }

        //if filename has ~ head, then return true, else return false
        public static bool checkWave(String filename)
        {
            if (String.IsNullOrEmpty(filename) == true && String.IsNullOrWhiteSpace(filename) == true)
                return false;


            ////console.WriteLine(filename.Length);
            ////console.WriteLine(filename.IndexOf("~"));
            int position = filename.IndexOf("~");
            if (position == 0)
            {
                return true;
            }


            return false;
        }

        static void Main(string[] args)
        {
           
            if (authen() == false)
            {
                System.Environment.Exit(1);
            }
            //console.WriteLine();
            //console.WriteLine("Authentication Success, Proceed");



            //thread and thread spawn
            DirWatcher.Watcher newWatcher = new DirWatcher.Watcher();

            //runner thread
            ThreadStart generatorThread = new ThreadStart(newWatcher.Run);
            Thread mainThread = new Thread(generatorThread);
            
            mainThread.Start();

            //waker thread, waits when there is work
            ThreadStart wakeThread = new ThreadStart(waker);
            Thread wakerThread = new Thread(wakeThread);
            wakerThread.Start();
            

            //worker thread, waits when there is no more work
            ThreadStart workThread = new ThreadStart(Entry.worker);
            Thread workerThread = new Thread(workThread);
            workerThread.Start(); 
        }

    }
}

