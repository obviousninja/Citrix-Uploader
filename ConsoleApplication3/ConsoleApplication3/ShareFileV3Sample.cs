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
using ShareFile.Api.Client.Extensions;

namespace ShareFileV3Sample
{
    class OAuth2Token
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public string Appcp { get; set; }
        public string Apicp { get; set; }
        public string Subdomain { get; set; }
        public int ExpiresIn { get; set; }

        public OAuth2Token(JObject json)
        {
            if (json != null)
            {
                AccessToken = (string)json["access_token"];
                RefreshToken = (string)json["refresh_token"];
                TokenType = (string)json["token_type"];
                Appcp = (string)json["appcp"];
                Apicp = (string)json["apicp"];
                Subdomain = (string)json["subdomain"];
                ExpiresIn = (int)json["expires_in"];
            }
            else
            {
                AccessToken = "";
                RefreshToken = "";
                TokenType = "";
                Appcp = "";
                Apicp = "";
                Subdomain = "";
                ExpiresIn = 0;
            }
        }
    }

    class ShareFileV3Sample
    {

        /// <summary>
        /// Authenticate via username/password
        /// </summary>
        /// <param name="hostname">hostname like "myaccount.sharefile.com"</param>
        /// <param name="clientId">my client id</param>
        /// <param name="clientSecret">my client secret</param>
        /// <param name="username">my@user.name</param>
        /// <param name="password">mypassword</param>
        /// <returns></returns>
        public static OAuth2Token Authenticate(string hostname, string clientId, string clientSecret, string username, string password)
        {
            String uri = string.Format("https://{0}/oauth/token", hostname);
            //console.WriteLine(uri);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("grant_type", "password");
            parameters.Add("client_id", clientId);
            parameters.Add("client_secret", clientSecret);
            parameters.Add("username", username);
            parameters.Add("password", password);

            ArrayList bodyParameters = new ArrayList();
            foreach (KeyValuePair<string, string> kv in parameters)
            {
                bodyParameters.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(kv.Key), HttpUtility.UrlEncode(kv.Value.ToString())));
            }
            string requestBody = String.Join("&", bodyParameters.ToArray());

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(requestBody);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            
            JObject token = null;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();
                token = JObject.Parse(body);
            }

            return new OAuth2Token(token);
        }

        public static void addAuthorizationHeader(HttpWebRequest request, OAuth2Token token)
        {
            request.Headers.Add(string.Format("Authorization: Bearer {0}", token.AccessToken));
            
        }

        public static string GetHostname(OAuth2Token token)
        {
 //           //console.WriteLine("SubDomain is: " + token.Subdomain);
            return string.Format("{0}.sf-api.com", token.Subdomain);
        }

//returns the filenameid if item name provided is within this folder, otherwise return null
//parameter: token, folderid, file name, stringbuilder: answer, optional: getchildren, default is true to compare to against the entire folder's content
 //stringbuilder answer is used to retrieve the correct fileid
//Solution ONLY assumes that there is one distinct file with the given filename, if there are multiple files, then the fileid returned would be random
        public static String searchExpandFolderById(OAuth2Token token, String folderId, String fileName, StringBuilder answer, bool getChildren = true)
        {
            //foeefacf-7f53-48ff-a5fd-29adcd016419  
            if (token == null || folderId == null || fileName == null)
            {
                //console.WriteLine("returned false on head");
                return null;
            }


            String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), folderId);
            if (getChildren)
            {
                uri += "?$expand=Children";
            }
            ////console.WriteLine(uri);
 
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            //console.WriteLine("current file name: " + fileName);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();

                JObject root = JObject.Parse(body);

                // just print Id, CreationDate, Name of each element
                //console.WriteLine("ROOT ELEMENT" + root["Id"] + " " + root["CreationDate"] + " " + root["Name"]);
                JArray children = (JArray)root["Children"];
                if (children != null)
                {
                    foreach (JObject child in children)
                    {
                        
                        //if it is a file and filename matches, then check 
                        if (isCloudFile(child["Id"].ToString()) == true)
                        {
                            //if the childname is the filename, then true, otherwise continue
                            if (child["Name"].ToString().Equals(fileName))
                            {
                                //console.WriteLine("Match Found! {0} belongs to {1}!", child["Id"].ToString(), child["Name"].ToString());

                                //return child["Id"].ToString();
                                answer.Append(child["Id"].ToString());
                                //console.WriteLine("Match Found! answer is now {0}!", answer);

                                return answer.ToString() ;
                            
                            }
                        }
                        else if (isCloudFolder(child["Id"].ToString()) == true)
                        {
                            //console.WriteLine("child id is a Folder: ");

                            if (child["Name"].ToString().Equals(fileName) == true)
                            {
                                //if folder name is the filename we are looking for then return its id

                                //console.WriteLine("Match Found! {0} belongs to {1}!", child["Id"].ToString(), child["Name"].ToString());

                                //return child["Id"].ToString();
                                answer.Append(child["Id"].ToString());
                                //console.WriteLine("Match Found! answer is now {0}!", answer);

                                return answer.ToString();
                            }
                            else
                            {
                                //if the foldername is not the filename we are looking for then call the function recursively on it to return an itemid
                                searchExpandFolderById(token, child["Id"].ToString(), fileName, answer, true);

                            }
                            

                        }
                        else
                        {
                            //not a file and not a directory
                            //not likely, so we do nothing
                            //console.WriteLine("missed casesssssss");
                            
                        }

                        //if it is a directory, then check if it matches the filename

                      
                        //console.WriteLine(child["Id"] + " " + child["CreationDate"] + " " + child["Name"]);
                    }
                }
            }
            //console.WriteLine("very very last");
            return null;
        }

        //basic pattern fixxxxxxxxxx
        //return true if a itemid is a cloudfile
        public static bool isCloudFile(String id)
        {
            if (id == null)
                return false;

            int boolFlag = -1;

            boolFlag = id.IndexOf("fi");
            ////console.WriteLine("{0} and indexed at {1}", id, boolFlag);
            if (boolFlag == 0)
            {
                return true;
            }

            return false;
        }

        //basic pattern foxxxxxxxxxx
        //return true if an itemid is a cloudfolder
        public static bool isCloudFolder(String id)
        {
            if (id == null)
                return false;


            int boolFlag = -1;

            boolFlag = id.IndexOf("fo");
            ////console.WriteLine("{0} and indexed at {1}", id, boolFlag);
            if (boolFlag == 0)
            {
                return true;
            }
            return false;
        }

        //return parentid given child id, if itemid is null, then return null, if token is null then return null
        //if item is not found, then return null, otherwise it will return an itemid
        public static String getParentofChild(OAuth2Token token, String itemId)
        {
            if (token == null)
                return null;

            if (String.IsNullOrWhiteSpace(itemId) || String.IsNullOrEmpty(itemId))
            {
                return null;
            }
            
            String uri = string.Format("https://{0}/sf/v3/Items({1})/Parent", ShareFileV3Sample.GetHostname(token), itemId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //console.WriteLine(response.StatusCode);
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    String body = reader.ReadToEnd();

                    JObject item = JObject.Parse(body);
                    return item["Id"].ToString();
                    ////console.WriteLine(item["Id"] + " " + item["CreationDate"] + " " + item["Name"]);
                }
            }
            catch (Exception web)
            {
                //item is not found
                return null;
            }

         

           // return null;
        }

        /// <summary>
        /// Get the root level Item for the provided user. To retrieve Children the $expand=Children
        /// parameter can be added.
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="getChildren">retrieve Children Items if true, default is false</param>
        public static void GetRoot(OAuth2Token token, bool getChildren = false)
        {
            String uri = string.Format("https://{0}/sf/v3/Items", ShareFileV3Sample.GetHostname(token));
            if (getChildren)
            {
                uri += "?$expand=Children";
            }
            //console.WriteLine(uri);
            //console.WriteLine("Beginning death and destruction!");
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();

                JObject root = JObject.Parse(body);

                // just print Id, CreationDate, Name of each element
                //console.WriteLine(root["Id"] + " " + root["CreationDate"] + " " + root["Name"]);
                JArray children = (JArray)root["Children"];
                if (children != null)
                {
                    foreach (JObject child in children)
                    {
                        
                        //console.WriteLine(child["Id"] + " " + child["CreationDate"] + " " + child["Name"]);
                    }
                }
            }
        }

        /// <summary>
        /// Get a single Item by Id.
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="id">an item id</param>
        public static void GetItemById(OAuth2Token token, string id)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), id);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();

                JObject item = JObject.Parse(body);
                //console.WriteLine(item["Id"] + " " + item["CreationDate"] + " " + item["Name"]);
            }
        }

        /// <summary>
        /// Get a folder using some of the common query parameters that are available. This will
        /// add the expand, select parameters. The following are used:
        /// expand=Children to get any Children of the folder
        /// select=Id,Name,Children/Id,Children/Name,Children/CreationDate to get the Id, Name of the folder and the Id, Name, CreationDate of any Children
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="id">a folder id</param>
        public static void GetFolderWithQueryParameters(OAuth2Token token, string id)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})?$expand=Children&$select=Id,Name,Children/Id,Children/Name,Children/CreationDate", ShareFileV3Sample.GetHostname(token), id);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();

                JObject folder = JObject.Parse(body);
                // only Id and Name are available because we specifically selected only those two Properties
                ////console.WriteLine(folder["Id"] + " " + folder["Name"]);
                JArray children = (JArray)folder["Children"];
                if (children != null)
                {
                    foreach (JObject child in children)
                    {
                        // CreationDate is also available on Children because we specifically selected that property in addition to Id, Name
                        //console.WriteLine(child["Id"] + " " + child["CreationDate"] + " " + child["Name"]);
                    }
                }
            }
        }

        /// <summary>
        /// Create a new folder in the given parent folder. Returns the FileId of created Folder
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="parentId">the parent folder in which to create the new folder</param>
        /// <param name="name">the folder name</param>
        /// <param name="description">the folder description</param>
        public static String CreateFolder(OAuth2Token token, string parentId, string name, string description)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})/Folder", ShareFileV3Sample.GetHostname(token), parentId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            Dictionary<string, object> folder = new Dictionary<string, object>();
            folder.Add("Name", name);
            folder.Add("Description", description);
            string json = JsonConvert.SerializeObject(folder);

            //console.WriteLine(json);

            request.Method = "POST";
            request.ContentType = "application/json";
            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(json);
            }
            try
            {
                //try to assert create folder action and get a response

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //console.WriteLine(response.StatusCode);
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    String body = reader.ReadToEnd();
                    JObject newFolder = JObject.Parse(body);

                    return newFolder["Id"].ToString();
                }
            }
            catch (Exception e)
            {
                //folder already exist, hence return null
                return null;
            }
        
            
          
            
        }

        /// <summary>
        /// Update the name and description of an Item.
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="itemId">the id of the item to update</param>
        /// <param name="name">the item name</param>
        /// <param name="description">the item description</param>
        public static void UpdateItem(OAuth2Token token, string itemId, string name, string description)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), itemId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            Dictionary<string, object> folder = new Dictionary<string, object>();
            folder.Add("Name", name);
            folder.Add("Description", description);
            string json = JsonConvert.SerializeObject(folder);

            //console.WriteLine(json);

            request.Method = "PATCH";
            request.ContentType = "application/json";
            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(json);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();
                JObject newFolder = JObject.Parse(body);
                //console.WriteLine("Updated Folder: " + newFolder["Id"]);
            }
        }

        /// <summary>
        /// Delete an Item by Id.
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="itemId">the id of the item to delete</param>
        public static void DeleteItem(OAuth2Token token, string itemId)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), itemId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            request.Method = "DELETE";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine(response.StatusCode);
        }

        /// <summary>
        /// Downloads a single Item. 
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="itemId">the id of the item to download</param>
        /// <param name="localPath">where to download the item to, like "c:\\path\\to\\the.file". If downloading a folder the localPath name should end in .zip.</param>
        public static void DownloadItem(OAuth2Token token, string itemId, string localPath)
        {
            String uri = string.Format("https://{0}/sf/v3/Items({1})/Download", ShareFileV3Sample.GetHostname(token), itemId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);
            request.AllowAutoRedirect = true;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var source = new BufferedStream(response.GetResponseStream()))
            {
                using (var target = new FileStream(localPath, FileMode.Create))
                {
                    int chunkSize = 1024 * 8;
                    byte[] chunk = new byte[chunkSize];
                    int len = 0;
                    while ((len = source.Read(chunk, 0, chunkSize)) > 0)
                    {
                        target.Write(chunk, 0, len);
                    }
                    //console.WriteLine("Download complete");
                }
            }
            //console.WriteLine(response.StatusCode);
        }

        //wraper method that allows the upload of its original folder
        public static void UploadFolderWraper(OAuth2Token token, String parentId, String localPathRoot)
        {
            DirectoryInfo di;
            di = new DirectoryInfo(localPathRoot);
            //console.WriteLine(di.Name);
            //console.WriteLine(di.FullName);
            UploadFolder(token, CreateFolder(token, parentId, di.Name, "Citrix Uploader"), di.FullName);

        }

        //upload a folder using standard upload method
        //breath first search style
        //go into a folder, then read all files, upload as files come along
        //put folders in a queue and repeat the same process
        public static void UploadFolder(OAuth2Token token, String parentId, String localPathRoot)
        {

            //Queue for breathfirst traversal
            //Queue<String> aqueue = new Queue<String>();

            //push the first element into the queue
            //aqueue.Enqueue(localPathRoot);

            //to be used variables
            try
            {
                DirectoryInfo di;

                di = new DirectoryInfo(localPathRoot);

                foreach (var fi in di.GetFiles())
                {
                    //console.WriteLine(fi.Name);
                    UploadFile(token, parentId, fi.FullName);
                }

                // Display the names of the directories. 
                foreach (DirectoryInfo dri in di.GetDirectories())
                {
                    //console.WriteLine(dri.Name);
                    //console.WriteLine(dri.FullName);
                    UploadFolder(token, CreateFolder(token, parentId, dri.Name, "Citrix Uploader"), dri.FullName);
                }
            }
            catch (Exception e)
            {
                //give up
            }
           

        }

        //given complete path, get the local parent folder name of the path
        //parameter, String LocalPath
        public static String getLocalParentFolder(String localPath)
        {
            if (String.IsNullOrEmpty(localPath) || String.IsNullOrWhiteSpace(localPath))
            {
                return null;
            }

            ////console.WriteLine(localPath.LastIndexOf("\\"));
            StringBuilder newString = new StringBuilder();

            for (int i = 0; i < localPath.LastIndexOf("\\"); i++)
            {
                newString.Append(localPath[i]);
            }
            ////console.WriteLine("new String with one slice: " + newString.ToString());

            //second slice
            String retString = newString.ToString().Substring(newString.ToString().LastIndexOf("\\")+1);
            ////console.WriteLine("ret string with one slice: " + retString);
                return retString;
        }

        /// <summary>
        /// Uploads a File using the Standard upload method with a multipart/form mime encoded POST.
        /// </summary>
        /// <param name="token">the OAuth2Token returned from Authenticate</param>
        /// <param name="parentId">where to upload the file</param>
        /// <param name="localPath">the full path of the file to upload, like "c:\\path\\to\\file.name"</param>
        public static void UploadFile(OAuth2Token token, string parentId, string localPath)
        {
            while (IsFileLocked(new FileInfo(localPath)) == true)
            {
                //file is currently locked, so we must wait for it to unlock
                //console.WriteLine(localPath + "FIle is Currently Blocked");

                //passing the existing file descriptor to fwakehandle
                Entry.Entry.fWakeHandle = localPath;

                //let the thread rest
                Entry.Entry.singleEntranceEvent.WaitOne();
            }

            String uri = string.Format("https://{0}/sf/v3/Items({1})/Upload", ShareFileV3Sample.GetHostname(token), parentId);
            //console.WriteLine(uri);

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            ShareFileV3Sample.addAuthorizationHeader(request, token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                String body = reader.ReadToEnd();

                JObject uploadConfig = JObject.Parse(body);
                string chunkUri = (string)uploadConfig["ChunkUri"];
                if (chunkUri != null)
                {
                    //console.WriteLine("Starting Upload");
                    //File
                    UploadMultiPartFile("File1", new FileInfo(localPath), chunkUri);
                }
            }
        }
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        /// <summary>
        /// Does a multipart form post upload of a file to a url.
        /// </summary>
        /// <param name="parameterName">multipart parameter name. File1 for a standard upload.</param>
        /// <param name="file">the FileInfo to upload</param>
        /// <param name="uploadUrl">the url of the server to upload to</param>
        public static void UploadMultiPartFile(string parameterName, FileInfo file, string uploadUrl)
        {
            string boundaryGuid = "upload-" + Guid.NewGuid().ToString("n");
            string contentType = "multipart/form-data; boundary=" + boundaryGuid;

            MemoryStream ms = new MemoryStream();
            byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "\r\n");

            // Write MIME header
            ms.Write(boundaryBytes, 2, boundaryBytes.Length - 2);
            string header = String.Format(@"Content-Disposition: form-data; name=""{0}""; filename=""{1}""" +
                "\r\nContent-Type: application/octet-stream\r\n\r\n", parameterName, file.Name);
            byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
            ms.Write(headerBytes, 0, headerBytes.Length);

            while (IsFileLocked(file) == true)
            {
                //file is currently locked, so we must wait for it to unlock
                //console.WriteLine(file.FullName + "File is Currently Blocked");

                //passing the existing file descriptor to fwakehandle
                Entry.Entry.fWakeHandle = file.FullName;

                //let the thread rest
                Entry.Entry.singleEntranceEvent.WaitOne();
            }

            // Load the file into the byte array
            using (FileStream source = file.OpenRead())
            {
                byte[] buffer = new byte[1024 * 1024];
                int bytesRead;

                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }
            }

            // Write MIME footer
            boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "--\r\n");
            ms.Write(boundaryBytes, 0, boundaryBytes.Length);

            byte[] postBytes = ms.ToArray();
            ms.Close();

            HttpWebRequest request = WebRequest.CreateHttp(uploadUrl);
            request.Timeout = 1000 * 60; // 60 seconds
            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = postBytes.Length;
            request.Credentials = CredentialCache.DefaultCredentials;

            using (Stream postStream = request.GetRequestStream())
            {
                int chunkSize = 48 * 1024;
                int remaining = postBytes.Length;
                int offset = 0;

                do
                {
                    if (chunkSize > remaining) { chunkSize = remaining; }
                    postStream.Write(postBytes, offset, chunkSize);

                    remaining -= chunkSize;
                    offset += chunkSize;

                } while (remaining > 0);

                postStream.Close();
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //console.WriteLine("Upload Status: " + response.StatusCode);
            response.Close();
        }

    }
}

























