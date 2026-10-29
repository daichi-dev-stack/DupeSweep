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
            var path = txtFolder.Text;

            var scanner = new DuplicateScanner();

            _items = await Task.Run(() =>
                scanner.Scan(path,
                    p =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = p;
                            txtStatus.Text = $"SHA256計算中 {p}%";
                        });
                    }));

            gridFiles.ItemsSource = _items;

            txtStatus.Text = $"{_items.Count} 件";
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (gridFiles.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("ファイルを選択してください");
                return;
            }

            var item = (FileItem)gridFiles.SelectedItem;

            // 移動
            string destDir = @"C:\Temp\DuplicateTrash";
            Directory.CreateDirectory(destDir);

            var result = System.Windows.MessageBox.Show(
                $"選択したファイルを\n\n" +
                $"{destDir}\n\n" +
                $"へ移動しますか？\n\n" +
                item.FullPath,
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string destFile = MakeUniqueFileName(destDir, item.FullPath);

            System.IO.File.Move(item.FullPath, destFile);

            System.Windows.MessageBox.Show("移動しました");

            // 表示更新
            _items.Remove(item);
            gridFiles.ItemsSource = null;
            gridFiles.ItemsSource = _items;
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
    }
}