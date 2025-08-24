using DataConcentrator;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


namespace ScadaGUI
{
    public partial class TagDetailsWindow : Window
    {
        private Tag selectedTag;
        private Alarm selectedAlarm;


        public TagDetailsWindow(Tag tag)
        {
            InitializeComponent();

            using (var db = new ContextClass())
            {
                selectedTag = db.Tags
                                .Include(t => t.Alarms)
                                .FirstOrDefault(t => t.Id == tag.Id);
            }

            if (selectedTag == null)
            {
                MessageBox.Show("Tag nije pronađen u bazi!");
                Close();
                return;
            }

            txtTagInfo.Text = $"Tag: {selectedTag.Name} ({selectedTag.Type}) - {selectedTag.Description}, IO: {selectedTag.IOAddress}, Vrednost: {selectedTag.Value}";

            LoadExtraProps();

            if (selectedTag.Type == TagType.AI)
                panelAlarms.Visibility = Visibility.Visible;
            else
                panelAlarms.Visibility = Visibility.Collapsed;

            lstAlarms.Items.Clear();
            if (selectedTag.Alarms != null)
            {
                foreach (var alarm in selectedTag.Alarms)
                {
                    lstAlarms.Items.Add(alarm.ToString());
                }
            }
        }
        private void LoadExtraProps()
        {
            panelInputProps.Visibility = Visibility.Collapsed;
            panelAnalogProps.Visibility = Visibility.Collapsed;
            panelOutputProps.Visibility = Visibility.Collapsed;

            if (selectedTag.Type == TagType.DI || selectedTag.Type == TagType.AI)
            {
                panelInputProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.scantime, out object st))
                    txtScanTime.Text = GetValueAsString(st);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.onoffscan, out object onoff))
                    chkOnOffScan.IsChecked = GetValueAsBool(onoff);
            }

            if (selectedTag.Type == TagType.AI || selectedTag.Type == TagType.AO)
            {
                panelAnalogProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.lowlimit, out object ll))
                    txtLowLimit.Text = GetValueAsString(ll);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.highlimit, out object hl))
                    txtHighLimit.Text = GetValueAsString(hl);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.units, out object u))
                    txtUnits.Text = GetValueAsString(u);
            }

            if (selectedTag.Type == TagType.DO || selectedTag.Type == TagType.AO)
            {
                panelOutputProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.initialvalue, out object iv))
                    txtInitialValue.Text = GetValueAsString(iv);
            }
        }

        // Helper metode
        private string GetValueAsString(object value)
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number) return je.GetDouble().ToString();
                if (je.ValueKind == JsonValueKind.String) return je.GetString();
                if (je.ValueKind == JsonValueKind.True) return "True";
                if (je.ValueKind == JsonValueKind.False) return "False";
            }
            return value?.ToString() ?? "";
        }

        private bool? GetValueAsBool(object value)
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True) return true;
                if (je.ValueKind == JsonValueKind.False) return false;
            }
            if (value is bool b) return b;
            return null;
        }


        // Event handler za text polja
        private void txtExtraProp_LostFocus(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;

            try
            {
                if (sender == txtScanTime && double.TryParse(txtScanTime.Text, out double st))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.scantime, st);
                else if (sender == txtLowLimit && double.TryParse(txtLowLimit.Text, out double ll))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.lowlimit, ll);
                else if (sender == txtHighLimit && double.TryParse(txtHighLimit.Text, out double hl))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.highlimit, hl);
                else if (sender == txtUnits)
                    selectedTag.AddProperty(DataConcentrator.TagProperty.units, txtUnits.Text);
                else if (sender == txtInitialValue && double.TryParse(txtInitialValue.Text, out double iv))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.initialvalue, iv); // ovde se odmah update Value
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }

            updateTagInDB(selectedTag);
        }

        // Event handler za checkbox
        private void chkExtraProp_Changed(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;
            selectedTag.AddProperty(DataConcentrator.TagProperty.onoffscan, chkOnOffScan.IsChecked == true);
            updateTagInDB(selectedTag);
        }


        private void updateTagInDB(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }
        }

        private void btnWriteValue_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null)
            {
                MessageBox.Show("Tag nije selektovan!");
                return;
            }

            if (selectedTag.Type == TagType.DI || selectedTag.Type == TagType.AI)
            {
                MessageBox.Show("Ne možete da upišete vrednost u input tag!");
                return;
            }

            object value = txtNewValue.Text;
            if (!double.TryParse(Convert.ToString(value), out double dvalue))
            {
                MessageBox.Show("Greška: uneta vrednost nije broj!");
                return;
            }

            if (selectedTag.Type == TagType.DO && !(dvalue == 0 || dvalue == 1))
            {
                MessageBox.Show("Unesena je pogrešna vrednost za DO tag (dozvoljeno samo 0 ili 1)");
                return;
            }

            selectedTag.WriteValue(value);
            updateTagInDB(selectedTag);

            MessageBox.Show($"Vrednost taga {selectedTag.Name} je ažurirana na {value}");
            txtTagInfo.Text = $"Tag: {selectedTag.Name} ({selectedTag.Type}) - {selectedTag.Description}, IO: {selectedTag.IOAddress}, Vrednost: {selectedTag.Value}";
        }

        private void btnAddAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedTag == null)
                {
                    MessageBox.Show("Tag nije selektovan!");
                    return;
                }

                double limit = Convert.ToDouble(txtAlarmLimit.Text);
                AlarmType type = (AlarmType)Enum.Parse(typeof(AlarmType), ((ComboBoxItem)cbAlarmType.SelectedItem).Content.ToString());
                string message = txtAlarmMessage.Text;

                using (var db = new ContextClass())
                {
                    if (selectedAlarm == null) 
                    {
                        Alarm alarm = new Alarm(limit, type, message)
                        {
                            TagId = selectedTag.Id
                        };

                        db.Alarms.Add(alarm);
                        db.SaveChanges();

                        selectedTag.Alarms.Add(alarm);
                        lstAlarms.Items.Add(alarm.ToString());
                        MessageBox.Show("Alarm uspešno dodat!");
                    }
                    else 
                    {
                        var alarmToUpdate = db.Alarms.FirstOrDefault(a => a.Id == selectedAlarm.Id);
                        if (alarmToUpdate != null)
                        {
                            alarmToUpdate.Limit = limit;
                            alarmToUpdate.Type = type;
                            alarmToUpdate.Message = message;

                            db.SaveChanges();

                            selectedAlarm.Limit = limit;
                            selectedAlarm.Type = type;
                            selectedAlarm.Message = message;

                            int index = lstAlarms.Items.IndexOf(lstAlarms.SelectedItem);
                            lstAlarms.Items[index] = selectedAlarm.ToString();

                            MessageBox.Show("Alarm uspešno izmenjen!");
                        }
                    }
                }

                txtAlarmLimit.Clear();
                txtAlarmMessage.Clear();
                cbAlarmType.SelectedIndex = -1;
                lstAlarms.SelectedItem = null;
                selectedAlarm = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }
        }

        private void btnDeleteAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAlarm == null)
            {
                MessageBox.Show("Nije selektovan alarm za brisanje!");
                return;
            }

            try
            {
                using (var db = new ContextClass())
                {
                    var alarmToDelete = db.Alarms.FirstOrDefault(a => a.Id == selectedAlarm.Id);
                    if (alarmToDelete != null)
                    {
                        db.Alarms.Remove(alarmToDelete);
                        db.SaveChanges();
                    }
                }

                selectedTag.Alarms.Remove(selectedAlarm);
                lstAlarms.Items.Remove(lstAlarms.SelectedItem);

                MessageBox.Show("Alarm uspešno obrisan!");
                selectedAlarm = null;

                txtAlarmLimit.Clear();
                txtAlarmMessage.Clear();
                cbAlarmType.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }
        }


        private void lstAlarms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAlarms.SelectedItem == null) return;

            string selectedText = lstAlarms.SelectedItem.ToString();
            selectedAlarm = selectedTag.Alarms.FirstOrDefault(a => a.ToString() == selectedText);

            if (selectedAlarm != null)
            {
                txtAlarmLimit.Text = selectedAlarm.Limit.ToString();
                cbAlarmType.SelectedItem = cbAlarmType.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == selectedAlarm.Type.ToString());
                txtAlarmMessage.Text = selectedAlarm.Message;
            }
        }


    }
}
