using System.Collections;

namespace FontoXPathCSharp.Sequences;

using Value;

public class ArrayBackedSequence : ISequence, IEnumerable<AbstractValue>
{
    private readonly AbstractValue[] _values;

    public ArrayBackedSequence(AbstractValue[] values)
    {
        _values = values;
    }

    public bool IsEmpty()
    {
        return false;
    }

    public bool IsSingleton()
    {
        return false;
    }

    public AbstractValue? First()
    {
        return _values.Length == 0 ? null : _values[0];
    }

    public int GetLength()
    {
        return _values.Length;
    }

    public override string ToString()
    {
        return "<ArrayBackedSequence>[" + string.Join(", ", _values.Select(value => value.ToString()!).ToArray()) + "]";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<AbstractValue> GetEnumerator()
    {
        return _values.ToList().GetEnumerator();
    }
}