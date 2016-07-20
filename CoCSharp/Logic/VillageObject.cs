﻿using CoCSharp.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace CoCSharp.Logic
{
    /// <summary>
    /// Represents an object in a <see cref="Logic.Village"/>.
    /// </summary>
    [DebuggerDisplay("ID = {ID}")]
    public abstract class VillageObject
    {
        //TODO: Make it reusable.
        #region Constants
        // Represents the Base ID of every game ID & data ID.
        internal const int Base = 1000000;

        internal static int s_rested = 0;
        internal static int s_created = 0;

        private static readonly PropertyChangedEventArgs s_xChanged = new PropertyChangedEventArgs("X");
        private static readonly PropertyChangedEventArgs s_yChanged = new PropertyChangedEventArgs("Y");
        #endregion

        #region Constructors
        internal VillageObject(Village village)
        {
            if (village == null)
                throw new ArgumentNullException("village");

            _components = new Dictionary<Type, LogicComponent>();

            // Setter of Village property will call RegisterVillageObject.
            Village = village;

            Interlocked.Increment(ref s_created);
        }

        internal VillageObject(Village village, int x, int y) : this(village)
        {
            X = x;
            Y = y;
        }
        #endregion

        #region Fields & Properties
        // Amount of time the VillageObject was pushed back to the pool.
        private int _count;
        // Amount of time the VillageObject was reused.
        internal int _recycled;

        // TODO: Switch to an array.
        // Dictionary/List of LogicComponent in the Village Object.
        private Dictionary<Type, LogicComponent> _components;

        /// <summary>
        /// Gets or sets a value indicating whether to raise the <see cref="PropertyChanged"/> event.
        /// </summary>
        public bool IsPropertyChangedEnabled { get; set; }

        /// <summary>
        /// The event raised when a property value has changed.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;

        // Village in which the VillageObject is in.
        private Village _village;
        /// <summary>
        /// Gets the <see cref="Logic.Village"/> in which the current <see cref="VillageObject"/> is in.
        /// </summary>
        public Village Village
        {
            get
            {
                return _village;
            }
            internal set
            {
                _village = value;
                RegisterVillageObject();
            }
        }

        // ID of the VillageObject (IDs above or equal to 500000000). E.g: 500000001
        // This value should be relative to the Village the VillageObject is in.
        private int _instanceId;
        /// <summary>
        /// Gets or sets the instance/game ID of the <see cref="VillageObject"/>.
        /// </summary>
        public int ID
        {
            get
            {
                return _instanceId;
            }
            protected set
            {
                // TODO: Check range of ID.
                _instanceId = value;
            }
        }

        // X coordinate of object.
        private int _x;
        /// <summary>
        /// Gets or sets the X coordinate of the <see cref="VillageObject"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and Village.Width.</exception>
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                if (value < 0 || value > Village.Width)
                    throw new ArgumentOutOfRangeException("value", "value must be between 0 and Village.Width.");

                if (_x == value)
                    return;

                _x = value;
                OnPropertyChanged(s_xChanged);
            }
        }

        // Y coordinate of object.
        private int _y;
        /// <summary>
        /// Gets or sets the y coordinate of the <see cref="VillageObject"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and Village.Height.</exception>
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (value < 0 || value > Village.Height)
                    throw new ArgumentOutOfRangeException("value", "value must be between 0 and Village.Height.");

                if (_y == value)
                    return;

                _y = value;
                OnPropertyChanged(s_yChanged);
            }
        }

        /// <summary>
        /// Gets the <see cref="Data.AssetManager"/> of <see cref="Village"/>.
        /// </summary>
        protected AssetManager AssetManager
        {
            get
            {
                return Village.AssetManager;
            }
        }
        #endregion

        #region Methods
        #region Component
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddComponent<T>() where T : LogicComponent
        {
            var type = typeof(T);
            if (_components.ContainsKey(type))
                throw new ArgumentException("LogicComponent of type T was already added to the VillageObject.", "T");

            var instance = (T)Activator.CreateInstance(type, true);
            instance._vilObject = this;

            _components.Add(type, instance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool RemoveComponent<T>() where T : LogicComponent
        {
            var type = typeof(T);
            return _components.Remove(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>() where T : LogicComponent
        {
            var type = typeof(T);
            var instance = (LogicComponent)null;
            _components.TryGetValue(type, out instance);
            return (T)instance;
        }
        #endregion

        // Calls the PropertyChanged event.
        internal void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (PropertyChanged != null && IsPropertyChangedEnabled)
                PropertyChanged(this, args);
        }

        // Resets the VillageObject so that it can be reused.
        internal virtual void ResetVillageObject()
        {
            Interlocked.Increment(ref s_rested);

            _village = default(Village);
            _instanceId = default(int);
            _x = default(int);
            _y = default(int);
        }

        // Register the VillageObject to its Village.
        internal abstract void RegisterVillageObject();

        // Writes the current VillageObject to the JsonWriter.
        internal abstract void ToJsonWriter(JsonWriter writer);

        // Reads the current VillageObject to the JsonReader.
        internal abstract void FromJsonReader(JsonReader reader);

        internal void PushToPool()
        {
            // Set village to null, so it can get picked up by the GC.
            _village = null;
            VillageObjectPool.Push(this);
            _count++;
        }
        #endregion
    }
}
