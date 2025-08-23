using DataConcentrator;
using System;
using System.Linq;
using System.Windows;

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
            using (var db = new ContextClass())
            {
                // sortiranje: DI, DO, AI, AO
                var sortedTags = db.Tags.ToList()
                    .OrderBy(t => t.Type == TagType.DI ? 0 :
                                  t.Type == TagType.DO ? 1 :
                                  t.Type == TagType.AI ? 2 : 3)
                    .ToList();

                dgTags.ItemsSource = sortedTags;
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            // Ovdje možeš otvoriti prozor AddTagWindow
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
    }
}
