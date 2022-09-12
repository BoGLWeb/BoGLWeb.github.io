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
    /// Defines the view-model for a simple displayable rectangle.
    /// Idea from: http://www.codeproject.com/Articles/139216/A-Simple-Technique-for-Data-binding-to-the-Positio
    /// In this project rectangle means the text that is displayed
    /// Each sub-topic or topic that is created is the rectangle
    /// Each topic/rectangle has x,y which is the Canvas.LeftProperty and Canvas.TopProperty
    /// width is the textblock width and height is the textblock height
    /// content is what is entered in the textblock (rather there is a textbox in the background that has 
    /// the content and that is saved in the textblock)
    /// color gives the text color 
    /// font is the text font
    /// </summary>
    /// 
    // I am not sure if InotifypropertChanged is required because I dont think that is used
    public class RectangleViewModel : INotifyPropertyChanged
    {
        #region Data Members

        /// <summary>
        /// The X coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        private double x = 0;

        /// <summary>
        /// The Y coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        private double y = 0;

        /// <summary>
        /// The width of the rectangle (in content coordinates).
        /// </summary>
        private double width = 0;

        /// <summary>
        /// The height of the rectangle (in content coordinates).
        /// </summary>
        private double height = 0;

        private string nodeName;

        private string content;

        private string color;

        private string font;

        #endregion Data Members

        public RectangleViewModel()
        {
        }

        public RectangleViewModel(double x, double y, double width, double height,string nodeName,string content,string color, string font)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.content = content;
            this.color = color;
            this.font = font;
            this.nodeName = nodeName;
            
        }



        /// <summary>
        /// The X coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }
            set
            {
                if (x == value)
                {
                    return;
                }

                x = value;

                OnPropertyChanged("X");
            }
        }

        /// <summary>
        /// The Y coordinate of the location of the rectangle (in content coordinates).
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                if (y == value)
                {
                    return;
                }

                y = value;

                OnPropertyChanged("Y");
            }
        }

        /// <summary>
        /// The width of the rectangle (in content coordinates).
        /// </summary>
        public double Width
        {
            get
            {
                return width;
            }
            set
            {
                if (width == value)
                {
                    return;
                }

                width = value;

                OnPropertyChanged("Width");
            }
        }

        /// <summary>
        /// The height of the rectangle (in content coordinates).
        /// </summary>
        public double Height
        {
            get
            {
                return height;
            }
            set
            {
                if (height == value)
                {
                    return;
                }

                height = value;

                OnPropertyChanged("Height");
            }
        }

       

        public string NodeName
        {
            get
            {
                return nodeName;
            }
            set
            {
                if (nodeName == value)
                {
                    return;
                }

                nodeName = value;

                OnPropertyChanged("NodeName");
            }
        }

        public string Content
        {
            get
            {
                return content;
            }
            set
            {
                if (content == value)
                {
                    return;
                }

                content = value;

                OnPropertyChanged("Content");
            }
        }

        public string Color
        {
            get
            {
                return color;
            }
            set
            {
                if (color == value)
                {
                    return;
                }

                color = value;

                OnPropertyChanged("Color");
            }
        }

        public string Font
        {
            get
            {
                return font;
            }
            set
            {
                if (font == value)
                {
                    return;
                }

                font = value;

                OnPropertyChanged("Font");
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
