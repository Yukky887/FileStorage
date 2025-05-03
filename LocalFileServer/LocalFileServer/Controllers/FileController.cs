using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;


namespace LocalFileServer.Controllers
{
    [ApiController] // Указывает, что это Web API-контроллер
    [Route("api/[controller]")] // Адрес будет: api/file
    public class FileController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            string path = "D:\\ProgektsVS";

            if (!Directory.Exists(path))
            {
                return new List<string>{"Нет такой папки"};
            }

            var files = Directory.GetFiles(path);
            return files.Select(Path.GetFileName);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не передан или он пуст");
            }

            string uploadPath = "D:\\ProgektsVS";

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok("Файл " + file.FileName + " загружен!");
        }
    }
}
