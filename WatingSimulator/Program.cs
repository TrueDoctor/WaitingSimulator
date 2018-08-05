using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WatingSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Number of Persons");
            int count = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());

            Application.Run(new QueueView(count));
        }
    }
}
