using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace WatingSimulator
{
    class Person
    {
        private static Random rand = new Random();
        private static uint _id;
        private double _velocity;
        private double maxAcc = 5;
        private double maxVelocity = 2;
        private double growthRate = 5;
        private double comfortZone = 0.4;

        public Person(double position)
        {
            Position = position;
            ID = _id++;
            maxVelocity += (0.2 - rand.NextDouble()/2);
            maxAcc += (0.2 - rand.NextDouble()/2);
        }

        public uint ID { get; }
        public double Position { get; private set; }
        public int StartedWalking { get; set; } = 0;

        public double Velocity
        {
            get { return _velocity; }
            set => _velocity = value < 0 ? 0 : value;
        }

        public void DoStep(double distance, double dt)
        {
            var acc = GetAcc(distance);

            acc -= (maxAcc / (maxVelocity * maxVelocity * 2) )* Velocity * Velocity;

            Velocity += acc * dt;
            Position += Velocity * dt + (acc>0?acc:0) * dt * 0.5;
        }

        private double GetAcc(double distance)
        {
            var acc = (maxAcc / (1 + Math.Exp(-growthRate * (distance - comfortZone * 2)))) * 2 - maxAcc;

            if (distance < comfortZone/2)
            {
                return -acc * 5;
            }

            return acc;
        } 

    }
}
