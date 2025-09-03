using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLCSimulator
{
    /// <summary>
    /// PLC Simulator
    /// 
    /// 4 x ANALOG INPUT : ADDR001 - ADDR004
    /// 4 x ANALOG OUTPUT: ADDR005 - ADDR008
    /// 4 x DIGITAL INPUT: ADDR009 - ADDR012
    /// 4 x DIGITAL OUTPUT: ADDR013 - ADDR016
    /// </summary>
    public class PLCSimulatorManager
    {
        private Dictionary<string, double> addressValues; 
        private object locker = new object();
        private Thread t1;
        private Thread t2;

        public PLCSimulatorManager()
        {
            addressValues = new Dictionary<string, double>();

            // AI
            addressValues.Add("ADDR001", 0);
            addressValues.Add("ADDR002", 0);
            addressValues.Add("ADDR003", 0);
            addressValues.Add("ADDR004", 0);

            // AO
            addressValues.Add("ADDR005", 0);
            addressValues.Add("ADDR006", 0);
            addressValues.Add("ADDR007", 0);
            addressValues.Add("ADDR008", 0);

            // DI
            // TODO: dodati jos nekoliko adresa za DI (recimo po 4 za svaku vrstu tagova)
            addressValues.Add("ADDR009", 0);
            addressValues.Add("ADDR010", 0);
            addressValues.Add("ADDR011", 0);
            addressValues.Add("ADDR012", 0);

            // DO
            // TODO: dodati jos nekoliko adresa za DO (recimo po 4 za svaku vrstu tagova)
            addressValues.Add("ADDR013", 0);
            addressValues.Add("ADDR014", 0);
            addressValues.Add("ADDR015", 0);
            addressValues.Add("ADDR016", 0);
        }

        public void StartPLCSimulator()
        {
            t1 = new Thread(GeneratingAnalogInputs);
            t1.Start();

            t2 = new Thread(GeneratingDigitalInputs);
            t2.Start();
        }

        private void GeneratingAnalogInputs()
        {
            while (true)
            {
                Thread.Sleep(100);

                lock (locker)
                {
                    addressValues["ADDR001"] = 100 * Math.Sin((double)DateTime.Now.Second / 60 * Math.PI); //SINE
                    addressValues["ADDR002"] = 100 * DateTime.Now.Second / 60; //RAMP
                    addressValues["ADDR003"] = 50 * Math.Cos((double)DateTime.Now.Second / 60 * Math.PI); //COS
                    addressValues["ADDR004"] = RandomNumberBetween(0, 50);  //rand
                }
            }
        }

        private void GeneratingDigitalInputs()
        {
            while (true)
            {
                Thread.Sleep(1000);

                lock (locker)
                {
                    addressValues["ADDR009"] = addressValues["ADDR009"] == 1 ? 0 : 1; // Naizmenicno paljenje i gasenje
                    addressValues["ADDR010"] = RandomNumberBetween(0, 50) >= 10 ? 1 : 0; // Random paljenje i gasenje
                    addressValues["ADDR011"] = DateTime.Now.Minute%3==0 ? 0 : 1; // Svaki treci minut upaljen, inace ugasen
                    addressValues["ADDR012"] = (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 16)? 1 : 0; // radno vreme
                }
            }
        }

        public double GetValue(string address)
        {
            lock(locker) {
                if (addressValues.ContainsKey(address))
                {
                    return addressValues[address];
                }
                else
                {
                    return -1;
                }
            }
        }

        public void SetValue(string address, double value)
        {
            lock (locker)
            {
                if (addressValues.ContainsKey(address))
                {
                    addressValues[address] = value;
                }
                else
                {
                    throw new Exception("Ne radi ovde");
                }
            }
        }

        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            Random random = new Random();
            var next = random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        public void Abort()
        {
            t1.Abort();
            t2.Abort();
        }
    }
}
