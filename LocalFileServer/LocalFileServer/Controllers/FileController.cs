﻿using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;


namespace LocalFileServer.Controllers
{
    [ApiController] // Указывает, что это Web API-контроллер
    [Route("api/[controller]")] // Адрес будет: api/file
    public class FileController : ControllerBase
    {

        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "D:\\ProgektsVS");

        [HttpGet]
        public IEnumerable<FileItem> Get()
        {
            string path = "D:\\ProgektsVS";

            if (!Directory.Exists(path))
            {
                return new List<FileItem>();
            }

            var files = Directory.GetFiles(path);

            return files.Select(filePath =>
            {
                var info = new FileInfo(filePath);
                return new FileItem
                {
                    Name = info.Name,
                    Size = info.Length,
                    LastModified = info.LastWriteTime
                };
            });
        }


        [HttpPost("multiple")]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Файлы не выбраны");

            var uploadPath = "D:\\ProgektsVS";
            Directory.CreateDirectory(uploadPath);
            var results = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var safeName = Path.GetFileName(file.FileName);
                    var savePath = Path.Combine(uploadPath, safeName);

                    using var stream = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    results.Add($"Загружен: {safeName}");
                }
                catch (Exception ex)
                {
                    results.Add($"Ошибка: {file.FileName} — {ex.Message}");
                }
            }

            return Ok(results);
        }


        [HttpGet("download")]
        public IActionResult Download([FromQuery] string name)
        {

            Console.WriteLine($"Запрошен файл: {name}");

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Имя файла не указано");
            }

            string path = Path.Combine("D:\\ProgektsVS", name);

            if (!System.IO.File.Exists(path))
            {
                return NotFound($"Файл не найден: {path}");
            }

            var fileBytes = System.IO.File.ReadAllBytes(path);
            var contentType = "application/octet-stream";
            return File(fileBytes, contentType, name);
        }

        [HttpPost("download-multiple")]
        public IActionResult DownloadMultipleFiles([FromBody] List<FileItem> fileNames)
        {
            if (fileNames == null || fileNames.Count == 0)
                return BadRequest("Список файлов пуст или не передан.");

            var filesPath = "D:\\ProgektsVS";

            using var memoryStream = new MemoryStream();
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var item in fileNames)
                {
                    var safeName = Path.GetFileName(item.Name);
                    var filePath = Path.Combine(filesPath, safeName);

                    if (!System.IO.File.Exists(filePath))
                        continue; // можно логировать пропущенные файлы

                    var entry = zip.CreateEntry(safeName);

                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    fileStream.CopyTo(entryStream);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return File(memoryStream.ToArray(), "application/zip", "files.zip");
        }

        [HttpDelete]
        public IActionResult Delete([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Имя файла не указано");
            }

            string path = Path.Combine("D:\\ProgektsVS", name);

            try
            {
                System.IO.File.Delete(path);
                return Ok("Файл удален: " + name);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Ошибка при удалении: " + ex.Message);
            }
        }

        [HttpDelete("multiple")]
        public IActionResult DeleteMultiple([FromBody] List<FileItem> names)
        {
            if (names == null || names.Count == 0)
            {
                return BadRequest("Список файлов пуст.");
            }


            int deletedCount = 0;


            foreach (var item in names)
            {
                var safeName = Path.GetFileName(item.Name);
                var path = Path.Combine("D:\\ProgektsVS", safeName);

                Console.WriteLine($"Путь к удаляемому файлу: {path}");
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    deletedCount++;
                }
            }
           

            return Ok($"Удалено файлов: {deletedCount} из {names.Count}");
           

        }

    }
}
