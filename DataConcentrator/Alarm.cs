using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConcentrator
{
    public enum AlarmType
    {
        Above, 
        Below 
    }

    public class Alarm
    {
        [Key]
        public int Id {  get; set; }
        public double Limit { get; set; }
        public AlarmType Type { get; set; }
        public string Message { get; set; }

        public int TagId { get; set; }
        
        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }

        public Alarm() { }
        public Alarm(double limit, AlarmType type, string message)
        {
            Limit = limit;
            Type = type;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Type} {Limit}: {Message}";
        }
    }

}
