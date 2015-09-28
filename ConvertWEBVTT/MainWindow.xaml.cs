using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace ConvertWEBVTT
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CbSaveTo.IsChecked = Properties.Settings.Default.SaveToValue;
            TxtEx.Text = Properties.Settings.Default.Ext;
        }

        #region METHODS
        private async Task<string[]> GetString(string path)
        {
            //string[] lines = File.ReadAllLines(path);
            string[] lines = await ReadAllLinesAsync(path, Encoding.UTF8);
            RichTextBoxContent.Document.Blocks.Clear();
            return lines;
        }
        private async Task<string[]> ReadAllLinesAsync(string path, Encoding encoding)
        {
            var lines = new List<string>();

            using (var reader = new StreamReader(path, encoding))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines.ToArray();
        }
        private static string ConvertWebvttToSrt(string webvttContent)
        {
            if (webvttContent == null)
                throw new ArgumentNullException("webvttContent");
            var srtResult = webvttContent;
            var srtPartLineNumber = 0;
            srtResult = Regex.Replace(srtResult, @"(WEBVTT\s+)(\d{2}:)", "$2"); // Removes 'WEBVTT' word
            srtResult = Regex.Replace(srtResult, @"(\d{2}:\d{2}:\d{2})\.(\d{3}\s+)-->(\s+\d{2}:\d{2}:\d{2})\.(\d{3}\s*)", match =>
            {
                srtPartLineNumber++;
                return Convert.ToString(srtPartLineNumber) + Environment.NewLine +
                    Regex.Replace(match.Value, @"(\d{2}:\d{2}:\d{2})\.(\d{3}\s+)-->(\s+\d{2}:\d{2}:\d{2})\.(\d{3}\s*)", "$1,$2-->$3,$4");
                // Writes '00:00:19.620' instead of '00:00:19,620'
            }); // Writes Srt section numbers for each section
            return srtResult;
        }
        #endregion

        #region BUTTON CONVERT EVENT HANDLER
        private async void BtnConvertAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (DropListBox.Items.Count <= 0)
                return;

            var path = string.Empty;
            if (!string.IsNullOrEmpty(TxtFolder.Text) && CbSaveTo.IsChecked == true)
            {
                path = TxtFolder.Text;
            }
            else
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                dlg.RootFolder = Environment.SpecialFolder.MyComputer;
                dlg.ShowDialog();
                path = dlg.SelectedPath;
            }
            foreach (ListBoxItem item in DropListBox.Items)
            {
                string subtitles = string.Join("\r\n", await GetString(item.ToolTip as string));
                var result = ConvertWebvttToSrt(subtitles);
                try
                {
                    var fullPath = path + "\\" + item.Content + ".srt";
                    var sw = new StreamWriter(fullPath);
                    sw.WriteLine(result);
                    sw.Flush();
                    sw.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show("Couldn't find a path");
                    return;
                }
            }
            MessageBox.Show("Success!");
        }

        private void BtnConvert_OnClick(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(
                    // TextPointer to the start of content in the RichTextBox.
                    RichTextBoxContent.Document.ContentStart,
                    // TextPointer to the end of content in the RichTextBox.
                    RichTextBoxContent.Document.ContentEnd
                );

            if (string.IsNullOrEmpty(textRange.Text) || textRange.Text.Length <= 4)
                return;
            var result = ConvertWebvttToSrt(textRange.Text);



            FileDialog sfd = new SaveFileDialog();
            sfd.Filter = "SRT Files|*.srt";
            if (!String.IsNullOrEmpty(TxtFolder.Text) && CbSaveTo.IsChecked == true)
                sfd.InitialDirectory = TxtFolder.Text;
            var res = sfd.ShowDialog();
            if (res == false)
            {
                return;
            }
            var fname = sfd.FileName;
            MessageBox.Show(fname);

            var sw = new StreamWriter(fname);
            sw.WriteLine(result);
            sw.Flush();
            sw.Close();
            RichTextBoxContent.Document.Blocks.Clear();
            RichTextBoxContent.Document.Blocks.Add(new Paragraph(new Run("Success!")));
        }
        #endregion

        #region LISTBOX EVENT HANDLER       
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //DropListBox.Items.Clear();
                string[] droppedFilePaths =
                    e.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (string droppedFilePath in droppedFilePaths)
                {
                    ListBoxItem fileItem = new ListBoxItem();
                    fileItem.Content = Path.GetFileNameWithoutExtension(droppedFilePath);
                    fileItem.ToolTip = droppedFilePath;
                    DropListBox.Items.Add(fileItem);
                }
            }
        }

        private async void DropListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = DropListBox.SelectedItem as ListBoxItem;
                var path = item.ToolTip as string;
                ////string[] lines = File.ReadAllLines(path);
                //string[] lines = await ReadAllLinesAsync(path, Encoding.UTF8);

                var subtitles = await GetString(path);
                RichTextBoxContent.Document.Blocks.Clear();
                foreach (var line in subtitles)
                {
                    RichTextBoxContent.Document.Blocks.Add(new Paragraph(new Run(line)));
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't working on it...");
                //DropListBox.Items.Remove(DropListBox.SelectedItem);
            }


        }
        #endregion

        #region BROWSE EVENT HANDLER

        private void BrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "WebVTT files (*.webvtt)|*.webvtt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    ListBoxItem fileItem = new ListBoxItem();
                    fileItem.Content = Path.GetFileNameWithoutExtension(filename);
                    fileItem.ToolTip = filename;
                    DropListBox.Items.Add(fileItem);
                }
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            try
            {
                if (!String.IsNullOrEmpty(TxtFolder.Text))
                {
                    dlg.SelectedPath = TxtFolder.Text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            dlg.ShowDialog();
            TxtFolder.Text = dlg.SelectedPath;
        }
        #endregion

        #region CHECKED EVENT HANDLER
        private void SaveTo_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SaveToValue = true;
            Properties.Settings.Default.Save();
        }

        private void SaveTo_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SaveToValue = false;
            Properties.Settings.Default.Save();
        }
        #endregion

        private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }
        private void BtnGetByLink_Click(object sender, RoutedEventArgs e)
        {
            GridGetLink.Visibility = Visibility.Visible;
        }
        private void BtnGet_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TxtLink.Text))
            {
                WebClient web = new WebClient();
                Stream stream = web.OpenRead(TxtLink.Text + Properties.Settings.Default.Ext);
                using (StreamReader reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    if (text.Contains("WEBVTT"))
                    {
                        RichTextBoxContent.Document.Blocks.Clear();
                        RichTextBoxContent.Document.Blocks.Add(new Paragraph(new Run(text)));
                    }
                    else
                    {
                        RichTextBoxContent.Document.Blocks.Clear();
                        MessageBox.Show("Subtitles not found!");
                    }
                }
            }

        }
        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            DropListBox.Items.Clear();
        }
    }
}
