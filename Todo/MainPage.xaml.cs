using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Todo.Models;
using Todo.Services;

namespace Todo
{
    public partial class MainPage : ContentPage
    {
        List<TodoItem> allItems = new();
        ObservableCollection<TodoItem> visibleItems = new();
        ITodoService todoService = new TodoService();

        public MainPage()
        {
            try
            {
                InitializeComponent();
                // set the ItemsSource once to an observable collection to avoid swapping the source and triggering layout issues
                TasksCollection.ItemsSource = visibleItems;
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

                // update observable collection on UI thread to avoid collection modified exceptions
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        visibleItems.Clear();
                        foreach (var it in list)
                            visibleItems.Add(it);

                        if (EmptyLabel != null)
                            EmptyLabel.IsVisible = list.Count == 0;
                    }
                    catch (Exception ex)
                    {
                        // fallback: display error
                        Content = new ScrollView
                        {
                            Content = new Label { Text = $"ApplyFilter UI update error:\n{ex}", TextColor = Colors.Red }
                        };
                    }
                });
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

        async void OnItemTapped(object? sender, EventArgs e)
        {
            try
            {
                if (sender is VisualElement ve && ve.BindingContext is TodoItem item)
                {
                    await Navigation.PushAsync(new EditPage(item, OnItemSaved));
                }
                else if (sender is TapGestureRecognizer tr && tr.CommandParameter is TodoItem itemParam)
                {
                    await Navigation.PushAsync(new EditPage(itemParam, OnItemSaved));
                }
            }
            catch (Exception ex)
            {
                Content = new ScrollView
                {
                    Content = new Label { Text = $"OnItemTapped error:\n{ex}", TextColor = Colors.Red }
                };
            }
        }

        async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
        {
            // no longer used; kept for compatibility
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

        async void OnCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
        {
            try
            {
                if (sender is CheckBox cb && cb.BindingContext is TodoItem item)
                {
                    // prevent tap gestures from acting on checkbox taps
                    cb.InputTransparent = false;

                    item.IsCompleted = e.Value;
                    // update in allItems
                    var idx = allItems.FindIndex(i => i.Id == item.Id);
                    if (idx >= 0)
                        allItems[idx] = item;
                    else
                        allItems.Add(item);

                    await todoService.SaveAll(allItems);
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
