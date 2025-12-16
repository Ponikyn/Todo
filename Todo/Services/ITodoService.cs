using System.Collections.Generic;
using System.Threading.Tasks;
using Todo.Models;

namespace Todo.Services
{
    public interface ITodoService
    {
        Task<List<TodoItem>> GetAll();
        Task SaveAll(List<TodoItem> items);
    }
}
