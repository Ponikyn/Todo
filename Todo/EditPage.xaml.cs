using Todo.Models;
using Todo.Services;

namespace Todo
{
    public partial class EditPage : ContentPage
    {
        TodoItem item;
        Action onSaved;
        List<TodoItem> allItems;

        ITodoService todoService = new TodoService();

        public EditPage(TodoItem? existing, Action onSaved)
        {
            InitializeComponent();
            this.onSaved = onSaved;

            if (existing is null)
            {
                item = new TodoItem();
                DeleteBtn.IsVisible = false;
                // ToolbarItem has no IsVisible property; add/remove it instead
                if (ToolbarItems.Contains(DeleteToolbar))
                    ToolbarItems.Remove(DeleteToolbar);

                Title = "New Task";
            }
            else
            {
                item = new TodoItem
                {
                    Id = existing.Id,
                    Title = existing.Title,
                    Description = existing.Description,
                    IsCompleted = existing.IsCompleted
                };
                DeleteBtn.IsVisible = true;
                if (!ToolbarItems.Contains(DeleteToolbar))
                    ToolbarItems.Add(DeleteToolbar);

                Title = "Edit Task";
            }

            TitleEntry.Text = item.Title;
            DescriptionEditor.Text = item.Description;
        }

        async void OnSaveClicked(object? sender, EventArgs e)
        {
            item.Title = TitleEntry.Text ?? string.Empty;
            item.Description = DescriptionEditor.Text ?? string.Empty;

            allItems = await todoService.GetAll();
            var idx = allItems.FindIndex(i => i.Id == item.Id);
            if (idx >= 0)
                allItems[idx] = item;
            else
                allItems.Add(item);

            await todoService.SaveAll(allItems);
            onSaved?.Invoke();
            await Navigation.PopAsync();
        }

        async void OnDeleteClicked(object? sender, EventArgs e)
        {
            var confirmed = await DisplayAlert("Delete", "Delete task?", "Yes", "No");
            if (!confirmed) return;

            allItems = await todoService.GetAll();
            allItems.RemoveAll(i => i.Id == item.Id);
            await todoService.SaveAll(allItems);
            onSaved?.Invoke();
            await Navigation.PopAsync();
        }
    }
}
