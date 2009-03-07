using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml.Serialization;
using System.IO;

namespace FlyBy
{
    [XmlRoot("UserOptions")]
    public class UserOptions
    {
        /// <summary>
        /// Constructor, makes a new instance of class
        /// </summary>
        public UserOptions()
        {
            FlickrCredentials = new Dictionary<string, string>();
            TwitterCredentials = new Dictionary<string, string>();
        }

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
        public void Serialize()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(UserOptions));
            string filename = "options.xml";
            StreamWriter outStream = new StreamWriter(filename, false);
            serializer.Serialize(outStream, this);
            outStream.Close();
        }
    }
}
