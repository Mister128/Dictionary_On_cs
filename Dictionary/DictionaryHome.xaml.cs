using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;

namespace Dictionary
{
    /// <summary>
    /// Логика взаимодействия для DictionaryHome.xaml
    /// </summary>
    public partial class DictionaryHome : Page
    {
        public DictionaryHome()
        {
            InitializeComponent();
            var navWindow = Window.GetWindow(this) as NavigationWindow;
            if (navWindow != null) navWindow.ShowsNavigationUI = false;
            LoadWord();
        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(AddWord.Text))
            {
                AddWordInDB(AddWord.Text.Trim());
            }
        }
        private void AddClick(object sender, RoutedEventArgs e)
        {   
            if (!string.IsNullOrWhiteSpace(AddWord.Text))
            {
                AddWordInDB(AddWord.Text.Trim());
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var parentBorder = button.Tag as Border;
            var id = parentBorder.Tag.ToString().Split("/", count: 2)[0];
            using (var conn = new SqliteConnection("Data Source=Dictionary.db"))
            {
                conn.Open();
                SqliteCommand delete = new SqliteCommand($"DELETE FROM dictionary WHERE id={id}", conn);
                delete.ExecuteNonQuery();
                conn.Close();
            }
            WordsList.Children.Remove(parentBorder);
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tuple = button.Tag as Tuple<DockPanel, string>;

            var dockPanel = tuple.Item1;
            var textToEdit = tuple.Item2;

            var textBlock = dockPanel.Children[2] as TextBlock;
            var editBtn = dockPanel.Children[1] as Button;
            var deleteBtn = dockPanel.Children[0] as Button;
            textBlock.Visibility = Visibility.Collapsed;
            editBtn.Visibility = Visibility.Collapsed;
            deleteBtn.Visibility = Visibility.Collapsed;

            Button cancelBtn = new()
            {
                Content = "Cancel",
                Width = 60,
                Margin = new Thickness(10),
                Foreground = Brushes.White,
                Template = CreateRoundedButtonTemplate(),
                Tag = dockPanel
            };
            cancelBtn.Click += CancelClick;
            DockPanel.SetDock(cancelBtn, Dock.Right);
            dockPanel.Children.Add(cancelBtn);

            Button confrimBtn = new()
            {
                Content = "Confirm",
                Width = 60,
                Margin = new Thickness(10),
                Foreground = Brushes.White,
                Template = CreateRoundedButtonTemplate(),
                Tag = dockPanel
            };
            confrimBtn.Click += ConfirmClick;
            DockPanel.SetDock(confrimBtn, Dock.Right);
            dockPanel.Children.Add(confrimBtn);

            TextBox textBox = new() {
                Text = textToEdit,
                CaretBrush = Brushes.White,
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                FontSize = 18 };
            DockPanel.SetDock(textBox, Dock.Left);
            dockPanel.Children.Add(textBox);
        }

        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dockPanel = button.Tag as DockPanel;

            var parentBorder = dockPanel.Parent as Border;
            var id = parentBorder.Tag.ToString().Split("/", count: 2)[0];

            foreach (var child in dockPanel.Children)
            {
                if (child is TextBox textBox)
                {
                    var updatedText = textBox.Text.Trim();
                    using (var conn = new SqliteConnection("Data Source=Dictionary.db"))
                    {
                        conn.Open();
                        SqliteCommand update = new($"UPDATE dictionary SET word ='{updatedText}' WHERE id={id}", conn);
                        update.ExecuteNonQuery();
                        conn.Close();
                    }

                    WordsList.Children.Remove(parentBorder);

                    AddElement(int.Parse(id), updatedText);
                    break;
                }
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dockPanel = button.Tag as DockPanel;

            int childCount = dockPanel.Children.Count;

            for (int i = childCount-1; i > 2; i--)
            {
                dockPanel.Children.RemoveAt(i);
            }

            for (int j = 0; j <= 2; j++)
            {
                var child = dockPanel.Children[j];
                child.Visibility = Visibility.Visible;
            }
        }
        private void Filter(object sender, TextChangedEventArgs e)
        {
            var filterText = ((TextBox)sender).Text?.Trim()?.ToLowerInvariant() ?? "";

            foreach (var item in WordsList.Children)
            {
                var border = item as Border;
                if (border != null)
                {
                    var dockPanel = border.Child as DockPanel;
                    if (dockPanel != null)
                    {
                        var textBlock = dockPanel.Children[2] as TextBlock;
                        if (textBlock != null)
                        {
                            bool visible = textBlock.Text.ToLowerInvariant().Contains(filterText);
                            border.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private void AddElement(long id, string text)
        {
            //AddWord.Text = string.Empty;

            var border = new Border
            {
                Tag = $"{id}/{text}",
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(5)
            };

            var dockPanel = new DockPanel();

            Button deleteBtn = new()
            {
                Content = "Delete",
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Template = CreateRoundedButtonTemplate(),
                Tag = border
            };
            deleteBtn.Click += DeleteItem_Click;
            DockPanel.SetDock(deleteBtn, Dock.Right);
            dockPanel.Children.Add(deleteBtn);

            Button editBtn = new()
            {
                Content = "Edit",
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Padding = new Thickness(5),
                Template = CreateRoundedButtonTemplate(),
                Tag = Tuple.Create(dockPanel, text)
            };
            editBtn.Click += EditClick;
            DockPanel.SetDock(editBtn, Dock.Right);
            dockPanel.Children.Add(editBtn);

            TextBlock textBlock = new TextBlock
            {
                Text = text,
                FontSize = 18,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.Transparent
            };
            dockPanel.Children.Add(textBlock);

            border.Child = dockPanel;

            int insertPosition = FindInsertPosition(WordsList.Children.Cast<Border>(), text);

            WordsList.Children.Insert(insertPosition, border);
        }
        private int FindInsertPosition(IEnumerable<Border> elements, string newText)
        {
            foreach (var element in elements)
            {
                var currentText = ((TextBlock)((DockPanel)element.Child).Children[2]).Text;
                if (newText.CompareTo(currentText) < 0)
                {
                    return elements.ToList().IndexOf(element); // Позиция текущего элемента, куда будем вставлять
                }
            }
            return elements.Count(); // Если новых элемент больше всех остальных, добавляем в конец списка
        }

        private ControlTemplate CreateRoundedButtonTemplate()
        {
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.CursorProperty, Cursors.Hand);
            border.SetValue(Border.BorderBrushProperty, Brushes.White);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.PaddingProperty, new Thickness(6));
            border.AppendChild(contentPresenter);

            return new ControlTemplate(typeof(Button))
            {
                VisualTree = border
            };
        }

        // DataBase
        private void LoadWord()
        {
            using (var conn = new SqliteConnection("Data Source=Dictionary.db"))
            {
                conn.Open();
                SqliteCommand load = new("SELECT * FROM dictionary", conn);
                using (SqliteDataReader reader = load.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long id = (long)reader.GetValue(0);
                        string text = reader.GetValue(1).ToString();
                        AddElement(id, text);
                    }
                }
                conn.Close();
            }
        }
        private void AddWordInDB(string text)
        {   
            AddWord.Text = string.Empty;
            int id = 0;
            using (var conn = new SqliteConnection("Data Source=Dictionary.db"))
            {
                conn.Open();
                SqliteCommand insert = new($"INSERT INTO dictionary (word) VALUES ('{text}')", conn);
                insert.ExecuteNonQuery();

                SqliteCommand findId = new("SELECT MAX(id) AS last_id FROM dictionary;", conn);
                id = Convert.ToInt32(findId.ExecuteScalar());
                conn.Close();
            }
            AddElement(id, text);
        }
    }
}