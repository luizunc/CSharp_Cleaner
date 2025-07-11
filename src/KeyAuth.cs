﻿using System;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Windows;

namespace KeyAuth
{
    public class api
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        // Import the required Atom Table functions from kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ushort GlobalAddAtom(string lpString);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ushort GlobalFindAtom(string lpString);

        public string name, ownerid, secret, version, path, seed;
        /// <summary>
        /// Set up your application credentials in order to use keyauth
        /// </summary>
        /// <param name="name">Application Name</param>
        /// <param name="ownerid">Your OwnerID, found in your account settings.</param>
        /// <param name="secret">Application Secret</param>
        /// <param name="version">Application Version, if version doesnt match it will open the download link you set up in your application settings and close the app, if empty the app will close</param>
        public api(string name, string ownerid, string secret, string version, string path = null)
        {
            if (ownerid.Length != 10)
            {
                Process.Start("https://youtube.com/watch?v=RfDTdiBq4_o");
                Process.Start("https://keyauth.cc/app/");
                Thread.Sleep(2000);
                error("Application not setup correctly. Please watch the YouTube video for setup.");
                return;
            }

            this.name = name;
            this.ownerid = ownerid;
            this.secret = secret;
            this.version = version;
            this.path = path;
        }

        #region structures
        [DataContract]
        private class response_structure
        {
            [DataMember]
            public bool success { get; set; }

            [DataMember]
            public bool newSession { get; set; }

            [DataMember]
            public string sessionid { get; set; }

            [DataMember]
            public string contents { get; set; }

            [DataMember]
            public string response { get; set; }

            [DataMember]
            public string message { get; set; }

            [DataMember]
            public string ownerid { get; set; }

            [DataMember]
            public string download { get; set; }

            [DataMember(IsRequired = false, EmitDefaultValue = false)]
            public user_data_structure info { get; set; }

            [DataMember(IsRequired = false, EmitDefaultValue = false)]
            public app_data_structure appinfo { get; set; }

            [DataMember]
            public List<msg> messages { get; set; }

            [DataMember]
            public List<users> users { get; set; }

            [DataMember(Name = "2fa", IsRequired = false, EmitDefaultValue = false)] // Ensure mapping to "2fa"
            public TwoFactorData twoFactor { get; set; } // Add a property for the 2FA data
        }

        public class msg
        {
            public string message { get; set; }
            public string author { get; set; }
            public string timestamp { get; set; }
        }

        public class users
        {
            public string credential { get; set; }
        }

        [DataContract]
        private class user_data_structure
        {
            [DataMember]
            public string username { get; set; }

            [DataMember]
            public string ip { get; set; }
            [DataMember]
            public string hwid { get; set; }
            [DataMember]
            public string createdate { get; set; }
            [DataMember]
            public string lastlogin { get; set; }
            [DataMember]
            public List<Data> subscriptions { get; set; } // array of subscriptions (basically multiple user ranks for user with individual expiry dates
        }

        [DataContract]
        private class app_data_structure
        {
            [DataMember]
            public string numUsers { get; set; }
            [DataMember]
            public string numOnlineUsers { get; set; }
            [DataMember]
            public string numKeys { get; set; }
            [DataMember]
            public string version { get; set; }
            [DataMember]
            public string customerPanelLink { get; set; }
            [DataMember]
            public string downloadLink { get; set; }
        }
        #endregion
        private static string sessionid;
        private static readonly string encryptionKey = GenerateSecureKey();
        bool initialized;
        /// <summary>
        /// Initializes the connection with keyauth in order to use any of the functions
        /// </summary>
        public async Task init()
        {
            Random random = new Random();

            // Generate a random length for the string (let's assume between 5 and 50 characters)
            int length = random.Next(5, 51); // Min length: 5, Max length: 50

            StringBuilder sb = new StringBuilder(length);

            // Define the range of printable ASCII characters (32-126)
            for (int i = 0; i < length; i++)
            {
                // Generate a random printable ASCII character
                char randomChar = (char)random.Next(32, 127); // ASCII 32 to 126
                sb.Append(randomChar);
            }

            seed = sb.ToString();
            checkAtom();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "init",
                ["ver"] = version,
                ["hash"] = checksum(Process.GetCurrentProcess().MainModule.FileName),
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            if (!string.IsNullOrEmpty(path))
            {
                values_to_upload.Add("token", File.ReadAllText(path));
                values_to_upload.Add("thash", TokenHash(path));
            }

            var response = await req(values_to_upload);

            if (response == "KeyAuth_Invalid")
            {
                error("Application not found");
                return;
            }

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
                if (json.success)
                {
                    sessionid = json.sessionid;
                    initialized = true;
                }
                else if (json.message == "invalidver")
                {
                    app_data.downloadLink = json.download;
                }
            }
        }

#pragma warning disable IDE0052
        private System.Threading.Timer atomTimer;
#pragma warning restore IDE0052
        void checkAtom()
        {
            atomTimer = new System.Threading.Timer(_ =>
            {
                ushort foundAtom = GlobalFindAtom(seed);
                if (foundAtom == 0)
                {
                    return;
                }
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public static string TokenHash(string tokenPath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var s = File.OpenRead(tokenPath))
                {
                    byte[] bytes = sha256.ComputeHash(s);
                    return BitConverter.ToString(bytes).Replace("-", string.Empty);
                }
            }
        }
        /// <summary>
        /// Checks if Keyauth is been Initalized
        /// </summary>
        public void CheckInit()
        {
            if (!initialized)
            {
                error("You must run the function KeyAuthApp.init(); first");
                return;
            }
        }

        public string expirydaysleft()
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(long.Parse(user_data.subscriptions[0].expiry)).ToLocalTime();
            TimeSpan difference = dtDateTime - DateTime.Now;
            return Convert.ToString(difference.Days + " Days " + difference.Hours + " Hours Left");
        }

        public static DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            try
            {
                dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            }
            catch
            {
                dtDateTime = DateTime.MaxValue;
            }
            return dtDateTime;
        }

        /// <summary>
        /// Registers the user using a license and gives the user a subscription that matches their license level
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="pass">Password</param>
        /// <param name="key">License key</param>
        public async Task register(string username, string pass, string key, string email = "")
        {
            CheckInit();

            string hwid = WindowsIdentity.GetCurrent().User.Value;

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "register",
                ["username"] = username,
                ["pass"] = pass,
                ["key"] = key,
                ["email"] = email,
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                GlobalAddAtom(seed);
                GlobalAddAtom(ownerid);

                load_response_struct(json);
                if (json.success)
                    load_user_data(json.info);
            }
        }
        /// <summary>
        /// Allow users to enter their account information and recieve an email to reset their password.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="email">Email address</param>
        public async Task forgot(string username, string email)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "forgot",
                ["username"] = username,
                ["email"] = email,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
        }
        /// <summary>
        /// Authenticates the user using their username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="pass">Password</param>
        public async Task login(string username, string pass, string code = null)
        {
            CheckInit();

            string hwid = WindowsIdentity.GetCurrent().User.Value;

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "login",
                ["username"] = username,
                ["pass"] = pass,
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid,
                ["code"] = code ?? null
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                GlobalAddAtom(seed);
                GlobalAddAtom(ownerid);

                load_response_struct(json);
                if (json.success)
                    load_user_data(json.info);
            }
        }

        public async Task logout()
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "logout",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
            }
        }

        public async Task web_login()
        {
            CheckInit();

            string hwid = WindowsIdentity.GetCurrent().User.Value;

            string datastore, datastore2, outputten;

        start:

            HttpListener listener = new HttpListener();

            outputten = "handshake";
            outputten = "http://localhost:1337/" + outputten + "/";

            listener.Prefixes.Add(outputten);

            listener.Start();

            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse responsepp = context.Response;

            responsepp.AddHeader("Access-Control-Allow-Methods", "GET, POST");
            responsepp.AddHeader("Access-Control-Allow-Origin", "*");
            responsepp.AddHeader("Via", "hugzho's big brain");
            responsepp.AddHeader("Location", "your kernel ;)");
            responsepp.AddHeader("Retry-After", "never lmao");
            responsepp.Headers.Add("Server", "\r\n\r\n");

            if (request.HttpMethod == "OPTIONS")
            {
                responsepp.StatusCode = (int)HttpStatusCode.OK;
                Thread.Sleep(1); // without this, the response doesn't return to the website, and the web buttons can't be shown
                listener.Stop();
                goto start;
            }

            listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;
            listener.UnsafeConnectionNtlmAuthentication = true;
            listener.IgnoreWriteExceptions = true;

            string data = request.RawUrl;

            datastore2 = data.Replace("/handshake?user=", "");
            datastore2 = datastore2.Replace("&token=", " ");

            datastore = datastore2;

            string user = datastore.Split()[0];
            string token = datastore.Split(' ')[1];

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "login",
                ["username"] = user,
                ["token"] = token,
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            bool success = true;
            if (json.ownerid == ownerid)
            {
                GlobalAddAtom(seed);
                GlobalAddAtom(ownerid);

                load_response_struct(json);

                if (json.success)
                {
                    load_user_data(json.info);

                    responsepp.StatusCode = 420;
                    responsepp.StatusDescription = "SHEESH";
                }
                else
                {
                    Console.WriteLine(json.message);
                    responsepp.StatusCode = (int)HttpStatusCode.OK;
                    responsepp.StatusDescription = json.message;
                    success = false;
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes("Complete");

            responsepp.ContentLength64 = buffer.Length;
            Stream output = responsepp.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            Thread.Sleep(1); // without this, the response doesn't return to the website, and the web buttons can't be shown
            listener.Stop();

            if (!success)
                return;
        }

        /// <summary>
        /// Use Buttons from KeyAuth Customer Panel
        /// </summary>
        /// <param name="button">Button Name</param>

        public void button(string button)
        {
            CheckInit();

            HttpListener listener = new HttpListener();

            string output;

            output = button;
            output = "http://localhost:1337/" + output + "/";

            listener.Prefixes.Add(output);

            listener.Start();

            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse responsepp = context.Response;

            responsepp.AddHeader("Access-Control-Allow-Methods", "GET, POST");
            responsepp.AddHeader("Access-Control-Allow-Origin", "*");
            responsepp.AddHeader("Via", "hugzho's big brain");
            responsepp.AddHeader("Location", "your kernel ;)");
            responsepp.AddHeader("Retry-After", "never lmao");
            responsepp.Headers.Add("Server", "\r\n\r\n");

            responsepp.StatusCode = 420;
            responsepp.StatusDescription = "SHEESH";

            listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;
            listener.UnsafeConnectionNtlmAuthentication = true;
            listener.IgnoreWriteExceptions = true;

            listener.Stop();
        }

        /// <summary>
        /// Gives the user a subscription that has the same level as the key
        /// </summary>
        /// <param name="username">Username of the user thats going to get upgraded</param>
        /// <param name="key">License with the same level as the subscription you want to give the user</param>
        public async Task upgrade(string username, string key)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "upgrade",
                ["username"] = username,
                ["key"] = key,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                json.success = false;
                load_response_struct(json);
            }
        }

        /// <summary>
        /// Authenticate without using usernames and passwords
        /// </summary>
        /// <param name="key">Licence used to login with</param>
        public async Task license(string key, string code = null)
        {
            CheckInit();

            string hwid = WindowsIdentity.GetCurrent().User.Value;

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "license",
                ["key"] = key,
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid,
                ["code"] = code ?? null
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);

            if (json.ownerid == ownerid)
            {
                GlobalAddAtom(seed);
                GlobalAddAtom(ownerid);

                load_response_struct(json);
                if (json.success)
                    load_user_data(json.info);
            }
        }
        /// <summary>
        /// Checks if the current session is validated or not
        /// </summary>
        public async Task check()
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "check",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
            }
        }
        /// <summary>
        /// Disable two factor authentication (2fa)
        /// </summary>
        public async Task disable2fa(string code)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "2fadisable",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid,
                ["code"] = code
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
        }
        /// <summary>
        /// Enable two factor authentication (2fa)
        /// </summary>
        public async Task enable2fa(string code = null)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "2faenable",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid,
                ["code"] = code
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
        }
        /// <summary>
        /// Change the data of an existing user variable, *User must be logged in*
        /// </summary>
        /// <param name="var">User variable name</param>
        /// <param name="data">The content of the variable</param>
        public async Task setvar(string var, string data)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "setvar",
                ["var"] = var,
                ["data"] = data,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
            }
        }
        /// <summary>
        /// Gets the an existing user variable
        /// </summary>
        /// <param name="var">User Variable Name</param>
        /// <returns>The content of the user variable</returns>
        public async Task<string> getvar(string var)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "getvar",
                ["var"] = var,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
                if (json.success)
                    return json.response;
            }
            return null;
        }
        /// <summary>
        /// Bans the current logged in user
        /// </summary>
        public async Task ban(string reason = null)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "ban",
                ["reason"] = reason,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
            }
        }
        /// <summary>
        /// Gets an existing global variable
        /// </summary>
        /// <param name="varid">Variable ID</param>
        /// <returns>The content of the variable</returns>
        public async Task<string> var(string varid)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "var",
                ["varid"] = varid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
                if (json.success)
                    return json.message;
            }
            return null;
        }
        /// <summary>
        /// Fetch usernames of online users
        /// </summary>
        /// <returns>ArrayList of usernames</returns>
        public async Task<List<users>> fetchOnline()
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "fetchOnline",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);

            if (json.success)
                return json.users;
            return null;
        }
        /// <summary>
        /// Fetch app statistic counts
        /// </summary>
        public async Task fetchStats()
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "fetchStats",
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);

            if (json.success)
                load_app_data(json.appinfo);
        }
        /// <summary>
        /// Gets the last 50 sent messages of that channel
        /// </summary>
        /// <param name="channelname">The channel name</param>
        /// <returns>the last 50 sent messages of that channel</returns>
        public async Task<List<msg>> chatget(string channelname)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "chatget",
                ["channel"] = channelname,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
            if (json.success)
            {
                return json.messages;
            }
            return null;
        }
        /// <summary>
        /// Sends a message to the given channel name
        /// </summary>
        /// <param name="msg">Message</param>
        /// <param name="channelname">Channel Name</param>
        /// <returns>If the message was sent successfully, it returns true if not false</returns>
        public async Task<bool> chatsend(string msg, string channelname)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "chatsend",
                ["message"] = msg,
                ["channel"] = channelname,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
            if (json.success)
                return true;
            return false;
        }
        /// <summary>
        /// Checks if the current ip address/hwid is blacklisted
        /// </summary>
        /// <returns>If found blacklisted returns true if not false</returns>
        public async Task<bool> checkblack()
        {
            CheckInit();
            string hwid = WindowsIdentity.GetCurrent().User.Value;

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "checkblacklist",
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
                if (json.success)
                    return true;
                else
                    return false;
            }
            return true; // return yes blacklisted if the OwnerID is spoofed
        }
        /// <summary>
        /// Sends a request to a webhook that you've added in the dashboard in a safe way without it being showed for example a http debugger
        /// </summary>
        /// <param name="webid">Webhook ID</param>
        /// <param name="param">Parameters</param>
        /// <param name="body">Body of the request, empty by default</param>
        /// <param name="conttype">Content type, empty by default</param>
        /// <returns>the webhook's response</returns>
        public async Task<string> webhook(string webid, string param, string body = "", string conttype = "")
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "webhook",
                ["webid"] = webid,
                ["params"] = param,
                ["body"] = body,
                ["conttype"] = conttype,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.ownerid == ownerid)
            {
                load_response_struct(json);
                if (json.success)
                    return json.response;
            }
            return null;
        }
        /// <summary>
        /// KeyAuth acts as proxy and downlods the file in a secure way
        /// </summary>
        /// <param name="fileid">File ID</param>
        /// <returns>The bytes of the download file</returns>
        public async Task<byte[]> download(string fileid)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {

                ["type"] = "file",
                ["fileid"] = fileid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
            if (json.success)
                return encryption.str_to_byte_arr(json.contents);
            return null;
        }
        /// <summary>
        /// Logs the IP address,PC Name with a message, if a discord webhook is set up in the app settings, the log will get sent there and the dashboard if not set up it will only be in the dashboard
        /// </summary>
        /// <param name="message">Message</param>
        public async Task log(string message)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "log",
                ["pcuser"] = Environment.UserName,
                ["message"] = message,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            await req(values_to_upload);
        }
        /// <summary>
        /// Change the username of a user, *User must be logged in*
        /// </summary>
        /// <param username="username">New username.</param>
        public async Task changeUsername(string username)
        {
            CheckInit();

            var values_to_upload = new NameValueCollection
            {
                ["type"] = "changeUsername",
                ["newUsername"] = username,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var response = await req(values_to_upload);

            var json = response_decoder.string_to_generic<response_structure>(response);
            load_response_struct(json);
        }

        public static string checksum(string filename)
        {
            string result;
            using (MD5 md = MD5.Create())
            {
                using (FileStream fileStream = File.OpenRead(filename))
                {
                    byte[] value = md.ComputeHash(fileStream);
                    result = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
                }
            }
            return result;
        }

        public static void error(string message)
        {
            string folder = @"Logs", file = Path.Combine(folder, "ErrorLogs.txt");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!File.Exists(file))
            {
                using (FileStream stream = File.Create(file))
                {
                    File.AppendAllText(file, DateTime.Now + " > This is the start of your error logs file");
                }
            }

            File.AppendAllText(file, DateTime.Now + $" > {message}" + Environment.NewLine);

            throw new InvalidOperationException("Erro fatal da KeyAuth: " + message);
        }

        private static async Task<string> req(NameValueCollection post_data)
        {
            try
            {
                if (FileCheck("keyauth.win")) // change this url if you're using a custom domain
                {
                    error("File manipulation detected. Terminating process.");
                    Logger.LogEvent("File manipulation detected.");
                    return "";
                }

                var formData = new List<KeyValuePair<string, string>>();
                foreach (string key in post_data)
                {
                    formData.Add(new KeyValuePair<string, string>(key, post_data[key]));
                }
                var content = new FormUrlEncodedContent(formData);

                var handler = new HttpClientHandler
                {
                    Proxy = null,
                    ServerCertificateCustomValidationCallback = (request, certificate, chain, sslPolicyErrors) =>
                    {
                        return assertSSL(request, certificate, chain, sslPolicyErrors);
                    }
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(20);

                    HttpResponseMessage response = await client.PostAsync("https://keyauth.win/api/1.3/", content); // change this url if you're using a custom domain

                    if (!response.IsSuccessStatusCode)
                    {
                        switch (response.StatusCode)
                        {
                            case (HttpStatusCode)429: // Rate Limited
                                error("You're connecting too faster to loader, slow down");
                                Logger.LogEvent("You're connecting too faster to loader, slow down");
                                return "";
                            default:
                                error("Connection failure. Please try again, or contact us for help.");
                                Logger.LogEvent("Connection failure. Please try again, or contact us for help.");
                                return "";
                        }
                    }

                    string raw_response = await response.Content.ReadAsStringAsync();

                    var headers = new WebHeaderCollection();
                    if (response.Headers.TryGetValues("x-signature-ed25519", out IEnumerable<string> signatureValues))
                        headers["x-signature-ed25519"] = signatureValues.FirstOrDefault();

                    if (response.Headers.TryGetValues("x-signature-timestamp", out IEnumerable<string> timeStampValues))
                        headers["x-signature-timestamp"] = timeStampValues.FirstOrDefault();

                    Logger.LogEvent(raw_response + "\n");

                    return raw_response;
                }
            }
            catch (Exception ex)
            {
                error("Connection failure. Please try again, or contact us for help. Exception: " + ex.Message);
                Logger.LogEvent("Connection failure. Please try again, or contact us for help. Exception: " + ex.Message);
                return "";
            }
        }

        private static bool FileCheck(string domain)
        {
            try
            {
                var address = Dns.GetHostAddresses(domain);
                foreach (var addr in address)
                {
                    if (IPAddress.IsLoopback(addr) || IsPrivateIP(addr))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return true;

            }
        }

        private static bool IsPrivateIP(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] < 32)
                return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            return false;
        }

        private static bool assertSSL(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if ((!certificate.Issuer.Contains("Google Trust Services") && !certificate.Issuer.Contains("Let's Encrypt")) || sslPolicyErrors != SslPolicyErrors.None)
            {
                error("SSL assertion fail, make sure you're not debugging Network. Disable internet firewall on router if possible. & echo: & echo If not, ask the developer of the program to use custom domains to fix this.");
                Logger.LogEvent("SSL assertion fail, make sure you're not debugging Network. Disable internet firewall on router if possible. If not, ask the developer of the program to use custom domains to fix this.");
                return false;
            }
            return true;
        }

        #region app_data
        public app_data_class app_data = new app_data_class();

        public class app_data_class
        {
            public string numUsers { get; set; }
            public string numOnlineUsers { get; set; }
            public string numKeys { get; set; }
            public string version { get; set; }
            public string customerPanelLink { get; set; }
            public string downloadLink { get; set; }
        }

        private void load_app_data(app_data_structure data)
        {
            app_data.numUsers = data.numUsers;
            app_data.numOnlineUsers = data.numOnlineUsers;
            app_data.numKeys = data.numKeys;
            app_data.version = data.version;
            app_data.customerPanelLink = data.customerPanelLink;
        }
        #endregion

        #region user_data
        public user_data_class user_data = new user_data_class();

        public class user_data_class
        {
            public string username { get; set; }
            public string ip { get; set; }
            public string hwid { get; set; }
            public string createdate { get; set; }
            public string lastlogin { get; set; }
            public List<Data> subscriptions { get; set; } // array of subscriptions (basically multiple user ranks for user with individual expiry dates

            public DateTime CreationDate => KeyAuth.api.UnixTimeToDateTime(long.Parse(createdate));
            public DateTime LastLoginDate => KeyAuth.api.UnixTimeToDateTime(long.Parse(lastlogin));
        }
        public class Data
        {
            public string subscription { get; set; }
            public string expiry { get; set; }
            public string timeleft { get; set; }
            public string key { get; set; }

            public DateTime expiration
            {
                get
                {
                    return KeyAuth.api.UnixTimeToDateTime(long.Parse(expiry));
                }
            }
        }

        private void load_user_data(user_data_structure data)
        {
            user_data.username = data.username;
            user_data.ip = data.ip;
            user_data.hwid = data.hwid;
            user_data.createdate = data.createdate;
            user_data.lastlogin = data.lastlogin;
            user_data.subscriptions = data.subscriptions; // array of subscriptions (basically multiple user ranks for user with individual expiry dates 
        }
        #endregion

        [DataContract]
        private class TwoFactorData
        {
            [DataMember(Name = "secret_code")]
            public string SecretCode { get; set; }

            [DataMember(Name = "QRCode")]
            public string QRCode { get; set; }
        }

        #region response_struct
        public response_class response = new response_class();

        public class response_class
        {
            public bool success { get; set; }
            public string message { get; set; }
        }

        private void load_response_struct(response_structure data)
        {
            response.success = data.success;
            response.message = data.message;
        }
        #endregion

        private json_wrapper response_decoder = new json_wrapper(new response_structure());

        private static string GenerateSecureKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var key = new byte[32];
                rng.GetBytes(key);
                return Convert.ToBase64String(key);
            }
        }
    }

    public static class Logger
    {
        public static bool IsLoggingEnabled { get; set; } = true; // Alterado para true
        public static void LogEvent(string content)
        {
            if (!IsLoggingEnabled)
            {
                //Console.WriteLine("Debug mode disabled."); // Optional: Message when logging is disabled
                return; // Exit the method if logging is disabled
            }

            string exeName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);

            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KeyAuth", "debug", exeName);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFileName = $"{DateTime.Now:MMM_dd_yyyy}_logs.txt";
            string logFilePath = Path.Combine(logDirectory, logFileName);

            try
            {
                // Redact sensitive fields - Add more if you would like. 
                content = RedactField(content, "sessionid");
                content = RedactField(content, "ownerid");
                content = RedactField(content, "app");
                content = RedactField(content, "version");
                content = RedactField(content, "fileid");
                content = RedactField(content, "webhooks");
                content = RedactField(content, "nonce");

                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine($"[{DateTime.Now}] [{AppDomain.CurrentDomain.FriendlyName}] {content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging data: {ex.Message}");
            }
        }

        private static string RedactField(string content, string fieldName)
        {
            // Basic pattern matching to replace values of sensitive fields
            string pattern = $"\"{fieldName}\":\"[^\"]*\"";
            string replacement = $"\"{fieldName}\":\"REDACTED\"";

            return System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);
        }
    }

    public static class encryption
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        public static string HashHMAC(string enckey, string resp)
        {
            byte[] key = Encoding.UTF8.GetBytes(enckey);
            byte[] message = Encoding.UTF8.GetBytes(resp);
            var hash = new HMACSHA256(key);
            return byte_arr_to_str(hash.ComputeHash(message));
        }

        public static string byte_arr_to_str(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] str_to_byte_arr(string hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch
            {
                throw new InvalidOperationException("Erro ao converter string hexadecimal para array de bytes: A sessão pode ter expirado.");
            }
        }

        public static string iv_key() =>
            Guid.NewGuid().ToString().Substring(0, 16);
    }

    public class json_wrapper
    {
        public static bool is_serializable(Type to_check) =>
            to_check.IsSerializable || to_check.IsDefined(typeof(DataContractAttribute), true);

        public json_wrapper(object obj_to_work_with)
        {
            current_object = obj_to_work_with;

            var object_type = current_object.GetType();

            serializer = new DataContractJsonSerializer(object_type);

            if (!is_serializable(object_type))
                throw new Exception($"the object {current_object} isn't a serializable");
        }

        public object string_to_object(string json)
        {
            var buffer = Encoding.Default.GetBytes(json);

            //SerializationException = session expired

            using (var mem_stream = new MemoryStream(buffer))
                return serializer.ReadObject(mem_stream);
        }

        public T string_to_generic<T>(string json) =>
            (T)string_to_object(json);

        private DataContractJsonSerializer serializer;

        private object current_object;
    }
}