using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class Tag
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string IOAddress { get; private set; }
        public TagType Type { get; private set; }

        public Dictionary<string, object> ExtraProperties { get; set; }

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
            ExtraProperties = new Dictionary<string, object>();
        }

        public void AddProperty(string key, object value)
        {
            switch (key)
            {
                case "scantime":
                case "onoffscan":
                    if (Type == TagType.DO || Type == TagType.AO)
                        throw new Exception($"{key} is allowed only for input tags (DI, AI).");
                    break;

                case "lowlimit":
                case "highlimit":
                case "units":
                    if (Type == TagType.DO || Type == TagType.DI)
                        throw new Exception($"{key} is allowed only for analog tags (AI, AO).");
                    break;

                case "initialvalue":
                    if (Type == TagType.DI && Type == TagType.AI)
                        throw new Exception($"{key} is allowed only for output tags (DO, AO).");
                    break;

                default:
                    throw new Exception($"Property {key} is not supported.");
            }

            ExtraProperties[key] = value;
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

            return $"{Name} ({Type}) - {Description}, I/O: {IOAddress}" +
                   (props != "" ? $", Properties: {props}" : "");
        }

    }
}
