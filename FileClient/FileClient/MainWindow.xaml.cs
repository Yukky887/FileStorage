using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace FileClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            _client.BaseAddress = new Uri("http://178.141.247.15:5125");
            RefreshFileList(); // Загружаем список при запуске
        }

        private async void RefreshFileList()
        {
            try
            {
                var files = await _client.GetFromJsonAsync<List<FileItem>>("api/file");
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
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var content = new MultipartFormDataContent();

                foreach (var filePath in dialog.FileNames)
                {
                    var stream = File.OpenRead(filePath);
                    var fileName = System.IO.Path.GetFileName(filePath);
                    content.Add(new StreamContent(stream), "files", fileName);
                }

                try
                {
                    var response = await _client.PostAsync("api/file/multiple", content);
                    var resultJson = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var messages = JsonSerializer.Deserialize<List<string>>(resultJson);
                        MessageBox.Show(string.Join("\n", messages));
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Ошибка при разборе ответа:\n" + ex.Message + "\nСырой ответ:\n" + resultJson);
                    }
                    

                    var files = await _client.GetFromJsonAsync<List<string>>("api/file");
                    FilesList.ItemsSource = files;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке:\n" + ex.Message);
                }
            }
        }

        private async void OnDownloadFileClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = FilesList.SelectedItems.Cast<FileItem>().ToList();
            var fileNames = selectedItems.Select(f => f.Name).ToList();
            if (!selectedItems.Any())
            {
                MessageBox.Show("Выберите один или несколько файлов из списка.");
                return;
            }

            // Если выбрано несколько файлов или включена галочка — архивируем
            if (selectedItems.Count > 1 || DownloadAsZipCheckBox.IsChecked == true)
            {
                var dialog = new SaveFileDialog
                {
                    FileName = "files.zip",
                    Filter = "ZIP Archive (*.zip)|*.zip"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        var content = JsonContent.Create(selectedItems);
                        var response = await _client.PostAsync("api/file/download-multiple", content);

                        if (response.IsSuccessStatusCode)
                        {
                            var zipBytes = await response.Content.ReadAsByteArrayAsync();
                            File.WriteAllBytes(dialog.FileName, zipBytes);
                            MessageBox.Show("ZIP-файл сохранён.");
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            MessageBox.Show("Ошибка: " + error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при скачивании:\n" + ex.Message);
                    }
                }
            }
            else
            {
                // Обычное скачивание одного файла
                var fileName = selectedItems.First().Name;
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
                            MessageBox.Show("Файл сохранён.");
                        }
                        else
                        {
                            var msg = await response.Content.ReadAsStringAsync();
                            MessageBox.Show("Ошибка: " + msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при скачивании:\n" + ex.Message);
                    }
                }
            }
        }

        private async void OnDeleteFileClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = FilesList.SelectedItems.Cast<FileItem>().ToList();

            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один файл для удаления.");
                return;
            }

            var confirm = MessageBox.Show($"Удалить выбранные файлы ({selectedItems.Count})?", "Подтверждение", MessageBoxButton.YesNo);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                var response = await _client.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(_client.BaseAddress, "api/file/multiple"),
                    Content = JsonContent.Create(selectedItems)
                });

                var message = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(message);
                    var files = await _client.GetFromJsonAsync<List<FileItem>>("api/file");
                    FilesList.ItemsSource = files;
                }
                else
                {
                    MessageBox.Show("Ошибка: " + message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении:\n" + ex.Message);
            }
        }

        private void FilesList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private async void FileList_Drop(object sender, DragEventArgs e)
        {
            if(!e.Data.GetDataPresent(DataFormats.FileDrop)) { return; }

            string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            var content = new MultipartFormDataContent();

            foreach (var filePath in droppedFiles)
            {
                var fileName = System.IO.Path.GetFileName(filePath);
                var bytes = await File.ReadAllBytesAsync(filePath); // загрузка в память
                var byteContent = new ByteArrayContent(bytes);
                content.Add(byteContent, "files", fileName);
            }
            try
            {
                var response = await _client.PostAsync("api/file/multiple", content);
                var result = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Ответ сервера: " + result);

                RefreshFileList();
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Ошибка при загрузке файлов:\n" + ex.Message);
            }
        }

    }
}
