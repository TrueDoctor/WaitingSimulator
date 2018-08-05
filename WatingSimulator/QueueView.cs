using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WatingSimulator
{
    public partial class QueueView : Form
    {
        private readonly double _initialSpace = 0.5;
        private bool _open;
        private readonly double scale = 80;
        private readonly double finish;
        private List<Person> _persons = new List<Person>();
        private readonly object _lockList = new object();
        private uint bufferSize = 10;
        private uint buffer = 10;

        public QueueView(int count)
        {
            InitializeComponent();

            for (int i = count - 1; i >= 0; i--)
            {
                _persons.Add(new Person(i * _initialSpace));
            }

            finish = count * _initialSpace;

            new Thread(DoPhysics).Start();

            var time = new Timer { Interval = 30 };
            time.Tick += (sender, args) => PaintFrame();
            time.Start();

            var queueProcessing = new Timer {Interval = 5000};
            queueProcessing.Tick += (sender, args) => { buffer -= (uint)(buffer == 0 ? 0 : 1);};
            queueProcessing.Start();

        }

        private void PaintFrame()
        {
            var frame = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            using (Graphics g = Graphics.FromImage(frame))
            {
                var temp = new List<Person>();

                lock (_lockList)
                {
                    _persons.ForEach(element => { temp.Add(element); });
                }

                var offset = pictureBox1.Height - ConvertToPixel(finish).Y;

                foreach (var person in temp)
                {
                    var pt = ConvertToPixel(person.Position, offset);
                    //frame.SetPixel(pt.X, pt.Y, Color.Black);
                    if(pt.Y>=0)
                    { g.FillEllipse(new SolidBrush(Color.Black), pt.X, pt.Y, (int)scale/5, (int)scale/2);}
                }
            }

            pictureBox1.Image?.Dispose();

            pictureBox1.Image = frame;

        }

        private Point ConvertToPixel(double position, int Y = 0)
        {
            int yHelp = 0;
            position *= scale;
            //position += 169;
            
            var width = pictureBox1.Size.Width - (int)scale/5;
            int height = pictureBox1.Size.Height - (int)scale/2;

            //position -= ( (int) (finish * scale % width));
            
            yHelp += (int) (finish*scale / width);

            position += (yHelp-1) * width;

            position -= ( (int) (finish * scale % width));

            int x = (int)(position % width);

            int y = (int)(position / width) * (int)scale;
            y -= (yHelp+1) * (int)scale - height;
            y += Y;

            
            //int y = (int)((int)(scale * position / -height) / scale + (height - (int)(scale * finish * scale / height) / scale));

            return new Point(x, y);
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Size = Size;
        }


        private void DoPhysics()
        {
            var watch = new Stopwatch();
            watch.Start();
            double time = 0.001;
            while (true)
            {
                //Parallel.For(0, persons.Count - 2,
                //i => persons[i].DoStep(persons[i + 1].Position - persons[i].Position,
                // time));
                for (int i = 0; i < _persons.Count - 1; i++)
                {
                    _persons[i].DoStep(_persons[i + 1].Position - _persons[i].Position, time);
                }

                if (_persons.Count != 0)
                {
                    var last = _persons.Last();

                    double distance = _open && buffer < bufferSize? int.MaxValue : finish - last.Position;
                    last.DoStep(distance, time);
                    if (last.Position > finish)
                    {
                        buffer++;
                        _persons.Remove(last);
                    }
                }

                time = watch.Elapsed.TotalSeconds * 10;
                Thread.Sleep(1);
                watch.Restart();
                _persons = _persons.OrderBy(x => x.Position).ToList();
            }

        }

        private void QueueView_KeyPress(object sender, KeyPressEventArgs e)
        {
            _open = !_open;
        }
    }


    public class ThreadSafeListWithLock<T> : IList<T>
    {
        private List<T> internalList;

        private readonly object lockList = new object();

        public ThreadSafeListWithLock()
        {
            internalList = new List<T>();
        }

        // Other Elements of IList implementation

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        public List<T> Clone()
        {
            ThreadLocal<List<T>> threadClonedList = new ThreadLocal<List<T>>();

            lock (lockList)
            {
                internalList.ForEach(element => { threadClonedList.Value.Add(element); });
            }

            return (threadClonedList.Value);
        }

        public void Add(T item)
        {
            lock (lockList)
            {
                internalList.Add(item);
            }
        }

        public bool Remove(T item)
        {
            bool isRemoved;

            lock (lockList)
            {
                isRemoved = internalList.Remove(item);
            }

            return (isRemoved);
        }

        public void Clear()
        {
            lock (lockList)
            {
                internalList.Clear();
            }
        }

        public bool Contains(T item)
        {
            bool containsItem;

            lock (lockList)
            {
                containsItem = internalList.Contains(item);
            }

            return (containsItem);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (lockList)
            {
                internalList.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                int count;

                lock ((lockList))
                {
                    count = internalList.Count;
                }

                return (count);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            int itemIndex;

            lock ((lockList))
            {
                itemIndex = internalList.IndexOf(item);
            }

            return (itemIndex);
        }

        public void Insert(int index, T item)
        {
            lock ((lockList))
            {
                internalList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock ((lockList))
            {
                internalList.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                lock ((lockList))
                {
                    return internalList[index];
                }
            }
            set
            {
                lock ((lockList))
                {
                    internalList[index] = value;
                }
            }
        }
    }
}
