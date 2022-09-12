using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;

namespace AVL_Prototype_1
{
    /// <summary>
    /// This defines the details of the line used to connect two rectangles 
    /// x1,y1 defines the line's connection spot in the first rectangle
    /// x2,y2 defines the line's connection spot in the second rectangle
    /// all the other items are self explaining 
    /// </summary>
    public class LineConnections : INotifyPropertyChanged
    {
        #region Data Members

        /// <summary>
        /// The X coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        private double x1 = 0;

        /// <summary>
        /// The Y coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        private double y1 = 0;

        /// <summary>
        /// The width of the rectangle (in content coordinates).
        /// </summary>
        private double x2 = 0;

        /// <summary>
        /// The height of the rectangle (in content coordinates).
        /// </summary>
        private double y2 = 0;

        private string linetype_dashed;

        private string linetype_arrowType;

        private string lineColor;
        private string name;
        private int thickness;

        private string arrowend;
        private string nodename;

        #endregion Data Members

        public LineConnections()
        {
        }

        public LineConnections(string name,double x1, double y1, double x2, double y2,string linetype_dashed,string linetype_arrowType,string lineColor,int thickness, string arrowend)
        {
        
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.linetype_dashed = linetype_dashed;
            this.linetype_arrowType = linetype_arrowType;
            this.lineColor = lineColor;
            this.thickness = thickness;
            this.arrowend = arrowend;
            
            this.name = name;
            
        }

     

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name == value)
                {
                    return;
                }

                name = value;

                OnPropertyChanged("Name");
            }
        }

        public string ArrowEnd
        {
            get
            {
                return arrowend;
            }
            set
            {
                if (arrowend == value)
                {
                    return;
                }

                arrowend = value;

                OnPropertyChanged("ArrowEnd");
            }
        }

        /// <summary>
        /// The X coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        public double X1
        {
            get
            {
                return x1;
            }
            set
            {
                if (x1 == value)
                {
                    return;
                }

                x1 = value;

                OnPropertyChanged("X1");
            }
        }

        /// <summary>
        /// The Y coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        public double Y1
        {
            get
            {
                return y1;
            }
            set
            {
                if (y1 == value)
                {
                    return;
                }

                y1 = value;

                OnPropertyChanged("Y1");
            }
        }

        public double X2
        {
            get
            {
                return x2;
            }
            set
            {
                if (x2 == value)
                {
                    return;
                }

                x2 = value;

                OnPropertyChanged("X2");
            }
        }

        public double Y2
        {
            get
            {
                return y2;
            }
            set
            {
                if (y2 == value)
                {
                    return;
                }

                y2 = value;

                OnPropertyChanged("Y2");
            }
        }

        public string LTD
        {
            get
            {
                return linetype_dashed;
            }
            set
            {
                if (linetype_dashed == value)
                {
                    return;
                }

                linetype_dashed = value;

                OnPropertyChanged("LTD");
            }
        }

        public string LTA
        {
            get
            {
                return linetype_arrowType;
            }
            set
            {
                if (linetype_arrowType == value)
                {
                    return;
                }

                linetype_arrowType = value;

                OnPropertyChanged("LTA");
            }
        }

        public string LC
        {
            get
            {
                return lineColor;
            }
            set
            {
                if (lineColor == value)
                {
                    return;
                }

                lineColor = value;

                OnPropertyChanged("LC");
            }
        }


        public int Thickness
        {
            get
            {
                return thickness;
            }
            set
            {
                if (thickness == value)
                {
                    return;
                }

                thickness = value;

                OnPropertyChanged("Thickness");
            }
        }
       

        


        


        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raises the 'PropertyChanged' event when the value of a property of the view model has changed.
        /// </summary>
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// 'PropertyChanged' event that is raised when the value of a property of the view model has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        #endregion

    }
}
