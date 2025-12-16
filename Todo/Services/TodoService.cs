using System.Text.Json;
using Todo.Models;

namespace Todo.Services
{
    public class TodoService : ITodoService
    {
        const string FileName = "todos.json";

        string GetPath()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(folder, FileName);
        }

        public async Task<List<TodoItem>> GetAll()
        {
            try
            {
                var path = GetPath();
                if (!File.Exists(path))
                    return new List<TodoItem>();

                var text = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<TodoItem>>(text) ?? new List<TodoItem>();
            }
            catch
            {
                return new List<TodoItem>();
            }
        }

        public async Task SaveAll(List<TodoItem> items)
        {
            var path = GetPath();
            var text = JsonSerializer.Serialize(items);
            await File.WriteAllTextAsync(path, text);
        }
    }
}
