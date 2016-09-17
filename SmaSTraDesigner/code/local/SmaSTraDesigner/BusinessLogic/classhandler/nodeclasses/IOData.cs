﻿using System;
using System.ComponentModel;

namespace SmaSTraDesigner.BusinessLogic.classhandler
{
    public class IOData : INotifyPropertyChanged, ICloneable
    {

        private DataType type;
        private string value; //TODO: needs better, typesafe solution

        public IOData(DataType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }

        public DataType Type
        {
            get
            {
                return type;
            }
            set
            {
                if (type != value)
                {
                    type = value;
                    this.NotifyPropertyChanged("Type");
                }
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    this.NotifyPropertyChanged("Value");
                }
            }
        }


        /// <summary>
        /// If the value is set to something.
        /// </summary>
        public bool IsSet()
        {
            return !string.IsNullOrWhiteSpace(Value);
        }


        public object Clone()
        {
            return MemberwiseClone();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}