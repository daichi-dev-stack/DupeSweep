using DupeSweep.Core;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DupeSweep
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<FileItem> _items = [];

        public MainWindow()
        {
            InitializeComponent();

            btnScan.Click += BtnScan_Click;
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            btnScan.IsEnabled = false;

            var path = txtFolder.Text;

            var scanner = new DuplicateScanner();

            _items = await Task.Run(() =>
                scanner.Scan(path,
                    p =>
                    {
                        if ((p % 10) == 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = p;
                                txtStatus.Text = $"SHA256計算中 {p}%";
                            });
                        }
                    }));

            progressBar.Value = 100;

            gridFiles.ItemsSource = _items;

            UpdateStatus();

            btnScan.IsEnabled = true;
        }

        private void UpdateStatus()
        {
            txtDuplicateCount.Text = $"重複:{_items.Count}件";
            txtSelectedCount.Text = $"選択:{_items.Count(x => x.IsSelected)}件";
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFolder.Text = dialog.SelectedPath;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var targets = _items.Where(x => x.IsSelected).ToList();

            // チェックが無ければ選択行
            if (targets.Count == 0)
            {
                if (gridFiles.SelectedItem is FileItem item)
                {
                    targets.Add(item);
                }
            }

            if (targets.Count == 0)
            {
                System.Windows.MessageBox.Show("削除対象が選択されていません。");
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"{targets.Count}件を削除しますか？",
                "確認",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            progressBar.Value = 0;
            txtStatus.Text = "削除中...";

            List<string> errors = [];

            await Task.Run(() =>
            {
                int total = targets.Count;
                int current = 0;

                foreach (var item in targets)
                {
                    if (!MoveToTrash(item, out var error))
                    {
                        errors.Add($"{item.FileName}: {error}");
                    }

                    current++;

                    int percent = current * 100 / total;

                    if ((current % 10) == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = percent;
                            txtStatus.Text = $"削除中 {current}/{total}";
                        });
                    }
                }
            });

            if (errors.Count > 0)
            {
                System.Windows.MessageBox.Show($"{errors.Count}件失敗しました");
            }

            progressBar.Value = 100;

            txtStatus.Text = $"完了 {targets.Count}件移動";

            var targetSet = targets.ToHashSet();
            _items.RemoveAll(x => targetSet.Contains(x));

            _items = _items
                .GroupBy(x => x.Hash)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            gridFiles.ItemsSource = null;
            gridFiles.ItemsSource = _items;

            UpdateStatus();
        }

        private static bool MoveToTrash(FileItem item, out string error)
        {
            try
            {
                // 移動
                string destDir = @"C:\Temp\DuplicateTrash";
                Directory.CreateDirectory(destDir);

                string destFile = MakeUniqueFileName(destDir, System.IO.Path.GetFileName(item.FullPath));

                System.IO.File.Move(item.FullPath, destFile);

                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// ユニーク名を返す
        /// </summary>
        /// <param name="destDir">転送先フォルダ</param>
        /// <param name="fileName">転送したいファイル</param>
        /// <returns></returns>
        private static string MakeUniqueFileName(string destDir, string fileName)
        {
            string path = System.IO.Path.Combine(destDir, fileName);

            if (!System.IO.File.Exists(path))
            {
                return path;
            }

            string name = System.IO.Path.GetFileNameWithoutExtension(fileName);

            string ext = System.IO.Path.GetExtension(fileName);

            int index = 1;

            while (true)
            {
                path = System.IO.Path.Combine(destDir, $"{name}({index}){ext}");
                if (!System.IO.File.Exists(path))
                {
                    return path;
                }
                index++;
            }
        }

        private void BtnSelectDuplicate_Click(object sender, EventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = false;
            }

            var groups = _items.GroupBy(x => x.Hash);

            foreach (var group in groups)
            {
                var keep = group.OrderByDescending(x => File.GetLastWriteTime(x.FullPath)).First();
                foreach (var item in group)
                {
                    item.IsSelected = item != keep;
                }
            }

            gridFiles.Items.Refresh();

            UpdateStatus();
        }

        private void BtnClearSelection_Click(object sender, EventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = false;
            }

            gridFiles.Items.Refresh();

            UpdateStatus();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }
    }
}