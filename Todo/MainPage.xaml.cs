using System;
using System.Collections.Generic;
using System.Linq;
using Todo.Models;
using Todo.Services;

namespace Todo
{
    public partial class MainPage : ContentPage
    {
        List<TodoItem> allItems = new();
        ITodoService todoService = new TodoService();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            allItems = await todoService.GetAll();
            if (FilterPicker.SelectedIndex < 0)
                FilterPicker.SelectedIndex = 0;
            ApplyFilter();
        }

        void ApplyFilter()
        {
            var idx = FilterPicker.SelectedIndex;
            IEnumerable<TodoItem> items = allItems;
            if (idx == 0) // current
                items = allItems.Where(i => !i.IsCompleted);
            else if (idx == 1) // completed
                items = allItems.Where(i => i.IsCompleted);

            var list = items.ToList();
            TasksCollection.ItemsSource = list;
            EmptyLabel.IsVisible = list.Count == 0;
        }

        void OnFilterChanged(object? sender, EventArgs e)
        {
            ApplyFilter();
        }

        async void OnAddClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditPage(null, OnItemSaved));
        }

        async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
        {
            var item = e.CurrentSelection.FirstOrDefault() as TodoItem;
            if (item == null) return;
            // deselect
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new EditPage(item, OnItemSaved));
        }

        async void OnItemSaved()
        {
            allItems = await todoService.GetAll();
            ApplyFilter();
        }

        void OnCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is TodoItem item)
            {
                item.IsCompleted = e.Value;
                // ensure item exists in list
                var idx = allItems.FindIndex(i => i.Id == item.Id);
                if (idx >= 0)
                    allItems[idx] = item;
                else
                    allItems.Add(item);

                _ = todoService.SaveAll(allItems);
                ApplyFilter();
            }
        }
    }
}
