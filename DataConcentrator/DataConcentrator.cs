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

        public void UpdateTagValue(Tag tag, object newValue)
        {
            tag.Value = Convert.ToDouble(newValue);
            ValueChanged?.Invoke(this, EventArgs.Empty);

            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }


            foreach (var alarm in tag.Alarms)
            {
                if ((alarm.Type == AlarmType.Above && (double)newValue > alarm.Limit) ||
                    (alarm.Type == AlarmType.Below && (double)newValue < alarm.Limit))
                {
                    var activated = new ActivatedAlarm
                    {
                        AlarmId = alarm.Id,
                        TagName = tag.Name,
                        Timestamp = DateTime.Now,
                        Type = Convert.ToString(alarm.Type),
                        Limit = alarm.Limit,
                        Value = tag.Value,
                        Message = alarm.Message
                    };

                    using (var db = new ContextClass())
                    {
                        bool alreadyActive = db.ActivatedAlarms
                            .Any(a => a.AlarmId == alarm.Id && a.TagName == tag.Name);

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
