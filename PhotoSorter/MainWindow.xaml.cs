using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Threading;
/*
 Co dodano:
rotowanie zdjec pod lewa strzalka i prawa
odczyt video z sekundnikiem oraz poprawa wygladu opisu w lewym gornym rogu
zapisywanie nazwa folderow
 */
namespace PhotoSorter
{
    public partial class MainWindow : Window
    {
        private string sourcePath = "";

        private List<string> files;
        private int currentIndex = 0;
        private DispatcherTimer videoTimer;
        private int currentRotation = 0;
        private string configFilePath = "folders_config.txt";


        private string[] folderNames = new string[9];

        private List<HistoryActionStack> history = new List<HistoryActionStack>();

        public MainWindow()
        {
            InitializeComponent();
            LoadFolderConfig();

            videoTimer = new DispatcherTimer();
            videoTimer.Interval = TimeSpan.FromMilliseconds(500); // Odświeżaj co pół sekundy
            videoTimer.Tick += VideoTimer_Tick;
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
                        StopAndReleaseMedia();

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

        // zapis do txt
        private void SaveFolderConfig()
        {
            try
            {
                string[] folderNamesToSave =
                {
                    ConfigBox1.Text, ConfigBox2.Text, ConfigBox3.Text, ConfigBox4.Text, ConfigBox5.Text, ConfigBox6.Text, ConfigBox7.Text, ConfigBox8.Text
                };
                File.WriteAllLines(configFilePath, folderNamesToSave);
            }
            catch (Exception ex)
            {
            
                MessageBox.Show($"Blad zapisu konfiguracji: {ex.Message}");
            }
        }
        private void LoadFolderConfig()
        {
            try
            {
                if(File.Exists(configFilePath))
                {
                    string[] loadedFolderNames = File.ReadAllLines(configFilePath);

                    if (loadedFolderNames.Length >= 8)
                    {
                        ConfigBox1.Text = loadedFolderNames[0];
                        ConfigBox2.Text = loadedFolderNames[1];
                        ConfigBox3.Text = loadedFolderNames[2];
                        ConfigBox4.Text = loadedFolderNames[3];
                        ConfigBox5.Text = loadedFolderNames[4];
                        ConfigBox6.Text = loadedFolderNames[5];
                        ConfigBox7.Text = loadedFolderNames[6];
                        ConfigBox8.Text = loadedFolderNames[7];

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania konfiguracji: {ex.Message}");
            }
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

            SaveFolderConfig();

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
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                switch (btn.Tag.ToString())
                {
                    case "1": ConfigBox1.Text = "-"; break;
                    case "2": ConfigBox2.Text = "-"; break;
                    case "3": ConfigBox3.Text = "-"; break;
                    case "4": ConfigBox4.Text = "-"; break;
                    case "5": ConfigBox5.Text = "-"; break;
                    case "6": ConfigBox6.Text = "-"; break;
                    case "7": ConfigBox7.Text = "-"; break;
                    case "8": ConfigBox8.Text = "-"; break;
                }
            }
        }

        // pobieranie i wyswietlanie pliku
        private void RotateMediaRight()
        {
            if (currentRotation != 360)
            {
                currentRotation += 90;
            }
            else
            {
                currentRotation = 90;
            }
            RotateTransform rotateTransform = new RotateTransform(currentRotation);
            MainImage.LayoutTransform = rotateTransform;
            MainVideo.LayoutTransform = rotateTransform;
            
        }
        private void RotateMediaLeft()
        {
            if(currentRotation != 0)
            {
                currentRotation -= 90;
            }
            else
            {
                currentRotation = 270;
            }
            RotateTransform rotateTransform = new RotateTransform(currentRotation);
            MainImage.LayoutTransform = rotateTransform;
            MainVideo.LayoutTransform = rotateTransform;
        }
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
                                         s.ToLower().EndsWith(".gif") ||
                                         s.ToLower().EndsWith(".mp4") ||
                                         s.ToLower().EndsWith(".avi") ||
                                         s.ToLower().EndsWith(".mov"))
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
        private void StopAndReleaseMedia()
        {
            if (videoTimer != null) videoTimer.Stop();

            MainImage.Source = null;
            MainVideo.Stop();
            MainVideo.Close(); // Bardzo ważne dla wideo!
            MainVideo.Source = null;
        }
        private void DisplayCurrentImage()
        {
            StopAndReleaseMedia();

            MainImage.LayoutTransform = new RotateTransform(currentRotation);
            MainVideo.LayoutTransform = new RotateTransform(currentRotation);

            if (currentIndex < files.Count)
            {
                string filePath = files[currentIndex];
                string extension = Path.GetExtension(filePath).ToLower();

                FileNameText.Text = $"Nazwa pliku: {Path.GetFileName(filePath)}\nPrzesortowano: ({currentIndex + 1}/{files.Count})";

                try
                {
                    if (extension == ".mp4" || extension == ".avi" || extension == ".mov")
                    {
                        // Tryb WIDEO
                        MainImage.Visibility = Visibility.Collapsed;
                        MainVideo.Visibility = Visibility.Visible;

                        MainVideo.Source = new Uri(filePath);

                        MainVideo.Play();
                    }
                    else
                    {
                        // Tryb ZDJĘCIE
                        MainVideo.Visibility = Visibility.Collapsed;
                        MainImage.Visibility = Visibility.Visible;

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Zapobiega blokowaniu plików .jpg
                        bitmap.EndInit();

                        MainImage.Source = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd ładowania zdjęcia: " + ex.Message);
                }
            }
            else
            {
                MainImage.Visibility = Visibility.Collapsed;
                MainVideo.Visibility = Visibility.Collapsed;
                FileNameText.Text = "Koniec! Wszystkie zdjęcia posortowane.";
            }
        }
        private void MainVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Cofa film na początek i odpala go ponownie (zapętlanie)
            MainVideo.Position = TimeSpan.Zero;
            MainVideo.Play();
        }
        private void MainVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Wideo jest już w pełni załadowane do pamięci i gotowe.
            // Czasami mały "reset" pozycji pomaga ubić ostatnie zacięcia:
            MainVideo.Position = TimeSpan.Zero;
            MainVideo.Play();
            videoTimer.Start();
        }
        private void VideoTimer_Tick(object sender, EventArgs e)
        {
            if (MainVideo.Source != null && MainVideo.NaturalDuration.HasTimeSpan)
            {
                TimeSpan currentPosition = MainVideo.Position;
                TimeSpan totalDuration = MainVideo.NaturalDuration.TimeSpan;

                string timeString = string.Format("{0:mm\\:ss} / {1:mm\\:ss}", currentPosition, totalDuration);
                string filePath = files[currentIndex];

                // Odświeżamy CAŁY tekst, dopisując na końcu nową linijkę z czasem
                FileNameText.Text = $"Nazwa pliku: {Path.GetFileName(filePath)}\nPrzesortowano: ({currentIndex + 1}/{files.Count})\nSekunda filmiku: {timeString}";
            }
        }
        private string ShowInputDialog(string title)
        {
            Window inputWindow = new Window()
            {
                Title = title,
                Width = 350,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel stack = new StackPanel() { Margin = new Thickness(15) };
            TextBlock textBlock = new TextBlock() { Text = "Wprowadź docelową nazwę dla tego folderu:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
            TextBox textBox = new TextBox() { FontSize = 14, Padding = new Thickness(5), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            Button okButton = new Button() { Content = "ZAPISZ I PRZENIEŚ", Margin = new Thickness(0, 15, 0, 0), Padding = new Thickness(8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")), Foreground = Brushes.White, BorderThickness = new Thickness(0), IsDefault = true, Cursor = Cursors.Hand };

            okButton.Click += (s, e) => { inputWindow.DialogResult = true; };

            stack.Children.Add(textBlock);
            stack.Children.Add(textBox);
            stack.Children.Add(okButton);
            inputWindow.Content = stack;

            inputWindow.Loaded += (s, e) => textBox.Focus();

            if (inputWindow.ShowDialog() == true)
            {
                return textBox.Text.Trim();
            }
            return null;
        }
        private void HandleFolderAction(int index, Border border, TextBlock label)
        {
            string currentName = folderNames[index];

            if (string.IsNullOrWhiteSpace(currentName) || currentName == "-")
            {
                string newName = ShowInputDialog($"Nazywanie folderu pod [{index}]");

                if (string.IsNullOrWhiteSpace(newName) || newName == "-")
                    return;

                newName = ValidateName(newName, "Folder_" + index);

                folderNames[index] = newName;
                label.Text = newName;

                switch (index)
                {
                    case 1: ConfigBox1.Text = newName; break;
                    case 2: ConfigBox2.Text = newName; break;
                    case 3: ConfigBox3.Text = newName; break;
                    case 4: ConfigBox4.Text = newName; break;
                    case 5: ConfigBox5.Text = newName; break;
                    case 6: ConfigBox6.Text = newName; break;
                    case 7: ConfigBox7.Text = newName; break;
                    case 8: ConfigBox8.Text = newName; break;
                }
                SaveFolderConfig();
            }

            HighlightTile(border);
            MoveFile(folderNames[index]);
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
                case Key.D1: HandleFolderAction(1, Border1, Lbl1); break;
                case Key.D2: HandleFolderAction(2, Border2, Lbl2); break;
                case Key.D3: HandleFolderAction(3, Border3, Lbl3); break;
                case Key.D4: HandleFolderAction(4, Border4, Lbl4); break;
                case Key.D5: HandleFolderAction(5, Border5, Lbl5); break;
                case Key.D6: HandleFolderAction(6, Border6, Lbl6); break;
                case Key.D7: HandleFolderAction(7, Border7, Lbl7); break;
                case Key.D8: HandleFolderAction(8, Border8, Lbl8); break;
                case Key.D9: HighlightTile(Border9, isSkip: true); SkipFile(); break;
                case Key.D0: HighlightTile(Border0, isDelete: true); DeleteFile(); break;
                case Key.Z: if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control){UndoLastAction(); HighlightTile(BorderB, isUndo: true);}break;
                case Key.Right: RotateMediaRight(); break;
                case Key.Left: RotateMediaLeft(); break;

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

                StopAndReleaseMedia();

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

                StopAndReleaseMedia();

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