using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace mdc.Controls.LiveGraph
{
    class GraphBackend
    {     
        protected Rect XAxisRect;
        protected Rect YAxisRect;
        protected Rect GraphRect; 
        protected RingBuffer<DateTime> Time;
        protected List<RingBuffer<float>> Channels;

        protected float m_viewWidth = 1;
        
        public int ChannelCount     { get { return Channels.Count; } }
        public int ChannelCapacity  { get; protected set; }
        public double Width         { get; protected set; }
        public double Height        { get; protected set; }                
        public float FrameRate      { get; protected set; }
        public float MinValue       { get; protected set; }
        public float MaxValue       { get; protected set; }    
        public bool IsLocked        { get; protected set; }
        public int Ticksize         { get; set; }
        public bool AutoScale       { get; set; }
        public float Minimum        { get; set; }
        public float Maximum        { get; set; }

        public GraphBackend()
        {
            Ticksize        = 2;                                 
            Minimum         = 0;
            Maximum         = 10;
            m_viewWidth     = 1;                                    
            Channels        = new List<RingBuffer<float>>(2);
            ChannelCapacity = 300;
        }

        public void Resize(double width, double height)
        {
            Width  = width;
            Height = height;
        }

        public void SetTimeChannel(RingBuffer<DateTime> time)
        {
            Time = time;            
        }

        public void ClearChannels()
        {
            if (IsLocked)
                return;

            Channels.Clear();
        }

        public void AddChannel()
        {
            if (IsLocked)
                return;

            if (Time == null)
                Time = new RingBuffer<DateTime>(ChannelCapacity);

            RingBuffer<float> channel = new RingBuffer<float>(ChannelCapacity);

            Channels.Add(channel);
        }

        public void AddDataset(float[] dataset)
        {
            if (Channels.Count == 0)
                return;

            if (dataset.Length < Channels.Count)
                return;

            Time.Add(DateTime.Now);

            for (int i = 0; i < Channels.Count; i++)
            {
                Channels[i].Add(dataset[i]);
            }
        }

        public void Clear()
        {
            if (IsLocked)
                return;

            Channels.Clear();
        }

        public int Calculate()
        {      
            //calculate ranges                        
            GraphRect.X      = 30;
            GraphRect.Width  = Width - GraphRect.X;
            GraphRect.Height = Height - 30;            
            XAxisRect        = new Rect(GraphRect.X, GraphRect.Y + GraphRect.Height, GraphRect.Width, 30);
            YAxisRect        = new Rect(0, 0, 30, GraphRect.Height);         

            //calculate min / max
            if (AutoScale)
            {
                if (Channels.Count == 0)
                    return 0;

                float min = Channels[0].First;
                float max = min;

                foreach (RingBuffer<float> channel in Channels)
                {
                    foreach (float d in channel)
                    {
                        if (d > max) max = d;
                        if (d < min) min = d;
                    }                
                }

                MinValue = (int)min - 1;
                MaxValue = (int)max + 1;
            }
            else
            {
                MinValue = Minimum;
                MaxValue = Maximum;
            }

            return 0;
        }

        public void GetPoints(out Point[] Points, int id)
        {
            double yFactor = (GraphRect.Height - 10) / (Maximum - Minimum);
            double xFactor = (GraphRect.Width * m_viewWidth) / Channels[0].Length;                        
            float x;
            float y;
            int index;

            Points = null; 

            try
            {                               
                RingBuffer<float> channel = Channels[id];                           

                if (channel.Length == 0)
                    return;

                Points = new Point[channel.Length];

                index = 0;

                BufferContent<float> iterator = channel.GetFirst();

                for (index = 0; index < channel.Length; index++)
                {
                    if (iterator == null)
                    {
                        Points[index] = (Points[index - 1]);
                    }
                    else
                    {
                        x = (float)((index * xFactor) + GraphRect.X);
                        y = (float)(GraphRect.Height - (((Math.Max(Convert.ToDouble(iterator.Content), Minimum) - Minimum) * yFactor)));

                        Points[index] = new Point(x, y);

                        iterator = iterator.Next;
                    }                        
                }                                                                      
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }            
        }
 
        public void GetXCoords(out Rect[] XLines, out DateTime[] XValues)
        {            
            const int TICKS = 2;
                        
            float xPos      = (float)XAxisRect.Left;
            float yPos      = (float)XAxisRect.Y; 
            DateTime start  = Time.First;
            double seconds  = Time.Last.Subtract(Time.First).TotalSeconds;            
            double ticks    = seconds / TICKS;
            double tickWidth= (XAxisRect.Width * m_viewWidth) / Time.Length;
            int lastSecond  = 0;

            List<Rect> xLines  = new List<Rect>(20);
            List<DateTime>xValues = new List<DateTime>(20);

            foreach (DateTime timeStamp in Time)
            {
                xPos += (float)tickWidth;

                if (timeStamp.Second % TICKS > 0)
                    continue;

                if (lastSecond == timeStamp.Second)
                    continue;

                lastSecond = timeStamp.Second;                

                if (xPos <= XAxisRect.X + tickWidth + 0.1)
                    continue;

                xLines.Add(new Rect(
                    new Point(xPos, yPos) , 
                    new Point(xPos, yPos + 10)));

                xValues.Add(timeStamp);
            }

            xLines.Add(new Rect(
                new Point((float)XAxisRect.X, yPos), 
                new Point((float)(XAxisRect.X + XAxisRect.Width), yPos)));

            XLines  = xLines.ToArray();
            XValues = xValues.ToArray();
        }

        public void GetYCoords(out Rect[] YLines, out float[] YValues)
        {
            double factor;
            float yPos;
            float yAxisEnd;
            float xOffset;

            YLines = null;
            YValues = null;

            //calulate scaling factor
            factor = (YAxisRect.Height - 10) / (MaxValue - MinValue);
            xOffset = 20;

            if (Ticksize == 0)
                return;

            if (double.IsInfinity(factor))
                return;

            List<Rect> yLines = new List<Rect>(20);
            List<float> yValues = new List<float>(20);

            //draw 0 line
            yPos     = (float)(YAxisRect.Height - ((0 - MinValue) * factor));
            yAxisEnd = (float)(YAxisRect.X + YAxisRect.Width);

            yLines.Add(new Rect(
                new Point((float)YAxisRect.X + xOffset,yPos),
                new Point(yAxisEnd, yPos)));            

            //draw axis
            for (double tick = MinValue; tick <= MaxValue; tick += Ticksize)
            {
                if (tick == 0)
                    continue;

                yPos = (float)(YAxisRect.Height - ((tick - MinValue) * factor));

                yLines.Add(new Rect(                
                    new Point((float)YAxisRect.X + xOffset,yPos),
                    new Point(yAxisEnd, yPos)));                

                //XGrids.Add(new Rect(             
                //    new Point((float)YAxisRect.X + xOffset, yPos),
                //    new Point((float)(GraphRect.X + GraphRect.Width), yPos)));
                           
            }

            yLines.Add(new Rect(
                new Point((float)GraphRect.X, (float)GraphRect.Y),
                new Point((float)GraphRect.X, (float)(GraphRect.Y + GraphRect.Height))));

            YLines = yLines.ToArray();
            YValues = yValues.ToArray();
        }
    }
}