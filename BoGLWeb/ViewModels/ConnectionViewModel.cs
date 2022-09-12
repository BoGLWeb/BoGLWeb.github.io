using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace AVL_Prototype_1
{
    /// <summary>
    /// View-model class that represents a connection between two rectangles
    /// This includes data about which two rectangles are connected
    /// Which type of line is used to connect them 
    /// Which side (left, right, top and bottom) is connected
    /// connection multiple is not used - is default at 1. 
    /// </summary>
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// References to the two rectangles that are connected.
        /// </summary>
        private RectangleViewModel rect1 = null;
        private RectangleViewModel rect2 = null;
        private LineConnections line = null;
        private string connectionSide = null;
        private int connectionMultiple;
        


        public ConnectionViewModel()
        {
        }

        public ConnectionViewModel(RectangleViewModel rect1, RectangleViewModel rect2, LineConnections line, string connectionSide,int connectionMultiple)
        {
            this.rect1 = rect1;
            this.rect2 = rect2;
            this.line = line;
            this.connectionSide = connectionSide;
            this.connectionMultiple = connectionMultiple;
        }

        /// <summary>
        /// References to the first connected rectangle.
        /// </summary>
        public RectangleViewModel Rect1
        {
            get
            {
                return rect1;
            }
            set
            {
                rect1 = value;

                OnPropertyChanged("Rect1");
            }
        }

        /// <summary>
        /// References to the second connected rectangle.
        /// </summary>
        public RectangleViewModel Rect2
        {
            get
            {
                return rect2;
            }
            set
            {
                rect2 = value;

                OnPropertyChanged("Rect2");
            }
        }

        public LineConnections Line
        {
            get
            {
                return line;
            }
            set
            {
                line = value;

                OnPropertyChanged("Line");
            }
        }

        public string ConnectionSide
        {
            get
            {
                return connectionSide;
            }
            set
            {
                connectionSide = value;

                OnPropertyChanged("ConnectionSide");
            }
        }

        public int ConnectionMultiple
        {
            get
            {
                return connectionMultiple;
            }
            set
            {
                connectionMultiple = value;

                OnPropertyChanged("ConnectionMultiple");
            }
        }


        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raises the 'PropertyChanged' event when the value of a property of the data model has changed.
        /// </summary>
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// 'PropertyChanged' event that is raised when the value of a property of the data model has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
