using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace DataConcentrator
{
    public class DataConcentrator
    {
        public event EventHandler<ActivatedAlarm> AlarmOccurred;
        public event EventHandler ValueChanged;
        public static PLCSimulatorManager PLC;


        public DataConcentrator() {
            PLC = new PLCSimulatorManager();
        }
        public void ReadTagValue(Tag tag)
        {
            // Uzmi novu vrednost iz simulatora
            tag.Value = Convert.ToDouble(PLC.GetValue(tag.IOAddress));

            using (var db = new ContextClass())
            {
                // Učitaj tag iz baze zajedno sa alarmima
                var selectedTag = db.Tags
                    .Include("Alarms")
                    .FirstOrDefault(t => t.Id == tag.Id);

                if (selectedTag != null)
                {
                    selectedTag.Value = tag.Value;

                    db.Tags.AddOrUpdate(selectedTag);
                    db.SaveChanges();

                    ValueChanged?.Invoke(this, EventArgs.Empty);

                    if (selectedTag.Alarms != null)
                    {
                        foreach (var alarm in selectedTag.Alarms)
                        {
                            if ((alarm.Type == AlarmType.Above && selectedTag.Value > alarm.Limit) ||
                                (alarm.Type == AlarmType.Below && selectedTag.Value < alarm.Limit))
                            {
                                var activated = new ActivatedAlarm
                                {
                                    AlarmId = alarm.Id,
                                    TagName = selectedTag.Name,
                                    Timestamp = DateTime.Now,
                                    Type = Convert.ToString(alarm.Type),
                                    Limit = alarm.Limit,
                                    Value = selectedTag.Value,
                                    Message = alarm.Message,
                                    Active = true
                                };

                                bool alreadyActive = db.ActivatedAlarms
                                    .Any(a => a.AlarmId == alarm.Id && a.TagName == selectedTag.Name && a.Active);

                                if (!alreadyActive)
                                {
                                    db.ActivatedAlarms.Add(activated);
                                    db.SaveChanges();
                                    AlarmOccurred?.Invoke(this, activated);
                                }
                            }
                        }
                    }
                }
            }
        }


        public void ForceTagValue(Tag tag, object newValue)
        {
            PLC.SetValue(tag.IOAddress, Convert.ToDouble(newValue));
            tag.Value = Convert.ToDouble(newValue);
            
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }
            
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void addTag(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }

            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
