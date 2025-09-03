using DataConcentrator;
using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            InitialWriteToPLC();
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
            using (var db = new ContextClass())
            {
                var sortedTags = db.Tags
                    .AsNoTracking()
                    .OrderBy(t => t.Type == TagType.DI ? 0 :
                                  t.Type == TagType.DO ? 1 :
                                  t.Type == TagType.AI ? 2 : 3)
                    .ToList();

                tags.Clear();                          
                foreach (var tag in sortedTags)
                {
                    tags.Add(tag);                     
                }
            }
        }

        private void InitialWriteToPLC()
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
                    db.Tags.Attach(selectedTag);
                    db.Tags.Remove(selectedTag);
                    db.SaveChanges();
                }

                LoadTagsFromDatabase();
                selectedTag = null;
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
                dgTags.ItemsSource = null;
                dgTags.ItemsSource = tags;
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
