using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PhotoSorter
{
    public partial class MainWindow : Window
    {
        private string sourcePath = "";

        private List<string> files;
        private int currentIndex = 0;

        private string[] folderNames = new string[9];

        private List<HistoryActionStack> history = new List<HistoryActionStack>();

        public MainWindow()
        {
            InitializeComponent();
        }

        // logika odpowiadajaca za cofanie
        private class HistoryActionStack
        {
            public string sourcePath { get; set; }
            public string destinationPath { get; set; }
            public bool IsSkipped { get; set; }
        }
        private void AddToHistoryActionStack(string src, string dest, bool isSkipped)
        {
            history.Add(new HistoryActionStack
            {
                IsSkipped = isSkipped,
                destinationPath = dest,
                sourcePath = src
            });
            if (history.Count > 5)
            {
                history.RemoveAt(0);
            }
        }
        private void UndoLastAction()
        {
            if (history.Count == 0) return;

            var lastAction = history[history.Count - 1];

            history.RemoveAt(history.Count - 1);

            if (currentIndex > 0) currentIndex--;

            if (!lastAction.IsSkipped)
            {
                try
                {
                    if (File.Exists(lastAction.destinationPath))
                    {
                        File.Move(lastAction.destinationPath, lastAction.sourcePath);
                    }
                    else
                    {
                        MessageBox.Show("Nie można cofnąć - plik został usunięty lub przeniesiony ręcznie!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas cofania: {ex.Message}");
                }
            }

            DisplayCurrentImage();
        }

        // logika main screena
        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                sourcePath = dialog.FolderName;
                PathDisplay.Text = sourcePath;
            }
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja
            if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
            {
                MessageBox.Show("Wybierz poprawny folder ze zdjęciami!");
                return;
            }

            folderNames[1] = ValidateName(ConfigBox1.Text, "Folder_1");
            folderNames[2] = ValidateName(ConfigBox2.Text, "Folder_2");
            folderNames[3] = ValidateName(ConfigBox3.Text, "Folder_3");
            folderNames[4] = ValidateName(ConfigBox4.Text, "Folder_4");
            folderNames[5] = ValidateName(ConfigBox5.Text, "Folder_5");
            folderNames[6] = ValidateName(ConfigBox6.Text, "Folder_6");
            folderNames[7] = ValidateName(ConfigBox7.Text, "Folder_7");
            folderNames[8] = ValidateName(ConfigBox8.Text, "Folder_8");

            Lbl1.Text = folderNames[1];
            Lbl2.Text = folderNames[2];
            Lbl3.Text = folderNames[3];
            Lbl4.Text = folderNames[4];
            Lbl5.Text = folderNames[5];
            Lbl6.Text = folderNames[6];
            Lbl7.Text = folderNames[7];
            Lbl8.Text = folderNames[8];

            MenuView.Visibility = Visibility.Collapsed;
            SortingView.Visibility = Visibility.Visible;

            LoadFileList();
        }
        private string ValidateName(string input, string def)
        {
            if (string.IsNullOrWhiteSpace(input)) return def;
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(input.Where(ch => !invalidChars.Contains(ch)).ToArray());
        }

        // pobieranie i wyswietlanie zdjec
        private void LoadFileList()
        {
            if (!Directory.Exists(sourcePath))
            {
                MessageBox.Show("Folder źródłowy nie istnieje! Sprawdź ścieżkę w kodzie.");
                return;
            }

            files = Directory.GetFiles(sourcePath)
                             .Where(s => s.ToLower().EndsWith(".jpg") ||
                                         s.ToLower().EndsWith(".jpeg") ||
                                         s.ToLower().EndsWith(".png") ||
                                         s.ToLower().EndsWith(".gif"))
                             .ToList();

            if (files.Count > 0)
            {
                DisplayCurrentImage();
            }
            else
            {
                FileNameText.Text = "Brak zdjęć w folderze.";
            }
        }
        private void DisplayCurrentImage()
        {
            if (currentIndex < files.Count)
            {
                string filePath = files[currentIndex];
                FileNameText.Text = Path.GetFileName(filePath) + $" ({currentIndex + 1}/{files.Count})";

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    MainImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd ładowania zdjęcia: " + ex.Message);
                }
            }
            else
            {
                MainImage.Source = null;
                FileNameText.Text = "Koniec! Wszystkie zdjęcia posortowane.";
            }
        }
        
        // podswietlanie uzytego folderu
        private void HighlightTile(Border border, bool isSkip = false, bool isDelete = false, bool isUndo = false)
        {
            if (!(border.Background is SolidColorBrush currentBrush)) return;
            Color defaultColor = (Color)ColorConverter.ConvertFromString("#252526");

            Color highlightColor;

            if (isDelete)
            {
                highlightColor = (Color)ColorConverter.ConvertFromString("#D13438");
            }
            else if (isSkip)
            {
                highlightColor = (Color)ColorConverter.ConvertFromString("#FFCC00");
            }
            else if (isUndo)
            {
                highlightColor = (Color)ColorConverter.ConvertFromString("#C0C0C0");
            }
            else
            {
                highlightColor = (Color)ColorConverter.ConvertFromString("#007ACC");
            }


            ColorAnimation fadeIn = new ColorAnimation();
            fadeIn.To = highlightColor;
            fadeIn.Duration = new Duration(TimeSpan.FromMilliseconds(100));
            fadeIn.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut };

            ColorAnimation fadeOut = new ColorAnimation();
            fadeOut.To = defaultColor;
            fadeOut.BeginTime = TimeSpan.FromMilliseconds(100);
            fadeOut.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            fadeOut.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn };

            Storyboard storyboard = new Storyboard();
            storyboard.Stop(border);
            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(fadeOut);

            string propertyPath = "(Border.Background).(SolidColorBrush.Color)";
            Storyboard.SetTarget(fadeIn, border);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(propertyPath));
            Storyboard.SetTarget(fadeOut, border);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(propertyPath));

            storyboard.Begin();
        }
        
        // dzialanie przyciskow
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (files == null || files.Count == 0) return;

            if (currentIndex >= files.Count) return;

            switch (e.Key)
            {
                case Key.D1: HighlightTile(Border1); MoveFile(folderNames[1]); break;
                case Key.D2: HighlightTile(Border2); MoveFile(folderNames[2]); break;
                case Key.D3: HighlightTile(Border3); MoveFile(folderNames[3]); break;
                case Key.D4: HighlightTile(Border4); MoveFile(folderNames[4]); break;
                case Key.D5: HighlightTile(Border5); MoveFile(folderNames[5]); break;
                case Key.D6: HighlightTile(Border6); MoveFile(folderNames[6]); break;
                case Key.D7: HighlightTile(Border7); MoveFile(folderNames[7]); break;
                case Key.D8: HighlightTile(Border8); MoveFile(folderNames[8]); break;
                case Key.D9: HighlightTile(Border9, isSkip: true); SkipFile(); break;
                case Key.D0: HighlightTile(Border0, isDelete: true); DeleteFile(); break;
                case Key.Z: if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control){UndoLastAction(); HighlightTile(BorderB, isUndo: true);}break;
            }
        }
        private void MoveFile(string folderName)
        {
            try
            {
                string currentFile = files[currentIndex];
                string targetFolder = Path.Combine(sourcePath, folderName);
                string destinationFile = Path.Combine(targetFolder, Path.GetFileName(currentFile));

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                AddToHistoryActionStack(currentFile, destinationFile, false);
                File.Move(currentFile, destinationFile);

                currentIndex++;
                DisplayCurrentImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przenoszenia pliku: {ex.Message}");
            }
        }
        private void SkipFile()
        {
            AddToHistoryActionStack(null, null, true);
            currentIndex++;
            DisplayCurrentImage();
        }
        private void DeleteFile()
        {
            try
            {
                string currentFile = files[currentIndex];

                string trashFolder = Path.Combine(sourcePath, "Kosz");
                if (!Directory.Exists(trashFolder)) Directory.CreateDirectory(trashFolder);

                AddToHistoryActionStack(currentFile, Path.Combine(trashFolder, Path.GetFileName(currentFile)), false);
                File.Move(currentFile, Path.Combine(trashFolder, Path.GetFileName(currentFile)));

                currentIndex++;
                DisplayCurrentImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania: {ex.Message}");
            }
        }
    }
}