using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace La1
{
    public partial class MainPage : ContentPage
    {
        private readonly TableEngine _tableEngine = new();

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
                savedFilesPicker.ItemsSource = Directory.GetFiles(folderPath, "*.json")
                                                        .Select(Path.GetFileName)
                                                        .ToList();
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
            for (int col = 0; col <= TableConfig.CountColumn; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = TableConfig.CellWidth });
                if (col <= 0) continue;

                var label = CreateHeaderLabel(GetColumnName(col), 0, col);
                grid.Children.Add(label);
            }
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 0; row < TableConfig.CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = TableConfig.CellHeight });

                var rowLabel = CreateHeaderLabel((row + 1).ToString(), row + 1, 0);
                grid.Children.Add(rowLabel);

                for (int col = 0; col < TableConfig.CountColumn; col++)
                {
                    string cellName = GetColumnName(col + 1) + (row + 1);
                    var entry = CreateCellEntry(cellName, row + 1, col + 1);
                    grid.Children.Add(entry);
                }
            }
        }

        private Label CreateHeaderLabel(string text, int row, int col)
        {
            var label = new Label
            {
                Text = text,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent
            };
            Microsoft.Maui.Controls.Grid.SetRow(label, row);
            Microsoft.Maui.Controls.Grid.SetColumn(label, col);
            return label;
        }

        private Entry CreateCellEntry(string cellName, int row, int col)
        {
            var entry = new Entry
            {
                Text = "",
                StyleId = cellName,
                WidthRequest = TableConfig.CellWidth,
                HeightRequest = TableConfig.CellHeight,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Colors.White,
                BackgroundColor = TableConfig.CellBackgroundColor
            };
            entry.Focused += Entry_Focused;
            entry.Unfocused += Entry_Unfocused;

            Microsoft.Maui.Controls.Grid.SetRow(entry, row);
            Microsoft.Maui.Controls.Grid.SetColumn(entry, col);
            return entry;
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

        private void RefreshGridIndices()
        {
            var rowLabels = grid.Children.OfType<Label>()
                .Where(l => Microsoft.Maui.Controls.Grid.GetColumn(l) == 0 && Microsoft.Maui.Controls.Grid.GetRow(l) > 0)
                .OrderBy(l => Microsoft.Maui.Controls.Grid.GetRow(l))
                .ToList();

            int index = 1;
            foreach (var lbl in rowLabels)
            {
                lbl.Text = index++.ToString();
            }

            for (int c = 1; c < grid.ColumnDefinitions.Count; c++)
            {
                if (grid.Children.FirstOrDefault(cld => cld is Label l && Microsoft.Maui.Controls.Grid.GetRow(l) == 0 && Microsoft.Maui.Controls.Grid.GetColumn(l) == c) is Label label)
                {
                    label.Text = GetColumnName(c);
                }
            }

            foreach (var child in grid.Children.OfType<Entry>())
            {
                int row = Microsoft.Maui.Controls.Grid.GetRow(child);
                int col = Microsoft.Maui.Controls.Grid.GetColumn(child);
                if (row > 0 && col > 0)
                {
                    child.StyleId = GetColumnName(col) + row;
                }
            }
        }

        private void Entry_Focused(object? sender, EventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
            {
                if (showFormulasSwitch?.IsToggled == true) return;

                if (_tableEngine.CellFormulas.TryGetValue(entry.StyleId, out string? formula))
                {
                    entry.Text = formula;
                }
                if (textInput != null)
                {
                    textInput.Text = entry.Text;
                }
            }
        }

        private void Entry_Unfocused(object? sender, EventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
            {
                if (showFormulasSwitch?.IsToggled == true) return;

                string text = entry.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    if (!text.StartsWith("=") && (text.Contains("+") || text.Contains("-") || text.Contains("*") || text.Contains("/") || text.Contains("^") || text.Contains("(")))
                    {
                        text = "=" + text;
                        entry.Text = text;
                    }
                    _tableEngine.SetCellFormula(entry.StyleId, text);
                }
                else
                {
                    _tableEngine.SetCellFormula(entry.StyleId, string.Empty);
                }

                if (textInput != null) textInput.Text = text;
            }
            RecalculateAllCells();
        }

        public void ShowFormulasSwitch_Toggled(object? sender, ToggledEventArgs e)
        {
            bool showFormulas = e.Value;

            foreach (var entry in grid.Children.OfType<Entry>().Where(e => !string.IsNullOrEmpty(e.StyleId)))
            {
                if (showFormulas)
                {
                    entry.Text = _tableEngine.CellFormulas.TryGetValue(entry.StyleId, out string? formula) ? formula : GetRawEntryValue(entry.StyleId);
                    entry.TextColor = Colors.White;
                }
                else if (!entry.IsFocused)
                {
                    RecalculateSingleCell(entry);
                }
            }

            if (!showFormulas) RecalculateAllCells();
        }

        private string GetRawEntryValue(string cellName)
        {
            return grid.Children.OfType<Entry>().FirstOrDefault(e => e.StyleId?.ToUpper() == cellName.ToUpper())?.Text?.Trim() ?? "";
        }

        private void RecalculateSingleCell(Entry entry)
        {
            bool hasFormula = _tableEngine.CellFormulas.TryGetValue(entry.StyleId, out string? formulaText) && !string.IsNullOrEmpty(formulaText);
            bool hasText = !string.IsNullOrEmpty(entry.Text);

            if (!hasFormula && !hasText)
            {
                entry.Text = "";
                entry.TextColor = Colors.White;
                return;
            }

            string currentText = entry.Text?.Trim() ?? "";

            if (IsInvalidCellReferenceFormat(currentText))
            {
                entry.TextColor = Colors.Red;
                entry.Text = "ERR: REF";
                return;
            }

            if (Regex.IsMatch(currentText, @"/\s*0(\.0+)?(\b|[\+\-\*\/\)]|$)", RegexOptions.IgnoreCase))
            {
                entry.TextColor = Colors.Red;
                entry.Text = "ERR: DIV/0";
                return;
            }

            if (!currentText.StartsWith("=") && !double.TryParse(currentText, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                entry.TextColor = Colors.Red;
                entry.Text = "ERR: NAME";
                return;
            }

            try
            {
                double result = GetCellValue(entry.StyleId, null);
                entry.Text = result.ToString(CultureInfo.InvariantCulture);
                entry.TextColor = Colors.White;
            }
            catch (Exception ex)
            {
                entry.TextColor = Colors.Red;
                entry.Text = ErrorResolver.Resolve(ex.Message);
            }
        }

        private bool IsInvalidCellReferenceFormat(string cellName)
        {
            if (cellName.StartsWith("=")) return false;
            Match m = Regex.Match(cellName, @"^([A-Z]+)(\d+)$");
            if (!m.Success) return false;

            string colStr = m.Groups[1].Value;
            int rowNum = int.Parse(m.Groups[2].Value);
            int colIndex = 0;
            foreach (char c in colStr) colIndex = colIndex * 26 + (c - 'A' + 1);

            return rowNum < 1 || rowNum > grid.RowDefinitions.Count - 1 || colIndex < 1 || colIndex > grid.ColumnDefinitions.Count - 1;
        }

        private void RecalculateAllCells()
        {
            if (showFormulasSwitch?.IsToggled == true) return;

            foreach (var child in grid.Children.OfType<Entry>().Where(c => !string.IsNullOrEmpty(c.StyleId) && !c.IsFocused))
            {
                bool hasFormula = _tableEngine.CellFormulas.TryGetValue(child.StyleId, out string? formulaText) && !string.IsNullOrEmpty(formulaText);
                bool hasText = !string.IsNullOrEmpty(child.Text);

                if (!hasFormula && !hasText)
                {
                    child.Text = "";
                    child.TextColor = Colors.White;
                    continue;
                }

                string currentText = child.Text?.Trim() ?? "";

                if (IsInvalidCellReferenceFormat(currentText))
                {
                    child.TextColor = Colors.Red;
                    child.Text = "ERR: REF";
                    continue;
                }

                if (Regex.IsMatch(currentText, @"/\s*0(\.0+)?(\b|[\+\-\*\/\)]|$)", RegexOptions.IgnoreCase))
                {
                    child.TextColor = Colors.Red;
                    child.Text = "ERR: DIV/0";
                    continue;
                }

                if (!currentText.StartsWith("=") && !double.TryParse(currentText, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    if (!_tableEngine.CellFormulas.TryGetValue(child.StyleId, out string? storedFormula) || !storedFormula.StartsWith("="))
                    {
                        child.TextColor = Colors.Red;
                        child.Text = "ERR: NAME";
                        continue;
                    }
                }

                try
                {
                    double result = GetCellValue(child.StyleId, null);
                    child.Text = result.ToString(CultureInfo.InvariantCulture);
                    child.TextColor = Colors.White;
                }
                catch (Exception ex)
                {
                    child.TextColor = Colors.Red;
                    child.Text = ErrorResolver.Resolve(ex.Message);
                }
            }
        }

        private double GetCellValue(string cellName, HashSet<string>? visiting)
        {
            cellName = cellName?.Trim().ToUpper() ?? "";

            Match nameMatch = Regex.Match(cellName, @"^([A-Z]+)(\d+)$");
            if (!nameMatch.Success) throw new Exception("Помилка імені");

            string colStr = nameMatch.Groups[1].Value;
            int rowNum = int.Parse(nameMatch.Groups[2].Value);
            int colIndex = 0;
            foreach (char c in colStr) colIndex = colIndex * 26 + (c - 'A' + 1);

            if (rowNum < 1 || rowNum > grid.RowDefinitions.Count - 1 || colIndex < 1 || colIndex > grid.ColumnDefinitions.Count - 1)
            {
                throw new Exception("Помилка посилання");
            }

            var entry = grid.Children.OfType<Entry>().FirstOrDefault(e =>
                Microsoft.Maui.Controls.Grid.GetRow(e) == rowNum &&
                Microsoft.Maui.Controls.Grid.GetColumn(e) == colIndex);

            if (entry == null) throw new Exception("Помилка посилання");

            string formulaSource = _tableEngine.CellFormulas.TryGetValue(cellName, out string? engineFormula) && !string.IsNullOrEmpty(engineFormula)
                ? engineFormula
                : (entry.Text?.Trim() ?? "");

            if (string.IsNullOrEmpty(formulaSource) || formulaSource == "=") return 0.0;

            if (formulaSource.Equals("#N/A", StringComparison.OrdinalIgnoreCase)) throw new Exception("#N/A");

            if (Regex.IsMatch(formulaSource, @"/\s*0(\.0+)?(\b|[\+\-\*\/\)]|$)", RegexOptions.IgnoreCase))
            {
                throw new DivideByZeroException();
            }

            if (!formulaSource.StartsWith("=") && !double.TryParse(formulaSource, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                if (visiting == null || visiting.Count == 0)
                {
                    throw new Exception("Помилка імені");
                }
                else
                {
                    throw new Exception("Помилка значення");
                }
            }

            visiting ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (visiting.Contains(cellName))
            {
                throw new Exception("Помилка циклу");
            }

            if (formulaSource.StartsWith("="))
            {
                visiting.Add(cellName);
                try
                {
                    string formula = formulaSource.Substring(1).Trim();

                    if (Regex.IsMatch(formula, @"/\s*0(\.0+)?(\b|[\+\-\*\/\)]|$)", RegexOptions.IgnoreCase))
                    {
                        throw new DivideByZeroException();
                    }

                    _tableEngine.ValidateFormula(formula);

                    if (Regex.IsMatch(formula, @"^[A-Z]+\d+$", RegexOptions.IgnoreCase))
                    {
                        return GetCellValue(formula, visiting);
                    }

                    var evaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name, visiting));
                    return evaluator.Evaluate(formula);
                }
                catch (DivideByZeroException)
                {
                    throw new Exception("Помилка ділення на нуль");
                }
                catch (Exception ex)
                {
                    string msg = ex.Message.ToLower();
                    if (msg.Contains("ділен") || msg.Contains("div") || msg.Contains("zero") || msg.Contains("infinity") || msg.Contains("nan"))
                        throw new Exception("Помилка ділення на нуль");
                    if (msg.Contains("посилан") || msg.Contains("ref"))
                        throw new Exception("Помилка посилання");
                    if (msg.Contains("цикл") || msg.Contains("CYCLE"))
                        throw new Exception("Помилка циклу");
                    if (msg.Contains("імен") || msg.Contains("NAME"))
                    {
                        if (visiting != null && visiting.Count > 1)
                            throw new Exception("Помилка значення");
                        throw;
                    }
                    throw new Exception("Помилка значення");
                }
                finally
                {
                    visiting.Remove(cellName);
                }
            }

            if (double.TryParse(formulaSource, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }

            throw new Exception("Помилка значення");
        }

        public async void SaveButton_Clicked(object? sender, EventArgs? e)
        {
            string fileName = await DisplayPromptAsync("Збереження", "Введіть назву файлу:", "Зберегти", "Скасувати", "table1.json");
            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (!fileName.EndsWith(".json")) fileName += ".json";
            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            var gridValues = grid.Children.OfType<Entry>()
                                 .Where(e => !string.IsNullOrEmpty(e.StyleId))
                                 .ToDictionary(e => e.StyleId!, e => e.Text?.Trim() ?? "");

            _tableEngine.SaveState(filePath, gridValues);
            LoadSavedFilesList();
            await DisplayAlert("Успіх", $"Файл {fileName} збережено!", "OK");
        }

        public async void ClearButton_Clicked(object? sender, EventArgs? e)
        {
            if (!await DisplayAlert("Очищення", "Ви дійсно хочете очистити всю таблицю?", "Так", "Ні")) return;

            _tableEngine.Clear();
            foreach (var entry in grid.Children.OfType<Entry>())
            {
                entry.Text = "";
                entry.TextColor = Colors.White;
            }
            if (textInput != null) textInput.Text = "";
        }

        public void SavedFilesPicker_SelectedIndexChanged(object? sender, EventArgs? e)
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
                string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                _tableEngine.LoadState(filePath);

                foreach (var entry in grid.Children.OfType<Entry>())
                {
                    entry.Text = "";
                    entry.TextColor = Colors.White;
                }

                foreach (var kvp in _tableEngine.CellFormulas)
                {
                    var entry = grid.Children.OfType<Entry>().FirstOrDefault(e => e.StyleId == kvp.Key);
                    if (entry != null) entry.Text = kvp.Value;
                }

                if (showFormulasSwitch != null) showFormulasSwitch.IsToggled = false;

                RecalculateAllCells();
                await DisplayAlert("Успіх", $"Файл {fileName} завантажено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Помилка завантаження: {ex.Message}", "OK");
            }
        }

        public async void ExitButton_Clicked(object? sender, EventArgs? e)
        {
            if (await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні"))
            {
                Environment.Exit(0);
            }
        }

        public async void ShowAboutInfo_Clicked(object? sender, EventArgs? e)
        {
            await DisplayAlert("Про програму", "Лабораторна робота 1\nВаріант 6\nВиконала: Мариніна Анна\nГрупа К-27", "OK");
        }

        public async void ShowVariant6Reference_Clicked(object? sender, EventArgs? e)
        {
            string referenceText =
                "Операції та функції:\n" +
                "• Бінарні операції: +, -, *, /\n" +
                "• Цілочисельне ділення та залишок: mod, div\n" +
                "• Піднесення у степінь: ^\n" +
                "• Порівняння: <=, >=, <>\n" +
                "• Логічне заперечення: not\n" +
                "• Логічні операції: or, and\n\n" +
                "Помилки:\n" +
                "• ERR: DIV/0 — ділення на нуль\n" +
                "• ERR: CYCLE — циклічна залежність\n" +
                "• ERR: NAME — помилка імені / невідома функція\n" +
                "• ERR: REF — неіснуюче посилання\n" +
                "• ERR: VALUE — помилка значень\n" +
                "• #N/A — відсутнє значення";

            await DisplayAlert("ДОВІДКА (ВАРІАНТ 6)", referenceText, "OK");
        }

        public void DeleteRowButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.RowDefinitions.Count <= 2) return;

            int targetRow = grid.Children.OfType<Entry>().FirstOrDefault(entry => entry.IsFocused) is Entry fe
                ? Microsoft.Maui.Controls.Grid.GetRow(fe)
                : grid.RowDefinitions.Count - 1;

            var childrenToRemove = grid.Children.Where(child => child is BindableObject b && Microsoft.Maui.Controls.Grid.GetRow(b) == targetRow).ToList();
            foreach (var child in childrenToRemove) grid.Children.Remove(child);

            grid.RowDefinitions.RemoveAt(targetRow);
            foreach (var child in grid.Children.OfType<BindableObject>())
            {
                int r = Microsoft.Maui.Controls.Grid.GetRow(child);
                if (r > targetRow) Microsoft.Maui.Controls.Grid.SetRow(child, r - 1);
            }
            RefreshGridIndices();
        }

        public void DeleteColumnButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.ColumnDefinitions.Count <= 2) return;

            int targetCol = grid.Children.OfType<Entry>().FirstOrDefault(entry => entry.IsFocused) is Entry fe
                ? Microsoft.Maui.Controls.Grid.GetColumn(fe)
                : grid.ColumnDefinitions.Count - 1;

            var childrenToRemove = grid.Children.Where(child => child is BindableObject b && Microsoft.Maui.Controls.Grid.GetColumn(b) == targetCol).ToList();
            foreach (var child in childrenToRemove) grid.Children.Remove(child);

            grid.ColumnDefinitions.RemoveAt(targetCol);
            foreach (var child in grid.Children.OfType<BindableObject>())
            {
                int c = Microsoft.Maui.Controls.Grid.GetColumn(child);
                if (c > targetCol) Microsoft.Maui.Controls.Grid.SetColumn(child, c - 1);
            }
            RefreshGridIndices();
        }

        public void AddRowButton_Clicked(object? sender, EventArgs? e)
        {
            int newRow = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = TableConfig.CellHeight });

            var rowLabel = CreateHeaderLabel(newRow.ToString(), newRow, 0);
            grid.Children.Add(rowLabel);

            for (int col = 0; col < grid.ColumnDefinitions.Count - 1; col++)
            {
                string cellName = GetColumnName(col + 1) + newRow;
                var entry = CreateCellEntry(cellName, newRow, col + 1);
                grid.Children.Add(entry);
            }
        }

        public void AddColumnButton_Clicked(object? sender, EventArgs? e)
        {
            int newColumn = grid.ColumnDefinitions.Count;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = TableConfig.CellWidth });

            var colLabel = CreateHeaderLabel(GetColumnName(newColumn), 0, newColumn);
            grid.Children.Add(colLabel);

            for (int row = 0; row < grid.RowDefinitions.Count - 1; row++)
            {
                string cellName = GetColumnName(newColumn) + (row + 1);
                var entry = CreateCellEntry(cellName, row + 1, newColumn);
                grid.Children.Add(entry);
            }
        }
    }
}