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
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // show error on the page so startup failures are visible
                Content = new ScrollView
                {
                    Content = new Label { Text = $"MainPage.InitializeComponent error:\n{ex}", TextColor = Colors.Red }
                };
                return;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                allItems = await todoService.GetAll();
                // guard UI elements in case XAML failed to initialize
                if (FilterPicker != null)
                {
                    if (FilterPicker.SelectedIndex < 0)
                        FilterPicker.SelectedIndex = 0;
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"MainPage.OnAppearing error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        void ApplyFilter()
        {
            try
            {
                int idx = 0;
                if (FilterPicker != null)
                    idx = FilterPicker.SelectedIndex >= 0 ? FilterPicker.SelectedIndex : 0;

                IEnumerable<TodoItem> items = allItems;
                if (idx == 0) // current
                    items = allItems.Where(i => !i.IsCompleted);
                else if (idx == 1) // completed
                    items = allItems.Where(i => i.IsCompleted);

                var list = items.ToList();

                if (TasksCollection != null)
                    TasksCollection.ItemsSource = list;

                if (EmptyLabel != null)
                    EmptyLabel.IsVisible = list.Count == 0;
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"ApplyFilter error:\n{ex}", TextColor = Colors.Red }
                };
            }
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
            try
            {
                var item = e.CurrentSelection.FirstOrDefault() as TodoItem;
                if (item == null) return;
                // deselect
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new EditPage(item, OnItemSaved));
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnTaskSelected error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        async void OnItemSaved()
        {
            try
            {
                allItems = await todoService.GetAll();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnItemSaved error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        void OnCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnCheckBoxChanged error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        async void OnEditInvoked(object? sender, EventArgs e)
        {
            try
            {
                TodoItem? item = null;
                if (sender is SwipeItem si && si.CommandParameter is TodoItem tp)
                    item = tp;

                // fallback: try to get bound item from visual tree
                if (item == null && sender is SwipeItem sip && sip.Parent is SwipeItems sis && sis.Parent is SwipeView sv)
                {
                    if (sv.Content is VisualElement ve && ve.BindingContext is TodoItem bc)
                        item = bc;
                }

                if (item != null)
                    await Navigation.PushAsync(new EditPage(item, OnItemSaved));
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnEditInvoked error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        async void OnDeleteInvoked(object? sender, EventArgs e)
        {
            try
            {
                TodoItem? item = null;
                if (sender is SwipeItem si && si.CommandParameter is TodoItem tp)
                    item = tp;

                if (item == null && sender is SwipeItem sip && sip.Parent is SwipeItems sis && sis.Parent is SwipeView sv)
                {
                    if (sv.Content is VisualElement ve && ve.BindingContext is TodoItem bc)
                        item = bc;
                }

                if (item != null)
                {
                    var confirmed = await DisplayAlert("Удалить","Удалить задачу?","Да","Нет");
                    if (!confirmed) return;

                    allItems.RemoveAll(i => i.Id == item.Id);
                    await todoService.SaveAll(allItems);
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnDeleteInvoked error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }
    }
}
