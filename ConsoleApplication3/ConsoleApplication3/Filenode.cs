using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStore
{
    class Filenode
    {
        private String action;  //always exist, never null. if we assume user aren't malicious
        private String oldPath;  //null if it doesn't exist
        private String newPath; //currentPath, never null, if we assume user aren't malicious
        private String cloudDestinationToRecieve; //indetermined

        public Filenode()
        {
            action = null;
            oldPath = null;
            newPath = null;
            cloudDestinationToRecieve = null;
        }
       public Filenode(String opAction, String oldUploadPath, String newUploadPath, String recievePath){

        action = opAction;
		oldPath = oldUploadPath;
        newPath = newUploadPath;
		cloudDestinationToRecieve = recievePath;
	
       }
       public String toString(){
           return "Action: " + action + " OldPath: " + oldPath + " newPath: " + newPath + " CloudDest: " + cloudDestinationToRecieve; 
       }
       //getter
       public String getAction()
       {
           return action;
       }
       public String getNewPath()
       {
           return newPath; 
        }       
       public String getOldPath()
       {
           return oldPath;
       }
       public String getcloudDestinationPath()
       {
           return cloudDestinationToRecieve;
       }

       //setter
       public bool setAction(String actionToTake)
       {
           if (actionToTake == null)
               return false;

           action = actionToTake;
           return true;
       }

       public bool setOldPath(String newPath)
       {

           if (newPath == null)
               return false;

           oldPath = newPath;
           return true;
       }
       public bool setNewPath(String newPath)
       {
           if (newPath == null)
               return false;

           this.newPath = newPath;
           return true;
       }
       public bool setCloudDestinationPath(String newPath)
       {
           if (newPath == null)
               return false;

           cloudDestinationToRecieve = newPath;
           return true;
       }
    }
}
