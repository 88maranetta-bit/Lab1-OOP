using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;

namespace La1
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 20;
        const int CountRow = 50;

        public MainPage()
        {
            InitializeComponent();
            CreateGrid();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSavedFilesList();
        }

        private void LoadSavedFilesList()
        {
            try
            {
                string folderPath = FileSystem.AppDataDirectory;
                var files = Directory.GetFiles(folderPath, "*.json")
                                     .Select(Path.GetFileName)
                                     .ToList();
                savedFilesPicker.ItemsSource = files;
            }
            catch
            {
            }
        }

        private void CreateGrid()
        {
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {
            for (int col = 0; col < CountColumn + 1; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 80 });
                if (col > 0)
                {
                    var label = new Label
                    {
                        Text = GetColumnName(col),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        BackgroundColor = Colors.Transparent
                    };
                    Microsoft.Maui.Controls.Grid.SetRow(label, 0);
                    Microsoft.Maui.Controls.Grid.SetColumn(label, col);
                    grid.Children.Add(label);
                }
            }
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 0; row < CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = 40 });

                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Transparent
                };
                Microsoft.Maui.Controls.Grid.SetRow(label, row + 1);
                Microsoft.Maui.Controls.Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                for (int col = 0; col < CountColumn; col++)
                {
                    string cellName = GetColumnName(col + 1) + (row + 1);

                    var entry = new Entry
                    {
                        Text = "",
                        StyleId = cellName,
                        WidthRequest = 80,
                        HeightRequest = 40,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = Colors.White,
                        BackgroundColor = Color.FromArgb("#2A2A2A")
                    };

                    entry.Unfocused += Entry_Unfocused;
                    Microsoft.Maui.Controls.Grid.SetRow(entry, row + 1);
                    Microsoft.Maui.Controls.Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }
            }
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }

        private void Entry_Unfocused(object? sender, EventArgs e)
        {
            if (sender is Entry entry)
            {
                textInput.Text = entry.Text;
            }
        }

        private double GetCellValue(string cellName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cellName))
                    return 0.0;

                string colStr = new string(cellName.TakeWhile(char.IsLetter).ToArray()).ToUpper();
                string rowStr = new string(cellName.SkipWhile(char.IsLetter).ToArray());

                if (string.IsNullOrEmpty(colStr) || !int.TryParse(rowStr, out int targetRow))
                    return 0.0;

                int targetCol = 0;
                foreach (char c in colStr)
                {
                    targetCol = targetCol * 26 + (c - 'A' + 1);
                }

                foreach (var child in grid.Children)
                {
                    if (child is Entry entry)
                    {
                        int row = Microsoft.Maui.Controls.Grid.GetRow(entry as BindableObject);
                        int col = Microsoft.Maui.Controls.Grid.GetColumn(entry as BindableObject);

                        if (row == targetRow && col == targetCol)
                        {
                            string text = entry.Text?.Trim() ?? "";
                            if (string.IsNullOrEmpty(text))
                                return 0.0;

                            if (text.StartsWith("="))
                            {
                                text = text.Substring(1);
                            }

                            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                            {
                                return val;
                            }

                            try
                            {
                                var subEvaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name));
                                return subEvaluator.Evaluate(text);
                            }
                            catch
                            {
                                return 0.0;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return 0.0;
        }

        private void CalculateButton_Clicked(object? sender, EventArgs? e)
        {
            try
            {
                string formulaText = textInput.Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(formulaText))
                {
                    DisplayAlert("Помилка", "Введіть формулу!", "OK");
                    return;
                }

                var evaluator = new FormulaEvaluatorVisitor(cellName => GetCellValue(cellName));
                double result = evaluator.Evaluate(formulaText);

                textInput.Text = result.ToString(CultureInfo.InvariantCulture);

                foreach (var child in grid.Children)
                {
                    if (child is Entry entry)
                    {
                        string currentText = entry.Text?.Trim() ?? "";
                        if (currentText.Equals(formulaText, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = result.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                }

                DisplayAlert("Результат", $"Пораховано: {result}", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Помилка", $"Помилка обчислення: {ex.Message}", "OK");
            }
        }

        private async void SaveButton_Clicked(object? sender, EventArgs? e)
        {
            try
            {
                string fileName = await DisplayPromptAsync("Збереження", "Введіть назву файлу:", "Зберегти", "Скасувати", "table1.json");
                if (string.IsNullOrWhiteSpace(fileName)) return;

                if (!fileName.EndsWith(".json"))
                    fileName += ".json";

                string folderPath = FileSystem.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                var cellData = new Dictionary<string, string>();
                foreach (var child in grid.Children)
                {
                    if (child is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
                    {
                        string text = entry.Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            cellData[entry.StyleId] = text;
                        }
                    }
                }

                string jsonString = System.Text.Json.JsonSerializer.Serialize(cellData);
                await File.WriteAllTextAsync(filePath, jsonString);

                LoadSavedFilesList();
                await DisplayAlert("Успіх", $"Файл {fileName} збережено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Не вдалося зберегти: {ex.Message}", "OK");
            }
        }

        private async void ReadButton_Clicked(object? sender, EventArgs? e)
        {
            if (savedFilesPicker.SelectedItem is string selectedFile)
            {
                LoadTableFromFile(selectedFile);
            }
            else
            {
                await DisplayAlert("Увага", "Виберіть файл зі списку!", "OK");
            }
        }

        private void SavedFilesPicker_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (savedFilesPicker.SelectedItem is string selectedFile)
            {
                LoadTableFromFile(selectedFile);
            }
        }

        private async void LoadTableFromFile(string fileName)
        {
            try
            {
                string folderPath = FileSystem.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                if (!File.Exists(filePath)) return;

                string jsonString = await File.ReadAllTextAsync(filePath);
                var cellData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                if (cellData == null) return;

                foreach (var child in grid.Children)
                {
                    if (child is Entry entry)
                    {
                        entry.Text = "";
                    }
                }

                foreach (var kvp in cellData)
                {
                    string cellName = kvp.Key;
                    string value = kvp.Value;

                    foreach (var child in grid.Children)
                    {
                        if (child is Entry entry && entry.StyleId == cellName)
                        {
                            entry.Text = value;
                        }
                    }
                }

                await DisplayAlert("Успіх", $"Файл {fileName} завантажено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Помилка завантаження: {ex.Message}", "OK");
            }
        }

        private async void ExitButton_Clicked(object? sender, EventArgs? e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object? sender, EventArgs? e)
        {
            await DisplayAlert(
                "Про програму",
                "Лабораторна робота 1\nВаріант 6\n\nВиконав: Мариніна Анна\nГрупа К-27",
                "OK"
            );
        }

        private void DeleteRowButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.RowDefinitions.Count > 1)
            {
                int lastRow = grid.RowDefinitions.Count - 1;
                grid.RowDefinitions.RemoveAt(lastRow);
                var itemsToRemove = grid.Children.Where(c => Microsoft.Maui.Controls.Grid.GetRow(c as BindableObject) == lastRow).ToList();
                foreach (var item in itemsToRemove)
                {
                    grid.Children.Remove(item);
                }
            }
        }

        private void DeleteColumnButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.ColumnDefinitions.Count > 1)
            {
                int lastCol = grid.ColumnDefinitions.Count - 1;
                grid.ColumnDefinitions.RemoveAt(lastCol);
                var itemsToRemove = grid.Children.Where(c => Microsoft.Maui.Controls.Grid.GetColumn(c as BindableObject) == lastCol).ToList();
                foreach (var item in itemsToRemove)
                {
                    grid.Children.Remove(item);
                }
            }
        }

        private void AddRowButton_Clicked(object? sender, EventArgs? e)
        {
            int newRow = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = 40 });

            var label = new Label
            {
                Text = newRow.ToString(),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent
            };
            Microsoft.Maui.Controls.Grid.SetRow(label, newRow);
            Microsoft.Maui.Controls.Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 0; col < CountColumn; col++)
            {
                string cellName = GetColumnName(col + 1) + newRow;

                var entry = new Entry
                {
                    Text = "",
                    StyleId = cellName,
                    WidthRequest = 80,
                    HeightRequest = 40,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A")
                };
                entry.Unfocused += Entry_Unfocused;
                Microsoft.Maui.Controls.Grid.SetRow(entry, newRow);
                Microsoft.Maui.Controls.Grid.SetColumn(entry, col + 1);
                grid.Children.Add(entry);
            }
        }

        private void AddColumnButton_Clicked(object? sender, EventArgs? e)
        {
            int newColumn = grid.ColumnDefinitions.Count;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 80 });

            var label = new Label
            {
                Text = GetColumnName(newColumn),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent
            };
            Microsoft.Maui.Controls.Grid.SetRow(label, 0);
            Microsoft.Maui.Controls.Grid.SetColumn(label, newColumn);
            grid.Children.Add(label);

            for (int row = 0; row < CountRow; row++)
            {
                string cellName = GetColumnName(newColumn) + (row + 1);

                var entry = new Entry
                {
                    Text = "",
                    StyleId = cellName,
                    WidthRequest = 80,
                    HeightRequest = 40,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#1E1E1E")
                };
                entry.Unfocused += Entry_Unfocused;
                Microsoft.Maui.Controls.Grid.SetRow(entry, row + 1);
                Microsoft.Maui.Controls.Grid.SetColumn(entry, newColumn);
                grid.Children.Add(entry);
            }
        }
    }
}