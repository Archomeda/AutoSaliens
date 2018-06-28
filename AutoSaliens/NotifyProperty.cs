using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutoSaliens
{
    [JsonConverter(typeof(NotifyPropertyJsonConverter))]
    internal class NotifyProperty<T>
    {
        private T value;

        public NotifyProperty(T value) => this.value = value;

        public event EventHandler<PropertyChangedEventArgs<T>> Changed;

        public T Value
        {
            get => this.value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(this.value, value))
                {
                    T oldValue = this.value;
                    this.value = value;
                    this.Changed?.Invoke(this, new PropertyChangedEventArgs<T>(oldValue, value));
                }
            }
        }

        public static implicit operator T(NotifyProperty<T> prop) => prop.Value;

        public static implicit operator NotifyProperty<T>(T value) => new NotifyProperty<T>(value);

        public override string ToString()
        {
            return this.value.ToString();
        }
    }

    internal class PropertyChangedEventArgs<T> : EventArgs
    {
        public PropertyChangedEventArgs(T oldValue, T newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public T OldValue { get; }

        public T NewValue { get; }
    }
}
