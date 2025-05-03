using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using Microsoft.Win32;

namespace FileClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            _client.BaseAddress = new Uri("http://localhost:5125");
            RefreshFileList(); // Загружаем список при запуске
        }

        private async void RefreshFileList()
        {
            try
            {
                var files = await _client.GetFromJsonAsync<List<string>>("api/file");
                FilesList.ItemsSource = files;
                StatusText.Text = "Список файлов обновлён.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка при получении списка файлов.";
                MessageBox.Show("Ошибка:\n" + ex.Message);
            }
        }

        private void OnGetFileClick(object sender, RoutedEventArgs e) => RefreshFileList();

        private async void OnUploadFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var stream = File.OpenRead(dialog.FileName);
                    var content = new MultipartFormDataContent();
                    content.Add(new StreamContent(stream), "file", Path.GetFileName(dialog.FileName));

                    var response = await _client.PostAsync("api/file", content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        StatusText.Text = $"Файл \"{Path.GetFileName(dialog.FileName)}\" загружен.";
                        RefreshFileList();
                    }
                    else
                    {
                        StatusText.Text = "Ошибка загрузки.";
                        MessageBox.Show("Ошибка: " + result);
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = "Ошибка при загрузке файла.";
                    MessageBox.Show("Ошибка:\n" + ex.Message);
                }
            }
        }

        private async void OnDownloadFileClick(object sender, RoutedEventArgs e)
        {
            if (FilesList.SelectedItem is not string fileName)
            {
                MessageBox.Show("Выберите файл из списка.");
                return;
            }

            var dialog = new SaveFileDialog { FileName = fileName };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var response = await _client.GetAsync($"api/file/download?name={Uri.EscapeDataString(fileName)}");

                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        File.WriteAllBytes(dialog.FileName, bytes);
                        StatusText.Text = $"Файл \"{fileName}\" сохранён.";
                    }
                    else
                    {
                        var msg = await response.Content.ReadAsStringAsync();
                        StatusText.Text = "Ошибка при скачивании.";
                        MessageBox.Show("Ошибка: " + msg);
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = "Ошибка при скачивании файла.";
                    MessageBox.Show("Ошибка:\n" + ex.Message);
                }
            }
        }

        private async void OnDeleteFileClick(object sender, RoutedEventArgs e)
        {
            if (FilesList.SelectedItem is not string fileName)
            {
                MessageBox.Show("Выберите файл из списка.");
                return;
            }

            var result = MessageBox.Show($"Удалить файл \"{fileName}\"?", "Подтверждение", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var response = await _client.DeleteAsync($"api/file?name={Uri.EscapeDataString(fileName)}");
                var message = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    StatusText.Text = $"Файл \"{fileName}\" удалён.";
                    RefreshFileList();
                }
                else
                {
                    StatusText.Text = "Ошибка при удалении.";
                    MessageBox.Show("Ошибка: " + message);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка при удалении файла.";
                MessageBox.Show("Ошибка:\n" + ex.Message);
            }
        }
    }
}
