using DataConcentrator;
using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ScadaGUI
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        private ObservableCollection<ActivatedAlarm> activeAlarms = new ObservableCollection<ActivatedAlarm>();
        private PLC plcWindow;

        private Tag selectedTag;
        public static DataConcentrator.DataConcentrator concentrator;
        private static Dictionary<int, Thread> scanThreadovi = new Dictionary<int, Thread>();

        public MainWindow()
        {
           

            InitializeComponent();
            InitializeDataBase();
            LoadTagsFromDatabase();
            LoadAlarmsFromDatabase();

            concentrator = new DataConcentrator.DataConcentrator();
            concentrator.AlarmOccurred += onAlarmOccurred;
            concentrator.ValueChanged += onValueChanged;

            dgLogs.ItemsSource = activeAlarms;
            dgTags.ItemsSource = tags;

            plcWindow = new PLC();
            plcWindow.Show();

            InitializeOutputs();
            InitializeInputs();
        }

        private void InitializeDataBase()
        {
            using (var db = new ContextClass())
            {
                db.Database.CreateIfNotExists();
            }
        }

        private void LoadTagsFromDatabase()
        {
            int? selectedTagId = null;
            int selectedIndex = -1;

            if (dgTags.SelectedItem is Tag selected)
            {
                selectedTagId = selected.Id;
                selectedIndex = dgTags.SelectedIndex;
            }

            using (var db = new ContextClass())
            {
                var sortedTags = db.Tags
                    .AsNoTracking()
                    .OrderBy(t => t.Type == TagType.DI ? 0 :
                                  t.Type == TagType.DO ? 1 :
                                  t.Type == TagType.AI ? 2 : 3)
                    .ThenBy(t => t.Name)
                    .ToList();

                tags.Clear();
                foreach (var tag in sortedTags)
                    tags.Add(tag);
            }

            if (selectedTagId.HasValue)
            {
                var restoredTag = tags.FirstOrDefault(t => t.Id == selectedTagId.Value);
                if (restoredTag != null)
                {
                    dgTags.SelectedItem = restoredTag;

                }
                else if (selectedIndex >= 0 && selectedIndex < dgTags.Items.Count)
                {
                    dgTags.SelectedIndex = selectedIndex;
                    dgTags.ScrollIntoView(dgTags.Items[selectedIndex]);
                }
            }
        }



        private void InitializeOutputs()
        {
            using (var db = new ContextClass())
            {
                var sortedTags = db.Tags
                    .AsNoTracking()                    
                    .ToList();

                foreach (var tag in sortedTags)
                {
                    if (tag.Type == TagType.DO || tag.Type == TagType.AO)
                    {
                        concentrator.ForceTagValue(tag, tag.Value);
                    }
                   
                }
            }
        }

        private void InitializeInputs()
        {
            using (var db = new ContextClass())
            {
                var sortedTags = db.Tags
                    .AsNoTracking()
                    .ToList();

                foreach (var tag in sortedTags)
                {
                    if (tag.Type == TagType.DI || tag.Type == TagType.AI)
                    {
                        if (Convert.ToString(tag.ExtraProperties[DataConcentrator.TagProperty.onoffscan]) == "True")
                        {
                            
                            ScanInputOn(tag); 
                        }                
                    }
                }
            }
        }

        public static void ScanInputOn(Tag tag)
        {
            int scanTime = (int)Convert.ToDouble(Convert.ToString(tag.ExtraProperties[DataConcentrator.TagProperty.scantime]));
            Thread t = new Thread(() =>
            {
                while(true)
                {
                   Thread.Sleep(scanTime);
                   concentrator.ReadTagValue(tag);
                }
            });

            if (!scanThreadovi.ContainsKey(tag.Id))
            {
                scanThreadovi.Add(tag.Id, t);
                t.Start();
            }
        }

        public static void ScanInputOff(Tag tag)
        {
            if (scanThreadovi.ContainsKey(tag.Id))
            {
                Thread t = scanThreadovi[tag.Id];
                t.Abort();
                scanThreadovi.Remove(tag.Id);
            }
        }

        private void LoadAlarmsFromDatabase()
        {
            using (var db = new ContextClass())
            {
                var sortedAlarms = db.ActivatedAlarms.ToList()
                    .Where(a => a.Active)
                    .OrderBy(a => a.Timestamp);

                activeAlarms = new ObservableCollection<ActivatedAlarm>(sortedAlarms);
                dgLogs.ItemsSource = activeAlarms;
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTagWindow addWindow = new AddTagWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadTagsFromDatabase();
                MessageBox.Show("Uspesno dodat Tag!");
            }
        }

        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            selectedTag = dgTags.SelectedItem as Tag;
            if (selectedTag == null)
            {
                MessageBox.Show("Niste selektovali tag!");
                return;
            }

            try
            {
                using (var db = new ContextClass())
                {
                    // Učitaj tag zajedno sa njegovim alarmima
                    var tagToDelete = db.Tags
                                        .Include("Alarms")
                                        .FirstOrDefault(t => t.Id == selectedTag.Id);

                    if (tagToDelete != null)
                    {
                        // Izvadi listu ID-eva alarm-a
                        var alarmIds = tagToDelete.Alarms.Select(al => al.Id).ToList();

                        // Obriši sve aktivne ili neaktivne ActivatedAlarms koji su vezani za ove alarme
                        var activatedToDelete = db.ActivatedAlarms
                            .Where(a => alarmIds.Contains(a.AlarmId));

                        db.ActivatedAlarms.RemoveRange(activatedToDelete);

                        // Obriši sve alarme vezane za tag
                        db.Alarms.RemoveRange(tagToDelete.Alarms);

                        // Obriši tag
                        db.Tags.Remove(tagToDelete);

                        db.SaveChanges();
                    }
                }

                // Osvježi prikaz
                LoadTagsFromDatabase();
                selectedTag = null;

                MessageBox.Show("Tag i svi povezani alarmi uspešno obrisani!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }
        }



        private void dgTags_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tag tag = dgTags.SelectedItem as Tag;
            if (tag == null) return;

            TagDetailsWindow detailsWindow = new TagDetailsWindow(tag);
            detailsWindow.ShowDialog();

            LoadTagsFromDatabase();
        }

        private void onAlarmOccurred(object sender, ActivatedAlarm e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                activeAlarms.Add(e);
            });
        }
        private void onValueChanged(object sender, EventArgs args)
        {
            Application.Current.Dispatcher.Invoke(() => {  
                LoadTagsFromDatabase();
            });
        }

        private void btnAckAlarm_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            if (button?.DataContext is ActivatedAlarm alarmToAck)
            {
                try
                {
                    using (var db = new ContextClass())
                    {
                        var alarm = db.ActivatedAlarms.Find(alarmToAck.Id);
                        if (alarm != null)
                        {
                            alarm.Active = false;
                            db.SaveChanges();
                        }
                    }

                    LoadAlarmsFromDatabase();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri potvrdi alarma: {ex.Message}");
                }
            }
        }


    }
}
