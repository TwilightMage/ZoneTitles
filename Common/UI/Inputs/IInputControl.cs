using System;

namespace ZoneTitles.Common.UI.Inputs;

public interface IInputControl<T>
{
    public T Value { get; set; }

    public event Action<T> OnValueChanged;
}