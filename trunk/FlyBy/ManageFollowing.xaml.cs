using System.Windows;
using System.Collections.Generic;
using System;
using System.Windows.Controls;
using TwitterLib;

namespace FlyBy
{
    public partial class ManageFollowing
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public ManageFollowing()
        {
            this.InitializeComponent();
        }


        /// <summary>
        /// Repopulate the combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TwitterListComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // clear the current items
            TwitterListComboBox.Items.Clear();

            // if they removed the "High Priority" list, put it back
            App.Instance().Options.AddUserList("High Priority");

            // otherwise the user can choose from any available user
            foreach (UserList listLine in App.Instance().Options.TwitterUserLists)
            {
                TwitterListComboBox.Items.Add(listLine.Name);
                if (TwitterListComboBox.SelectedItem == null)
                {
                    TwitterListComboBox.SelectedIndex = 0;
                }
            }
        }

        private void NewList_Click(object sender, EventArgs e)
        {
            TwitterListNameLabel.Visibility = Visibility.Visible;
            TwitterListNameBox.Visibility = Visibility.Visible;
            TwitterAddListButton.Visibility = Visibility.Visible;
        }

        private void TwitterListNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TwitterListNameBox.Text != "")
            {
                TwitterListNameLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                TwitterListNameLabel.Visibility = Visibility.Visible;
            }
        }

        private void Twitter_AddList_Click(object sender, EventArgs e)
        {
            if (TwitterListNameBox.Text != "")
            {
                App.Instance().Options.TwitterUserLists.Add(new UserList(TwitterListNameBox.Text));

                TwitterListComboBox.SelectedIndex = TwitterListComboBox.Items.Count - 1;
            }

            TwitterListNameLabel.Visibility = Visibility.Collapsed;
            TwitterListNameBox.Visibility = Visibility.Collapsed;
            TwitterAddListButton.Visibility = Visibility.Collapsed;
        }

        private void Twitter_DeleteList_Click(object sender, EventArgs e)
        {
            // delete the currently selected list
            if (TwitterListComboBox.SelectedItem != null)
            {
                App.Instance().Options.RemoveUserList(TwitterListComboBox.SelectedItem.ToString());

                TwitterListComboBox.SelectedIndex = 0;
            }
        }

        private void TwitterListComboBox_SelectionChanged(object sender, EventArgs e)
        {
            if(TwitterListComboBox.SelectedItem != null)
            {
                // populate the list of users you follow
                UserCollection uc = App.Instance().Twitter.GetFriends();
                FollowingListBox.Items.Clear();
                foreach (User u in uc)
                {
                    FollowingListBox.Items.Add(u.ScreenName);
                }

                // populate the userlist list
                // subtract out the people in the userlist list from the followers list
                UserList ul = App.Instance().Options.FindUserList(TwitterListComboBox.SelectedItem.ToString());
                ListMembersListBox.Items.Clear();
                foreach (string str in ul.UserArray)
                {
                    ListMembersListBox.Items.Add(str);
                    FollowingListBox.Items.Remove(str);
                }
            }
        }

        private void MoveSelectedUsersToList(object sender, EventArgs e)
        {
            // move the selected users from the 'friends' list to the 'list' list
            foreach (object o in FollowingListBox.SelectedItems)
            {
                ListMembersListBox.Items.Add(o.ToString());
                FollowingListBox.Items.Remove(o.ToString());

                MoveSelectedUsersToList(sender, e);
                break;
            }
            
            SerializeThisList();
        }

        private void MoveSelectedUsersFromList(object sender, EventArgs e)
        {
            foreach (object o in ListMembersListBox.SelectedItems)
            {
                FollowingListBox.Items.Add(o.ToString());
                ListMembersListBox.Items.Remove(o.ToString());

                MoveSelectedUsersFromList(sender, e);
                break;
            }

            SerializeThisList();
        }

        private void SerializeThisList()
        {
            // update it
            List<string> temp = new List<string>();
            foreach (object o in ListMembersListBox.Items)
            {
                temp.Add(o.ToString());
            }
            App.Instance().Options.UpdateUserList(TwitterListComboBox.SelectedItem.ToString(), temp.ToArray());
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}