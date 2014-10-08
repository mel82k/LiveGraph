using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace mdc.Controls.LiveGraph
{
    public class LiveGraph : FrameworkElement
    {
        private bool             _frozen;
        private Coords           _corrds;
        private List<Channel>    _channels;        
        private GraphBackend     _backend;

        public IAxisStyle AxisStyle     { get { return _corrds; } }
        public IChannelStyle[] Channels { get { return _channels.ToArray();} }
        
        public LiveGraph()
        {
            _channels          = new List<Channel>();
            _corrds            = new Coords();
            _backend           = new GraphBackend();            
            _corrds.Foreground = Brushes.Black;
            _corrds.Fill       = Brushes.Black;
            _corrds.Stroke     = new Pen(_corrds.Fill, 1);

            if (DesignerProperties.GetIsInDesignMode(this))
                _corrds.FontFamily = new FontFamily("Univers");
            else
                _corrds.FontFamily = Application.Current.MainWindow.FontFamily;
        }

        public IChannelStyle AddChannel(string name, Brush color)
        {
            Channel channel = new Channel() 
            { 
                ID   = _channels.Count,
                Name = name,
                Fill = color,
                Stroke = new Pen(color, 1)
            };

            _channels.Add(channel);
            _backend.AddChannel();

            return channel;
        }

        public void AddDataset(float[] data)
        {
            _backend.AddDataset(data);   
        }

        public void Clear()
        {
            _channels.Clear();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _backend.Resize(finalSize.Width, finalSize.Height);
            return base.ArrangeOverride(finalSize);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);   
        }

        protected virtual void GetData()
        {
            if (!_frozen)
            { 
                foreach (Channel c in Channels)
                {
                    c.Fill.Freeze();
                    c.Stroke.Freeze();                    
                }
                //_corrds.Background.Freeze();
                _corrds.Foreground.Freeze();
                _corrds.Fill.Freeze();
                _corrds.Stroke.Freeze();
                _frozen = true;
            }

            _backend.Calculate();
            _backend.GetXCoords(out _corrds.XAxis, out _corrds.XValues);
            _backend.GetYCoords(out _corrds.YAxis, out _corrds.YValues);

            for (int id = 0; id < _channels.Count; id++)
                _backend.GetPoints(out _channels[id].Data, id);
        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (_backend.ChannelCount == 0)
                return;

            GetData();
            
            StreamGeometry geometry;
            StreamGeometryContext gc;
            CultureInfo culture = CultureInfo.CurrentCulture;
            Typeface typeface = new Typeface(_corrds.FontFamily.Source);            

            #region draw channels

            foreach (Channel channel in _channels)
            {
                geometry  = new StreamGeometry();
                gc = geometry.Open();
                gc.BeginFigure(channel.Data[0], false, false);

                for (int i = 1; i < channel.Data.Length; i++)
                    gc.LineTo(channel.Data[i], true, false);

                gc.Close();
                geometry.Freeze();
                context.DrawGeometry(channel.Fill, channel.Stroke, geometry);
            }           

            #endregion            

            #region draw x corrds

            geometry = new StreamGeometry();

            int index = 0;
            foreach (Rect rect in _corrds.XAxis)
            {
                geometry = new StreamGeometry();     
                gc = geometry.Open();
                gc.BeginFigure(rect.TopLeft, false, true);
                gc.LineTo(rect.TopRight, false, true);
                gc.LineTo(rect.BottomRight, false, true);
                gc.LineTo(rect.BottomLeft, false, true);                
                gc.Close();
                geometry.Freeze();
                context.DrawGeometry(_corrds.Fill, _corrds.Stroke, geometry);

                //FormattedText text = new FormattedText(_corrds.XValues[index++].ToString("00"), culture, FlowDirection.LeftToRight, typeface, 8, _corrds.Foreground);
                //context.DrawText(text, rect.TopLeft);
            }

            #endregion

            #region draw y corrds

            index = 0;
            foreach (Rect rect in _corrds.YAxis)
            {
                geometry = new StreamGeometry();
                gc = geometry.Open();
                gc.BeginFigure(rect.TopLeft, false, true);
                gc.LineTo(rect.TopRight, false, true);
                gc.LineTo(rect.BottomRight, false, true);
                gc.LineTo(rect.BottomLeft, false, true);
                gc.Close();
                geometry.Freeze();
                context.DrawGeometry(_corrds.Fill, _corrds.Stroke, geometry);

                //FormattedText text = new FormattedText(_corrds.YValues[index++].ToString("00"), culture, FlowDirection.LeftToRight, typeface, 8, _corrds.Foreground);
                //context.DrawText(text, rect.TopLeft);
            }   

            #endregion
        }

        public interface IChannelStyle
        { 
            Brush Fill              { get; set; }
            Pen Stroke              { get; set; }
            string Name             { get; set; }
            int ID                  { get; set; }
        }

        public interface IAxisStyle
        {
            Brush Fill              { get; set; }
            Pen Stroke              { get; set; }
            Brush Foreground        { get; set; }
            FontFamily FontFamily   { get; set; }
        }

        class Channel : IChannelStyle
        {
            public Point[] Data;
            public Brush Fill       { get; set; }
            public Pen Stroke       { get; set; }
            public string Name      { get; set; }
            public int ID           { get; set; }
        }        

        class Coords : IAxisStyle
        {
            public DateTime[] XValues;
            public float[] YValues;
            public Rect[] XAxis;
            public Rect[] YAxis;
            public Brush Fill               { get; set; }
            public Pen Stroke               { get; set; }
            public Brush Foreground         { get; set; }
            public Brush Background         { get; set; }
            public FontFamily FontFamily    { get; set; }           
        }
    }
}
