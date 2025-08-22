using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConcentrator
{
    public class ActivatedAlarm
    {
        [Key]
        public int Id { get; set; }
        public Alarm Alarm { get; set; }
        public string TagName { get; set; }
        public DateTime timestamp { get; set; }

        public ActivatedAlarm() { }
        public ActivatedAlarm(Alarm alarm, string tagName)
        {
            Alarm = alarm;
            TagName = tagName;
            timestamp = DateTime.Now;
        }

    }
}
