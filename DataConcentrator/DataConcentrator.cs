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

            tag.Value = Convert.ToDouble(PLC.GetValue(tag.IOAddress));
           
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }

            ValueChanged?.Invoke(this, EventArgs.Empty);


            foreach (var alarm in tag.Alarms)
            {
                if ((alarm.Type == AlarmType.Above && (double)tag.Value > alarm.Limit) ||
                    (alarm.Type == AlarmType.Below && (double)tag.Value < alarm.Limit))
                {
                    var activated = new ActivatedAlarm
                    {
                        AlarmId = alarm.Id,
                        TagName = tag.Name,
                        Timestamp = DateTime.Now,
                        Type = Convert.ToString(alarm.Type),
                        Limit = alarm.Limit,
                        Value = tag.Value,
                        Message = alarm.Message,
                        Active = true
                    };

                    using (var db = new ContextClass())
                    {
                        bool alreadyActive = db.ActivatedAlarms
                            .Any(a => a.AlarmId == alarm.Id && a.TagName == tag.Name && a.Active);

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
