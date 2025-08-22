using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataConcentrator
{
    public enum TagType
    {
        DI,
        DO,
        AI,
        AO
    }
    public enum TagProperty
    {
        scantime,
        onoffscan,
        lowlimit,
        highlimit,
        units,
        initialvalue
    }

    public class Tag
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IOAddress { get; set; }
        public TagType Type { get; set; }
        public List<Alarm> Alarms { get; set; }
        public double Value { get; set; }
        
        [NotMapped]
        public Dictionary<TagProperty, object> ExtraProperties { get; set; }

        [Column("ExtraProperties")]
        public string ExtraPropertiesJson
        {
            get => JsonSerializer.Serialize(ExtraProperties);
            set => ExtraProperties = string.IsNullOrEmpty(value)
                ? new Dictionary<TagProperty, object>()
                : JsonSerializer.Deserialize<Dictionary<TagProperty, object>>(value);
        }


        public Tag() { }
        public Tag(string name, string description, string ioAddress, TagType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("ERROR: Wrong Format Tag name");
            if (string.IsNullOrWhiteSpace(ioAddress))
                throw new Exception("ERROR: Wrong Format I/O Address");

            Name = name;
            Description = description;
            IOAddress = ioAddress;
            Type = type;
            ExtraProperties = new Dictionary<TagProperty, object>();
            Alarms = new List<Alarm>();
            Value = 0;
        }

       
        public void AddProperty(TagProperty key, object value)
        {

            if (key == TagProperty.scantime || key == TagProperty.onoffscan)
            {
                if (Type == TagType.DO || Type == TagType.AO)
                    throw new Exception($"{key} is allowed only for input tags (DI, AI).");
            }

            if (key == TagProperty.lowlimit || key == TagProperty.highlimit || key == TagProperty.units)
            {
                if (Type == TagType.DO || Type == TagType.DI)
                    throw new Exception($"{key} is allowed only for analog tags (AI, AO).");
            }

            if (key == TagProperty.initialvalue)
            {
                if (Type == TagType.DI || Type == TagType.AI)
                    throw new Exception($"{key} is allowed only for output tags (DO, AO).");
            }

            ExtraProperties[key] = value;
        }

        

        public void EnableScan()
        {
            if (!(bool)ExtraProperties[TagProperty.onoffscan])
            {
                ExtraProperties[TagProperty.onoffscan] = true;
            }
        }
        public void DisableScan()
        {
            if ((bool)ExtraProperties[TagProperty.onoffscan])
            {
                ExtraProperties[TagProperty.onoffscan] = false;
            }
        }

        public void WriteValue(object value)
        {
            if (Type == TagType.DO || Type == TagType.AO)
            {
                Value = Convert.ToDouble(value);
            }
            else
            {
                Console.WriteLine("Cant write to Input Type Tags");
            }
        }

        public void addAlarm(Alarm alarm)
        {
            if (Type == TagType.AI)
            {
                Alarms.Add(alarm);
            }
        }

        public void removeAlarm(Alarm alarm)
        {
            Alarms.Remove(alarm);
        }

        public override string ToString()
        {
            string props = "";

            if (ExtraProperties.Count > 0)
            {
                foreach (var kvp in ExtraProperties)
                {
                    props += $"{kvp.Key}: {kvp.Value}, ";
                }
                props = props.Substring(0, props.Length - 2);
            }

            return $"{Name} ({Type}) - {Description}, I/O: {IOAddress}, Value: {Value}" +
                   (props != "" ? $", Properties: {props}" : "");
        }


    }
}
