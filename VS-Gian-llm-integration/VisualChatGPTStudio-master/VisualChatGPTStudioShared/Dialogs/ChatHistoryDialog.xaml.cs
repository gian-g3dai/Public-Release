using Community.VisualStudio.Toolkit;
using Unakin.Options;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Media;
using UnakinShared.Utils;
using OpenAI_API.Chat;
using System.Collections.ObjectModel;
using UnakinShared.DTO;
using System.IO;
using Microsoft.Win32;
using UnakinShared.Models;
using System.Xml;

namespace Unakin.ToolWindows
{
    public partial class ChatHistoryDialog : Window
    {
        List<ChatHistoryDTO> chatHistoryDTOs;
        public int Id { get; set; }
        public int ChatType { get; set; }
        public ChatHistoryDialog()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public async void ChatListLoad()
        {
            var masterList = await AppDatabase.database.Table<ChatMaster>().Where(x=>x.ChatType==this.ChatType).OrderByDescending(x => x.UpdatedTime).ToListAsync();
            chatHistoryDTOs = new List<ChatHistoryDTO>();

            foreach (var item in masterList)
            {
                chatHistoryDTOs.Add(new ChatHistoryDTO { Id = item.Id, Name = item.Desc, LastUpdated = item.UpdatedTime.Date.ToString("D") });
            }
            lvHistory.ItemsSource = chatHistoryDTOs;
            lvHistory.Items.Refresh();
        }

        void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            ChatHistoryDTO item = ((ListViewItem)sender).Content as ChatHistoryDTO;

            if (item == null)
            {
                return;
            }

            this.Id= item.Id;
            this.DialogResult= true;
        }

    }
}
