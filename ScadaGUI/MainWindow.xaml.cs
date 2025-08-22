using DataConcentrator;
using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ScadaGUI
{
    public partial class MainWindow : Window
    {
        private Tag selectedTag;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataBase();
            LoadTagsFromDatabase();
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
            lstTags.Items.Clear();
            using (var db = new ContextClass())
            {
                db.Database.CreateIfNotExists();
                foreach (var tag in db.Tags.ToList())
                {
                    lstTags.Items.Add(tag);
                }

            }
        }
        private void saveTagToDB(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.Add(tag);
                db.SaveChanges();
            }
        }

        private void updateTagInDB(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            string name = txtTagName.Text;
            string description = txtDescription.Text;
            string ioAddress = txtIOAddress.Text;
            TagType type = (TagType)Enum.Parse(typeof(TagType), ((ComboBoxItem)cmbTagType.SelectedItem).Content.ToString());

            try
            {
                Tag newTag = new Tag(name, description, ioAddress, type);

                saveTagToDB(newTag);

                lstTags.Items.Add(newTag);
                MessageBox.Show("Tag added successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void lstTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTag = lstTags.SelectedItem as Tag;
            if (selectedTag == null) return; 

            txtTagValue.Text = selectedTag.Value.ToString();

            lstAlarms.Items.Clear();
            if(selectedTag.Alarms != null)
            {
                foreach (var alarm in selectedTag.Alarms)
                {
                    lstAlarms.Items.Add(alarm.ToString());
                }
            }
            
        }


        private void btnWriteValue_Click(object sender, RoutedEventArgs e)
        {
            
            Tag selectedTag = lstTags.SelectedItem as Tag;
            if (selectedTag == null)
            {
                MessageBox.Show("Niste selektovali tag!");
                return;
            }
            if(selectedTag.Type == TagType.DI || selectedTag.Type == TagType.AI)
            {
                MessageBox.Show("Ne mozete da upisete vrednost u input tag");
                return;
            }

            object value = txtTagValue.Text;
            double dvalue;
            if (!double.TryParse(Convert.ToString(value), out dvalue))
            {
                MessageBox.Show("Greška: uneta vrednost nije broj!");
            }

            if (selectedTag.Type == TagType.DO && !(Convert.ToDouble(value) == 0 || Convert.ToDouble(value) == 1))
            {
                MessageBox.Show("Unesena je pogresna vrednost za DO tag");
                value = 0;
            }
            selectedTag.WriteValue(value);

            updateTagInDB(selectedTag);
            MessageBox.Show($"Vrednost taga {selectedTag.Name} je ažurirana na {value}");
        }


        private void btnAddAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;

            try
            {
                double limit = Convert.ToDouble(txtAlarmLimit.Text);
                AlarmType type = (AlarmType)Enum.Parse(typeof(AlarmType), ((ComboBoxItem)cbAlarmType.SelectedItem).Content.ToString());
                string message = txtAlarmMessage.Text;

                Alarm alarm = new Alarm(limit, type, message);
                selectedTag.addAlarm(alarm);

                using (var db = new ContextClass())
                {
                    db.Tags.Attach(selectedTag);
                    db.Entry(selectedTag).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                lstAlarms.Items.Add(alarm.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;

            try
            {
                using (var db = new ContextClass())
                {
                    db.Tags.Attach(selectedTag);
                    db.Tags.Remove(selectedTag);
                    db.SaveChanges();
                }

                lstTags.Items.Remove(selectedTag);
                lstAlarms.Items.Clear();
                selectedTag = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
