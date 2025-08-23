using DataConcentrator;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ScadaGUI
{
    public partial class AddTagWindow : Window
    {
        public Tag CreatedTag { get; private set; }

        public AddTagWindow()
        {
            InitializeComponent();
        }
       
        private void cmbTagType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedType = ((ComboBoxItem)cmbTagType.SelectedItem)?.Content.ToString();

            // Sakrij sve panele
            panelInputProps.Visibility = Visibility.Collapsed;
            panelAnalogProps.Visibility = Visibility.Collapsed;
            panelOutputProps.Visibility = Visibility.Collapsed;

            if (selectedType == "DI" || selectedType == "AI")
                panelInputProps.Visibility = Visibility.Visible;

            if (selectedType == "AI")
                panelAnalogProps.Visibility = Visibility.Visible;

            if (selectedType == "DO" || selectedType == "AO")
                panelOutputProps.Visibility = Visibility.Visible;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tag newTag = new Tag
                {
                    Name = txtName.Text,
                    Description = txtDescription.Text,
                    IOAddress = txtIOAddress.Text,
                    Type = (TagType)Enum.Parse(typeof(TagType), ((ComboBoxItem)cmbTagType.SelectedItem).Content.ToString()),
                    Alarms = new List<Alarm>(),
                    ExtraProperties = new Dictionary<TagProperty, object>()
                };

                // Extra props za input tagove
                if (newTag.Type == TagType.DI || newTag.Type == TagType.AI)
                {
                    if (!string.IsNullOrWhiteSpace(txtScanTime.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.scantime] = int.Parse(txtScanTime.Text);
                    newTag.ExtraProperties[DataConcentrator.TagProperty.onoffscan] = chkOnOffScan.IsChecked == true;
                }

                // Extra props za analogne tagove
                if (newTag.Type == TagType.AI)
                {
                    if (!string.IsNullOrWhiteSpace(txtLowLimit.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.lowlimit] = double.Parse(txtLowLimit.Text);
                    if (!string.IsNullOrWhiteSpace(txtHighLimit.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.highlimit] = double.Parse(txtHighLimit.Text);
                    newTag.ExtraProperties[DataConcentrator.TagProperty.units] = txtUnits.Text;
                }

                // Extra props za output tagove
                if (newTag.Type == TagType.DO || newTag.Type == TagType.AO)
                {
                    if (!string.IsNullOrWhiteSpace(txtInitialValue.Text))
                    {
                        newTag.ExtraProperties[DataConcentrator.TagProperty.initialvalue] = double.Parse(txtInitialValue.Text);
                        if (newTag.Type == TagType.DO && !(Convert.ToDouble(txtInitialValue.Text) == 0 || Convert.ToDouble(txtInitialValue.Text) == 1)) 
                        { 
                            newTag.Value = 0;
                            newTag.ExtraProperties[DataConcentrator.TagProperty.initialvalue] = 0;
                            MessageBox.Show("Vrednost DO taga moze biti samo 0 ili 1, postavljenjo na 0");
                        } 
                        else { 
                            newTag.Value = Convert.ToDouble(txtInitialValue.Text); 
                        }
                    }


                }

                // Snimanje u bazu
                using (var db = new ContextClass())
                {
                    db.Tags.Add(newTag);
                    db.SaveChanges();
                }

                this.DialogResult = true; 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
