using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using FlickrNet;
using System.Collections.Generic;
using TwitterLib;
using TwitterLib.Utilities;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace FlyBy
{
    public partial class MainWindow
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Insert code required on object creation below this point.
            ResetTwitterOptions();
            ResetFlickrAccountList();

            TrapUnhandledExceptions();

            SetupNotifyIcon();

            SetupSingleInstance();

            SetDataContextForAllOfTabs();

            SetHowOftenToGetUpdatesFromTwitter();

            // TODO: InitializeClickOnceTimer();

            // TODO: InitializeSoundPlayer();

            InitializeMiscSettings();

            // TODO: DisplayLoginIfUserNotLoggedIn();

            // TODO: SetTweetSorting();

            UpdateFilter();
            if (TwitterListsListBox.SelectedItem == null)
            {
                TwitterListsListBox.SelectedIndex = 0;
            }
        }

        private void TweetsListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.DirectlyOver != null && e.MouseDevice.DirectlyOver.GetType() == typeof(TextBlock))
            {
                TextBlock textBlock = (TextBlock)e.MouseDevice.DirectlyOver;

                try
                {
                    ListBox listbox = (ListBox)sender;

                    if (textBlock.Name == "ScreenName")
                    {
                        if (listbox.SelectedItem != null && currentView != "User")
                        {
                            Tweet tweet = (Tweet)listbox.SelectedItem;
                            //System.Diagnostics.Process.Start(tweet.User.TwitterUrl);
                            DelegateUserTimelineFetch(tweet.User.ScreenName);
                        }
                    }
                }
                catch (Win32Exception)
                {
                    //App.Logger.Debug(String.Format("Exception: {0}", ex.ToString()));
                }
            }
        }

        private string displayUser;

        // Main collection of user Tweets
        private TweetCollection userTweets = new TweetCollection();

        private void DelegateUserTimelineFetch(string userId)
        {
            displayUser = userId;

            TwitterListsListBox.SelectedItem = TwitterListsListBox.FindName("User");
            //UserTab.IsSelected = true;
            //UserContextMenu.IsEnabled = true;  // JMF

            userTweets.Clear();

            // Let the user know what's going on
            StatusTextBlock.Text = "Retrieving user's tweets...";

            //PlayStoryboard("Fetching");

            // Create a Dispatcher to fetching new tweets
            LayoutRoot.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new OneStringArgDelegate(GetUserTimeline), userId);
        }

        private void UpdateUsersTimelineInterface(TweetCollection newTweets)
        {
            StatusTextBlock.Text = displayUser + "'s Timeline Updated: " + App.Instance().Options.TwitterRepliesLastUpdated.ToLongTimeString();
            User u = null;

            for (int i = newTweets.Count - 1; i >= 0; i--)
            {
                Tweet tweet = newTweets[i];
                u = tweet.User;

                if (!userTweets.Contains(tweet))
                {
                    userTweets.Insert(0, tweet);
                    tweet.IsNew = true;
                }
                else
                {
                    // update the relativetime for existing tweets
                    userTweets[i].UpdateRelativeTime();
                }
            }

            if (userTweets.Count > 0)
                UserTimelineListBox.SelectedIndex = 0;
        }

        private void ContextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteTweet();
        }

        private void DeleteTweet()
        {
            if (null != SelectedTweet)
            {
                DeleteTweet(SelectedTweet.Id);
            }
        }

        // Main collection of replies
        private TweetCollection replies = new TweetCollection();

        private void DeleteTweet(double id)
        {

            /* By: Keith Elder
             * You can only destroy a tweet if you are the one that created it
             * or if it is a direct message to you.  This is causing exceptions.
             */
            foreach (KeyValuePair<string, string> userLine in App.Instance().Options.TwitterCredentials)
            {
                if (SelectedTweet.User.ScreenName == userLine.Key)
                {
                    if (MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to permanently delete your tweet?\nThis action is irreversible. Select No to only delete it from the application or Yes to delete permanently.",
                        "FlyBy", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    {
                        LayoutRoot.Dispatcher.BeginInvoke(
                                        DispatcherPriority.Background,
                                        new DeleteTweetDelegate(App.Instance().Twitter.DestroyTweet), id);
                    }
                    if (tweets.Contains(SelectedTweet))
                        tweets.Remove(SelectedTweet);
                    else if (replies.Contains(SelectedTweet))
                        replies.Remove(SelectedTweet);

                    if (userTweets.Contains(SelectedTweet))
                        userTweets.Remove(SelectedTweet);
                }
            }
        }

        private void ContextMenuReply_Click(object sender, RoutedEventArgs e)
        {
            CreateReply();
        }

        private void ContextMenuRetweet_Click(object sender, RoutedEventArgs e)
        {
            createRetweet();
        }

        private void ScreenName_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            LaunchUrlIfValid(textBlock.Tag.ToString());
        }

        private void Url_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            LaunchUrlIfValid(textBlock.Text);
        }

        private void ContextMenuFollow_Click(object sender, RoutedEventArgs e)
        {
            FollowUser();
        }

        private void FollowUser()
        {
            if (null != SelectedTweet)
            {
                FollowUser(SelectedTweet.User.ScreenName);
            }
        }

        private void FollowUser(string username)
        {
            LayoutRoot.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new OneStringArgDelegate(App.Instance().Twitter.FollowUser), username);
            DispatchFriendsList();
        }

        private void LaunchUrlIfValid(string candidateUrlString)
        {
            if (Uri.IsWellFormedUriString(candidateUrlString, UriKind.Absolute))
            {
                try
                {
                    System.Diagnostics.Process.Start(candidateUrlString);
                }
                catch (Win32Exception)
                {
                    //App.Logger.Debug(String.Format("Exception: {0}", ex.ToString()));
                }
            }
        }

        private void createRetweet()
        {
            Tweet selectedTweet = SelectedTweet as Tweet;
            if (selectedTweet != null)
            {
                /*
                if (!isExpanded)
                {
                    this.Tabs.SelectedIndex = 0;
                    ToggleUpdate();
                }
                */
                string message = string.Format("retweet @{0}: {1}", selectedTweet.User.ScreenName, selectedTweet.Text);
                message = TruncateMessage(message);
                TweetTextBox.Text = message;
                TweetTextBox.Select(TweetTextBox.Text.Length, 0);
            }

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.Refresh();
        }

        private void Refresh()
        {
            switch (TwitterListsListBox.SelectedItem.ToString())
            {
                case "All":
                    DelegateRecentFetch();

                    // After a manual refresh, reset the timer
                    refreshTimer.Stop();
                    refreshTimer.Start();

                    break;
                case "Replies":
                    DelegateRepliesFetch();
                    break;
                case "Messages":
                    DelegateMessagesFetch();
                    break;
                case "User":
                    DelegateUserTimelineFetch(displayUser);
                    break;
            }
        }

        private void DelegateMessagesFetch()
        {
            // Let the user know what's going on
            StatusTextBlock.Text = "Retrieving direct messages...";

            //PlayStoryboard("Fetching");

            // Create a Dispatcher to fetching new tweets
            NoArgDelegate fetcher = new NoArgDelegate(
                this.GetMessages);

            fetcher.BeginInvoke(null, null);
        }

        private void GetMessages()
        {
            try
            {
                // Schedule the update function in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new MessagesDelegate(UpdateMessagesInterface), App.Instance().Twitter.RetrieveMessages());
            }
            catch (WebException)
            {
                //App.Logger.Debug(String.Format("There was a problem fetching your direct messages from Twitter.com: {0}", ex.ToString()));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }
        }

        // Main collection of direct messages
        private DirectMessageCollection messages = new DirectMessageCollection();

        private void UpdateMessagesInterface(DirectMessageCollection newMessages)
        {
            App.Instance().Options.TwitterMessagesLastUpdated = DateTime.Now;
            StatusTextBlock.Text = "Messages Updated: " + App.Instance().Options.TwitterMessagesLastUpdated.ToLongTimeString();

            for (int i = newMessages.Count - 1; i >= 0; i--)
            {
                DirectMessage message = newMessages[i];
                if (!messages.Contains(message))
                {
                    messages.Insert(0, message);
                    message.IsNew = true;

                }
                else
                {
                    // update the relativetime for existing messages
                    //messages[i].UpdateRelativeTime();
                }
            }

            //StopStoryboard("Fetching");
        }

        private void DelegateRepliesFetch()
        {
            // Let the user know what's going on
            StatusTextBlock.Text = "Retrieving replies...";

            //PlayStoryboard("Fetching");

            // Create a Dispatcher to fetching new tweets
            NoArgDelegate fetcher = new NoArgDelegate(
                this.GetReplies);

            fetcher.BeginInvoke(null, null);
        }

        private void GetReplies()
        {
            try
            {
                // Schedule the update function in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new OneArgDelegate(UpdateRepliesInterface), App.Instance().Twitter.GetReplies());
            }
            catch (WebException)
            {
                //App.Logger.Debug(String.Format("There was a problem fetching your replies from Twitter.com. ", ex.Message));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }

        }

        private void UpdateRepliesInterface(TweetCollection newReplies)
        {
            App.Instance().Options.TwitterRepliesLastUpdated = DateTime.Now;
            StatusTextBlock.Text = "Replies Updated: " + App.Instance().Options.TwitterRepliesLastUpdated.ToLongTimeString();

            UpdateExistingTweets(replies);
            TweetCollection addedReplies = new TweetCollection();

            for (int i = newReplies.Count - 1; i >= 0; i--)
            {
                Tweet reply = newReplies[i];
                if (!replies.Contains(reply))
                {
                    replies.Insert(0, reply);
                    reply.Index = replies.Count;
                    reply.IsNew = true;
                    addedReplies.Add(reply);
                }
            }

            if (addedReplies.Count > 0)
            {
                if (App.Instance().Options.TwitterDisplayNotifications && !(bool)this.IsActive)
                    NotifyOnNewTweets(addedReplies, "reply");
            }

            //StopStoryboard("Fetching");
        }

        private void HashtagClickedInTweet(object sender, RoutedEventArgs reArgs)
        {
            if (reArgs.OriginalSource is System.Windows.Documents.Hyperlink)
            {
                Hyperlink h = reArgs.OriginalSource as Hyperlink;

                string hashtag = h.Tag.ToString();
                string hashtagUrl = String.Format("http://search.twitter.com/search?q=%23{0}", Uri.EscapeDataString(hashtag));

                try
                {
                    System.Diagnostics.Process.Start(hashtagUrl);
                }
                catch
                {
                    // TODO: Warn the user? Log the error? Do nothing since Witty itself is not affected?
                }

                reArgs.Handled = true;
            }
        }

        private void NameClickedInTweet(object sender, RoutedEventArgs reArgs)
        {
            if (reArgs.OriginalSource is System.Windows.Documents.Hyperlink)
            {
                Hyperlink h = reArgs.OriginalSource as Hyperlink;

                string userId = h.Tag.ToString();
                DelegateUserTimelineFetch(userId);

                reArgs.Handled = true;
            }
        }

        private void GetUserTimeline(string userId)
        {
            try
            {
                // Schedule the update function in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new OneArgDelegate(UpdateUsersTimelineInterface), App.Instance().Twitter.GetUserTimeline(userId));
            }

            // Jason Follas: Added the following UI feedback behavior for when users weren't found.
            catch (UserNotFoundException userNotFoundEx)
            {

                TweetCollection fakeTweets = new TweetCollection();
                fakeTweets.Add(new Tweet());
                fakeTweets[0].Id = -1;
                fakeTweets[0].Text = userNotFoundEx.Message;
                fakeTweets[0].Source = "Witty Error Handler";
                fakeTweets[0].User = new User();
                fakeTweets[0].User.ScreenName = "@" + userNotFoundEx.UserId;
                fakeTweets[0].User.Description = userNotFoundEx.Message;

                //StopStoryboard("Fetching");

                this.UserContextMenu.IsEnabled = false;

                UpdateUsersTimelineInterface(fakeTweets);

            }
            catch (System.Security.SecurityException)
            {
                //App.Logger.DebugFormat("User not allowed to get protected tweets from {0}. Exception details: {1}", userId, ex.ToString());
                StatusTextBlock.Text = userId + "'s updates are protected.";
                //StopStoryboard("Fetching");
            }
            catch (WebException)
            {
                //App.Logger.DebugFormat("There was a problem fetching the user's timeline from Twitter.com: {0}", ex.ToString());
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }

        }

        private string currentView
        {
            get
            {
                return TwitterListsListBox.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// Update the text length counter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TweetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NumberLabel.Content = (TwitterNet.CharacterLimit - TweetTextBox.Text.Length).ToString();
            if (TweetTextBox.Text.Length > TwitterNet.CharacterLimit)
            {
                NumberLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                NumberLabel.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// Check if the user has pressed enter; if so, attempt to submit the tweet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TweetTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SubmitTweet();
            }
        }

        /// <summary>
        /// Submit the tweet
        /// </summary>
        /// <returns></returns>
        private void SubmitTweet()
        {
            // Schedule posting the tweet
            TweetTextBox.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new OneStringArgDelegate(AddTweet), TweetTextBox.Text);

        }

        private void AddTweet(string tweetText)
        {
            try
            {
                //bgriswold: Keeping check in place in case final character string is URL and it wasn't reformatted on the fly.
                if (tweetText.Length > TwitterNet.CharacterLimit)
                {
                    ParseTextHereAndTinyUpAnyURLsFound(ref tweetText);
                }

                var addedTweets = new TweetCollection();

                string[] statuses = TweetSplitter.SplitTweet(tweetText);
                foreach (string status in statuses)
                {
                    if (isReplyMessage)
                    {
                        addedTweets.Add(App.Instance().Twitter.AddTweet(status, SelectedTweet.Id));
                    }
                    else
                    {
                        if (App.Instance().Twitter != null)
                        {
                            addedTweets.Add(App.Instance().Twitter.AddTweet(status));
                        }
                    }
                }

                if (statuses.Length > 0)
                {
                    ScheduleUpdateFunctionInUIThread(addedTweets);
                }

            }
            catch (WebException)
            {
                //UpdateTextBlock.Text = "Update failed.";
                //App.Logger.Debug(String.Format("There was a problem fetching new tweets from Twitter.com: {0}", ex.ToString()));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }

        }

        private void ScheduleUpdateFunctionInUIThread(TweetCollection addedTweets)
        {
            LayoutRoot.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new AddTweetsUpdateDelegate(UpdatePostUserInterface), addedTweets);
        }


        private void UpdatePostUserInterface(TweetCollection addedTweets)
        {
            if (addedTweets != null)
            {
                //UpdateTextBlock.Text = "Update";
                StatusTextBlock.Text = "Status Updated!";
                //PlayStoryboard("CollapseUpdate");
                isExpanded = false;
                isReplyMessage = false;
                TweetTextBox.Clear();

                foreach (Tweet tweet in addedTweets)
                {
                    tweets.Insert(0, tweet);
                }
            }
            else
            {
                //App.Logger.Error("There was a problem posting your tweet to Twitter.com.");
                MessageBox.Show("There was a problem posting your tweet to Twitter.com.");
            }
        }


        private void ParseTextHereAndTinyUpAnyURLsFound(ref string tweetText)
        {
            //parse the text here and tiny up any URLs found.
            ShorteningService service;
            if (!string.IsNullOrEmpty(App.Instance().Options.UrlShorteningService))
                service = (ShorteningService)Enum.Parse(typeof(ShorteningService), App.Instance().Options.UrlShorteningService);
            else
                service = ShorteningService.TinyUrl;

            UrlShorteningService urlHelper = new UrlShorteningService(service);
            tweetText = urlHelper.ShrinkUrls(tweetText);
        }


        private void SendMessage(string user, string messageText)
        {
            try
            {
                App.Instance().Twitter.SendMessage(user, messageText);

                // Schedule the update function in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new NoArgDelegate(UpdateMessageUserInterface));
            }
            catch (WebException)
            {
                //UpdateTextBlock.Text = "Message failed.";
                //App.Logger.Debug(String.Format("There was a problem sending your message: {0}", ex.ToString()));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }

        }

        private void UpdateMessageUserInterface()
        {
            //UpdateTextBlock.Text = "Send Message";
            StatusTextBlock.Text = "Message Sent!";
            //PlayStoryboard("CollapseMessage");
            //isMessageExpanded = false;
            //MessageTextBox.Clear();

            UpdateExistingTweets();
        }

        /// <summary>
        /// Copy "RT @author: tweet" to the tweetbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Retweet_Click(object sender, RoutedEventArgs e)
        {
            TweetTextBox.Text = "RT " + TweetsListBox.SelectedItem.ToString();
        }

        /// <summary>
        /// Copy the text of this tweet to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyTweet_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(System.Windows.DataFormats.Text, TweetsListBox.SelectedItem.ToString());
        }

        /// <summary>
        /// Open this tweet in a browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTweet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(TweetsListBox.SelectedItem.ToString());
            }
            catch (Exception)
            {
                // couldn't open it, oh well
            }
        }

        /// <summary>
        /// Show the Flickr username and password fields
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Flickr_NewAccount_Click(object sender, RoutedEventArgs e)
        {
            FlickrUsernameBox.Clear();
            FlickrUsernameLabel.Visibility = Visibility.Visible;
            FlickrUsernameBox.Visibility = Visibility.Visible;

            FlickrPasswordBox.Clear();
            FlickrPasswordLabel.Visibility = Visibility.Visible;
            FlickrPasswordBox.Visibility = Visibility.Visible;

            FlickrNewAccountButton.Visibility = Visibility.Collapsed;
            FlickrAddUserButton.Visibility = Visibility.Visible;

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        private void FlickrUsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FlickrUsernameBox.Text != "")
            {
                FlickrUsernameLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                FlickrUsernameLabel.Visibility = Visibility.Visible;
            }
        }

        private void FlickrAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (FlickrUsernameBox.Text != "" && FlickrPasswordBox.Password != "")
            {
                App.Instance().Options.AddFlickrUser(FlickrUsernameBox.Text, FlickrPasswordBox.Password);

                // update the username box
                ResetFlickrAccountList();

                // reset the controls
                FlickrUsernameBox.Clear();
                FlickrUsernameLabel.Visibility = Visibility.Collapsed;
                FlickrUsernameBox.Visibility = Visibility.Collapsed;

                FlickrPasswordBox.Clear();
                FlickrPasswordLabel.Visibility = Visibility.Collapsed;
                FlickrPasswordBox.Visibility = Visibility.Collapsed;

                FlickrNewAccountButton.Visibility = Visibility.Visible;
                FlickrAddUserButton.Visibility = Visibility.Collapsed;
            }

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        /// <summary>
        /// Populate the list of twitter users
        /// </summary>
        private void ResetTwitterOptions()
        {
            TwitterAccountList.Items.Clear();
            // fill in the users list
            foreach (KeyValuePair<string, string> userLine in App.Instance().Options.TwitterCredentials)
            {
                TwitterAccountList.Items.Add(userLine.Key);
            }

            ShowTwitterBalloonPopupCheckBox.IsChecked = App.Instance().Options.ShowTwitterBalloonPopup;
        }

        /// <summary>
        /// Populate the list of flickr users
        /// </summary>
        private void ResetFlickrAccountList()
        {
            FlickrAccountList.Items.Clear();
            // fill in the users list
            foreach (KeyValuePair<string, string> userLine in App.Instance().Options.FlickrCredentials)
            {
                FlickrAccountList.Items.Add(userLine.Key);
            }
        }

        private void FlickrPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (FlickrPasswordBox.Password != "")
            {
                FlickrPasswordLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                FlickrPasswordLabel.Visibility = Visibility.Visible;
            }
        }

        private void FlickrDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string username = FlickrAccountList.SelectedItem.ToString().ToLower();

            App.Instance().Options.DeleteFlickrUser(username);

            ResetFlickrAccountList();
        }

        /// <summary>
        /// Show the username and password fields
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Twitter_NewAccount_Click(object sender, RoutedEventArgs e)
        {
            TwitterUsernameBox.Clear();
            TwitterUsernameLabel.Visibility = Visibility.Visible;
            TwitterUsernameBox.Visibility = Visibility.Visible;

            TwitterPasswordBox.Clear();
            TwitterPasswordLabel.Visibility = Visibility.Visible;
            TwitterPasswordBox.Visibility = Visibility.Visible;

            TwitterNewAccountButton.Visibility = Visibility.Collapsed;
            TwitterAddUserButton.Visibility = Visibility.Visible;

            TwitterOptionsGroupBox.InvalidateVisual();
        }

        private void TwitterUsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TwitterUsernameBox.Text != "")
            {
                TwitterUsernameLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                TwitterUsernameLabel.Visibility = Visibility.Visible;
            }
        }

        private void TwitterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (TwitterPasswordBox.Password != "")
            {
                TwitterPasswordLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                TwitterPasswordLabel.Visibility = Visibility.Visible;
            }
        }

        private void TwitterAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (TwitterUsernameBox.Text != "" && TwitterPasswordBox.Password != "")
            {
                App.Instance().Options.AddTwitterUser(TwitterUsernameBox.Text, TwitterPasswordBox.Password);

                // update the username box
                ResetTwitterOptions();

                // reset the controls
                TwitterUsernameBox.Clear();
                TwitterUsernameLabel.Visibility = Visibility.Collapsed;
                TwitterUsernameBox.Visibility = Visibility.Collapsed;

                TwitterPasswordBox.Clear();
                TwitterPasswordLabel.Visibility = Visibility.Collapsed;
                TwitterPasswordBox.Visibility = Visibility.Collapsed;

                TwitterNewAccountButton.Visibility = Visibility.Visible;
                TwitterAddUserButton.Visibility = Visibility.Collapsed;
            }

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        private void TwitterDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (TwitterAccountList.SelectedItem != null)
            {
                string username = TwitterAccountList.SelectedItem.ToString();

                App.Instance().Options.DeleteTwitterUser(username);

                ResetTwitterOptions();
            }
        }

        // Delegates for placing jobs onto the thread dispatcher.  
        // Used for making asynchronous calls to Twitter so that the UI does not lock up.
        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(TweetCollection arg);
        private delegate void OneStringArgDelegate(string arg);
        private delegate void AddTweetsUpdateDelegate(TweetCollection arg);
        private delegate void MessagesDelegate(DirectMessageCollection arg);
        private delegate void SendMessageDelegate(string user, string text);
        private delegate void LoginDelegate(User arg);
        private delegate void DeleteTweetDelegate(double id);

        #region Retrieve new tweets

        /// <summary>
        /// Encapsulated method to create dispatcher for fetching new tweets asynchronously
        /// </summary>
        private void DelegateRecentFetch()
        {
            // Let the user know what's going on
            StatusTextBlock.Text = "Retrieving tweets...";

            // TODO: PlayStoryboard("Fetching");

            // Create a Dispatcher to fetching new tweets
            NoArgDelegate fetcher = new NoArgDelegate(
                this.GetTweets);

            fetcher.BeginInvoke(null, null);

        }

        private void InitializeMiscSettings()
        {
            //AppSettings.UserBehaviorManager = new UserBehaviorManager(AppSettings.SerializedUserBehaviors);
            //this.Topmost = AlwaysOnTopMenuItem.IsChecked = AppSettings.AlwaysOnTop;
            //ScrollViewer.SetCanContentScroll(TweetsListBox, !AppSettings.SmoothScrolling);
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            DelegateRecentFetch();
        }

        private void GetTweets()
        {
            try
            {
                // Schedule the update functions in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new OneArgDelegate(UpdateUserInterface), App.Instance().Twitter.GetFriendsTimeline());

                // Direct message and replies < 70 hours old will be displayed on the recent tab.
                // Once this somewhat arbitrary (Friday, 5pm - Monday, 9am + 6 hours) threshold is met, 
                // users will still be able to access there direct messages and replies via their
                // respective tabs.  
                //TODO: Make DM and Reply threshold configurable.  Rework this logic once concept of viewed tweets is introduced.
                string since = DateTime.Now.AddHours(-70).ToString();

                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new OneArgDelegate(UpdateUserInterface), App.Instance().Twitter.GetReplies(since));

                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new OneArgDelegate(UpdateUserInterface), App.Instance().Twitter.RetrieveMessages(since).ToTweetCollection());
            }
            catch (RateLimitException ex)
            {
                //App.Logger.Debug(String.Format("There was a problem fetching new tweets from Twitter.com: {0}", ex.ToString()));
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new OneStringArgDelegate(ShowStatus), ex.Message);
            }
            catch (WebException)
            {
                //App.Logger.Debug(String.Format("There was a problem fetching new tweets from Twitter.com: {0}", ex.ToString()));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
            }

        }

        private void SetHowOftenToGetUpdatesFromTwitter()
        {
            // Set how often to get updates from Twitter
            if (App.Instance().Options.TwitterUpdateRate <= 0)
            {
                App.Instance().Options.TwitterUpdateRate = 3;
            }

            refreshInterval = new TimeSpan(0, App.Instance().Options.TwitterUpdateRate, 0);
        }

        private void SetDataContextForAllOfTabs()
        {
            // Set the data context for all of the tabs

            TweetsListBox.DataContext = tweets;
            // TODO: RepliesListBox.ItemsSource = replies;
            // TODO: UserTab.DataContext = userTweets;
            // TODO: MessagesListBox.ItemsSource = messages;
        }

        private void TrapUnhandledExceptions()
        {
            LayoutRoot.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);
        }

        /// <summary>
        /// Enforce single instance for release mode
        /// </summary>
        private void SetupSingleInstance()
        {
#if !DEBUG
            Application.Current.Exit += new ExitEventHandler(Current_Exit);
            _instanceManager = new SingleInstanceManager(this, ShowApplication);
#endif
        }

        /*
        private void DisplayLoginIfUserNotLoggedIn()
        {
            // Does the user need to login?
            if (string.IsNullOrEmpty(AppSettings.Username))
            {
                //PlayStoryboard("ShowLogin");
            }
            else
            {
                LoginControl.Visibility = Visibility.Hidden;

                System.Security.SecureString password = TwitterNet.DecryptString(AppSettings.Password);

                // Jason Follas: Reworked Web Proxy - don't need to explicitly pass into TwitterNet ctor
                //twitter = new TwitterNet(AppSettings.Username, password, WebProxyHelper.GetConfiguredWebProxy());
                App.Instance().Twitter = new TwitterNet(AppSettings.Username, password);

                // Jason Follas: Twitter proxy servers, anyone?  (Network Nazis who block twitter.com annoy me)
                twitter.TwitterServerUrl = AppSettings.TwitterHost;

                // Let the user know what's going on
                StatusTextBlock.Text = Properties.Resources.TryLogin;
                PlayStoryboard("Fetching");

                // Create a Dispatcher to attempt login on new thread
                NoArgDelegate loginFetcher = new NoArgDelegate(this.TryLogin);
                loginFetcher.BeginInvoke(null, null);

            }
        }
         */
        public void LoginTwitterUser()
        {
            bool loggedin = App.Instance().Options.TwitterLoggedIn;
            if (!loggedin)
            {
                //LoginControl.Visibility = Visibility.Hidden;

                foreach (KeyValuePair<string, string> kvp in App.Instance().Options.TwitterCredentials)
                {
                    string username = kvp.Key;
                    System.Security.SecureString password = TwitterNet.ToSecureString(kvp.Value);
                    //System.Security.SecureString password = TwitterNet.DecryptString(AppSettings.Password);

                    // Jason Follas: Reworked Web Proxy - don't need to explicitly pass into TwitterNet ctor
                    //twitter = new TwitterNet(AppSettings.Username, password, WebProxyHelper.GetConfiguredWebProxy());
                    App.Instance().Twitter = new TwitterNet(username, password);

                    // Jason Follas: Twitter proxy servers, anyone?  (Network Nazis who block twitter.com annoy me)
                    //twitter.TwitterServerUrl = AppSettings.TwitterHost;
                    App.Instance().Twitter.TwitterServerUrl = "http://twitter.com/";

                    // Let the user know what's going on
                    StatusTextBlock.Text = "Logging in"; //Properties.Resources.TryLogin;
                    //PlayStoryboard("Fetching");

                    // Create a Dispatcher to attempt login on new thread
                    NoArgDelegate loginFetcher = new NoArgDelegate(this.TryLogin);
                    loginFetcher.BeginInvoke(null, null);

                    return; // skip additional loops
                }
            }
            // we can only handle one account right now
        }

        private void TryLogin()
        {
            try
            {
                // Schedule the update function in the UI thread.
                LayoutRoot.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new LoginDelegate(UpdatePostLoginInterface), App.Instance().Twitter.Login());
            }
            catch (WebException)
            {
                //App.Logger.Debug(String.Format("There was a problem logging in to Twitter: {0}", ex.ToString()));
            }
            catch (ProxyAuthenticationRequiredException)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show("Proxy server is configured incorrectly.  Please correct the settings on the Options menu.");
                //LayoutRoot.Dispatcher.BeginInvoke(DispatcherPriority.Background, new NoArgDelegate(UpdateLoginFailedInterface));
            }
            catch (ProxyNotFoundException ex)
            {
                //App.Logger.Error("Incorrect proxy configuration.");
                MessageBox.Show(ex.Message);
                //LayoutRoot.Dispatcher.BeginInvoke(DispatcherPriority.Background, new NoArgDelegate(UpdateLoginFailedInterface));
            }
        }

        // Timer used for automatic tweet updates
        private DispatcherTimer refreshTimer = new DispatcherTimer();

        private void UpdatePostLoginInterface(User user)
        {
            App.Instance().Options.TwitterLoggedInUser = user;
            if (App.Instance().Options.TwitterLoggedInUser != null)
            {
                App.Instance().Options.TwitterLoggedIn = true;
                //RefreshButton.IsEnabled = true;
                //OptionsButton.IsEnabled = true;
                //FilterToggleButton.IsEnabled = true;
                App.Instance().Options.TwitterLastUpdated = string.Empty;
                //Filter.IsEnabled = true;

                DelegateRecentFetch();

                // Setup refresh timer
                refreshTimer.Interval = refreshInterval;
                refreshTimer.Tick += new EventHandler(Timer_Elapsed);
                refreshTimer.Start();

                // Setup friendslist timer for AutoSuggestPattern matching support
                SetupFriendsListTimer();
            }
            else
            {
                // login info from user settings is not valid, re-display the login screen.
                //PlayStoryboard("ShowLogin");
            }

            if (user != null)
            {
                UserImage.Source = new ImageSourceConverter().ConvertFromString(user.ImageUrl) as ImageSource;
                TwitterUserName.Text = user.ScreenName;
                TwitterUserName.Tag = user.SiteUrl;
                TwitterFullName.Text = user.Name;
            }
        }

        private void SetupFriendsListTimer()
        {
            DispatchFriendsList();

            friendsRefreshTimer.Interval = friendsRefreshInterval;
            friendsRefreshTimer.IsEnabled = true;
            friendsRefreshTimer.Start();
            friendsRefreshTimer.Tick += new EventHandler(friendsRefreshTimer_Tick);
        }

        private DispatcherTimer friendsRefreshTimer = new DispatcherTimer();

        private void DispatchFriendsList()
        {
            LayoutRoot.Dispatcher.BeginInvoke(DispatcherPriority.Background, new NoArgDelegate(UpdateFriendsList));
        }

        private UserCollection friends = new UserCollection();

        private void UpdateFriendsList()
        {
            if (App.Instance().Options.TwitterLoggedIn)
            {
                friends = App.Instance().Twitter.GetFriends();
                App.Instance().Options.TwitterLastFriendsUpdate = DateTime.Now;
            }
        }

        void friendsRefreshTimer_Tick(object sender, EventArgs e)
        {
            DispatchFriendsList();
        }

        private DateTime? lastTruncatedTweetTime;

        private void UpdateUserInterface(TweetCollection newTweets)
        {
            DateTime lastUpdated = DateTime.Now;
            StatusTextBlock.Text = "Last Updated: " + lastUpdated.ToLongTimeString();

            App.Instance().Options.TwitterLastUpdated = lastUpdated.ToString();

            //FilterTweets(newTweets);
            HighlightTweets(newTweets);
            UpdateExistingTweets();

            TweetCollection addedTweets = new TweetCollection();

            //prevents huge number of notifications appearing on startup
            bool displayPopups = !(tweets.Count == 0);

            // Add the new tweets
            for (int i = newTweets.Count - 1; i >= 0; i--)
            {
                Tweet tweet = newTweets[i];

                if (tweets.Contains(tweet) || HasBehavior(tweet, UserBehavior.Ignore) || IsTruncatedTweet(tweet))
                    continue;

                tweets.Add(tweet);
                tweet.Index = tweets.Count;
                tweet.IsNew = true;
                addedTweets.Add(tweet);
            }

            // tweets listbox ScrollViewer.CanContentScroll is set to "False", which means it scrolls more smooth,
            // However it disables Virtualization
            // Remove tweets pass 100 should improve performance reasons.
            if (App.Instance().Options.TwitterKeepLatest != 0)
            {
                if (tweets.Count > App.Instance().Options.TwitterKeepLatest)
                    lastTruncatedTweetTime = tweets[App.Instance().Options.TwitterKeepLatest - 1].DateCreated;

                tweets.TruncateAfter(App.Instance().Options.TwitterKeepLatest);
            }

            if (addedTweets.Count > 0)
            {
                if (App.Instance().Options.TwitterDisplayNotifications && !(bool)this.IsActive)
                    NotifyOnNewTweets(addedTweets, "tweet");

                /*
                if (AppSettings.PlaySounds)
                {
                    // Author: Keith Elder
                    // I wrapped a try catch around this and added logging.
                    // I found that the Popup screen and this were causing 
                    // a threading issue.  At least that is my theory.  When
                    // new items would come in, and play a sound as well as 
                    // pop a new message there was no need to recreate and load
                    // the wave file.  InitializeSoundPlayer() was added on load
                    // to do that just once.
                    try
                    {
                        // Play tweets found sound for new tweets
                        _player.Play();
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Error("Error playing sound", ex);
                    }
                }
                 */
            }

            //StopStoryboard("Fetching");
        }

        private bool IsTruncatedTweet(Tweet tweet)
        {
            if (tweet.DateCreated < lastTruncatedTweetTime)
                return true;

            return false;
        }

        TweetCollection filtered = new TweetCollection();

        private void FilterTweets(TweetCollection tweets, string selectedItem)
        {
            if (selectedItem.Equals("All"))
            {
                TweetsListBox.DataContext = tweets;
                return;
            }

            // empty out the filter
            filtered = new TweetCollection();

            // user list to filter on
            UserList ul = App.Instance().Options.FindUserList(selectedItem);

            for (int i = tweets.Count - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(tweets[i].User.ScreenName, ul.UserRegex, RegexOptions.IgnoreCase))
                {
                    filtered.Add(tweets[i]);
                }
            }

            // set the current data context to the filtered tweets
            TweetsListBox.DataContext = filtered;

            TweetsListBox.Visibility = Visibility.Hidden;
            TweetsListBox.InvalidateVisual();
            TweetsListBox.Visibility = Visibility.Visible;
        }

        private void HighlightTweets(TweetCollection tweets)
        {
            /*
            if (string.IsNullOrEmpty(AppSettings.HighlightRegex))
                return;

            foreach (Tweet tweet in tweets)
            {
                if (Regex.IsMatch(tweet.Text, AppSettings.HighlightRegex, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(tweet.User.ScreenName, AppSettings.HighlightRegex, RegexOptions.IgnoreCase))
                {
                    tweet.IsInteresting = true;
                }
            }
            */
        }

        private System.Windows.Forms.NotifyIcon _notifyIcon;

        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = _storedWindowState;
        }

        private WindowState _storedWindowState = WindowState.Normal;

        private void SetupNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.BalloonTipText = "Right-click for more options";
            _notifyIcon.BalloonTipTitle = "FlyBy";
            _notifyIcon.Text = "FlyBy - Social Media Aggregator";
            _notifyIcon.Icon = FlyBy.Properties.Resources.App;
            _notifyIcon.DoubleClick += new EventHandler(m_notifyIcon_Click);

            System.Windows.Forms.ContextMenu notifyMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem openMenuItem = new System.Windows.Forms.MenuItem();
            System.Windows.Forms.MenuItem exitMenuItem = new System.Windows.Forms.MenuItem();

            notifyMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { openMenuItem, exitMenuItem });
            openMenuItem.Index = 0;
            openMenuItem.Text = "Open";
            openMenuItem.Click += new EventHandler(openMenuItem_Click);
            exitMenuItem.Index = 1;
            exitMenuItem.Text = "Exit";
            exitMenuItem.Click += new EventHandler(exitMenuItem_Click);

            _notifyIcon.ContextMenu = notifyMenu;

            this.Closed += new EventHandler(OnClosed);
            this.StateChanged += new EventHandler(OnStateChanged);
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
            OverrideClosing();
        }

        /// <summary>
        /// Used to override closings and minimize instead
        /// </summary>
        private void OverrideClosing()
        {
            this.Closing += new CancelEventHandler(MainWindow_Closing);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // If the user selected to minimize on close and the window state is normal
            // just minimize the app
            if (App.Instance().Options.MinimizeOnClose && App.Instance().Options.ReallyExit == false)
            {
                e.Cancel = true;
                _storedWindowState = this.WindowState;
                this.WindowState = WindowState.Minimized;
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(2000);
                }
            }
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = show;
        }

        DispatcherTimer hideTimer = new DispatcherTimer();

        void OnStateChanged(object sender, EventArgs args)
        {
            if (App.Instance().Options.MinimizeToTray)
            {
                if (WindowState == WindowState.Minimized)
                {
                    hideTimer.Interval = new TimeSpan(500);
                    hideTimer.Tick += new EventHandler(HideTimer_Elapsed);
                    hideTimer.Start();
                }
                else
                {
                    _storedWindowState = WindowState;
                }
            }
        }

        private void HideTimer_Elapsed(object sender, EventArgs e)
        {
            this.Hide();
            hideTimer.Stop();
        }

        void OnClosed(object sender, EventArgs e)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        void openMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = _storedWindowState;
        }

        void exitMenuItem_Click(object sender, EventArgs e)
        {
            App.Instance().Options.ReallyExit = true;
            this.Close();
        }

        private void ParoleIgnoredUsers()
        {
            /*
            List<string> paroledUsers = new List<string>();
            foreach (KeyValuePair<string, DateTime> ignoredUser in ignoredUsers)
            {
                if (ignoredUser.Value < DateTime.Now.AddHours(6))
                    paroledUsers.Add(ignoredUser.Key);
            }
            paroledUsers.ForEach(userName => ignoredUsers.Remove(userName));
            */
        }

        private void NotifyOnNewTweets(TweetCollection newTweets, string type)
        {

            PopUpNotify(newTweets);
        }

        private void SetupPopupProps(Popup p)
        {
            p.FadeOutFinished += new FadeOutFinishedDelegate(RemovePopup);
            p.ReplyClicked += new PopupReplyClickedDelegate(PopupReplyClicked);
            p.DirectMessageClicked += new PopupDirectMessageClickedDelegate(PopupDirectMessageClicked);
            p.Clicked += new PopupClickedDelegate(PopupClicked);
            p.CloseButtonClicked += new PopupCloseButtonClickedDelegate(RemovePopup);
        }

        private bool ShouldPopUp(Tweet tweet)
        {
            return true;
            /*
            if (AppSettings.AlertSelectedOnly)
                return HasBehavior(tweet, UserBehavior.AlwaysAlert);

            return (!HasBehavior(tweet, UserBehavior.NeverAlert) || HasBehavior(tweet, UserBehavior.Ignore));
            */

        }

        private bool HasBehavior(Tweet tweet, UserBehavior behavior)
        {
            //return AppSettings.UserBehaviorManager.HasBehavior(tweet.User.Name, behavior);
            return false;
        }

        private void PopUpNotify(TweetCollection newTweets)
        {
            TweetCollection popUpTweets = new TweetCollection();

            foreach (var tweet in newTweets)
            {
                if (ShouldPopUp(tweet))
                    popUpTweets.Add(tweet);
            }

            if (popUpTweets.Count > App.Instance().Options.MaximumIndividualAlerts)
            {
                Popup p = new Popup("New Tweets", BuiltNewTweetMessage(popUpTweets),
                    App.Instance().Twitter.CurrentlyLoggedInUser.ImageUrl, 0);
                SetupPopupProps(p);
                p.Show();
            }
            else
            {
                int index = 0;
                foreach (Tweet tweet in popUpTweets)
                {
                    Popup p = new Popup(tweet, index++);
                    SetupPopupProps(p);
                    p.Show();
                }
            }
        }

        private static string BuiltNewTweetMessage(TweetCollection newTweets)
        {
            string message = string.Format("You have {0} new tweets!\n", newTweets.Count);
            foreach (Tweet tweet in newTweets)
            {
                message += " " + tweet.User.ScreenName;
            }
            if (message.Length > TwitterNet.CharacterLimit)
            {
                message = message.Substring(0, (TwitterNet.CharacterLimit - 5));
                int lastSpace = message.LastIndexOf(' ');
                message = message.Substring(0, lastSpace) + "...";
            }
            return TruncateMessage(message);
        }

        private static string TruncateMessage(string message)
        {
            if (message.Length > TwitterNet.CharacterLimit)
            {
                message = message.Substring(0, (TwitterNet.CharacterLimit - 5));
                int lastSpace = message.LastIndexOf(' ');
                message = message.Substring(0, lastSpace) + "...";
            }
            return message;
        }

        private void UpdateExistingTweets()
        {
            UpdateExistingTweets(tweets);
        }

        private static void UpdateExistingTweets(TweetCollection oldTweets)
        {
            // Update existing tweets
            foreach (Tweet tweet in oldTweets)
            {
                tweet.IsNew = false;
                tweet.UpdateRelativeTime();
            }
        }

        #endregion

        #region Popup Event Handlers

        private int popupCount = 0;

        private void RemovePopup(Popup popup)
        {
            popupCount--;
            popup.Close();
            popup = null;
        }

        private void PopupReplyClicked(string screenName)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = _storedWindowState;
            }
            CreateReply(screenName);
        }

        private void CreateReply()
        {
            //reply to user
            if (null != SelectedTweet)
            {
                CreateReply(SelectedTweet.User.ScreenName);
            }
        }

        internal Tweet SelectedTweet
        {
            get
            {
                Tweet selectedTweet = null;
                if (null != TweetsListBox.SelectedItem) selectedTweet = (Tweet)TweetsListBox.SelectedItem;
                /*
                if (this.currentView == CurrentView.Replies)
                {
                    if (null != RepliesListBox.SelectedItem) selectedTweet = (Tweet)RepliesListBox.SelectedItem;
                }
                else if (this.currentView == CurrentView.Messages)
                {
                    if (null != MessagesListBox.SelectedItem) selectedTweet = ((DirectMessage)MessagesListBox.SelectedItem).ToTweet();
                }
                else if (this.currentView == CurrentView.User)
                {
                    if (null != UserTimelineListBox.SelectedItem) selectedTweet = (Tweet)UserTimelineListBox.SelectedItem;
                }
                else
                {
                    if (null != TweetsListBox.SelectedItem) selectedTweet = (Tweet)TweetsListBox.SelectedItem;
                }
                 */
                return selectedTweet;
            }
        }

        private void CreateReply(string screenName)
        {
            /*
            if (!isExpanded)
            {
                this.Tabs.SelectedIndex = 0;
                ToggleUpdate();
            }
            */
            isReplyMessage = true;
            TweetTextBox.Text = string.Empty;
            TweetTextBox.Text = "@" + screenName + " ";
            TweetTextBox.Select(TweetTextBox.Text.Length, 0);
        }

        // booleans to keep track of state
        private bool isExpanded;
        private bool isLoggedIn;
        private bool isReplyMessage;
        private bool isMessageExpanded;
        private bool ignoreKey;
        private bool tweetFormattingMayBeRequired;
        private bool isInAutocompleteMode;

        private void PopupDirectMessageClicked(string screenName)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = _storedWindowState;
            }
            CreateDirectMessage(screenName);
        }

        private void CreateDirectMessage()
        {
            Tweet selectedTweet = SelectedTweet as Tweet;
            if (null != selectedTweet)
            {
                CreateDirectMessage(selectedTweet.User.ScreenName);
            }
        }

        private void CreateDirectMessage(string screenName)
        {
            //Direct message to user
            /*
            if (!isExpanded)
            {
                this.Tabs.SelectedIndex = 0;
                ToggleUpdate();
            }
            */
            TweetTextBox.Text = string.Empty;
            TweetTextBox.Text = "D ";

            TweetTextBox.Text += screenName;
            TweetTextBox.Text += " ";
            MoveTweetTextBoxCursorToEnd();
        }

        private void MoveTweetTextBoxCursorToEnd()
        {
            TweetTextBox.Select(TweetTextBox.Text.Length, 0);
        }

        void PopupClicked(Tweet tweet)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = _storedWindowState;
            }

            if (tweet != null)
            {
                TweetsListBox.ScrollIntoView(tweet);
            }
        }

        #endregion

        // Main collection of tweets
        private TweetCollection tweets = new TweetCollection();

        private void ShowStatus(string status)
        {
            StatusTextBlock.Text = status;
        }

        // How often the automatic tweet updates occur.  TODO: Make this configurable
        private TimeSpan refreshInterval;
        private TimeSpan friendsRefreshInterval = new TimeSpan(0, 45, 0);

        private void TwitterUpdateRateDropDown_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // NOTE that this only grabs the first digit from the text
            string[] bits = TwitterUpdateRateDropDown.SelectedValue.ToString().Split(' ');
            int rate = Convert.ToInt32(bits[2]);
            App.Instance().Options.TwitterUpdateRate = rate;
        }

        /// <summary>
        /// This event is VERY important since it traps errors that happen unexpectedly. This has been unstable
        /// due to the fact that there are actions in the API that don't account for the business rules.  So when 
        /// an action occurs, the program crashes.  This handler traps those errors and logs them.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //TODO: Figure out a better option to do with these unhandled exceptions.  Maybe email them or something?
            //App.Logger.Error("Unhandled exception occurred.", e.Exception);
#if DEBUG
            string error = String.Empty;
            if (e.Exception.InnerException != null)
            {
                error = e.Exception.InnerException.Message;
            }
            else
            {
                error = e.Exception.Message;
            }
            //MessageBox.Show("An unhandled error occurred. See the log for details.\nError: " + error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            e.Handled = true;
        }

        private void ShowTwitterBalloonPopupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            App.Instance().Options.ShowTwitterBalloonPopup = ShowTwitterBalloonPopupCheckBox.IsChecked;
        }

        private void ManageLists_Click(object sender, RoutedEventArgs e)
        {
            App.Instance().ShowListManager();
        }

        internal void UpdateFilter()
        {
            TwitterListsListBox.Items.Clear();
            TwitterListsListBox.Items.Add("All");

            // update the filter list view
            foreach (UserList ul in App.Instance().Options.TwitterUserLists)
            {
                TwitterListsListBox.Items.Add(ul.Name);
            }
        }

        private void TwitterListsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // this changes based on what view (main, replies, etc we're viewing; 
            // as only the main view is implemented, we're just concerning ourselves with it
            if (e.AddedItems.Count > 0)
            {
                string selectedView = "";
                if (TwitterViewsListBox == null)
                {
                    selectedView = "Recent";
                }
                else if (TwitterViewsListBox.SelectedItem is ListBoxItem)
                {
                    selectedView = (TwitterViewsListBox.SelectedItem as ListBoxItem).Content.ToString();
                }
                else
                {
                    selectedView = TwitterListsListBox.SelectedItem.ToString();
                }

                string selectedFilter = "";
                if (TwitterListsListBox == null)
                {
                    selectedFilter = "All";
                }
                else if (TwitterListsListBox.SelectedItem is ListBoxItem)
                {
                    selectedFilter = (TwitterListsListBox.SelectedItem as ListBoxItem).Content.ToString();
                }
                else
                {
                    selectedFilter = TwitterListsListBox.SelectedItem.ToString();
                }

                if (selectedView == null || selectedView.Equals("") || selectedView.Equals("All") || selectedView.Equals("Recent"))
                {
                    FilterTweets(tweets, selectedFilter);
                }
                else if (selectedView == "Replies")
                {
                    if (replies == null || replies.Count == 0)
                    {
                        GetReplies();
                    }
                    FilterTweets(replies, selectedFilter);
                }
                else if (selectedView == "Messages")
                {
                    // DirectMessageCollection messages
                }
                else if (selectedView == "Favorites")
                {

                }
                else if (selectedView == "User")
                {
                    if (userTweets == null || userTweets.Count == 0)
                    {
                        GetUserTimeline(App.Instance().Twitter.CurrentlyLoggedInUser.ScreenName);
                    }
                    FilterTweets(userTweets, selectedFilter);
                }
            }
        }

        /// <summary>
        /// Exit application when this window is closed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event args</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // exit application
            Application.Current.Shutdown();
        }
    }
}