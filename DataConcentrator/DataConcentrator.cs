using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConcentrator
{
    public class DataConcentrator
    {
        public event EventHandler<ActivatedAlarm> AlarmOccurred;

        public void UpdateTagValue(Tag tag, object newValue)
        {
            tag.WriteValue(newValue);

            foreach (var alarm in tag.Alarms)
            {
                if ((alarm.Type == AlarmType.Above && (double)newValue > alarm.Limit) ||
                    (alarm.Type == AlarmType.Below && (double)newValue < alarm.Limit))
                {
                    var activated = new ActivatedAlarm(alarm, tag.Name);
                    using (var db = new ContextClass())
                    {
                        db.ActivatedAlarms.Add(activated);
                        db.SaveChanges();
                    }

                    AlarmOccurred?.Invoke(this, activated);
                }
            }
        }
    }
}
