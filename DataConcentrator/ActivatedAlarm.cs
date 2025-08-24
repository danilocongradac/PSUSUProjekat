using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    public class ActivatedAlarm
    {
        [Key]
        public int Id { get; set; }

        public int AlarmId { get; set; }
        [ForeignKey("AlarmId")]
        public Alarm Alarm { get; set; }

        public string TagName { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public double Value { get; set; }
        public double Limit { get; set; }
        public string Message { get; set; }

        public ActivatedAlarm() { }

        public ActivatedAlarm(Alarm alarm, string tagName)
        {
            Alarm = alarm;
            AlarmId = alarm.Id;
            TagName = tagName;
            Timestamp = DateTime.Now;
        }
    }
}
