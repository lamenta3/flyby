using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using TwitterLib;

namespace FlyBy
{
    [XmlRoot("UserOptions")]
    [XmlInclude(typeof(UserList))]
    public class UserOptions
    {
        /// <summary>
        /// Constructor, makes a new instance of class
        /// </summary>
        public UserOptions()
        {
            FlickrCredentials = new Dictionary<string, string>();
            TwitterCredentials = new Dictionary<string, string>();
            TwitterUserLists = new List<UserList>();

            twitterLoggedIn = false;
        }

        [XmlIgnore]
        public static string Filename = "options2.xml";

        [XmlIgnore]
        public static bool Deserializing = false;

        /// <summary>
        /// Flickr username and passwords
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> FlickrCredentials { get; set; }

        /// <summary>
        /// Property created to fool xml serialization
        /// </summary>
        [XmlArray("FlickrCredentials")]
        [XmlArrayItem("FlickrCredentialsLine", Type = typeof(DictionaryEntry))]
        public DictionaryEntry[] FlickrCredentialsArray
        {
            get
            {
                //Make an array of DictionaryEntries to return 
                DictionaryEntry[] ret = new DictionaryEntry[FlickrCredentials.Count];
                int i = 0;
                DictionaryEntry de;
                //Iterate through Stuff to load items into the array. 
                foreach (KeyValuePair<string, string> FlickrCredentialsLine in FlickrCredentials)
                {
                    de = new DictionaryEntry();
                    de.Key = FlickrCredentialsLine.Key;
                    de.Value = FlickrCredentialsLine.Value;
                    ret[i] = de;
                    i++;
                }
                return ret;
            }
            set
            {
                FlickrCredentials.Clear();
                for (int i = 0; i < value.Length; i++)
                {
                    FlickrCredentials.Add((string)value[i].Key, (string)value[i].Value);
                }
            }
        }

        /// <summary>
        /// Twitter user lists
        /// </summary>
        public List<UserList> TwitterUserLists { get; set; }

        public bool RemoveUserList(string name)
        {
            foreach(UserList ul in TwitterUserLists)
            {
                if(ul.Name.Equals(name))
                {
                    TwitterUserLists.Remove(ul);

                    Serialize();

                    App.Instance().UpdateMainWindowFilter();

                    return true;
                }
            }

            return false;
        }

        public UserList FindUserList(string name)
        {
            foreach (UserList ul in TwitterUserLists)
            {
                if (ul.Name.Equals(name))
                {
                    return ul;
                }
            }

            return null;
        }

        public bool UpdateUserList(string name, string[] users)
        {
            UserList ul = FindUserList(name);

            if (ul != null)
            {
                ul.UserArray = users;

                Serialize();

                App.Instance().UpdateMainWindowFilter();

                return true;
            }

            return false;
        }

        public bool AddUserList(string name)
        {
            foreach(UserList ul in TwitterUserLists)
            {
                if (ul.Name.Equals(name))
                {
                    // this list is already in there
                    return false;
                }
            }

            TwitterUserLists.Add(new UserList(name));

            Serialize();

            App.Instance().UpdateMainWindowFilter();

            return true;
        }

        /// <summary>
        /// Add a user to the list of flickr users
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void AddFlickrUser(string username, string password)
        {
            username = username.ToLower();

            try
            {
                FlickrCredentials.Add(username, password);
            }
            catch (ArgumentException)
            {
                FlickrCredentials[username] = password;
            }

            Serialize();
        }

        /// <summary>
        /// Delete the account details for a flickr user
        /// </summary>
        /// <param name="username"></param>
        public void DeleteFlickrUser(string username)
        {
            FlickrCredentials.Remove(username);

            Serialize();
        }

        /// <summary>
        /// Twitter username and passwords
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> TwitterCredentials { get; set; }

        /// <summary>
        /// Property created to fool xml serialization
        /// </summary>
        [XmlArray("TwitterCredentials")]
        [XmlArrayItem("TwitterCredentialsLine", Type = typeof(DictionaryEntry))]
        public DictionaryEntry[] TwitterCredentialsArray
        {
            get
            {
                //Make an array of DictionaryEntries to return 
                DictionaryEntry[] ret = new DictionaryEntry[TwitterCredentials.Count];
                int i = 0;
                DictionaryEntry de;
                //Iterate through Stuff to load items into the array. 
                foreach (KeyValuePair<string, string> TwitterCredentialsLine in TwitterCredentials)
                {
                    de = new DictionaryEntry();
                    de.Key = TwitterCredentialsLine.Key;
                    de.Value = TwitterCredentialsLine.Value;
                    ret[i] = de;
                    i++;
                }
                return ret;
            }
            set
            {
                TwitterCredentials.Clear();
                for (int i = 0; i < value.Length; i++)
                {
                    TwitterCredentials.Add((string)value[i].Key, (string)value[i].Value);
                }
            }
        }

        /// <summary>
        /// Add a twitter account
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void AddTwitterUser(string username, string password)
        {
            TwitterCredentials.Add(username, password);

            Serialize();
        }

        /// <summary>
        /// Remove a twitter account
        /// </summary>
        /// <param name="username"></param>
        public void DeleteTwitterUser(string username)
        {
            TwitterCredentials.Remove(username);

            Serialize();
        }

        /// <summary>
        /// Serialize the list of registered users. This method runs every time a new user is added.
        /// </summary>
        protected void Serialize()
        {
            if(!UserOptions.Deserializing)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserOptions));
                StreamWriter outStream = new StreamWriter(Filename, false);
                serializer.Serialize(outStream, this);
                outStream.Close();
            }
        }

        private double notificationDisplayTime;
        public double NotificationDisplayTime 
        { 
            get
            {
                return notificationDisplayTime;  
            } 
            set
            {
                notificationDisplayTime = value;
                Serialize();
            } 
        }

        private bool? showTwitterBalloonPopup;
        public bool? ShowTwitterBalloonPopup 
        { 
            get
            {
                return showTwitterBalloonPopup;
            }
            set
            {
                showTwitterBalloonPopup = value;
                Serialize();
            }
        }

        // this value is in minutes
        private int twitterUpdateRate;
        public int TwitterUpdateRate 
        { 
            get
            {
                return twitterUpdateRate;   
            }
            set
            {
                twitterUpdateRate = value;
                Serialize();
            } 
        }

        [XmlIgnore]
        private bool twitterLoggedIn;
        [XmlIgnore]
        public bool TwitterLoggedIn 
        { 
            get
            {
                return twitterLoggedIn;
            }
            set
            {
                twitterLoggedIn = value;
                Serialize();
            }
        }

        private string twitterLastUpdated;
        public string TwitterLastUpdated 
        { 
            get
            {
                return twitterLastUpdated;
            }
            set
            {
                twitterLastUpdated = value;
                Serialize();
            }
        }

        private DateTime twitterRepliesLastUpdated;
        public DateTime TwitterRepliesLastUpdated
        {
            get
            {
                return twitterRepliesLastUpdated;
            }
            set
            {
                twitterRepliesLastUpdated = value;
                Serialize();
            }
        }

        private DateTime twitterMessagesLastUpdated;
        public DateTime TwitterMessagesLastUpdated
        {
            get
            {
                return twitterMessagesLastUpdated;
            }
            set
            {
                twitterMessagesLastUpdated = value;
                Serialize();
            }
        }

        private int twitterKeepLatest;
        public int TwitterKeepLatest
        {
            get
            {
                return twitterKeepLatest;
            }
            set
            {
                twitterKeepLatest = value;
                Serialize();
            }
        }

        private bool twitterDisplayNotifications;
        public bool TwitterDisplayNotifications
        {
            get
            {
                return twitterDisplayNotifications;
            }
            set
            {
                twitterDisplayNotifications = value;
                Serialize();
            }
        }

        [XmlIgnore]
        public User TwitterLoggedInUser;

        private DateTime twitterLastFriendsUpdate;
        public DateTime TwitterLastFriendsUpdate
        {
            get
            {
                return twitterLastFriendsUpdate;
            }
            set
            {
                twitterLastFriendsUpdate = value;
                Serialize();
            }
        }

        private bool minimizeOnClose;
        public bool MinimizeOnClose
        {
            get
            {
                return minimizeOnClose;
            }
            set
            {
                minimizeOnClose = value;
                Serialize();
            }
        }

        private bool reallyExit;
        public bool ReallyExit
        {
            get
            {
                return reallyExit;
            }
            set
            {
                reallyExit = value;
                Serialize();
            }
        }

        private bool minimizeToTray;
        public bool MinimizeToTray
        {
            get
            {
                return minimizeToTray;
            }
            set
            {
                minimizeToTray = value;
                Serialize();
            }
        }

        private double maximumIndividualAlerts;
        public double MaximumIndividualAlerts
        {
            get
            {
                return maximumIndividualAlerts;
            }
            set
            {
                maximumIndividualAlerts = value;
                Serialize();
            }
        }

        private string urlShorteningService;
        public string UrlShorteningService
        {
            get
            {
                return urlShorteningService;
            }
            set
            {
                value = urlShorteningService;
                Serialize();
            }
        }
    }

    public class UserList
    {
        public UserList()
        {
            Users = new List<string>();
        }

        public UserList(string name)
        {
            Users = new List<string>();
            this.Name = name;
        }

        /// <summary>
        /// Name of the list
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// list of users
        /// </summary>
        [XmlIgnore]
        public List<string> Users;

        /// <summary>
        /// Property created to fool xml serialization
        /// </summary>
        [XmlArray("Users")]
        [XmlArrayItem("UsersLine", Type = typeof(string))]
        public string[] UserArray
        {
            get
            {
                //Make an array of DictionaryEntries to return 
                string[] ret = new string[Users.Count];
                int i = 0;
                //Iterate through Stuff to load items into the array. 
                foreach (string str in Users)
                {
                    ret[i] = str;
                    i++;
                }
                return ret;
            }
            set
            {
                Users.Clear();
                for (int i = 0; i < value.Length; i++)
                {
                    Users.Add((string)value[i]);
                }
            }
        }

        [XmlIgnore]
        public string UserRegex
        {
            get
            {
                string res = "";
                for (int i = 0; i < Users.Count - 2; i++)
                {
                    res += Users[i];
                    res += "|";
                }
                res += Users[Users.Count - 1];

                return res;
            }
        }
    }
}
