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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Win32;
using System.IO;


namespace FileClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
            _client.BaseAddress = new Uri("https://localhost:7012");
        }

        private async void OnGetFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await _client.GetFromJsonAsync<List<string>>("api/file");
                FilesList.ItemsSource = file;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении файлов:\n" + ex.Message);
            }
        }

        private async void OnUploadFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var fileSream = File.OpenRead(dialog.FileName);
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileSream), "file", System.IO.Path.GetFileName(dialog.FileName));

                try
                {
                    var response = await _client.PostAsync("api/file", content);
                    var result = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Ответ сервера: " + result);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке:\n" + ex.Message);
                }
            }

        }
    }
}
